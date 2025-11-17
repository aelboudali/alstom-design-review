using System;
using Unity.AppUI.UI;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Streaming.Measurement
{
    [Serializable]
    public class MeasureLabel
    {
        private GameObject LabelParent => m_Document.transform.parent.gameObject;
        
        [SerializeField]
        UIDocument m_Document;
        
        [SerializeField]
        string m_BackgroundElementName = "line_measure_label";

        public Text m_MeasureLabelElement { get; private set; }

        VisualElement m_LabelBackground;

        public void Init()
        {
            m_MeasureLabelElement = m_Document.rootVisualElement.Q<Text>();

            m_LabelBackground = m_MeasureLabelElement.parent;
        }

        public void SetLabelPosition(Vector3 startPos, Vector3 endPos)
        {
            Vector3 midPoint = (startPos + endPos) * 0.5f;
            LabelParent.transform.position = midPoint;
        }

        public void SetLabelVisible(bool visible)
        {
            if (visible)
            {
                m_LabelBackground.DisplayOn();
            }
            else
            {
                m_LabelBackground.DisplayOff();
            }
        }

        public void SetColor(Color color)
        {
            m_LabelBackground.style.backgroundColor = color;
        }
    }
}
