using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Cloud.Common;
using Unity.Collections;
using Unity.Industry.Viewer.Multiplay;
using Unity.Industry.Viewer.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Industry.Viewer.Streaming.Hierarchy
{
    [Serializable]
    public struct HierarchySyncData : INetworkSerializable, IEquatable<HierarchySyncData>
    {
        public string AssetId;
        public string ProjectID;
        public string OrgID;
        public string GameObjectName;
        public string InstanceId;
        public bool root;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AssetId);
            serializer.SerializeValue(ref ProjectID);
            serializer.SerializeValue(ref OrgID);
            serializer.SerializeValue(ref GameObjectName);
            serializer.SerializeValue(ref InstanceId);
            serializer.SerializeValue(ref root);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(OrgID, ProjectID, AssetId, GameObjectName, InstanceId, root);
        }

        public bool Equals(HierarchySyncData other)
        {
            return OrgID == other.OrgID
                   && ProjectID == other.ProjectID
                   && AssetId == other.AssetId
                   && GameObjectName == other.GameObjectName
                   && InstanceId == other.InstanceId
                   && root == other.root;
        }

        public override bool Equals(object obj)
        {
            if (obj is not HierarchySyncData other) return false;
            return Equals(other);
        }

        public static bool operator ==(HierarchySyncData left, HierarchySyncData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HierarchySyncData left, HierarchySyncData right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"OrgID: {OrgID}, ProjectID: {ProjectID}, AssetId: {AssetId}, GameObjectName: {GameObjectName}, InstanceId: {InstanceId}, root: {root}";
        }
    }

    // This script manages the network synchronization of hierarchy data in a Unity project.
    // It uses Unity's Netcode for GameObjects to handle network variables and RPCs for data synchronization.
    // The script listens for network events such as session ownership changes and updates the visibility of instances accordingly.
    // It integrates with Unity's MonoBehaviour for lifecycle management and interacts with the HierarchyToolSceneListener for data updates.
    public class HierarchyToolNetworkObject : NetworkBehaviour
    {
        public List<FixedString64Bytes> LockList => m_LockList.Value;
        
        private NetworkVariable<List<FixedString64Bytes>> m_LockList = new NetworkVariable<List<FixedString64Bytes>>(new List<FixedString64Bytes>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private NetworkVariable<List<HierarchySyncData>> m_HierarchySyncData = new NetworkVariable<List<HierarchySyncData>>(new List<HierarchySyncData>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private HierarchyToolSceneListener m_HierarchyToolSceneListener;
        
        public override void OnNetworkSpawn()
        {
#if ENABLE_MULTIPLAY
            HierarchyToolController.LockModel += LockModel;
#endif
            m_HierarchySyncData.OnValueChanged += OnHierarchyDataChanged;
            NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
            HierarchyToolController.InstanceVisibilityChanged += OnInstanceVisibilityChanged;
            HierarchyToolController.VisibilityReset += OnVisibilityReset;

            m_HierarchyToolSceneListener = FindAnyObjectByType<HierarchyToolSceneListener>();
            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                if (m_HierarchyToolSceneListener.InstanceStates != null)
                {
                    var streamingModels = TransformController.Instance.transform.GetComponentsInChildren<StreamingModel>(true);

                    // Process all hidden sub models
                    foreach (var KeyPairValue in m_HierarchyToolSceneListener.InstanceStates)
                    {
                        if(KeyPairValue.Value.Hidden != null && KeyPairValue.Value.Hidden.Count > 0)
                        {
                            var streamingModel = streamingModels.FirstOrDefault(x => x.ModelStreamId == KeyPairValue.Key);
                            if (streamingModel == null) continue;

                            foreach (var instanceData in KeyPairValue.Value.Hidden)
                            {
                                var data = new HierarchySyncData()
                                {
                                    AssetId = streamingModel.AssetId,
                                    ProjectID = streamingModel.ProjectID,
                                    OrgID = streamingModel.OrgID,
                                    GameObjectName = streamingModel.gameObject.name
                                };
                                if (instanceData.Instance != null)
                                {
                                    data.InstanceId = instanceData.Instance.Id.ToString();
                                    data.root = instanceData.Instance.AncestorIds.Count == 0;
                                }
                                else
                                {
                                    data.InstanceId = string.Empty;
                                    data.root = true;
                                }

                                if (!m_HierarchySyncData.Value.Contains(data))
                                {
                                    m_HierarchySyncData.Value.Add(data);
                                }
                            }
                        }
                    }

                    // Process all hidden root models
                    foreach (var streamingModel in streamingModels)
                    {
                        if (!streamingModel.gameObject.activeSelf)
                        {
                            var data = new HierarchySyncData()
                            {
                                AssetId = streamingModel.AssetId,
                                ProjectID = streamingModel.ProjectID,
                                OrgID = streamingModel.OrgID,
                                GameObjectName = streamingModel.gameObject.name,
                                InstanceId = string.Empty,
                                root = true
                            };

                            if (!m_HierarchySyncData.Value.Contains(data))
                            {
                                m_HierarchySyncData.Value.Add(data);
                            }
                        }
                    }
                }

                m_HierarchySyncData.CheckDirtyState();
            }
            else
            {
                _ = WaitForModelsAndApplyNetworkVisibilityAsync();
            }

            async Task WaitForModelsAndApplyNetworkVisibilityAsync()
            {
                try
                {
                    // Wait until NetworkModelSync is spawned
                    NetworkModelSync networkModelSync = null;
                    while (networkModelSync == null)
                    {
                        await Task.Yield();
                        networkModelSync = FindAnyObjectByType<NetworkModelSync>();
                    }

                    // Wait until local scene is syncked with networked models
                    while (!networkModelSync.IsSceneSynckedWithNetworkModels())
                    {
                        await Task.Yield();
                    }

                    // Sub objects of models may still be loading, wait for stage to be fully loaded
                    var streamSceneUIController = FindAnyObjectByType<StreamSceneUIController>();
                    if (streamSceneUIController == null)
                    {
                        Debug.LogError("Network: Can't find StreamSceneUIController.");
                    }
                    else
                    {
                        await streamSceneUIController.Stage.WaitUntilLoadedAsync();
                    }

                    // Make all root items visible (they could be hidden in offline mode)
                    var localModels = TransformController.Instance.transform.GetComponentsInChildren<StreamingModel>(true);
                    foreach (var streamingModel in localModels)
                    {
                        streamingModel.gameObject.SetActive(true);
                    }

                    // Hide objects (root and sub items) by networked data from the session owner
                    foreach (var syncData in m_HierarchySyncData.Value)
                    {
                        CheckHierarchyDataAgainstModel(syncData, false);
                    }

                    // Models can be added/removed, tree view may still have old items, rebuild it
                    var hierarchyToolController = FindFirstObjectByType<HierarchyToolController>();
                    if (hierarchyToolController != null)
                    {
                        await hierarchyToolController.UpdateTreeViewItems();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Network: Exception on models sync during Player joining: {ex}");
                }
            }
        }

        private void CheckHierarchyDataAgainstModel(HierarchySyncData data, bool visibility)
        {
            for (var i = 0; i < TransformController.Instance.transform.childCount; i++)
            {
                if (!TransformController.Instance.transform.GetChild(i)
                        .TryGetComponent(out StreamingModel model)) continue;
                if(data.ProjectID == model.ProjectID && data.OrgID == model.OrgID && data.AssetId == model.AssetId && data.GameObjectName == model.gameObject.name)
                {
                    _ = m_HierarchyToolSceneListener.UpdateVisibility(model, data.root,
                       new InstanceId(data.InstanceId), visibility);
                }
            }
        }
        
        public override void OnNetworkDespawn()
        {
#if ENABLE_MULTIPLAY
            HierarchyToolController.LockModel -= LockModel;
#endif
            HierarchyToolController.InstanceVisibilityChanged -= OnInstanceVisibilityChanged;
            HierarchyToolController.VisibilityReset -= OnVisibilityReset;
            m_HierarchySyncData.OnValueChanged -= OnHierarchyDataChanged;
            NetworkManager.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        }
        
#if ENABLE_MULTIPLAY
        private void LockModel(string modelName, bool toLock)
        {
            PongLockListUpdateRpc(new FixedString64Bytes(modelName), toLock);
        }
#endif
        
        [Rpc(SendTo.Authority, Delivery = RpcDelivery.Reliable)]
        private void PongLockListUpdateRpc(FixedString64Bytes modelName, bool toLock)
        {
            if(!IsOwner) return;
            if (toLock)
            {
                if(m_LockList.Value.Contains(modelName)) return;
                m_LockList.Value.Add(modelName);
                m_LockList.CheckDirtyState();
            }
            else
            {
                if(!m_LockList.Value.Contains(modelName)) return;
                m_LockList.Value.Remove(modelName);
                m_LockList.CheckDirtyState();
            }
        }

        private void OnInstanceVisibilityChanged(InstanceData arg1, bool arg2)
        {
            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                var data = new HierarchySyncData
                {
                    AssetId = arg1.StreamingModel.AssetId,
                    ProjectID = arg1.StreamingModel.ProjectID,
                    OrgID = arg1.StreamingModel.OrgID,
                    GameObjectName = arg1.StreamingModel.gameObject.name,
                    InstanceId = arg1.Instance == null ? string.Empty : arg1.Instance.Id.ToString(),
                    root = arg1.Instance == null || arg1.Instance.AncestorIds.Count == 0
                };

                if (!arg2)
                {
                    if (!m_HierarchySyncData.Value.Contains(data))
                    {
                        m_HierarchySyncData.Value.Add(data);
                    }
                }
                else
                {
                    if(m_HierarchySyncData.Value.Contains(data))
                    {
                        m_HierarchySyncData.Value.Remove(data);
                    }
                }
                m_HierarchySyncData.CheckDirtyState();
            }
            else
            {
                var instanceId = arg1.Instance == null ? string.Empty : arg1.Instance.Id.ToString();
                
                PongDataRpc(arg1.StreamingModel.AssetId,
                    arg1.StreamingModel.ProjectID,
                    arg1.StreamingModel.OrgID,
                    arg1.StreamingModel.gameObject.name,
                    instanceId,
                    arg1.Instance == null || arg1.Instance.AncestorIds.Count == 0,
                    arg2);
            }
        }
        
        [Rpc(SendTo.Authority, Delivery = RpcDelivery.Reliable)]
        private void PongDataRpc(FixedString64Bytes assetId, FixedString64Bytes projectId,
            FixedString64Bytes orgId, FixedString64Bytes gameObjectName, FixedString64Bytes instanceId, bool root, bool visibility)
        {
            if(!IsOwner) return;
            var data = new HierarchySyncData()
            {
                AssetId = assetId.ToString(),
                ProjectID = projectId.ToString(),
                OrgID = orgId.ToString(),
                GameObjectName = gameObjectName.ToString(),
                InstanceId = instanceId.ToString(),
                root = root
            };
            
            if (!visibility)
            {
                if (!m_HierarchySyncData.Value.Contains(data))
                {
                    m_HierarchySyncData.Value.Add(data);
                }
            }
            else
            {
                if(m_HierarchySyncData.Value.Contains(data))
                {
                    m_HierarchySyncData.Value.Remove(data);
                }
            }
            m_HierarchySyncData.CheckDirtyState();
        }

        private void OnVisibilityReset(bool resetHighlighted)
        {
            if (NetworkDetector.RequestedOfflineMode)
            {
                return;
            }

            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                InternalVisibilityReset();
            }
            else
            {
                VisibilityResetRpc();
            }
        }

        private void InternalVisibilityReset()
        {
            m_HierarchySyncData.Value.Clear();
            m_HierarchySyncData.CheckDirtyState();
        }

        [Rpc(SendTo.Authority, Delivery = RpcDelivery.Reliable)]
        private void VisibilityResetRpc()
        {
            if (!IsOwner) return;
            InternalVisibilityReset();
        }

        private void OnHierarchyDataChanged(List<HierarchySyncData> previousvalue, List<HierarchySyncData> newvalue)
        {
            FindRemovedItem(previousvalue, newvalue);
            FindNewItem(previousvalue, newvalue);
        }

        private void FindRemovedItem(List<HierarchySyncData> previousvalue, List<HierarchySyncData> newvalue)
        {
            Queue<HierarchySyncData> removedItems = new Queue<HierarchySyncData>();
            foreach (var hierarchySyncData in previousvalue)
            {
                if(newvalue.Contains(hierarchySyncData)) continue;
                removedItems.Enqueue(hierarchySyncData);
            }
            while (removedItems.Count > 0)
            {
                var data = removedItems.Dequeue();
                CheckHierarchyDataAgainstModel(data, true);
            }
        }

        private void FindNewItem(List<HierarchySyncData> previousvalue, List<HierarchySyncData> newvalue)
        {
            Queue<HierarchySyncData> newItems = new Queue<HierarchySyncData>();
            foreach (var hierarchySyncData in newvalue)
            {
                if(previousvalue.Contains(hierarchySyncData)) continue;
                newItems.Enqueue(hierarchySyncData);
            }

            while (newItems.Count > 0)
            {
                var data = newItems.Dequeue();
                CheckHierarchyDataAgainstModel(data, false);
            }
        }

        private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
        {
            if (NetworkManager.LocalClientId == sessionOwnerPromoted)
            {
                NetworkObject.ChangeOwnership(sessionOwnerPromoted);
            }
        }
    }
}
