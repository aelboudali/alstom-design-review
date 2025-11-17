using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cloud.Assets;
using Unity.Cloud.DataStreaming.Runtime;
using UnityEngine;
using AssetInfo = Unity.Industry.Viewer.Assets.AssetInfo;

namespace Unity.Industry.Viewer.Streaming
{
    public class StreamingModel : MonoBehaviour
    {
        public static Action<StreamingModel> OnActivityStateChanged;
        
        private IModelStream m_ModelStream;
        private IAsset m_Asset;
        private IDataset m_Dataset;
        private AssetProperties? m_AssetProperties;
        private int m_InstanceNumber;

        public string AssetId => m_Asset.Descriptor.AssetId.ToString();

        public string AssetName
        {
            get
            {
                if (m_Asset != null && m_Asset is not OfflineAsset && m_AssetProperties.HasValue)
                {
                    return m_AssetProperties.Value.Name;
                }
                if (m_Asset is OfflineAsset offlineAsset)
                {
                    return offlineAsset.OfflineAssetInfo.assetName;
                }

                return string.Empty;
            }
        }
        
        public string ProjectID => m_Asset != null ? m_Asset.Descriptor.ProjectId.ToString() : string.Empty;
        
        public string OrgID => m_Asset != null ? m_Asset.Descriptor.OrganizationId.ToString() : string.Empty;
        
        public int Version
        {
            get
            {
                if (m_Asset != null && m_Asset is not OfflineAsset && m_AssetProperties.HasValue)
                {
                    return m_AssetProperties.Value.FrozenSequenceNumber;
                }
                if (m_Asset is OfflineAsset offlineAsset)
                {
                    return offlineAsset.OfflineAssetInfo.assetVersion;
                }
                return 0;
            }
        }

        public string VersionID => m_Asset != null ? m_Asset.Descriptor.AssetVersion.ToString() : string.Empty;
        
        public ModelStreamId ModelStreamId => m_ModelStream.Id;
        public IModelStream ModelStream => m_ModelStream;
        public IAsset Asset => m_Asset;
        public IDataset Dataset => m_Dataset;
        public int InstanceNumber => m_InstanceNumber;
        public bool IsStreaming { get; private set; }

        public void Initialize(
            IModelStream modelStream,
            AssetInfo asset,
            IDataset dataset,
            bool isStreaming,
            int? instanceNumber = null)
        {
            m_ModelStream = modelStream;
            m_AssetProperties = asset.Properties;
            m_Asset = asset.Asset;
            m_Dataset = dataset;
            IsStreaming = isStreaming;
            m_InstanceNumber = instanceNumber is null or 0 ? GetInstanceNumber() : instanceNumber.Value;
        }

        public void Initialize(
            IModelStream modelStream,
            AssetInfo offlineAsset,
            bool isStreaming,
            int? instanceNumber = null)
        {
            m_Asset = offlineAsset.Asset;
            m_ModelStream = modelStream;
            IsStreaming = isStreaming;
            m_InstanceNumber = instanceNumber is null or 0 ? GetInstanceNumber() : instanceNumber.Value;
        }

        private int GetInstanceNumber()
        {
            var streamingModels = new List<StreamingModel>();
            TransformController.Instance.GetComponentsInChildren(true, streamingModels);
            var assetId = AssetId;

            return streamingModels
                .Where(streamingModel => streamingModel.AssetId == assetId)
                .Select(streamingModel => streamingModel.InstanceNumber)
                .DefaultIfEmpty()
                .Max() + 1;
        }

        private void OnEnable()
        {
            if(m_ModelStream == null) return;
            m_ModelStream.Visibility?.Set(true);
            OnActivityStateChanged?.Invoke(this);
        }

        private void OnDisable()
        {
            if(m_ModelStream == null) return;
            m_ModelStream.Visibility?.Set(false);
            OnActivityStateChanged?.Invoke(this);
        }

        public override bool Equals(object obj)
        {
            if (obj is StreamingModel other)
            {
                return AssetId == other.AssetId &&
                       AssetName == other.AssetName &&
                       gameObject.name == other.gameObject.name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (AssetId, AssetName, gameObject.name).GetHashCode();
        }
    }
}
