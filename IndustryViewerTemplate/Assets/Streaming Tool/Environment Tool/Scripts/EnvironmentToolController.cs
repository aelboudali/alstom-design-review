using System;
using UnityEngine;

namespace Unity.Industry.Viewer.Streaming.Environment
{
    public class EnvironmentToolController: StreamToolControllerBase
    {
        public const string k_DefaultEnvironmentId = "default";

        public static Action<LayerMask> EnvironmentLoaded;
        public static Action DefaultEnvironmentLoaded;
        public static Action<string> SetEnvironment;

        public static EnvironmentSceneSettings CurrentEnvironmentSettings;

        public static bool IsLoading;
        
        private Camera m_EnvironmentCamera;

        public override void OnToolOpened()
        {
            ToolOpened?.Invoke();
        }

        public override void OnToolClosed()
        {
            ToolClosed?.Invoke();
        }
    }
}
