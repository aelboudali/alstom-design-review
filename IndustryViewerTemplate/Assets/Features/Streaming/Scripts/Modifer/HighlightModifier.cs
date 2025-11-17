using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Cloud.Common;
using Unity.Cloud.DataStreaming.Runtime;
using Unity.Cloud.DataStreaming.Metadata;
using UnityEngine;
using System.Threading;

namespace Unity.Industry.Viewer.Streaming
{
    public class HighlightModifier : Modifier, IDisposable
    {
        private Dictionary<ModelStreamId, HashSet<InstanceId>> m_CurrentExistingInstances;
        
        private Dictionary<ModelStreamId, IMetadataRepository> m_Repositories;

        private HashSet<InstanceData> m_CurrentHighlightList;
        
        /// <summary>
        /// Value used when removing the highlight state on an instance.
        /// </summary>
        static readonly Color32 k_DefaultColor = new(0, 0, 0, 0);

        /// <summary>
        /// Value used when highlighting an instance.
        /// </summary>
        static Color32 _highlightColor = new(0, 200, 255, 255);

        public HighlightModifier(Color color)
        {
            _highlightColor = (Color32)color;
            m_CurrentHighlightList = new HashSet<InstanceData>();
        }
        
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

                m_CurrentExistingInstances[modelStreamId].Add(errorState.InstanceId);
            }
            
            return Task.CompletedTask;
        }
        
        public override Task UnloadAsync(ModelStreamId modelStreamId, IEnumerable<InstanceGeometricErrorState> states)
        {
            
            if (m_CurrentExistingInstances == null)
            {
                return Task.CompletedTask;
            }
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
            
            InstanceUpdater.SetHighlight(modelStreamId, leafNodes, k_DefaultColor);
            
            return Task.CompletedTask;
        }

        public void UpdateList(InstanceData instanceData, bool highlight, CancellationTokenSource tokenSource)
        {
            if (highlight)
            {
                m_CurrentHighlightList.Add(instanceData);
            }
            else
            {
                m_CurrentHighlightList.Remove(instanceData);
            }

            if (m_CurrentHighlightList.Count == 0)
            {
                Reset();
                return;
            }
            
            var modelStreamId = instanceData.StreamingModel.ModelStream.Id;
            m_Repositories ??= new Dictionary<ModelStreamId, IMetadataRepository>();
            m_Repositories.TryAdd(modelStreamId, instanceData.Repository);
            var leafNodes = instanceData.Instance.HasChildren && m_CurrentExistingInstances.TryGetValue(modelStreamId, out var instance)? 
                instance : 
                new HashSet<InstanceId>() {instanceData.Instance.Id};
            _ = UpdateHighlight(modelStreamId, leafNodes, instanceData.Instance.HasChildren, instanceData, tokenSource);
        }

        private async Task UpdateHighlight(ModelStreamId modelStreamId, HashSet<InstanceId> leafNodes,
            bool isBranchNode, InstanceData node, CancellationTokenSource tokenSource)
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
                .ToListAsync(tokenSource.Token);

            var highlightInstances = new HashSet<InstanceId>();
            var normalInstances = new HashSet<InstanceId>();
            
            if (isBranchNode && node != InstanceData.Placeholder)
            {
                highlightInstances.Add(node.Instance.Id);
                //We need to check if any of the leaf nodes' ancestors are hidden.
                foreach (var metadataInstance in instanceAncestry)
                {
                    bool ancestorHighlighted = false;
                    foreach (var ancestorId in metadataInstance.AncestorIds)
                    {
                        if (AnyHighlightedInstanceMatches(x => x.Instance.Id == ancestorId && x.StreamingModel.ModelStream.Id == modelStreamId))
                        {
                            ancestorHighlighted = true;
                            break;
                        }
                    }

                    if (ancestorHighlighted)
                    {
                        highlightInstances.Add(metadataInstance.Id);
                    }
                    else
                    {
                        normalInstances.Add(metadataInstance.Id);
                    }
                }
            } else if (!isBranchNode)
            {
                //We need to check if any of the leaf nodes' ancestors are hidden.
                foreach (var metadataInstance in instanceAncestry)
                {
                    if (metadataInstance.Id == node.Instance.Id)
                    {
                        highlightInstances.Add(metadataInstance.Id);
                    }
                    else
                    {
                        normalInstances.Add(metadataInstance.Id);
                    }
                }
            }
            
            InstanceUpdater.SetHighlight(modelStreamId, highlightInstances, _highlightColor);
            InstanceUpdater.SetHighlight(modelStreamId, normalInstances, k_DefaultColor);
        }

        public override void Reset()
        {
            base.Reset();

            if (m_CurrentExistingInstances != null)
            {
                foreach (var key in m_CurrentExistingInstances.Keys)
                {
                    InstanceUpdater.SetHighlight(key, m_CurrentExistingInstances[key], k_DefaultColor);
                }
            }
            
            m_CurrentHighlightList?.Clear();
        }

        protected override void Update(ModelStreamId modelId, InstanceId instanceId, bool state)
        {
            
            InstanceUpdater.SetHighlight(modelId, new InstanceId[] {instanceId}, state? _highlightColor : k_DefaultColor);
        }

        private bool AnyHighlightedInstanceMatches(Func<InstanceData, bool> predicate)
        {
            if(m_CurrentHighlightList == null) return false;
            
            foreach (var instance in m_CurrentHighlightList)
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
            m_CurrentHighlightList?.Clear();
            m_CurrentHighlightList = null;
            m_CurrentExistingInstances = null;
        }
    }
}
