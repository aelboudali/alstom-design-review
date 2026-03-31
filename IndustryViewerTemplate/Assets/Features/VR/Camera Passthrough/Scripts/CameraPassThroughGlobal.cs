using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Unity.Industry.Viewer.VR.CameraPassThrough
{
    public static class CameraPassThroughGlobal
    {
        public const string k_CameraPassThroughToggleName = "CameraPassThroughToggle";
        public static bool isCameraPassThroughEnabled = false;
        public static bool InMRMode = false;
        public const string k_CameraPassThroughEnabledKey = "CameraPassThroughEnabled";
        private static Color originalBackgroundColor;
        private static CameraClearFlags originalCameraFlags;
        
        public static void ToggleCameraPassThrough(bool newValue)
        {
            if (!Camera.main.transform.TryGetComponent(out ARCameraManager ARCameraManager))
            {
                ARCameraManager = Camera.main.gameObject.AddComponent<ARCameraManager>();
            }
            ARCameraManager.enabled = newValue;
            
            ARSession ARSession = Object.FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
            if (ARSession == null)
            {
                var arSessionObject = new GameObject("ARSession");
                arSessionObject.SetActive(false);
                ARSession = arSessionObject.AddComponent<ARSession>();
                ARSession.gameObject.AddComponent<ARInputManager>();
            }

            if (newValue)
            {
                ARSession.gameObject.SetActive(true);
            }
            
            ARSession.enabled = newValue;
            if (newValue)
            {
                originalCameraFlags = Camera.main.clearFlags;
                if (originalCameraFlags == CameraClearFlags.Color || originalCameraFlags == CameraClearFlags.SolidColor)
                {
                    originalBackgroundColor = Camera.main.backgroundColor;
                }
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.clear;
            }
            else
            {
                Camera.main.clearFlags = originalCameraFlags;
                Camera.main.backgroundColor = originalBackgroundColor;
            }
        }
    }
}
