using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using System.Threading.Tasks;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Threading;
using System.Text.RegularExpressions;
using UnityEngine.Localization;

namespace Unity.Industry.Viewer.VR
{
    public class XRToastPanel : MonoBehaviour
    {
        private CancellationTokenSource m_dismissCancellationTokenSource;
        
        private static XRToastPanel instance;
        
        private UIDocument m_toastDocument;
        
        private LazyFollow m_toastFollow;

        private VisualElement BG => m_toastDocument.rootVisualElement.Q<VisualElement>("BG");
        private Text m_toastMessage;
        
        private NotificationDuration m_notificationDuration;
        private NotificationStyle m_notificationStyle;
        
        public Action<XRToastPanel> Shown;
        
        public Action<XRToastPanel> Dismissed;
        
        private void Awake()
        {
            instance = this;
            m_toastFollow ??= GetComponent<LazyFollow>();
            m_toastDocument ??= GetComponent<UIDocument>();
            m_toastFollow.enabled = false;
            m_toastMessage = m_toastDocument.rootVisualElement.Q<Text>();
        }

        private void Start()
        {
            BG.style.display = DisplayStyle.None;
        }
        
        private void OnDestroy()
        {
            m_dismissCancellationTokenSource?.Cancel();
            m_dismissCancellationTokenSource?.Dispose();
        }

        public static XRToastPanel Build(string message, NotificationDuration duration)
        {
            if (instance == null)
            {
                Debug.LogError("No XRToastPanel instance found in the scene.");
                return null;
            }
            
            instance.Shown = null;
            instance.Dismissed = null;
            
            var match = Regex.Match(message, @"^@([^:]+):\s*(.+)$");
            if (match.Success)
            {
                string stringTable =  match.Groups[1].Value.TrimStart();
                string stringName = match.Groups[2].Value.TrimStart();
                
                LocalizedString myString = new LocalizedString
                {
                    TableReference = stringTable,
                    TableEntryReference = stringName
                };
                var operation = myString.GetLocalizedStringAsync();
                operation.WaitForCompletion();
                message = operation.Result;
                instance.m_toastMessage.text = message;
            }
            
            instance.m_notificationDuration = duration;
            instance.m_notificationStyle = NotificationStyle.Default;
            instance.UpdateClasses();
            
            return instance;
        }

        public XRToastPanel SetStyle(NotificationStyle style)
        {
            instance.m_notificationStyle = style;
            UpdateClasses();
            return instance;
        }

        private void UpdateClasses()
        {
            instance.BG.ClearClassList();
            instance.BG.AddToClassList("appui-toast");
            instance.BG.AddToClassList("appui-toast--" + instance.m_notificationStyle.ToString().ToLower());
        }

        public void Show()
        {
            // Cancel any existing delay
            m_dismissCancellationTokenSource?.Cancel();
            m_dismissCancellationTokenSource?.Dispose();
            instance.m_toastFollow.enabled = true;
            instance.BG.style.display = DisplayStyle.Flex;
            instance.Shown?.Invoke(this);
            
            if(instance.m_notificationDuration == NotificationDuration.Indefinite) return;
    
            // Create new cancellation token for this toast
            m_dismissCancellationTokenSource = new CancellationTokenSource();
            _ = DismissAfterDelay(m_dismissCancellationTokenSource.Token);
            return;

            async Task DismissAfterDelay(CancellationToken cancellationToken)
            {
                try
                {
                    await Task.Delay((int)instance.m_notificationDuration, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Dismiss();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancelled, do nothing
                }
                finally
                {
                    m_dismissCancellationTokenSource = null;
                }
            }
        }
        
        public void Dismiss()
        {
            instance.m_toastFollow.enabled = false;
            instance.BG.style.display = DisplayStyle.None;
            m_dismissCancellationTokenSource?.Dispose();
            instance.Dismissed?.Invoke(this);
        }
    }
}
