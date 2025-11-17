using Unity.Industry.Viewer.Streaming.AddModel;
using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;
using Unity.Industry.Viewer.Assets;

namespace Unity.Industry.Viewer.VR
{
    public class AddModelToolVRUIController : AddModelToolUIController
    {
        [SerializeField]
        private XRControllerMenu m_XRControllerMenu;
        [SerializeField]
        private Texture2D m_AddModelIcon;
        
        private XRRoundButton m_ToolButton;
        
        private StreamSceneVRUIController m_StreamVRUIController;
        
        private BoxCollider m_BoxCollider;

        protected override void InitializeToolButton()
        {
            m_ToolButton = new XRRoundButton
            {
                IconTexture = m_AddModelIcon,
            };

            m_ToolButton.clicked += FolderButtonOnClicked;
            m_XRControllerMenu ??= new XRControllerMenu();
            m_XRControllerMenu.Initialize();
            
            StartCoroutine(Wait());

            IEnumerator Wait()
            {
                // Wait for the end of the frame to ensure the XRControllerMenu is fully initialized
                yield return new WaitForEndOfFrame();
                m_XRControllerMenu.Insert(m_XRControllerMenu.Count, m_ToolButton);
            }
        }

        protected override void RemoveToolButton()
        {
            if (m_ToolButton != null)
            {
                m_ToolButton.clicked -= FolderButtonOnClicked;
                m_ToolButton.RemoveFromHierarchy();
                m_ToolButton = null;
            }
        }

        protected override void OnGeometryChanged(GeometryChangedEvent evt)
        {
            base.OnGeometryChanged(evt);
            m_BoxCollider ??= SharedUIManager.Instance.AssetsUIDocument.gameObject.GetComponent<BoxCollider>();
            m_BoxCollider.enabled = (evt.target as VisualElement).style.display == DisplayStyle.Flex;
            if (m_ToolButton != null)
            {
                m_ToolButton.primary = (evt.target as VisualElement).style.display == DisplayStyle.Flex;
            }
        }

        protected override void InitializeUI()
        {
            base.InitializeUI();
            m_StreamVRUIController ??= FindFirstObjectByType<StreamSceneVRUIController>(FindObjectsInactive.Include);
            m_StreamVRUIController.BehaviourEnabled(true);
        }

        protected override void UninitializeUI()
        {
            base.UninitializeUI();
            if (m_ToolButton != null)
            {
                m_ToolButton.primary = false;
            }
            m_StreamVRUIController ??= FindFirstObjectByType<StreamSceneVRUIController>(FindObjectsInactive.Include);
            m_StreamVRUIController.BehaviourEnabled(false);
        }
    }
}
