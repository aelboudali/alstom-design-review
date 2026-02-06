using Unity.AppUI.UI;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Streaming
{
    [DefaultExecutionOrder(101)]
    public abstract class NavigationOptionUI : MonoBehaviour
    {
        [SerializeField]
        private Texture2D navigationIcon;
        public Texture2D NavigationIcon => navigationIcon;

        [SerializeField]
        private LocalizedString navigationName;
        public LocalizedString NavigationName => navigationName;

        [SerializeField]
        protected VisualTreeAsset navigationOptionUIAsset;
        public VisualTreeAsset NavigationOptionUIAsset => navigationOptionUIAsset;

        protected VisualElement m_SettingsPanel;
        protected VisualElement m_Title;

        protected abstract void InitialUI(VisualElement panel);

        public virtual void CreatePanel()
        {
            // Do nothing by default
        }

        protected virtual async void ChangeCameraTitle(VisualTreeAsset titleTemplate)
        {
            var titleText = m_Title.Q<Text>("Title");
            titleText.text = await navigationName.GetTitleLocalizedStringForAppUIAsync();
        }

        protected void SettingsPanelUp(VisualElement settingsWindow, VisualTreeAsset titleTemplate)
        {
            m_SettingsPanel = settingsWindow;
            m_Title = titleTemplate.Instantiate();
            ChangeCameraTitle(titleTemplate);
            // Insert the title as the element after the General Settings in it's Scroll View
            m_SettingsPanel.Q<ScrollView>().Insert(1, m_Title);
            var m_CameraSettings = navigationOptionUIAsset.Instantiate();
            m_Title.Q<VisualElement>("Content").Add(m_CameraSettings);
            InitialUI(m_CameraSettings);
        }
    }
}
