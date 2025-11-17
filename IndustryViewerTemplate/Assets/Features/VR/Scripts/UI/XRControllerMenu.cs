using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Hands;

namespace Unity.Industry.Viewer.VR
{
    [Serializable]
    public class XRControllerMenu: IXRControllerMenu
    {
        public static event Action<Handedness> ContainerElementChanged;
        
        public Handedness Side => m_Side;
        public IXRControllerMenu.MenuPosition Position => m_Position;
        
        [SerializeField]
        private Handedness m_Side;
        [SerializeField]
        private IXRControllerMenu.MenuPosition m_Position;

        public bool IsInitialized => m_ToolListContainer != null;
        
        public int Count => m_ToolListContainer?.childCount ?? 0;
        
        private VisualElement m_ToolListContainer;
        
        public UIDocument MenuDocument { get; private set; }

        public void Initialize()
        {
            var XRControllerManeuBasees = UnityEngine.Object.FindObjectsByType<XRControllerMenuBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if(XRControllerManeuBasees == null || XRControllerManeuBasees.Length == 0)
            {
                Debug.LogWarning("XRControllerMenu: No XRControllerMenuBase found in the scene.");
                return;
            }
            
            var XRControllerMenuBase = XRControllerManeuBasees.First(x => x.Side == m_Side);
            if (XRControllerMenuBase == null)
            {
                return;
            }
            MenuDocument = XRControllerMenuBase.GetComponentInChildren<UIDocument>();
            if (MenuDocument == null)
            {
                return;
            }
            var containerName = Position == IXRControllerMenu.MenuPosition.Bottom ? "BottomContainer" : "TopContainer";
            m_ToolListContainer = MenuDocument.rootVisualElement.Q<VisualElement>(containerName);
        }

        public void Add(VisualElement element)
        {
            if (m_ToolListContainer == null)
            {
                Debug.LogWarning("XRControllerMenu: ToolListContainer is not initialized.");
                return;
            }
            m_ToolListContainer.Add(element);
            ContainerElementChanged?.Invoke(Side);
            element.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public void Insert(int index, VisualElement element)
        {
            if (m_ToolListContainer == null)
            {
                return;
            }
            if (index < 0)
            {
                return;
            }
            m_ToolListContainer.Insert(index, element);
            ContainerElementChanged?.Invoke(Side);
            element.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var element = evt.target as VisualElement;
            if (element == null) return;
            element.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            if (m_ToolListContainer == null) return;
            ContainerElementChanged?.Invoke(Side);
        }
    }
}
