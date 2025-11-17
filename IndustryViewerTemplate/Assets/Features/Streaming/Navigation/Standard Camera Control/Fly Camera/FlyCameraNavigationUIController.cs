using Unity.AppUI.UI;
using Unity.Industry.Viewer.AppSettings;
using Unity.Industry.Viewer.Assets;
using Unity.Industry.Viewer.Navigation.StandardCameraControl.Shared;
using Unity.Industry.Viewer.Streaming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Navigation.FlyCamera
{
    public class FlyCameraNavigationUIController : NavigationJoysticksOptionUI
    {
        const string k_MoveSensitivitySlider = "MoveSensitivitySlider";
        const string k_RotationSensitivitySlider = "RotationSensitivitySlider";

        [SerializeField]
        private FlyCameraNavigationController m_FlyCameraNavigationController;

        [SerializeField]
        private FlyCameraInputSystemController m_CameraInputSystemController;

        private IconButton m_HomeButton;

        private TouchSliderFloat m_MoveSensitivitySlider;
        private TouchSliderFloat m_RotationSensitivitySlider;

        protected override void OnEnable()
        {
            base.m_baseCameraInputSystemController = m_CameraInputSystemController;
            base.OnEnable();

            if (m_HomeButton == null)
            {
                var UIDocument = SharedUIManager.Instance.AssetsUIDocument;
                var streamingContainer = UIDocument.rootVisualElement.Q<VisualElement>(StreamingUtils.StreamingPanelName);
                var bottomLeftContainer = streamingContainer.Q<VisualElement>(StreamingUtils.BottomLeftContainerName);
                
                m_HomeButton = new IconButton()
                {
                    icon = "camera-overhead"
                };

                m_HomeButton.AddToClassList(StreamingUtils.BottomLeftButtonStyleName);
                m_HomeButton.clicked += OnHomeButtonClicked;
                bottomLeftContainer.Insert(bottomLeftContainer.childCount, m_HomeButton);
            }
            else
            {
                m_HomeButton.style.display = DisplayStyle.Flex;
            }

            InAppSettings.SettingsPanelShown += SettingsPanelUp;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_HomeButton != null)
            {
                m_HomeButton.style.display = DisplayStyle.None;
            }
            
            InAppSettings.SettingsPanelShown -= SettingsPanelUp;
            if (m_SettingsPanel != null && m_SettingsPanel.Contains(m_Title))
                m_SettingsPanel.Q<ScrollView>().Remove(m_Title);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_HomeButton != null)
            {
                m_HomeButton.clicked -= OnHomeButtonClicked;
                m_HomeButton.RemoveFromHierarchy();
            }
        }

        private void OnHomeButtonClicked()
        {
            NavigationController.RequestDefaultHomeView?.Invoke();
        }

        protected override void InitialUI(VisualElement panel)
        {
            base.InitialUI(panel);

            m_MoveSensitivitySlider = panel.Q<TouchSliderFloat>(k_MoveSensitivitySlider);
            m_RotationSensitivitySlider = panel.Q<TouchSliderFloat>(k_RotationSensitivitySlider);

            m_MoveSensitivitySlider.RegisterValueChangingCallback(OnMoveSensitivityChanging);
            m_MoveSensitivitySlider.RegisterValueChangedCallback(OnMoveSensitivityChanged);
            
            m_RotationSensitivitySlider.RegisterValueChangingCallback(OnRotateSensitivityChanging);
            m_RotationSensitivitySlider.RegisterValueChangedCallback(OnRotateSensitivityChanged);
            
            m_MoveSensitivitySlider.SetValueWithoutNotify(m_CameraInputSystemController.MoveSensitivity);
            m_RotationSensitivitySlider.SetValueWithoutNotify(m_CameraInputSystemController.RotateSensitivity);
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
    }
}