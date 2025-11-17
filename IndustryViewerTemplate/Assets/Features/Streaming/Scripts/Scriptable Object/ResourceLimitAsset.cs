using System;
using UnityEngine;

namespace Unity.Industry.Viewer.Streaming
{
    [Serializable]
    public class ResourceLimitEntry
    {
        public RuntimePlatform Platform;
        public int MaxResources;
        [Tooltip("For devices like standalone VR for example, that are not tethered to a PC")]
        public bool IsNonTethered;
    }
    
    [CreateAssetMenu(fileName = "Resource Limit Asset", menuName = "IVT/Streaming/Resource Limit Asset")]
    public class ResourceLimitAsset : ScriptableObject
    {
        public ResourceLimitEntry[] ResourceLimits => _resourceLimits;
        [SerializeField]
        private ResourceLimitEntry[] _resourceLimits;
    }
}
