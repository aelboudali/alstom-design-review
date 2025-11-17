using UnityEngine;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Avatar = Unity.AppUI.UI.Avatar;

namespace Unity.Industry.Viewer.Collaboration
{
    [UxmlElement("NameSuggestionButton")]
    public partial class NameSuggestionButton : ActionButton
    {
        public readonly Avatar Avatar;
        private Text avatarlabel;
        
        
        public string AvatarLabel
        {
            get => avatarlabel?.text;
            set
            {
                if (avatarlabel != null)
                    avatarlabel.text = value;
            }
        }
        
        public NameSuggestionButton()
        {
            style.justifyContent = Justify.FlexStart;
            AddToClassList("thread");
            AddToClassList("NameSuggestionButton");
            Avatar = new Avatar
            {
                size = Size.S,
                variant = AvatarVariant.Circular
            };
            Avatar.style.marginRight = new Length(8, LengthUnit.Pixel);
            hierarchy.Insert(0, Avatar);
            avatarlabel = new Text();
            Avatar.Add(avatarlabel);
        }
    }
}
