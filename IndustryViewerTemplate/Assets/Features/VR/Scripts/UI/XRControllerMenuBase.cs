using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Hands;

namespace Unity.Industry.Viewer.VR
{
    [DefaultExecutionOrder(int.MinValue)]
    public class XRControllerMenuBase : MonoBehaviour
    {
        public static event Action<Handedness, bool> MenuButtonClicked;
        
        public Handedness Side => m_Side;
        
        [SerializeField] private Handedness m_Side;
        
        private const string k_MenuButtonName = "MenuTrigger";
        private const string k_MenuContainerName = "MenuPanel";
        private const string k_BottomContainerName = "BottomContainer";
        private const string k_TopContainerName = "TopContainer";
        private const string k_RootPanelName = "root-panel";
        
        [SerializeField]
        private Texture iconTexture;

        UIDocument uiDocument;
        
        private XRRoundButton m_MenuButton;
        private VisualElement m_MenuContainer, m_BottomContainer, m_TopContainer, m_RootPanel;
        private XRPokeFilter m_PokeFilter;
        private BoxCollider m_BoxCollider;
        
        private bool m_ContainButtons => m_BottomContainer.childCount > 0 || m_TopContainer.childCount > 0;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            if (uiDocument == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            MenuButtonClicked += OnMenuButtonIsBeingClicked;
            XRControllerMenu.ContainerElementChanged += XRControllerMenuOnContainerElementChanged;
            
            m_RootPanel = uiDocument.rootVisualElement.Q<VisualElement>(k_RootPanelName);
            m_MenuButton = uiDocument.rootVisualElement.Q<XRRoundButton>(k_MenuButtonName);
            m_MenuContainer = uiDocument.rootVisualElement.Q<VisualElement>(k_MenuContainerName);
            m_BottomContainer = uiDocument.rootVisualElement.Q<VisualElement>(k_BottomContainerName);
            m_TopContainer = uiDocument.rootVisualElement.Q<VisualElement>(k_TopContainerName);
            
            if (iconTexture != null)
            {
                m_MenuButton.IconTexture = iconTexture;
            }
            
            m_MenuContainer.style.display = DisplayStyle.None;
            m_MenuButton.clicked += OnMenuButtonClicked;
            m_PokeFilter = GetComponent<XRPokeFilter>();
        }

        private void OnDestroy()
        {
            if(uiDocument == null) return;
            XRControllerMenu.ContainerElementChanged -= XRControllerMenuOnContainerElementChanged;
            m_MenuButton.clicked -= OnMenuButtonClicked;
            MenuButtonClicked -= OnMenuButtonIsBeingClicked;
        }

        private void OnMenuButtonIsBeingClicked(Handedness side, bool isOn)
        {
            if (isOn)
            {
                if (side == m_Side) return;
                m_RootPanel.style.display = DisplayStyle.None;
                m_BoxCollider ??= GetComponent<BoxCollider>();
                if (m_BoxCollider != null)
                {
                    m_BoxCollider.enabled = false;
                }
                m_PokeFilter.enabled = false;
            }
            else
            {
                if (side == m_Side) return;
                m_RootPanel.style.display = DisplayStyle.Flex;
                m_BoxCollider ??= GetComponent<BoxCollider>();
                if (m_BoxCollider != null)
                {
                    m_BoxCollider.enabled = true;
                }
                m_PokeFilter.enabled = true;
            }
        }

        private void XRControllerMenuOnContainerElementChanged(Handedness obj)
        {
            if (obj != m_Side) return;
            if(m_MenuButton == null) return;
            m_MenuButton.style.display = m_ContainButtons? DisplayStyle.Flex : DisplayStyle.None;
            if (m_MenuButton.style.display == DisplayStyle.None)
            {
                m_MenuContainer.style.display = DisplayStyle.None;
            }
        }

        private void OnMenuButtonClicked()
        {
            if(!m_ContainButtons) return;
            m_MenuButton.primary = !m_MenuButton.primary;
            m_MenuContainer.style.display = m_MenuButton.primary ? DisplayStyle.Flex : DisplayStyle.None;
            MenuButtonClicked?.Invoke(Side, m_MenuButton.primary);
        }
    }
}
