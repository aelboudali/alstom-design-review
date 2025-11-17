using Unity.Industry.Viewer.Vivox;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Vivox;
using Unity.AppUI.UI;

namespace Unity.Industry.Viewer.VR.Vivox
{
    public class XRVivoxUIController : VivoxUIController
    {
        private const string k_FirstNameLabelName = "First-Name-Label";
        
        [SerializeField]
        UIDocument m_UiDocument;
        
        protected override void InitializeUI()
        {
            if (!m_UiDocument.rootVisualElement.styleSheets.Contains(m_StyleSheet))
            {
                m_UiDocument.rootVisualElement.styleSheets.Add(m_StyleSheet);
            }
            
            var micButton = m_UiDocument.rootVisualElement.Q<MicComponent>();
            if (micButton != null)
            {
                micButton.clicked -= OnMicButtonClicked;
                micButton.RemoveFromHierarchy();
            }
            
            m_MicButton = new MicComponent(VivoxService.Instance.IsInputDeviceMuted);
            UpdateToolTips();
            m_MicButton.clicked += OnMicButtonClicked;
            var firstNameText = m_UiDocument.rootVisualElement.Q<Text>(k_FirstNameLabelName);
            firstNameText.parent.Insert(0, m_MicButton);
        }
    }
}
