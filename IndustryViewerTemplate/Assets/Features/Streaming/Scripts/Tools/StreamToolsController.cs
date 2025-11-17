using System;
using System.Collections;
using System.Linq;
using Unity.Industry.Viewer.Assets;
using UnityEngine;
using UnityEngine.SceneManagement;
using AssetInfo = Unity.Industry.Viewer.Assets.AssetInfo;

namespace Unity.Industry.Viewer.Streaming
{
    [DefaultExecutionOrder(100)]
    public class StreamToolsController : MonoBehaviour
    {
        public static Action<StreamingToolAsset> ToolSelected;
        public static Action<StreamingToolAsset[]> ToolInitializing;
        public static Action<StreamingToolAsset, bool> ToolActiveChanged;
        public static Action<bool> DisableAllTools;
        
        private (StreamingToolAsset toolAsset, GameObject toolInstance)? m_currentActiveTool;
        
        [SerializeField]
        private StreamingToolAsset[] streamingToolAssets;
        
        private StreamToolSubmenuController m_CurrentSubmenuController;
        private int m_subToolAssetInstanceId = -1;
        
        private Guid m_toolPanelUpdateGuid = Guid.NewGuid();
        
        private void Start()
        {
            ToolSelected += OnToolSelected;
            DisableAllTools += OnDisableAllTools;
            ToolInitializing?.Invoke(streamingToolAssets);
            StreamSceneController.ExitSceneConfirmed += OnExitSceneConfirmed;
            AssetsController.AssetSelected += OnAssetSelected;
            foreach (var tool in streamingToolAssets)
            {
                if (tool.toolPrefab.TryGetComponent(out StreamToolSubmenuController submenuController))
                {
                    foreach (var submenuControllerSubmenuToolAsset in submenuController.SubmenuToolAssets)
                    {
                        if (submenuControllerSubmenuToolAsset.sceneListener == null) continue;
                        var listener = Instantiate(submenuControllerSubmenuToolAsset.sceneListener);
                        SceneManager.MoveGameObjectToScene(listener, gameObject.scene);
                    } 
                }
                if(tool.sceneListener == null) continue;
                var otherListener = Instantiate(tool.sceneListener);
                SceneManager.MoveGameObjectToScene(otherListener, gameObject.scene);
            }
        }

        private void OnDestroy()
        {
            ToolSelected -= OnToolSelected;
            DisableAllTools -= OnDisableAllTools;
            AssetsController.AssetSelected -= OnAssetSelected;
            StreamSceneController.ExitSceneConfirmed -= OnExitSceneConfirmed;
        }

        private void OnDisableAllTools(bool includeSubmenus)
        {
            foreach (var tool in streamingToolAssets)
            {
                if (!includeSubmenus && m_CurrentSubmenuController != null && tool.GetInstanceID() == m_subToolAssetInstanceId)
                {
                    continue;
                }
                ToolActiveChanged?.Invoke(tool, false);
            }

            if (m_CurrentSubmenuController != null && !includeSubmenus)
            {
                return;
            }
            
            if (!m_currentActiveTool.HasValue) return;
            
            if (m_currentActiveTool.Value.toolAsset.GetInstanceID() == m_subToolAssetInstanceId &&
                m_CurrentSubmenuController != null)
            {
                Destroy(m_currentActiveTool.Value.toolInstance);
                m_CurrentSubmenuController = null;
                m_subToolAssetInstanceId = -1;
            }
            
            if(m_currentActiveTool.Value.toolInstance.TryGetComponent(out StreamToolControllerBase controller))
            {
                controller.OnToolClosed();
            }

            if (m_currentActiveTool.Value.toolInstance != null)
            {
                Destroy(m_currentActiveTool.Value.toolInstance);
            }
            
            m_currentActiveTool = null;
        }

        // Turn off all tools when asset is updated
        private void OnAssetSelected(AssetInfo assetInfo)
        {
            if (m_currentActiveTool.HasValue)
            {
                StreamToolsUIControllerBase.UpdateToolPanel?.Invoke(m_currentActiveTool.Value.toolAsset, null, false);
            }
            
            OnDisableAllTools(true);
        }

        private void OnToolSelected(StreamingToolAsset toolAsset)
        {
            var pass = streamingToolAssets.Any(tool => tool == toolAsset);

            if (!pass)
            {
                if (m_CurrentSubmenuController != null && m_CurrentSubmenuController.SubmenuToolAssets.Any(tool => tool == toolAsset))
                {
                    m_CurrentSubmenuController.SelectTool(toolAsset);
                }
                return;
            }

            if (m_currentActiveTool.HasValue)
            {
                if(m_currentActiveTool.Value.toolInstance.TryGetComponent(out StreamToolControllerBase controller))
                {
                    controller.OnToolClosed();
                }

                if (m_currentActiveTool.Value.toolInstance != null)
                {
                    Destroy(m_currentActiveTool.Value.toolInstance);
                }
                
                var currentTool = m_currentActiveTool.Value.toolAsset;
                ToolActiveChanged?.Invoke(m_currentActiveTool.Value.toolAsset, false);
                if (m_CurrentSubmenuController == null || m_CurrentSubmenuController != null &&
                    toolAsset.GetInstanceID() != m_subToolAssetInstanceId)
                {
                    StreamToolsUIControllerBase.UpdateToolPanel?.Invoke(m_currentActiveTool.Value.toolAsset, null, false);
                }
                
                if (currentTool.GetInstanceID() == m_subToolAssetInstanceId && toolAsset.GetInstanceID() != m_subToolAssetInstanceId)
                {
                    m_CurrentSubmenuController = null;
                    m_subToolAssetInstanceId = -1;
                    StartCoroutine(WaitForUIUpdateAndSelectAgain());
                    if (currentTool != toolAsset)
                    {
                        m_currentActiveTool = null;
                        return;
                    }
                }
                
                if (currentTool == toolAsset)
                {
                    m_currentActiveTool = null;
                    return;
                }
            }

            var toolInstance = Instantiate(toolAsset.toolPrefab);
            SceneManager.MoveGameObjectToScene(toolInstance, gameObject.scene);
            if (toolInstance.TryGetComponent(out m_CurrentSubmenuController))
            {
                m_subToolAssetInstanceId = toolAsset.GetInstanceID();
            }
            
            m_currentActiveTool = (toolAsset, toolInstance);
            
            ToolActiveChanged?.Invoke(toolAsset, true);
            
            m_toolPanelUpdateGuid = Guid.NewGuid(); // Generate new GUID for each selection
            var currentVersion = m_toolPanelUpdateGuid;
            
            StartCoroutine(UpdateToolPanel(currentVersion));

            IEnumerator UpdateToolPanel(Guid version)
            {
                yield return null; // Ensure the tool is fully initialized before invoking the controller
                if (version == m_toolPanelUpdateGuid)
                {
                    StreamToolsUIControllerBase.UpdateToolPanel?.Invoke(toolAsset, m_currentActiveTool.Value.toolInstance, true);
                }
            }

            IEnumerator WaitForUIUpdateAndSelectAgain()
            {
                yield return null;
                OnToolSelected(toolAsset);
            }
        }

        private void OnExitSceneConfirmed()
        {
            if (!m_currentActiveTool.HasValue) return;
            if(m_currentActiveTool.Value.toolInstance.TryGetComponent(out StreamToolControllerBase controller))
            {
                controller.OnToolClosed();
            }
            Destroy(m_currentActiveTool.Value.toolInstance);
            ToolActiveChanged?.Invoke(m_currentActiveTool.Value.toolAsset, false);
            StreamToolsUIControllerBase.UpdateToolPanel?.Invoke(m_currentActiveTool.Value.toolAsset, null, false);
        }
    }
}
