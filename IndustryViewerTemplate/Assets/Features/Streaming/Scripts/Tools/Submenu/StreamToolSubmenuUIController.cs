using UnityEngine;
#if !VR_MODE
using System;
using UnityEngine.UIElements;
using Unity.Industry.Viewer.Assets;
#endif

namespace Unity.Industry.Viewer.Streaming
{
    [DefaultExecutionOrder(-100)]
    public class StreamToolSubmenuUIController : StreamToolsUIController
    {
#if !VR_MODE
        private void Start()
        {
            InitializeUI();
            StreamToolSubmenuController.InitializeTools += OnInitializeTools;
            StreamToolsController.ToolActiveChanged += OnToolActiveChanged;
        }

        private void OnDestroy()
        {
            m_SubToolScrollList.Clear();
            m_SubToolScrollList.style.display = DisplayStyle.None;
            m_toolButtons?.Clear();
            StreamToolSubmenuController.InitializeTools -= OnInitializeTools;
            StreamToolsController.ToolActiveChanged -= OnToolActiveChanged;
        }

        
        protected override void InitializeUI()
        {
            m_UIDocument = SharedUIManager.Instance.AssetsUIDocument;
            m_SubToolScrollList = m_UIDocument.rootVisualElement.Q<ScrollView>(k_SubToolScrollListName);
            m_SubToolScrollList.style.display = DisplayStyle.None;

        }
        
        private void OnInitializeTools(StreamingToolAsset[] toolAssets)
        {
            DistributeToolsIcons(toolAssets, "SubToolIcon", m_SubToolScrollList, ref m_toolButtons);
        }
#endif
    }
}
