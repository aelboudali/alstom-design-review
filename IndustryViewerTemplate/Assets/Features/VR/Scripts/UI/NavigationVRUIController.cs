using System;
using UnityEngine;
using Unity.Industry.Viewer.Streaming;
using Unity.Industry.Viewer.Shared;
using UnityEngine.UIElements;
using UnityEngine.XR.Hands;

namespace Unity.Industry.Viewer.VR
{
    public class NavigationVRUIController : MonoBehaviour
    {
        private const string k_ContainerName = "Container";
        
        [SerializeField]
        private NavigationController m_NavigationController;
        
        [SerializeField]
        private GameObject m_NavigationOptionsParent;
        
        private UIDocument m_navigationButtonUIDocument;
        
        [SerializeField]
        private XRControllerMenu m_XRControllerMenu;
        
        private XRRoundButton m_NavigationButton;
        
        private bool m_NavigationOptionsEnabled;
        
        private VisualElement m_NavigationOptionsContainer;

        private void Awake()
        {
            m_NavigationController ??= GetComponent<NavigationController>();
        }
        
        private void Start()
        {
            NavigationController.OnNavigationOptionChanged += OnNavigationOptionChanged;
            InitializeUI();
        }
        private void LateUpdate()
        {
            if(!m_NavigationOptionsEnabled || m_XRControllerMenu.MenuDocument == null) return;
            Vector3 localPosition = m_NavigationButton.worldBound.center;
            m_NavigationOptionsParent.gameObject.transform.SetPositionAndRotation(
                m_XRControllerMenu.MenuDocument.transform.TransformPoint(localPosition), 
                m_XRControllerMenu.MenuDocument.transform.rotation);
        }

        private void OnDestroy()
        {
            XRControllerMenuBase.MenuButtonClicked -= OnMenuButtonClicked;
            m_NavigationButton.clicked -= OnNavigationButtonClicked;
            m_NavigationOptionsContainer?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            NavigationController.OnNavigationOptionChanged -= OnNavigationOptionChanged;
            m_NavigationButton.clickable.longClicked -= OnNavigationLongPress;
            
            var optionButtons = m_NavigationOptionsContainer?.Query<XRRoundButton>().ToList();
            if (optionButtons != null)
            {
                foreach (var optionButton in optionButtons)
                {
                    optionButton.UnregisterCallback<ClickEvent>(Callback);
                }
            }
            m_NavigationOptionsContainer?.Clear();
        }
        
        private void OnNavigationOptionChanged(NavigationOption newNavigation)
        {
            m_NavigationButton.IconTexture = newNavigation.NavigationOptionUIComponent.NavigationIcon;
        }

        private void InitializeUI()
        {
            m_XRControllerMenu ??= new XRControllerMenu();
            XRControllerMenuBase.MenuButtonClicked += OnMenuButtonClicked;
            m_XRControllerMenu.Initialize();
            m_NavigationButton = new XRRoundButton
            {
                IconTexture = m_NavigationController.DefaultNavigationOption.NavigationOptionUIComponent.NavigationIcon
            };
            m_XRControllerMenu.Add(m_NavigationButton);
            
            m_NavigationButton.userData = m_NavigationController.DefaultNavigationOption;
            m_NavigationButton.tooltip =
                m_NavigationController.DefaultNavigationOption.NavigationName.GetTitleLocalizedStringForAppUI();
            m_NavigationButton.clicked += OnNavigationButtonClicked;
            m_NavigationButton.clickable.longPressDuration = 1500; // 1500 ms
            m_NavigationButton.clickable.longClicked += OnNavigationLongPress;
            m_NavigationOptionsContainer = m_NavigationOptionsParent.GetComponentInChildren<UIDocument>().rootVisualElement.Q<VisualElement>(k_ContainerName);
            m_NavigationOptionsContainer.style.display = DisplayStyle.None;
            m_NavigationOptionsContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnMenuButtonClicked(Handedness hand, bool isOn)
        {
            if (hand != m_XRControllerMenu.Side) return;
            if(isOn) return;
            if (m_NavigationOptionsEnabled)
            {
                OnNavigationButtonClicked();
            }
        }

        private void OnNavigationLongPress()
        {
            NavigationController.CurrentNavigationOption.NavigationOptionUIComponent.CreatePanel();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var container = evt.target as VisualElement;
            if (container == null) return;
            if (container.resolvedStyle.display != DisplayStyle.Flex) return;

            if (container.childCount > 0)
            {
                EnableNavigationOptions();
                return;
            }

            foreach (var option in m_NavigationController.NavigationOptions)
            {
                var newNavigationOption = new XRRoundButton
                {
                    IconTexture = option.NavigationOptionUIComponent.NavigationIcon,
                    style =
                    {
                        marginRight = new Length(5f, LengthUnit.Pixel),
                        marginLeft = new Length(5f, LengthUnit.Pixel)
                    },
                    userData = option
                };
                newNavigationOption.RegisterCallback<ClickEvent>(Callback);
                container.Add(newNavigationOption);
            }
        }

        private void Callback(ClickEvent evt)
        {
            var button = evt.target as XRRoundButton;
            if (button == null) return;
            var option = button.userData as NavigationOption;
            if (option == null) return;
            NavigationController.ChangeToNewNavigationOption?.Invoke(option);
            OnNavigationButtonClicked();
        }

        private void EnableNavigationOptions()
        {
            var currentOption = NavigationController.CurrentNavigationOption;
            foreach (var optionButton in m_NavigationOptionsContainer.Query<XRRoundButton>().ToList())
            {
                NavigationOption option = (NavigationOption)optionButton.userData;
                if (currentOption == option)
                {
                    optionButton.style.display = DisplayStyle.None;
                }
                else
                {
                    optionButton.style.display = option.IsSupported() ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void OnNavigationButtonClicked()
        {
            m_NavigationOptionsEnabled = !m_NavigationOptionsEnabled;
            m_NavigationOptionsContainer.style.display = m_NavigationOptionsEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
