using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace Unity.Industry.Viewer.VR
{
    [UxmlElement]
    public partial class XRRoundButton : IconButton
    {
        private Texture _iconTexture;
        
        private float _topMargin;
        private float _bottomMargin;

        [UxmlAttribute]
        public float TopPadding
        {
            get => _topMargin;
            // Setting padding to a negative value is not allowed, so we check for it.
            set
            {
                _topMargin = value;
                style.marginTop = value;
            }
        }

        [UxmlAttribute]
        public Texture IconTexture
        {
            get => _iconTexture;
            set {
                _iconTexture = value;
                var icon = this.Q<Icon>(iconUssClassName);
                if (icon != null)
                {
                    icon.image = _iconTexture;
                    icon.parent.style.display = _iconTexture == null ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }
        
        [UxmlAttribute]
        public float BottomPadding
        {
            get => _bottomMargin;
            set
            {
                _bottomMargin = value;
                style.marginBottom = value;
            }
        }

        public XRRoundButton()
        {
            AddToClassList("xr-round-button-style");
        }
    }
}
