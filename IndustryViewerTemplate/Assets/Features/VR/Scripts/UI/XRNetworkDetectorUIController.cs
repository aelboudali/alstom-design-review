using UnityEngine;
using Unity.Industry.Viewer.Shared;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.VR
{
    public class XRNetworkDetectorUIController : NetworkDetectorUIController
    {
        private bool _inStreaming = false;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SceneManager.activeSceneChanged += OnSceneManagerOnactiveSceneChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.activeSceneChanged -= OnSceneManagerOnactiveSceneChanged;
        }

        protected override void OnNetworkStatusChanged(bool connected)
        {
            if(_inStreaming) return;
            m_offlineModeVE.style.display = connected ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnSceneManagerOnactiveSceneChanged(Scene from, Scene to)
        {
            _inStreaming = to != gameObject.scene;
            if (_inStreaming)
            {
                m_offlineModeVE.style.display = DisplayStyle.None;
            } else {
                m_offlineModeVE.style.display = NetworkDetector.IsOffline ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
