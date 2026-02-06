using UnityEngine.Localization;
using Unity.Cloud.Assets;
using System;
using System.Threading.Tasks;

namespace Unity.Industry.Viewer.Assets
{
    // TODO: need to be updated, now asset type has more members
    public static class CustomAssetTypeExtension
    {
        private const string k_AssetsTableKey = "Assets";
        private const string k_AssetType_2D = "Type 2D Asset";
        private const string k_AssetType_3D = "Type 3D Model";
        private const string k_AssetType_Audio = "Type Audio";
        private const string k_AssetType_Material = "Type Material";
        private const string k_AssetType_Other = "Type Other";
        private const string k_AssetType_Script = "Type Script";
        private const string k_AssetType_Video = "Type Video";
        private const string k_AssetType_UnityEditor = "Type Unity Editor";
        
        public static string GetAssetTypeAsString(this AssetType assetType)
        {
            return assetType switch
            {
                AssetType.Asset_2D => $"@{k_AssetsTableKey}:{k_AssetType_2D}",
                AssetType.Model_3D => $"@{k_AssetsTableKey}:{k_AssetType_3D}",
                AssetType.Audio => $"@{k_AssetsTableKey}:{k_AssetType_Audio}",
                AssetType.Material => $"@{k_AssetsTableKey}:{k_AssetType_Material}",
                AssetType.Other => $"@{k_AssetsTableKey}:{k_AssetType_Other}",
                AssetType.Script => $"@{k_AssetsTableKey}:{k_AssetType_Script}",
                AssetType.Video => $"@{k_AssetsTableKey}:{k_AssetType_Video}",
                AssetType.Unity_Editor => $"@{k_AssetsTableKey}:{k_AssetType_UnityEditor}",
                _ => null
            };
        }

        public static AssetType[] AssetTypeList()
        {
            return (AssetType[])Enum.GetValues(typeof(AssetType));
        }
    }
}
