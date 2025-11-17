using System;
using Unity.Industry.Viewer.Assets;
using UnityEngine;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Unity.Industry.Viewer.VR
{
    public class XRLoadingUIPanel: LoadingUIPanel
    {
        [SerializeField]
        XRUIToolkitManager m_XRUIToolkitManager;
        
        private XRPanel.CustomXRPanel m_loadingPanel;
        
        protected override void Start()
        {
            base.Start();
            m_XRUIToolkitManager ??= FindAnyObjectByType<XRUIToolkitManager>();
        }

        protected override void OnShowLoadingPanel(Action callback)
        {
            IsLoadingPanelVisible = true;
            m_loadingPanel?.Dismiss();
            var process = new CircularProgress
            {
                size = Size.L
            };
            m_loadingPanel = new XRPanel.CustomXRPanel(string.Empty);
            if (m_loadingPanel.Content != null)
            {
                m_loadingPanel.Content?.RemoveFromHierarchy();
            }
            XRPanel.Build(m_loadingPanel, process).SetCloseButton(false).SetBackground(false).SetZOffset(1f).Build();
            m_loadingPanel.Shown += OnXRSettingsPanelShown;
            m_loadingPanel.Show();
            return;
            
            void OnXRSettingsPanelShown(XRPanel.CustomXRPanel panel)
            {
                m_loadingPanel.Shown -= OnXRSettingsPanelShown;
                m_XRUIToolkitManager.gameObject.SetActive(false);
                callback?.Invoke();
            }
        }

        protected override void OnHideLoadingPanel(Action onHiddenCallback)
        {
            if (!IsLoadingPanelVisible)
            {
                IsLoadingPanelVisible = false;
                onHiddenCallback?.Invoke();
                return;
            }
            
            m_loadingPanel.Dismissed += OnXRSettingsPanelDismissed;
            m_loadingPanel.Dismiss();
            return;
            
            void OnXRSettingsPanelDismissed(XRPanel.CustomXRPanel obj)
            {
                IsLoadingPanelVisible = false;
                m_loadingPanel.Dismissed -= OnXRSettingsPanelDismissed;
                m_XRUIToolkitManager.gameObject.SetActive(true);
                if (obj.Content is CircularProgress)
                {
                    obj.Content?.RemoveFromHierarchy();
                }
               
                m_loadingPanel = null;
                onHiddenCallback?.Invoke();
            }
        }
    }
}
