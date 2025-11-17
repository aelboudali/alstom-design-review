using Unity.AppUI.UI;
using UnityEngine.UIElements;
using System;

namespace Unity.Industry.Viewer.Identity
{
    [UxmlElement("CustomAvatar")]
    public partial class CustomAvatar: Avatar, IPressable
    {
        public CustomAvatar() : this(null) { }
        
        public CustomAvatar(Action clickEvent)
        {
            clickable = new Pressable(clickEvent);
        }
        
        public Pressable clickable
        {
            get => m_Clickable;
            set
            {
                if (m_Clickable != null)
                {
                    m_Clickable.clicked -= OnClick;
                    if (m_Clickable.target == this)
                        this.RemoveManipulator(m_Clickable);
                }
                m_Clickable = value;
                if (m_Clickable == null)
                    return;
                this.AddManipulator(m_Clickable);
                m_Clickable.clicked += OnClick;
            }
        }
        
        Pressable m_Clickable;
        
        public event Action clicked
        {
            add => clickable.clicked += value;
            remove => clickable.clicked -= value;
        }
        
        void OnClick()
        {
            using var evt = ActionTriggeredEvent.GetPooled();
            evt.target = this;
            SendEvent(evt);
        }
    }
}