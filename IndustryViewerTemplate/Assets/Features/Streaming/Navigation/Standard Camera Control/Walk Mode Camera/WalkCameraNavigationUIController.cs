using Unity.AppUI.UI;
using Unity.Industry.Viewer.AppSettings;
using Unity.Industry.Viewer.Navigation.StandardCameraControl.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Navigation.WalkModeCamera
{
    public class WalkCameraNavigationUIController : NavigationJoysticksOptionUI
    {
        const string k_MoveSensitivitySlider = "MoveSensitivitySlider";
        const string k_RotationSensitivitySlider = "RotationSensitivitySlider";
        const string k_CameraHeightSlider = "CameraHeightSlider";

        [SerializeField]
        private WalkCameraNavigationController m_WalkCameraNavigationController;

        [SerializeField]
        private WalkCameraInputSystemController m_CameraInputSystemController;

        private float m_CameraHeight;
        private TouchSliderFloat m_MoveSensitivitySlider;
        private TouchSliderFloat m_RotationSensitivitySlider;
        private TouchSliderFloat m_CameraHeightSlider;

        protected override void OnEnable()
        {
            base.m_baseCameraInputSystemController = m_CameraInputSystemController;
            base.OnEnable();

            InAppSettings.SettingsPanelShown += SettingsPanelUp;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            InAppSettings.SettingsPanelShown -= SettingsPanelUp;
            if (m_SettingsPanel != null && m_SettingsPanel.Contains(m_Title))
                m_SettingsPanel.Q<ScrollView>().Remove(m_Title);
        }

        protected override void InitialUI(VisualElement panel)
        {
            base.InitialUI(panel);

            m_MoveSensitivitySlider = panel.Q<TouchSliderFloat>(k_MoveSensitivitySlider);
            m_RotationSensitivitySlider = panel.Q<TouchSliderFloat>(k_RotationSensitivitySlider);
            m_CameraHeightSlider = panel.Q<TouchSliderFloat>(k_CameraHeightSlider);

            m_MoveSensitivitySlider.RegisterValueChangingCallback(OnMoveSensitivityChanging);
            m_MoveSensitivitySlider.RegisterValueChangedCallback(OnMoveSensitivityChanged);

            m_RotationSensitivitySlider.RegisterValueChangingCallback(OnRotateSensitivityChanging);
            m_RotationSensitivitySlider.RegisterValueChangedCallback(OnRotateSensitivityChanged);

            m_CameraHeightSlider.RegisterValueChangingCallback(OnCameraHeightChanging);
            m_CameraHeightSlider.RegisterValueChangedCallback(OnCameraHeightChanged);

            m_MoveSensitivitySlider.SetValueWithoutNotify(m_CameraInputSystemController.MoveSensitivity);
            m_RotationSensitivitySlider.SetValueWithoutNotify(m_CameraInputSystemController.RotateSensitivity);
            m_CameraHeight = m_CameraInputSystemController.WalkModeMoveController.CharacterHeight;
            m_CameraHeightSlider.SetValueWithoutNotify(m_CameraHeight);
        }

        private void OnRotateSensitivityChanged(ChangeEvent<float> evt)
        {
            m_CameraInputSystemController.UpdateRotateSensitivity(evt.newValue);
        }

        private void OnRotateSensitivityChanging(ChangingEvent<float> evt)
        {
            m_CameraInputSystemController.UpdateRotateSensitivity(evt.newValue);
        }

        private void OnMoveSensitivityChanged(ChangeEvent<float> evt)
        {
            m_CameraInputSystemController.UpdateMoveSensitivity(evt.newValue);
        }

        private void OnMoveSensitivityChanging(ChangingEvent<float> evt)
        {
            m_CameraInputSystemController.UpdateMoveSensitivity(evt.newValue);
        }

        private void OnCameraHeightChanging(ChangingEvent<float> evt)
        {
            m_CameraInputSystemController.WalkModeMoveController.CharacterHeight = evt.newValue;
            m_CameraHeight = evt.newValue;
        }

        private void OnCameraHeightChanged(ChangeEvent<float> evt)
        {
            m_CameraInputSystemController.WalkModeMoveController.CharacterHeight = evt.newValue;
            m_CameraHeight = evt.newValue;
        }
    }
}