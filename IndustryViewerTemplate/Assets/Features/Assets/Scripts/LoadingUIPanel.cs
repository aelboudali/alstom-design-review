using UnityEngine;
using System;
using Unity.AppUI.UI;
using Unity.AppUI.Core;

namespace Unity.Industry.Viewer.Assets
{
    public class LoadingUIPanel : MonoBehaviour
    {
        public static Action<Action> ShowLoadingPanel;
        public static Action<Action> HideLoadingPanel;
        public static bool IsLoadingPanelVisible;
        
        private static Modal m_LoadingModal;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {
            ShowLoadingPanel += OnShowLoadingPanel;
            HideLoadingPanel += OnHideLoadingPanel;
        }
        
        protected virtual void OnDestroy()
        {
            ShowLoadingPanel -= OnShowLoadingPanel;
            HideLoadingPanel -= OnHideLoadingPanel;
        }

        protected virtual void OnHideLoadingPanel(Action callback)
        {
            IsLoadingPanelVisible = false;
            if (m_LoadingModal == null)
            {
                callback?.Invoke();
                return;
            }
            m_LoadingModal.dismissed += LoadingModalOnDismissed;
            m_LoadingModal?.Dismiss();
            return;
            
            void LoadingModalOnDismissed(Modal modal, DismissType dismissType)
            {
                modal.dismissed -= LoadingModalOnDismissed;
                m_LoadingModal = null;
                callback?.Invoke();
            }
        }

        protected virtual void OnShowLoadingPanel(Action onShownCallback)
        {
            IsLoadingPanelVisible = true;
            m_LoadingModal?.Dismiss();
            var process = new CircularProgress
            {
                size = Size.L
            };
            m_LoadingModal = Modal.Build(SharedUIManager.Instance.AssetsContainer, process);
            m_LoadingModal.shown += LoadingModalOnShown;

            m_LoadingModal.Show();
            return;
            
            void LoadingModalOnShown(Modal modal)
            {
                modal.shown -= LoadingModalOnShown;
                modal.contentView.parent.RemoveFromClassList("appui-modal__content");
                onShownCallback?.Invoke();
            }
        }
    }
}
