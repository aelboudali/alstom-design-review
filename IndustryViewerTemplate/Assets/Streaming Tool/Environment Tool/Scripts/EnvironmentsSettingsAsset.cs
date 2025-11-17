using UnityEngine;

namespace Unity.Industry.Viewer.Streaming.Environment
{
    /// <summary>
    /// Provides setting values for Environment Tool functionality.
    /// Here all environment scenes and their settings are registered.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(EnvironmentsSettingsAsset), menuName = "IVT/Streaming/" + nameof(EnvironmentsSettingsAsset))]
    public class EnvironmentsSettingsAsset : ScriptableObject
    {
        public EnvironmentSceneSettings[] Scenes;
    }
}
