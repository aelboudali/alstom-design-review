using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AssetInfo = Unity.Industry.Viewer.Assets.AssetInfo;

namespace Unity.Industry.Viewer.Streaming.AddModel
{
    // This script manages the addition of models in a Unity project.
    // It integrates with Unity's MonoBehaviour for lifecycle management and supports both online and offline modes.
    // The script also includes VR-specific handling for interactions and model manipulation.
    public class AddModelToolController : MonoBehaviour
    {
        public static Action<HashSet<AssetInfo>> OnSelectedAssetChanged;
        
        // Events for tool lifecycle management
        private HashSet<AssetInfo> SelectedAssets
        {
            get => m_SelectedAssets;
            set
            {
                m_SelectedAssets = value;
                OnSelectedAssetChanged?.Invoke(m_SelectedAssets);
            }
        }
        
        private HashSet<AssetInfo> m_SelectedAssets;
        
        private StreamingModelController m_StreamingModelController;

        private void Start()
        {
            m_SelectedAssets ??= new HashSet<AssetInfo>();
            m_StreamingModelController = FindAnyObjectByType<StreamingModelController>(FindObjectsInactive.Include);
        }

        public bool SelectedAssetsContainExactVersion(AssetInfo assetInfo)
        {
            return m_SelectedAssets.Any(selectedAsset =>
                selectedAsset.Asset.Descriptor.AssetId == assetInfo.Asset.Descriptor.AssetId
                && selectedAsset.Asset.Descriptor.AssetVersion == assetInfo.Asset.Descriptor.AssetVersion);
        }

        public bool SelectedAssetsContainAnyVersion(AssetInfo assetInfo)
        {
            return m_SelectedAssets.Any(selectedAsset => selectedAsset.Asset.Descriptor.AssetId == assetInfo.Asset.Descriptor.AssetId);
        }

        public AssetInfo? GetSelectedAssetVersion(AssetInfo assetInfo)
        {
            var result = m_SelectedAssets.FirstOrDefault(selectedAsset => selectedAsset.Asset.Descriptor.AssetId == assetInfo.Asset.Descriptor.AssetId);
            return result.Asset != null ? result : null;
        }

        public int GetSelectedAssetCount()
        {
            return m_SelectedAssets.Count;
        }

        public bool RemoveExactVersionOfSelectedAsset(AssetInfo assetInfo)
        {
            return m_SelectedAssets.Remove(assetInfo);
        }

        public bool ManageSelectedAssets(AssetInfo assetInfo, bool add)
        {
            // NOTE currently only single version of an asset can be selected at a time
            var existingAsset = m_SelectedAssets.FirstOrDefault(selectedAsset => selectedAsset.Asset.Descriptor.AssetId == assetInfo.Asset.Descriptor.AssetId);

            if (add)
            {
                if (existingAsset.Asset != null
                    && existingAsset.Asset.Descriptor.AssetId == assetInfo.Asset.Descriptor.AssetId
                    && existingAsset.Asset.Descriptor.AssetVersion == assetInfo.Asset.Descriptor.AssetVersion)
                {
                    // Same version already selected, nothing to do
                    return true;
                }

                if (existingAsset.Asset == null)
                {
                    // Nothing selected, add it
                    m_SelectedAssets.Add(assetInfo);
                    return true;
                }

                // Different version selected, replace it
                m_SelectedAssets.Remove(existingAsset);
                m_SelectedAssets.Add(assetInfo);
                return true;
            }

            // No version found, nothing to remove
            if (existingAsset.Asset == null)
            {
                return false;
            }

            // Remove any version of the asset
            m_SelectedAssets.Remove(existingAsset);
            return false;
        }

        public void ClearSelectedAssets()
        {
            m_SelectedAssets.Clear();
        }

        public void AddToScene()
        {
            var newLayoutJson = new LayoutJson();
            foreach (var selectedAsset in m_SelectedAssets)
            {
                newLayoutJson.LayoutModels ??= new List<LayoutModelEntity>();
                newLayoutJson.LayoutModels.Add(new LayoutModelEntity()
                {
                    assetID = selectedAsset.Asset.Descriptor.AssetId.ToString(),
                    orgID = selectedAsset.Asset.Descriptor.OrganizationId.ToString(),
                    projectID = selectedAsset.Asset.Descriptor.ProjectDescriptor.ProjectId.ToString(),
                    versionID = selectedAsset.Asset.Descriptor.AssetVersion.ToString(),
                    version = selectedAsset.Properties?.FrozenSequenceNumber ?? 0,
                });
            }

            ClearSelectedAssets();

            if (newLayoutJson.LayoutModels == null || newLayoutJson.LayoutModels.Count == 0) return;
            _ = m_StreamingModelController.ProcessLayoutJson(newLayoutJson, this);
        }
    }
}
