using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Industry.Viewer.Streaming
{
    public static class SceneUtility
    {
        public static bool IsMainSceneActive => Object.FindFirstObjectByType<MainSceneController>().gameObject.scene == SceneManager.GetActiveScene();

        public static string GetStreamingSceneName()
        {
            return Object.FindFirstObjectByType<MainSceneController>().StreamingSceneName;
        }
    }
}
