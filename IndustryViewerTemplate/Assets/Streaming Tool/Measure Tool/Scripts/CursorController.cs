using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Unity.Industry.Viewer.Shared;

namespace Unity.Industry.Viewer.Streaming.Measurement
{
    public class CursorController : MonoBehaviour
    {
        private const string k_Cursor = "Cursor";
        
        private VisualElement m_Cursor;

        [SerializeField] private Color hoverColor;
        
        private Color m_DefaultColor;
        
        [SerializeField]
        private UIDocument m_CursorDocument;
        
        [SerializeField]
        private MeasurementToolUIController m_MeasurementToolUIController;
        
        void Awake()
        {
            if (m_CursorDocument == null) return;
            m_Cursor = m_CursorDocument.rootVisualElement.Q(k_Cursor);
            m_Cursor.RegisterCallback<ClickEvent>(OnClickCursor);
            m_Cursor.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            m_Cursor.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            m_Cursor.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        private void OnDestroy()
        {
            m_Cursor?.UnregisterCallback<ClickEvent>(OnClickCursor);
            m_Cursor?.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            m_Cursor?.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            m_Cursor?.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        }
        
        private void OnPointerEnter(PointerEnterEvent evt)
        {
            SetColor(hoverColor, false);
        }
        
        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            SetColor(m_DefaultColor, false);
        }
        
        private void OnPointerCancel(PointerCancelEvent evt)
        {
            SetColor(m_DefaultColor, false);
        }

        private void OnClickCursor(ClickEvent evt)
        { 
            m_MeasurementToolUIController?.PickCursor(this);
        }

        public void ShowCursor(bool visible)
        {
            if (visible)
            {
                m_Cursor.DisplayOn();
            }
            else
            {
                m_Cursor.DisplayOff();
            }
        }

        public bool IsCursorVisible()
        {
            return m_Cursor.IsDisplayOn();
        }
        
        public void SetColor(Color color, bool overrideDefault = true)
        {
            if (m_CursorDocument == null) return;
            if (overrideDefault)
            {
                m_DefaultColor = color;
            }
            var icon = m_CursorDocument.rootVisualElement.Children().First().Q<VisualElement>();
            icon.style.unityBackgroundImageTintColor = color;
        }
    }
}
