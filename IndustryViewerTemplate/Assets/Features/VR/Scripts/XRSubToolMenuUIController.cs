using System;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using Unity.Industry.Viewer.Streaming;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine.XR.Hands;

namespace Unity.Industry.Viewer.VR
{
    [DefaultExecutionOrder(-100)]
    public class XRSubToolMenuUIController : StreamToolsUIControllerBase
    {
        private const string k_XRSubToolIconClassName = "XRSubToolButton";
        
        [SerializeField]
        private string m_SubToolMenuName = "SubToolMenu";
        private VisualElement m_SubToolMenu;
        
#if VR_MODE
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            InitializeUI();
            StreamToolSubmenuController.InitializeTools += OnInitializeTools;
            XRControllerMenuBase.MenuButtonClicked += OnMenuButtonIsBeingClicked;
        }

        private void Start()
        {
            StreamToolsController.ToolActiveChanged += OnToolActiveChanged;
        }

        private void OnDestroy()
        {
            XRControllerMenuBase.MenuButtonClicked -= OnMenuButtonIsBeingClicked;
            StreamToolSubmenuController.InitializeTools -= OnInitializeTools;
            StreamToolsController.ToolActiveChanged -= OnToolActiveChanged;
            if (m_SubToolMenu != null)
            {
                m_SubToolMenu.Clear();
                m_SubToolMenu.parent.DisplayOff();
                m_SubToolMenu.DisplayOff();
            }
        }

        private void OnMenuButtonIsBeingClicked(Handedness hand, bool isOn)
        {
            if(hand != Handedness.Left) return;
            if (isOn)
            {
                m_SubToolMenu.DisplayOn();
            }
            else
            {
                m_SubToolMenu.DisplayOff();
            }
        }

        private void OnToolActiveChanged(StreamingToolAsset toolAsset, bool active)
        {
            if(m_toolButtons == null || !m_toolButtons.TryGetValue(toolAsset, out var button)) return;
            var xrButton = button as XRRoundButton;
            xrButton.primary = active;
        }

        private void OnInitializeTools(StreamingToolAsset[] tools)
        {
            if (tools == null || tools.Length == 0 || m_SubToolMenu == null)
            {
                return;
            }
            for (var i = 0; i < tools.Length; i++)
            {
                var newButton = new XRRoundButton
                {
                    primary = false
                };
                if (!string.IsNullOrEmpty(k_XRSubToolIconClassName))
                {
                    newButton.AddToClassList(k_XRSubToolIconClassName);
                }
                
                newButton.IconTexture = tools[i].toolIcon;

                var newButtonData = new StreamToolData(tools[i]);
                newButton.userData = newButtonData;
                newButton.tooltip = tools[i].ToolName.GetTitleLocalizedStringForAppUI();

                newButton.clicked += newButtonData.OnButtonPress;
                m_toolButtons ??= new Dictionary<StreamingToolAsset, IPressable>();
                m_toolButtons.Add(tools[i], newButton);
                m_SubToolMenu.Add(newButton);
                m_SubToolMenu.DisplayOn();
            }
        }

        private void InitializeUI()
        {
            var gameObjectUI = GameObject.Find(m_SubToolMenuName);
            if(gameObjectUI == null) return;
            var uiDocument = gameObjectUI.GetComponent<UIDocument>();
            if (uiDocument == null) return;
            m_SubToolMenu = uiDocument.rootVisualElement.Q<VisualElement>("MenuPanel");
            m_SubToolMenu.parent.DisplayOn();
            m_SubToolMenu.DisplayOff();
        }
#endif
    }
}
