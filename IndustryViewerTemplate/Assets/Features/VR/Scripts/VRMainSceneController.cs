using Unity.Industry.Viewer.Streaming;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Unity.Industry.Viewer.VR
{
    public class VRMainSceneController : MainSceneController
    {
        [SerializeField] private Light m_MainLight;
        private float defaultNearClipPlane;
        private float defaultFarClipPlane;
        
        protected override void Start()
        {
            base.Start();
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
            defaultNearClipPlane = Camera.main.nearClipPlane;
            defaultFarClipPlane = Camera.main.farClipPlane;
        }

        protected override void OnActiveSceneChanged(Scene fromScene, Scene toScene)
        {
            if (toScene == gameObject.scene)
            {
                StartCoroutine(WaitForFrame());
            }
            base.OnActiveSceneChanged(fromScene, toScene);
        }

        IEnumerator WaitForFrame()
        {
            yield return null;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
            m_MainLight.gameObject.SetActive(true);
            Camera.main.nearClipPlane = defaultNearClipPlane;
            Camera.main.farClipPlane = defaultFarClipPlane;
        }
    }
}
