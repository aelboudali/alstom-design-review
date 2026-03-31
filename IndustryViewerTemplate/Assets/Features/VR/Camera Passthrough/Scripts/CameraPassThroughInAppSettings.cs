using UnityEngine;
using Unity.Industry.Viewer.AppSettings;
using UnityEngine.UIElements;
using System.Linq;
using Toggle = Unity.AppUI.UI.Toggle;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Unity.Industry.Viewer.VR.CameraPassThrough
{
    public class CameraPassThroughInAppSettings : MonoBehaviour
    {
        
        [SerializeField] private VisualTreeAsset m_CameraPassThroughSettingsUI;
        Toggle m_CameraPassThroughToggle;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private IEnumerator Start()
        {
            SceneManager.activeSceneChanged += OnSceneManagerOnactiveSceneChanged;
            InAppSettings.SettingsPanelShow += OnSettingsPanelShow;
            InAppSettings.SettingsPanelDismissed += OnSettingsPanelDismissed;
            CameraPassThroughGlobal.InMRMode = false;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
            VRMainSceneController mainSceneController = FindFirstObjectByType<VRMainSceneController>();
            if (mainSceneController == null)
            {
                yield break;
            }
            //Wait for 1 second to make sure the main scene is fully loaded and the user is not in MR mode, then check if we need to enable camera passthrough based on the saved value or the default value
            yield return new WaitForSeconds(1f);
            if (!PlayerPrefs.HasKey(CameraPassThroughGlobal.k_CameraPassThroughEnabledKey))
            {
                if (mainSceneController.EnableCameraPassthroughOnStart)
                {
                    OnCameraPassThroughValueChanged(true);
                }
            }
            else
            {
                bool isEnabled = PlayerPrefs.GetInt(CameraPassThroughGlobal.k_CameraPassThroughEnabledKey) == 1;
                if (isEnabled)
                {
                    OnCameraPassThroughValueChanged(true);
                }
            }
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneManagerOnactiveSceneChanged;
            InAppSettings.SettingsPanelShow -= OnSettingsPanelShow;
            InAppSettings.SettingsPanelDismissed -= OnSettingsPanelDismissed;
        }

        private void OnSceneManagerOnactiveSceneChanged(Scene fromScene, Scene toScene)
        {
            if (toScene == gameObject.scene)
            {
                if (CameraPassThroughGlobal.isCameraPassThroughEnabled)
                {
                    return;
                }
                CameraPassThroughGlobal.ToggleCameraPassThrough(false);
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
            }
        }

        private void OnSettingsPanelDismissed()
        {
            m_CameraPassThroughToggle.UnregisterValueChangedCallback(OnCameraPassThroughToggleValueChanged);
            m_CameraPassThroughToggle = null;
        }

        private void OnSettingsPanelShow(VisualElement vePanel, VisualTreeAsset titleTemplate)
        {
            var newTitle = titleTemplate.Instantiate().Children().First();
            var m_settings = m_CameraPassThroughSettingsUI.Instantiate().Children().First();
            
            m_CameraPassThroughToggle = m_settings.Q<Toggle>(CameraPassThroughGlobal.k_CameraPassThroughToggleName);
            m_CameraPassThroughToggle.value = CameraPassThroughGlobal.isCameraPassThroughEnabled;
            m_CameraPassThroughToggle.RegisterValueChangedCallback(OnCameraPassThroughToggleValueChanged);

            m_CameraPassThroughToggle.SetEnabled(!CameraPassThroughGlobal.InMRMode);
            
            InAppSettings.InitializeSection(string.Empty, ref newTitle, m_settings);
            vePanel.Q<ScrollView>().Add(newTitle);
        }

        private void OnCameraPassThroughToggleValueChanged(ChangeEvent<bool> evt)
        {
            OnCameraPassThroughValueChanged(evt.newValue);
        }

        private void OnCameraPassThroughValueChanged(bool newValue)
        {
            CameraPassThroughGlobal.isCameraPassThroughEnabled = newValue;
            PlayerPrefs.SetInt(CameraPassThroughGlobal.k_CameraPassThroughEnabledKey, CameraPassThroughGlobal.isCameraPassThroughEnabled? 1 : 0);
            if (!CameraPassThroughGlobal.isCameraPassThroughEnabled)
            {
                PlayerPrefs.DeleteKey(CameraPassThroughGlobal.k_CameraPassThroughEnabledKey);
            }
            CameraPassThroughGlobal.ToggleCameraPassThrough(newValue);
        }
    }
}
