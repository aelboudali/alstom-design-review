using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Cloud.Common;
using Unity.Cloud.DataStreaming.Runtime;
using Unity.Cloud.DataStreaming.Metadata;
using Unity.Cloud.DataStreaming.Runtime.AssetManager;
using Unity.Industry.Viewer.Identity;
using Unity.Industry.Viewer.Shared;
#if ENABLE_MULTIPLAY
using Unity.Netcode;
#endif

namespace Unity.Industry.Viewer.Streaming.Hierarchy
{
    public class InstanceState
    {
        public readonly Dictionary<Color, List<InstanceId>> Highlighted = new();
        public readonly List<InstanceData> Hidden = new();
        public InstanceId Isolated = InstanceId.None;
    }
    
    // This script listens for and manages hierarchy-related events in a Unity project.
    // It handles the visibility and highlighting of streaming models based on user interactions and metadata queries.
    // The script supports asynchronous operations to fetch and update instance data from metadata repositories.
    // It integrates with Unity's MonoBehaviour for lifecycle management and supports both VR and non-VR modes.
    // The script provides user feedback through various events and updates the UI accordingly.
    public class HierarchyToolSceneListener : MonoBehaviour
    {
        
        [SerializeField] private GameObject hierarchyMultiplaySyncPrefab;
        
        public StreamingModelController StreamingModelController => m_StreamingModelController;
        
        private StreamingModelController m_StreamingModelController;
        
        private IServiceHttpClient m_ServiceHttpClient => IdentityController.GuestMode? 
            PlatformServices.ServiceAccountServiceHttpClient : PlatformServices.ServiceHttpClient;
        
        private IServiceHostResolver m_ServiceHostResolver => PlatformServices.ServiceHostResolver;
        
        public Dictionary<StreamingModel, IMetadataRepository> StreamModelRepositoriesMapping => m_StreamModelRepositoriesMapping;
        
        private Dictionary<StreamingModel, IMetadataRepository> m_StreamModelRepositoriesMapping = new();
        
        public Dictionary<ModelStreamId, InstanceState> InstanceStates => m_InstanceStates;
        private Dictionary<ModelStreamId, InstanceState> m_InstanceStates = new();
        
        [SerializeField]
        private Color highlightColor = new Color(0, 200, 255, 127);
        
        private HierarchyModifier m_hierarchyModifier;
        
        public CancellationTokenSource QueryTokenSource;
        
#if ENABLE_MULTIPLAY
        NetworkObject m_hierarchyNetworkObject;
#endif

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            TransformController.ModelAdded += OnNewModelAdded;
            TransformController.ModelRemoved += OnModelRemoved;
            HierarchyToolController.InstanceVisibilityChanged += OnInstanceVisibilityChanged;
            HierarchyToolController.VisibilityReset += OnVisibilityReset;
            NetworkDetector.OnNetworkStatusChanged += OnNetworkStatusChanged;

            m_StreamingModelController = FindAnyObjectByType<StreamingModelController>(FindObjectsInactive.Include);

            m_hierarchyModifier = new HierarchyModifier();
            m_StreamingModelController.Stage.InstanceModifiers.Add(m_hierarchyModifier);
            
            _ = CreateRepositories();
            
#if ENABLE_MULTIPLAY
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientStarted;
            }
#endif
        }

        private void OnDestroy()
        {
            TransformController.ModelAdded -= OnNewModelAdded;
            TransformController.ModelRemoved -= OnModelRemoved;
            HierarchyToolController.InstanceVisibilityChanged -= OnInstanceVisibilityChanged;
            HierarchyToolController.VisibilityReset -= OnVisibilityReset;
            NetworkDetector.OnNetworkStatusChanged -= OnNetworkStatusChanged;

            if (m_StreamingModelController != null)
            {
                m_StreamingModelController.Stage.InstanceModifiers.Remove(m_hierarchyModifier);
            }

            foreach (var value in m_StreamModelRepositoriesMapping.Keys)
            {
                m_hierarchyModifier.RemoveMetadataRepository(value.ModelStream.Id);
            }
            
#if ENABLE_MULTIPLAY
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientStarted;
            }
#endif
        }


#if ENABLE_MULTIPLAY
        private void OnClientStarted(ulong obj)
        {
            if (!NetworkManager.Singleton.LocalClient.IsSessionOwner || m_hierarchyNetworkObject != null) return;
            var addModelSyncObject = Instantiate(hierarchyMultiplaySyncPrefab);
            if (addModelSyncObject.TryGetComponent(out m_hierarchyNetworkObject))
            {
                m_hierarchyNetworkObject.Spawn(true);
            }
        }
#endif
        
        // This function handles the visibility changes of instances in the hierarchy.
        // It updates the visibility of the instance and its children based on the provided visibility flag.
        // If the instance has no ancestors, it directly updates the visibility and the UI toggle.
        // If the instance has children, it queries the metadata repository to update the visibility of the children.
        // The function also manages the HidingList to keep track of hidden instances.
        private void OnInstanceVisibilityChanged(InstanceData data, bool visible)
        {
            UpdateVisibility(data.StreamingModel, data.Instance == null || data.Instance.AncestorIds.Count == 0, data, visible);
        }
        
        private void OnNewModelAdded(GameObject newGameObject, ITransformValuesAccessor newTransform)
        {
            if(newGameObject.TryGetComponent(out StreamingModel model))
            {
                _ = NewRepository(model);
            }
        }
        
        private void OnModelRemoved(StreamingModel obj)
        {
            if (!m_StreamModelRepositoriesMapping.ContainsKey(obj))
            {
                return;
            }
            m_StreamModelRepositoriesMapping.Remove(obj);
        }
        
        private async Task CreateRepositories()
        {
            while (TransformController.Instance.transform.childCount == 0)
            {
                await Task.Yield();
            }
            for(var i = 0; i < TransformController.Instance.transform.childCount; i++)
            {
                if (!TransformController.Instance.transform.GetChild(i).TryGetComponent(out StreamingModel model))
                {
                    continue;
                }
                await NewRepository(model);
            }
        }
        
        public async Task<List<InstanceData>> QueryHierarchyData(InstanceId instanceId, IMetadataRepository repository)
        {
            ResetToken();
            
            return await GetChildItems().ToListAsync(QueryTokenSource.Token);

            async IAsyncEnumerable<InstanceData> GetChildItems()
            {
                ResetToken();
                
                var query = repository
                    .Query()
                    .Select(MetadataPathCollection.All)
                    .WhereHasAncestor(instanceId, 0)
                    .WithCancellation(QueryTokenSource.Token);
                
                StreamingModel streamingModel = null;

                foreach (var streamModelKeyPairValue in m_StreamModelRepositoriesMapping)
                {
                    if (streamModelKeyPairValue.Value == repository)
                    {
                        streamingModel = streamModelKeyPairValue.Key;
                        break;
                    }
                }

                if (streamingModel == null) yield break;
                
                await foreach (var each in query)
                {
                    if (QueryTokenSource.IsCancellationRequested)
                    {
                        yield break;
                    }
                    yield return new InstanceData(each, streamingModel, repository);
                }
            }
        }

        public void ResetToken()
        {
            QueryTokenSource?.Cancel();
            QueryTokenSource?.Dispose();
            QueryTokenSource = new CancellationTokenSource();
        }
        
        private async Task NewRepository(StreamingModel model)
        {
            await Task.Yield();
            var newFactory = new MetadataRepositoryFactory();
            if (model.Dataset == null)
            {
                m_StreamModelRepositoriesMapping.Add(model, null);
                return;
            }
            var metadataRepository = newFactory.Create(model.Dataset, m_ServiceHttpClient, m_ServiceHostResolver);
            m_StreamModelRepositoriesMapping.Add(model, metadataRepository);
            m_hierarchyModifier.AddMetadataRepository(model.ModelStream.Id, metadataRepository, CancellationToken.None);
        }
        
        public void HighlightInstance(ModelStreamId modelId, InstanceId instanceId)
        {
            foreach (var state in m_InstanceStates.Values)
            {
                state.Highlighted.Clear();
            }
            
            if (!m_InstanceStates.TryGetValue(modelId, out var instanceState))
            {
                instanceState = new InstanceState();
                m_InstanceStates.Add(modelId, instanceState);
            }
            
            if (!instanceState.Highlighted.TryGetValue(highlightColor, out var ids))
            {
                ids = new List<InstanceId>();
                instanceState.Highlighted.Add(highlightColor, ids);
            }
            
            ids.Add(instanceId);
            _ = ApplyModifiersNoThrow();
        }

        public void ResetHierarchyModifiers(bool resetHighlighted, bool resetHidden)
        {
            foreach (var state in m_InstanceStates.Values)
            {
                if (resetHighlighted)
                {
                    state.Highlighted.Clear();
                }

                if (resetHidden)
                {
                    state.Hidden.Clear();
                    state.Isolated = InstanceId.None;
                }
            }

            _ = ApplyModifiersNoThrow();
        }

        public void ResetHighlight()
        {
            foreach (var state in m_InstanceStates.Values)
            {
                state.Highlighted.Clear();
            }
            
            _ = ApplyModifiersNoThrow();
        }

        public bool IsCurrentlyHidden(ModelStreamId modelStreamId, InstanceId instanceId)
        {
            if (!m_InstanceStates.TryGetValue(modelStreamId, out var instanceState)) return false;
            return instanceState.Hidden != null && instanceState.Hidden.Any(x => x.Instance.Id == instanceId);
        }
        
        async Task ApplyModifiersNoThrow()
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var (modelStreamId, instanceState) in m_InstanceStates)
                {
                    tasks.Add(m_hierarchyModifier.SetModifiers(
                        modelStreamId,
                        instanceState.Highlighted,
                        instanceState.Hidden.Select(x => x.Instance.Id),
                        instanceState.Isolated));
                }
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // the operation was canceled, no need to propagate the exception further
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnNetworkStatusChanged(bool connected)
        {
            if (!connected && !NetworkDetector.RequestedOfflineMode) return;

            HierarchyToolController.ResetVisibility(true);
        }

        private void OnVisibilityReset(bool resetHighlighted)
        {
#if ENABLE_MULTIPLAY
            // Assuming MP code handles this. If we go offline or in Guest mode, anyway need to reset locally.
            if (!NetworkDetector.RequestedOfflineMode && !IdentityController.GuestMode)
            {
                return;
            }
#endif

            foreach (var streamingModel in TransformController.Instance.GetComponentsInChildren<StreamingModel>(true))
            {
                streamingModel.gameObject.SetActive(true);
            }

            ResetHierarchyModifiers(resetHighlighted, true);
            _ = ApplyModifiersNoThrow();
        }

        public async Task UpdateVisibility(StreamingModel model, bool root, InstanceId instanceId, bool visible)
        {
            if (root)
            {
                UpdateVisibility(model, true, InstanceData.Placeholder, visible);
                return;
            }
            
            var repository = m_StreamModelRepositoriesMapping[model];
            if (repository == null)
            {
                Debug.LogWarning($"No metadata repository found for model {model.name}");
                return;
            }

            var query = await repository
                .Query()
                .Select(MetadataPathCollection.None, new OptionalData(OptionalData.Fields.Id))
                .WhereInstanceEquals(instanceId)
                .GetFirstOrDefaultAsync(CancellationToken.None);
            
            if (query == null) return;

            var instanceData = new InstanceData(query, model, repository);
            UpdateVisibility(model, false, instanceData, visible);
        }

        public void UpdateVisibility(StreamingModel model, bool root, InstanceData instanceData, bool visible)
        {
            if (root)
            {
                HierarchyToolController.UpdateToggleUI?.Invoke(new InstanceData(null, model, null), visible);
                model.gameObject.SetActive(visible);
                return;
            }

            HierarchyToolController.UpdateToggleUI?.Invoke(instanceData, visible);

            if (!m_InstanceStates.TryGetValue(instanceData.StreamModel.Id, out var instanceState))
            {
                instanceState = new InstanceState();
                m_InstanceStates.Add(instanceData.StreamModel.Id, instanceState);
            }

            if (visible)
            {
                var index = instanceState.Hidden.FindIndex(x => x.Instance.Id == instanceData.Instance.Id);
                if (index >= 0)
                {
                    instanceState.Hidden.RemoveAt(index);
                }
            }
            else
            {
                if (instanceState.Hidden.All(x => x.Instance.Id != instanceData.Instance.Id))
                {
                    instanceState.Hidden.Add(instanceData);
                }
            }
            
            _ = ApplyModifiersNoThrow();
        }
    }
}
