using System;
using Unity.Industry.Viewer.Assets;
using Unity.Industry.Viewer.Identity;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Industry.Viewer.Streaming
{
    public class MainSceneController : MonoBehaviour
    {
        public static Action StartStreaming;
        
        [SerializeField]
        private string streamingSceneName;
        public string StreamingSceneName => streamingSceneName;

        [SerializeField]
        private bool keepMainSceneCameraActive = false;
        
        [SerializeField] private Camera mainSceneCamera;

        protected virtual void Start()
        {
            StartStreaming += OnStartStreaming;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        
        private void OnDestroy()
        {
            StartStreaming -= OnStartStreaming;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        protected virtual void OnActiveSceneChanged(Scene fromScene, Scene toScene)
        {
            if (keepMainSceneCameraActive) return;
            mainSceneCamera.gameObject.SetActive(string.Equals(toScene.name, gameObject.scene.name));
        }

        private void OnStartStreaming()
        {
#if ENABLE_MULTIPLAY
            // CheckingForNewVersion logic can break flow if will be executed before multiplayer session is started.
            // After the session will be established the version check will be switched on back.
            if (!NetworkDetector.RequestedOfflineMode && !IdentityController.GuestMode)
            {
                AssetsController.IsCheckingForNewVersionEnabled = false;
            }
#endif
            SceneManager.LoadScene(streamingSceneName, LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
        {
            if (!string.Equals(loadedScene.name, streamingSceneName))
            {
                return;
            }

            if (!keepMainSceneCameraActive)
            {
                mainSceneCamera.gameObject.SetActive(false);
            }
            
            SceneManager.SetActiveScene(loadedScene);
        }
    }
}
