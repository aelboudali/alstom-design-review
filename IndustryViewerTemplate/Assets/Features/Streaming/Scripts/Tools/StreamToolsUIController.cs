using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.AppUI.UI;
using Unity.Industry.Viewer.Assets;
using Unity.Industry.Viewer.Shared;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Streaming
{
    [DefaultExecutionOrder(-100)]
    public class StreamToolsUIController : StreamToolsUIControllerBase
    {
        private const string k_MainToolIconClassName = "MainToolIcon";
        
        private const string k_ToolScrollListName = "ToolScrollList";
        protected const string k_SubToolScrollListName = "SubToolScrollList";
        
        protected UIDocument m_UIDocument;
        private ScrollView m_ToolScrollList;
        protected ScrollView m_SubToolScrollList;
        
        // Start is called before the first frame update
        void Start()
        {
            StreamToolsController.ToolInitializing += OnToolInitializing;
            StreamToolsController.ToolActiveChanged += OnToolActiveChanged;
            ToolPanelUIController.CloseToolPanel += CloseToolPanel;
            UpdateToolPanel += OnUpdateToolPanel;
            
            InitializeUI();
        }

        private void OnDestroy()
        {
            StreamToolsController.ToolInitializing -= OnToolInitializing;
            StreamToolsController.ToolActiveChanged -= OnToolActiveChanged;
            ToolPanelUIController.CloseToolPanel -= CloseToolPanel;
            UpdateToolPanel -= OnUpdateToolPanel;
            m_toolButtons?.Clear();
            m_ToolScrollList?.Clear();
            m_SubToolScrollList?.Clear();
        }

        private void CloseToolPanel()
        {
            StreamToolsController.DisableAllTools?.Invoke(false);
        }

        private void OnUpdateToolPanel(StreamingToolAsset toolAsset, GameObject controller, bool active)
        {
            if (active)
            {
                //Add tool to panel
                if(controller.TryGetComponent(out StreamToolUIBase toolUI))
                {
                    if(controller.TryGetComponent(out StreamToolControllerBase toolController))
                    {
                        toolController.OnToolOpened();
                    }

                    VisualElement toolPanel = null;
                    if (toolUI.ToolUIAsset != null)
                    {
                        toolPanel = toolUI.ToolUIAsset.Instantiate().Children().First();
                        toolPanel.userData = controller;
                    }
                    
                    toolUI.InitializeUI(m_UIDocument, toolPanel, controller);
                    if (toolPanel != null)
                    {
                        ToolPanelUIController.OpenToolPanel?.Invoke(toolAsset.ToolName, toolPanel, toolAsset.resizablePanel);
                    }
                }
            }
            else
            {
                //Remove tool from panel
                ToolPanelUIController.CloseToolPanel?.Invoke();
            }
        }

        protected virtual void InitializeUI()
        {
            m_UIDocument = SharedUIManager.Instance.AssetsUIDocument;
            m_ToolScrollList = m_UIDocument.rootVisualElement.Q<ScrollView>(k_ToolScrollListName);
            
            m_SubToolScrollList = m_UIDocument.rootVisualElement.Q<ScrollView>(k_SubToolScrollListName);
            m_SubToolScrollList.style.display = DisplayStyle.None;
        }

        protected void OnToolActiveChanged(StreamingToolAsset toolAsset, bool active)
        {
            if(m_toolButtons == null || !m_toolButtons.TryGetValue(toolAsset, out var button)) return;
            ActionButton actionButton = button as ActionButton;
            actionButton.accent = active;
            actionButton.selected = active;
        }

        private void OnToolInitializing(StreamingToolAsset[] tools)
        {
            DistributeToolsIcons(tools, k_MainToolIconClassName, m_ToolScrollList, ref m_toolButtons);
        }

        protected void DistributeToolsIcons(StreamingToolAsset[] tools, string styleClassName, ScrollView scrollView,
            ref Dictionary<StreamingToolAsset, IPressable> toolButtonDict)
        {
            if (tools == null || tools.Length == 0)
            {
                scrollView.style.display = DisplayStyle.None;
                return;
            }
            scrollView.style.display = DisplayStyle.Flex;
            scrollView.contentContainer.style.alignSelf = Align.Center;
            foreach (var toolAsset in tools)
            {
                var newButton = new ActionButton
                {
                    quiet = true
                };
                if (!string.IsNullOrEmpty(styleClassName))
                {
                    newButton.AddToClassList(styleClassName);
                }
                
                Icon icon = newButton.Q<Icon>("appui-actionbutton__icon");
                icon.image = toolAsset.toolIcon;
                var newButtonData = new StreamToolData(toolAsset);
                newButton.userData = newButtonData;
                newButton.tooltip = toolAsset.ToolName.GetTitleLocalizedStringForAppUI();

                newButton.clicked += newButtonData.OnButtonPress;
                toolButtonDict ??= new Dictionary<StreamingToolAsset, IPressable>();
                toolButtonDict.Add(toolAsset, newButton);
                scrollView.Add(newButton);
            }
        }
    }
}
