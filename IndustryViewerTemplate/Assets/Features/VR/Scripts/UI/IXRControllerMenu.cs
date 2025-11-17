using UnityEngine;
using UnityEngine.XR.Hands;

namespace Unity.Industry.Viewer.VR
{
    public interface IXRControllerMenu
    {
        public Handedness Side { get; }

        public enum MenuPosition
        {
            Top,
            Bottom,
        }
        
        public MenuPosition Position { get; }
    }
}
