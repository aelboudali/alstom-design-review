using System.Linq;
using Unity.AppUI.UI;
using Unity.Industry.Viewer.Streaming;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Industry.Viewer.AppSettings;
using Toggle = Unity.AppUI.UI.Toggle;

namespace Unity.Industry.Viewer.VR.FlyMode
{
    public class VRMovementUIController : NavigationOptionUI
    {
        private const string k_FlySensitivitySlider = "FlySensitivitySlider";
        private const string k_CollisionDetectionToggle = "CollisionDetectionToggle";

        [SerializeField]
        private XRControllerMenu m_XRControllerMenu;

        private XRRoundButton m_HomeButton;

        private SliderFloat m_FlySensitivitySlider;
        private Toggle m_CollisionDetectionToggle;

        [SerializeField]
        private VRMovementController m_VRFlyNavigationController;

        [SerializeField]
        private Texture2D m_OverheadIcon;

        private void OnEnable()
        {
            if (m_HomeButton == null)
            {
                m_XRControllerMenu ??= new XRControllerMenu();
                m_XRControllerMenu.Initialize();

                m_HomeButton = new XRRoundButton()
                {
                    IconTexture = m_OverheadIcon
                };

                m_HomeButton.clicked += OnHomeButtonClicked;
                m_XRControllerMenu.Insert(m_XRControllerMenu.Count, m_HomeButton);

            } else
            {
                m_HomeButton.style.display = DisplayStyle.Flex;
            }
            InAppSettings.SettingsPanelShown += SettingsPanelUp;
        }

        private void Start()
        {
            m_VRFlyNavigationController ??= GetComponent<VRMovementController>();
        }

        private void OnDisable()
        {
            if(m_HomeButton != null)
            {
                m_HomeButton.style.display = DisplayStyle.None;
            }
            InAppSettings.SettingsPanelShown -= SettingsPanelUp;
        }

        private void OnDestroy()
        {
            if(m_HomeButton != null)
            {
                m_HomeButton.clicked -= OnHomeButtonClicked;
                m_HomeButton.RemoveFromHierarchy();
            }
        }

        private void OnHomeButtonClicked()
        {
            m_VRFlyNavigationController.SetHomeView();
        }

        protected override void InitialUI(VisualElement panel)
        {
            m_FlySensitivitySlider = panel.Q<SliderFloat>(k_FlySensitivitySlider);
            m_FlySensitivitySlider.RegisterValueChangedCallback(OnFlySensitivitySliderValueChanged);
            m_FlySensitivitySlider.RegisterValueChangingCallback(OnFlySensitivitySliderValueChanging);
            m_FlySensitivitySlider.SetValueWithoutNotify(m_VRFlyNavigationController.MoveSensitivity);

            m_CollisionDetectionToggle = panel.Q<Toggle>(k_CollisionDetectionToggle);
            if (m_CollisionDetectionToggle != null)
            {
                m_CollisionDetectionToggle.RegisterValueChangedCallback(OnCollisionDetectionToggleValueChanged);
                m_CollisionDetectionToggle.SetValueWithoutNotify(m_VRFlyNavigationController.CollisionDetection);
            }
        }

        private void OnFlySensitivitySliderValueChanging(ChangingEvent<float> evt)
        {
            m_VRFlyNavigationController.UpdateMoveSensitivity(evt.newValue);
        }

        private void OnFlySensitivitySliderValueChanged(ChangeEvent<float> evt)
        {
            m_VRFlyNavigationController.UpdateMoveSensitivity(evt.newValue);
        }

        private void OnCollisionDetectionToggleValueChanged(ChangeEvent<bool> evt)
        {
            m_VRFlyNavigationController.UpdateCollisionDetection(evt.newValue);
        }

        public override void CreatePanel()
        {
            if (NavigationOptionUIAsset == null) return;
            var navigationOptionUIAsset = NavigationOptionUIAsset.Instantiate().Children().First();
            ToolPanelUIController.OpenToolPanel(m_VRFlyNavigationController.NavigationName, navigationOptionUIAsset, false);
            InitialUI(navigationOptionUIAsset);
        }
    }
}
