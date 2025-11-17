using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Unity.Industry.Viewer.Streaming.Environment
{
    [Serializable]
    public class SceneObject
    {
        public UnityEngine.Object SceneAsset => sceneAsset;
        
        [SerializeField]
        private UnityEngine.Object sceneAsset;
        
        [SerializeField]
        public string SceneName;
    }
    
    /// <summary>
    /// Provides setting values for an Environment Scene.
    /// </summary>
    [Serializable]
    public class EnvironmentSceneSettings
    {
        [Tooltip("Unique ID of the Environment")]
        public string id;

        public SceneObject Scene;

        public LocalizedString DisplayName;

        public Texture2D Thumbnail;
    }
}
