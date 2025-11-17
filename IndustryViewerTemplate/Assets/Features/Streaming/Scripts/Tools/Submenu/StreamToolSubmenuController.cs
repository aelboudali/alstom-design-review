using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Unity.Industry.Viewer.Streaming
{
    [DefaultExecutionOrder(100)]
    public class StreamToolSubmenuController : MonoBehaviour
    {
        public static Action<StreamingToolAsset[]> InitializeTools;
        
        public StreamingToolAsset[] SubmenuToolAssets => submenuToolAssets;
        
        [SerializeField]
        private StreamingToolAsset[] submenuToolAssets;
        
        private (StreamingToolAsset toolAsset, GameObject toolInstance)? m_currentActiveTool;
        
        private Guid m_toolPanelUpdateGuid = Guid.NewGuid();

        private void Start()
        {
            InitializeTools?.Invoke(submenuToolAssets);
            StreamToolsController.DisableAllTools += DisableAllTools;
        }

        private void DisableAllTools(bool removeSubmenu)
        {
            if (removeSubmenu || !m_currentActiveTool.HasValue) return;
            if(m_currentActiveTool.Value.toolInstance.TryGetComponent(out StreamToolControllerBase controller))
            {
                controller.OnToolClosed();
            }
            Destroy(m_currentActiveTool.Value.toolInstance);
            StreamToolsController.ToolActiveChanged?.Invoke(m_currentActiveTool.Value.toolAsset, false);
            m_currentActiveTool = null;
        }

        private void OnDestroy()
        {
            StreamToolsController.DisableAllTools -= DisableAllTools;
            if (m_currentActiveTool.HasValue)
            {
                CloseCurrentTool(out _);
            }
        }

        private void CloseCurrentTool(out StreamingToolAsset currentTool)
        {
            currentTool = m_currentActiveTool.Value.toolAsset;
            if(m_currentActiveTool.Value.toolInstance.TryGetComponent(out StreamToolControllerBase controller))
            {
                controller.OnToolClosed();
            }
            Destroy(m_currentActiveTool.Value.toolInstance);
            StreamToolsController.ToolActiveChanged?.Invoke(m_currentActiveTool.Value.toolAsset, false);
            StreamToolsUIControllerBase.UpdateToolPanel?.Invoke(m_currentActiveTool.Value.toolAsset, null, false);
        }

        public void SelectTool(StreamingToolAsset toolAsset)
        {
            var pass = submenuToolAssets.Any(tool => tool == toolAsset);
            if(!pass) return;
            
            if (m_currentActiveTool.HasValue)
            {
                CloseCurrentTool(out var currentTool);
                if (currentTool == toolAsset)
                {
                    m_currentActiveTool = null;
                    return;
                }
            }
            
            var toolInstance = Instantiate(toolAsset.toolPrefab);
            SceneManager.MoveGameObjectToScene(toolInstance, gameObject.scene);
            m_currentActiveTool = (toolAsset, toolInstance);
                
            StreamToolsController.ToolActiveChanged?.Invoke(toolAsset, true);
            
            m_toolPanelUpdateGuid = Guid.NewGuid(); // Generate new GUID for each selection
            var currentVersion = m_toolPanelUpdateGuid;
                
            StartCoroutine(UpdateToolPanel(currentVersion));
            return;
                
            IEnumerator UpdateToolPanel(Guid version)
            {
                yield return null; // Ensure the tool is fully initialized before invoking the controller
                if (version == m_toolPanelUpdateGuid)
                {
                    StreamToolsUIControllerBase.UpdateToolPanel?.Invoke(toolAsset, m_currentActiveTool.Value.toolInstance, true);
                }
            }
        }
    }
}
