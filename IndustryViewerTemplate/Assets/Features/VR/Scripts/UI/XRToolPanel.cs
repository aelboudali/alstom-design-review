using UnityEngine;
using Unity.Industry.Viewer.Streaming;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using UnityEngine.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Unity.Industry.Viewer.VR
{
    public class XRToolPanel : ToolPanelUIController
    {
        private HashSet<MonoBehaviour> m_MonoBehavioursToDisable;
        private Renderer m_GrabberRenderer;
        private Collider m_GrabberCollider;
        [SerializeField]
        private float m_SpawnDistance = 10f;
        private BoxCollider m_InteractionCollider;
        public UIDocument UIDocument => m_UIDocument;
        
        protected override void InitializeUI()
        {
            if(m_UIDocument == null) return;
            
            m_GrabberRenderer = m_UIDocument.transform.parent.GetComponentInParent<Renderer>();
            m_GrabberCollider = m_UIDocument.transform.parent.GetComponent<Collider>();
            m_MonoBehavioursToDisable ??= new HashSet<MonoBehaviour>();
            m_MonoBehavioursToDisable = m_UIDocument.transform.parent.GetComponents<MonoBehaviour>().Where(x => x != this).ToHashSet();
            GrabEnable(false);
            m_ToolPanelRoot = m_UIDocument.rootVisualElement.Q<VisualElement>(k_ToolPanelName);
            m_ToolPanelRoot.style.display = DisplayStyle.None;
            
            m_CloseToolPanelButton = m_ToolPanelRoot.Q<IconButton>(k_ToolCloseButtonName);
            m_CloseToolPanelButton.clickable.clicked += OnCloseToolPanelButtonClicked;
            m_ToolPanelTitle = m_ToolPanelRoot.Q<Text>(k_ToolTitleName);
            m_ContentPanel = m_ToolPanelRoot.Q<VisualElement>(k_ToolContentName);
            StartCoroutine(WaitForBoxCollider());
            return;

            IEnumerator WaitForBoxCollider()
            {
                do
                {
                    m_InteractionCollider = m_UIDocument.GetComponent<BoxCollider>();
                    yield return null;
                } while (m_InteractionCollider == null);
                m_InteractionCollider.enabled = false;
            }
        }

        private void GrabEnable(bool value)
        {
            foreach (var behaviour in m_MonoBehavioursToDisable)
            {
                behaviour.enabled = value;
            }
            
            if (m_GrabberRenderer != null)
            {
                m_GrabberRenderer.enabled = value;
            }

            if (m_GrabberCollider != null)
            {
                m_GrabberCollider.enabled = value;
            }
        }

        protected override void OnOpenToolPanel(LocalizedString title, VisualElement content, bool resizable)
        {
            IsOpened = true;
            GrabEnable(true);
            AddContentToPanel(title, content);
            m_InteractionCollider = m_UIDocument.GetComponent<BoxCollider>();
            m_InteractionCollider.enabled = true;
            
            Transform cam = Camera.main.transform;

            // Get the camera's forward direction and flatten it on the horizontal plane
            Vector3 forward = cam.forward;
            forward.y = 0;
            forward.Normalize();

            // Calculate the new position in front of the camera
            Vector3 newPosition = cam.position + forward * m_SpawnDistance;

            // Set the height to be relative to the camera's height plus an offset
            newPosition.y = cam.position.y;

            // Apply the new position
            transform.position = newPosition;

            // Make the UI panel face the camera
            transform.LookAt(cam.position);
            // Depending on your panel's geometry, you may need to reverse the forward vector
            transform.forward = -transform.forward;
        }

        protected override void ContentReset()
        {
            IsOpened = false;
            m_ToolPanelContent?.RemoveFromHierarchy();
            m_ToolPanelContent = null;
            if (m_InteractionCollider != null)
            {
                m_InteractionCollider.enabled = false;
            }
            GrabEnable(false);
        }
    }
}