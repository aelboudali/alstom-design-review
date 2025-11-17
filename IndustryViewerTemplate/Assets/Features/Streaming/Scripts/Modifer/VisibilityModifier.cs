using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Cloud.Common;
using Unity.Cloud.DataStreaming.Metadata;
using Unity.Cloud.DataStreaming.Runtime;

namespace Unity.Industry.Viewer.Streaming
{
    public class VisibilityModifier : Modifier, IDisposable
    {
        private Dictionary<ModelStreamId, HashSet<InstanceId>> m_CurrentExistingInstances;
        
        private Dictionary<ModelStreamId, IMetadataRepository> m_Repositories;

        public HashSet<InstanceData> HiddenInstances => m_HiddenInstances;
        
        private HashSet<InstanceData> m_HiddenInstances;
        
        public override void Reset()
        {
            base.Reset();
            m_HiddenInstances?.Clear();
            if (m_CurrentExistingInstances == null) return;
            foreach (var key in m_CurrentExistingInstances.Keys)
            {
                InstanceUpdater.SetVisibility(key, m_CurrentExistingInstances[key], true);
            }
        }

        protected override void Update(ModelStreamId modelId, InstanceId instanceId, bool state) { }

        public override Task LoadAsync(ModelStreamId modelStreamId, IEnumerable<InstanceGeometricErrorState> states)
        {
            base.LoadAsync(modelStreamId, states);
            m_CurrentExistingInstances ??= new Dictionary<ModelStreamId, HashSet<InstanceId>>();

            foreach (var errorState in states)
            {
                if (!m_CurrentExistingInstances.ContainsKey(modelStreamId))
                {
                    m_CurrentExistingInstances.Add(modelStreamId, new HashSet<InstanceId>());
                }
                //Store the instance ID in the current existing instances.
                m_CurrentExistingInstances[modelStreamId].Add(errorState.InstanceId);
            }
            
            if(m_HiddenInstances == null || m_HiddenInstances.Count == 0 || !AllHiddenInstancesMatch(modelStreamId)) return Task.CompletedTask;
            
            var leafNodes = new HashSet<InstanceId>();
            foreach (var state in states)
            {
                leafNodes.Add(state.InstanceId);
            }
            
            //Update the visibility in case we have loaded some new instances whose ancestors are hidden.
            _ = UpdateFilter(modelStreamId, leafNodes, false, InstanceData.Placeholder);
            
            return Task.CompletedTask;
        }
        
        private bool AllHiddenInstancesMatch(ModelStreamId modelStreamId)
        {
            if(m_HiddenInstances == null) return false;
            
            foreach (var instance in m_HiddenInstances)
            {
                if (instance.StreamingModel.ModelStream.Id != modelStreamId)
                {
                    return false;
                }
            }
            return true;
        }
        
        public override Task UnloadAsync(ModelStreamId modelStreamId, IEnumerable<InstanceGeometricErrorState> states)
        {
            var leafNodes = new HashSet<InstanceId>();
            foreach (var state in states)
            {
                leafNodes.Add(state.InstanceId);
                
                if (m_CurrentExistingInstances.ContainsKey(modelStreamId))
                {
                    m_CurrentExistingInstances[modelStreamId].Remove(state.InstanceId);
                }

                if (m_CurrentExistingInstances[modelStreamId].Count == 0)
                {
                    m_CurrentExistingInstances.Remove(modelStreamId);
                    m_Repositories?.Remove(modelStreamId);
                }
            }
            
            InstanceUpdater.SetVisibility(modelStreamId, leafNodes, true);
            
            return Task.CompletedTask;
        }

        public void UpdateVisibility(InstanceData instanceData, bool visible)
        {
            m_HiddenInstances ??= new HashSet<InstanceData>();
            if (visible)
            {
                m_HiddenInstances.Remove(instanceData);
            }
            else
            {
                m_HiddenInstances.Add(instanceData);
            }
            
            if (m_HiddenInstances.Count == 0)
            {
                Reset();
                return;
            }
            
            var modelStreamId = instanceData.StreamingModel.ModelStream.Id;
            m_Repositories ??= new Dictionary<ModelStreamId, IMetadataRepository>();
            m_Repositories.TryAdd(modelStreamId, instanceData.Repository);
            //If it is a branch node, we need to update the filter for all its leaf nodes.
            //Get all the leaf nodes
            var leafNodes = instanceData.Instance.HasChildren? 
                m_CurrentExistingInstances[modelStreamId] : 
                new HashSet<InstanceId>() {instanceData.Instance.Id};
            _ = UpdateFilter(modelStreamId, leafNodes, instanceData.Instance.HasChildren, instanceData);
        }
        
        private async Task UpdateFilter(ModelStreamId modelStreamId, HashSet<InstanceId> leafNodes, bool isBranchNode, InstanceData node)
        {
            if (m_CurrentExistingInstances == null || !m_CurrentExistingInstances.ContainsKey(modelStreamId))
            {
                return;
            }
            
            if(m_Repositories == null || m_Repositories.Count == 0 || !m_Repositories.TryGetValue(modelStreamId, out var repository))
            {
                return;
            }

            var instanceAncestry = await repository
                .Query()
                .Select(MetadataPathCollection.None, new OptionalData(OptionalData.Fields.AncestorIds | OptionalData.Fields.Id))
                .WhereInstanceEquals(leafNodes)
                .ToListAsync(CancellationToken.None);

            var hiddenInstances = new HashSet<InstanceId>();
            var showInstances = new HashSet<InstanceId>();
            
            if (isBranchNode && node != InstanceData.Placeholder)
            {
                //We need to check if any of the leaf nodes' ancestors are hidden.
                foreach (var metadataInstance in instanceAncestry)
                {
                    bool ancestorHidden = false;
                    foreach (var ancestorId in metadataInstance.AncestorIds)
                    {
                        if (AnyHiddenInstanceMatches(x => x.Instance.Id == ancestorId && x.StreamingModel.ModelStream.Id == modelStreamId))
                        {
                            ancestorHidden = true;
                            break;
                        }
                    }

                    if (ancestorHidden)
                    {
                        hiddenInstances.Add(metadataInstance.Id);
                    }
                    else
                    {
                        if(AnyHiddenInstanceMatches(x => x.StreamingModel.ModelStream.Id == modelStreamId && x.Instance.Id == metadataInstance.Id))
                        {
                            hiddenInstances.Add(metadataInstance.Id);
                        }
                        else
                        {
                            showInstances.Add(metadataInstance.Id);
                        }
                    }
                }
            } else if (!isBranchNode)
            {
                //We need to check if any of the leaf nodes' ancestors are hidden.
                foreach (var metadataInstance in instanceAncestry)
                {
                    bool ancestorHidden = false;
                    foreach (var ancestorId in metadataInstance.AncestorIds)
                    {
                        if (AnyHiddenInstanceMatches(x => x.Instance.Id == ancestorId && x.StreamingModel.ModelStream.Id == modelStreamId))
                        {
                            ancestorHidden = true;
                            break;
                        }
                    }

                    if (ancestorHidden)
                    {
                        hiddenInstances.Add(metadataInstance.Id);
                    }
                    else
                    {
                        if(AnyHiddenInstanceMatches(x => x.StreamingModel.ModelStream.Id == modelStreamId && x.Instance.Id == metadataInstance.Id))
                        {
                            hiddenInstances.Add(metadataInstance.Id);
                            m_HiddenInstances.Add(new InstanceData(metadataInstance, node.StreamingModel, node.Repository));
                        }
                        else
                        {
                            showInstances.Add(metadataInstance.Id);
                            m_HiddenInstances.RemoveWhere(x => x.StreamingModel.ModelStream.Id == modelStreamId && x.Instance.Id == metadataInstance.Id);
                        }
                    }
                }
            }
            
            InstanceUpdater.SetVisibility(modelStreamId, showInstances, true);
            InstanceUpdater.SetVisibility(modelStreamId, hiddenInstances, false);
        }

        private bool AnyHiddenInstanceMatches(Func<InstanceData, bool> predicate)
        {
            if(m_HiddenInstances == null) return false;
            
            foreach (var instance in m_HiddenInstances)
            {
                if (predicate(instance))
                {
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            m_CurrentExistingInstances?.Clear();
            m_HiddenInstances?.Clear();
            m_HiddenInstances = null;
            m_CurrentExistingInstances = null;
        }
    }
}