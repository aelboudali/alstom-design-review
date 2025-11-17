using UnityEngine;
using UnityEngine.Localization;

namespace Unity.Industry.Viewer.Streaming
{
    [CreateAssetMenu(fileName = "StreamingToolAsset", menuName = "IVT/Streaming/StreamingToolAsset")]
    public class StreamingToolAsset : ScriptableObject
    {
        public LocalizedString ToolName => toolName;
        
        [SerializeField]
        private LocalizedString toolName;
        public Texture2D toolIcon;
        public GameObject toolPrefab;

        public GameObject sceneListener;

        public bool resizablePanel = false;
    }
}