using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using Unity.Industry.Viewer.Assets;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Streaming.Environment
{
    public class EnvironmentToolUIController : StreamToolUIBase
    {
        private const string k_EnvironmentsListViewName = "EnvironmentsListView";
        private const string k_EnvironmentListItemPreviewName = "EnvironmentPreviewElement";
        private const string k_EnvironmentListItemCaptionName = "EnvironmentNameText";
        private const string k_UnselectedClass = "EnvironmentUnselected";
        private const string k_SelectedClass = "EnvironmentSelected";
        
        
        private ListView m_EnvironmentsListView;
        
        [SerializeField]
        private StyleSheet m_StyleSheet;

        private EnvironmentsSettingsAsset m_Settings =>
            m_EnvironmentToolSceneListener != null ? m_EnvironmentToolSceneListener.Settings: null;

        private EnvironmentToolSceneListener m_EnvironmentToolSceneListener;

        private void Start()
        {
            m_EnvironmentToolSceneListener = FindFirstObjectByType<EnvironmentToolSceneListener>();
            if (m_Settings == null)
            {
                Debug.LogError("EnvironmentsSettingsAsset is not set in EnvironmentToolUIController.");
                return;
            }
        }

        private void OnDestroy()
        {
            UninitializeUI();
        }

        public override void InitializeUI(UIDocument uiDocument, VisualElement parent, GameObject controller)
        {
            m_PanelDocument = uiDocument;
            if (!m_PanelDocument.rootVisualElement.styleSheets.Contains(m_StyleSheet))
            {
                m_PanelDocument.rootVisualElement.styleSheets.Add(m_StyleSheet);
            }
            m_EnvironmentsListView = parent.Q<ListView>(k_EnvironmentsListViewName);
            m_EnvironmentsListView.bindItem = BindListItem;
            m_EnvironmentsListView.itemsSource = m_Settings.Scenes;
            SetSelectedIndex();
            m_EnvironmentsListView.selectedIndicesChanged += OnSelectedIndicesChanged;
        }

        private void SetSelectedIndex()
        {
            m_EnvironmentsListView.selectedIndex = Array.FindIndex(m_Settings.Scenes, scene => scene.id == EnvironmentToolController.CurrentEnvironmentSettings.id);
        }

        private void BindListItem(VisualElement item, int index)
        {
            var environmentSettings = m_Settings.Scenes[index];
            if (environmentSettings == null)
            {
                Debug.LogError($"Environment settings at index {index} is null.");
                return;
            }
            
            bool isSelected = EnvironmentToolController.CurrentEnvironmentSettings.id == environmentSettings.id;
            
            var background = item.Q<VisualElement>("Background");
            SetSelectedVE(background, isSelected);
            
            background.parent.name = environmentSettings.id;

            if (environmentSettings.Thumbnail != null)
            {
                var itemImage = item.Q<VisualElement>(k_EnvironmentListItemPreviewName);
                if (itemImage != null)
                {
                    itemImage.style.backgroundImage = new StyleBackground(environmentSettings.Thumbnail);
                }
                else
                {
                    Debug.LogError($"Image visual element '{k_EnvironmentListItemPreviewName}' not found in list item template.");
                }
            }

            var itemCaption = item.Q<Text>(k_EnvironmentListItemCaptionName);
            if (itemCaption != null)
            {
                itemCaption.text = environmentSettings.DisplayName.GetTitleLocalizedStringForAppUI();
            }
            else
            {
                Debug.LogError($"Text element '{k_EnvironmentListItemCaptionName}' not found in list item template.");
            }
        }

        private void SetSelectedVE(VisualElement item, bool selected)
        {
            if (selected)
            {
                if (item.ClassListContains(k_UnselectedClass))
                {
                    item.RemoveFromClassList(k_UnselectedClass);
                }
                item.AddToClassList(k_SelectedClass);
            }
            else
            {
                if (item.ClassListContains(k_SelectedClass))
                {
                    item.RemoveFromClassList(k_SelectedClass);
                }
                item.AddToClassList(k_UnselectedClass);
            }
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            if (m_Settings == null || m_Settings.Scenes == null || !indices.Any()) return;
            EnvironmentToolController.SetEnvironment?.Invoke(m_Settings.Scenes[indices.First()].id);
            
            SetSelectedIndex();
            
            foreach (var visualElement in m_EnvironmentsListView.Query<VisualElement>("Background").ToList())
            {
                visualElement.ClearClassList();
                if (visualElement.parent.name == EnvironmentToolController.CurrentEnvironmentSettings.id)
                {
                    SetSelectedVE(visualElement, true);
                } 
                else
                {
                    SetSelectedVE(visualElement, false);
                }
            }
        }

        public override void UninitializeUI()
        {
            if (SharedUIManager.Instance.AssetsUIDocument.rootVisualElement.styleSheets.Contains(m_StyleSheet))
            {
                SharedUIManager.Instance.AssetsUIDocument.rootVisualElement.styleSheets.Remove(m_StyleSheet);
            }
            if (m_EnvironmentsListView != null)
            {
                m_EnvironmentsListView.selectedIndicesChanged -= OnSelectedIndicesChanged;
                m_EnvironmentsListView.ClearBindings();
            }
        }
    }
}
