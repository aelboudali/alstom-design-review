using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Unity.Industry.Viewer.VR
{
    public class UIResetPosition : MonoBehaviour
    {
        [SerializeField]
        private UIDocument m_UIDocument;
        
        private const string k_VRUITag = "VR-UI";
        XRRoundButton m_XRRoundButton;
        
        void Start()
        {
            m_XRRoundButton = m_UIDocument.rootVisualElement.Q<XRRoundButton>();
            if(m_XRRoundButton == null) return;
            m_XRRoundButton.clicked += XRRoundButtonOnClicked;
        }
        
        private void OnDestroy()
        {
            if(m_XRRoundButton == null) return;
            m_XRRoundButton.clicked -= XRRoundButtonOnClicked;
        }

        private void XRRoundButtonOnClicked()
        {
            var reparentToController = GetComponentInParent<ReparentToController>();
            if (reparentToController != null)
            {
                reparentToController.UpdateTrackedParentPosition();
            }
            
            var vrUIs = GameObject.FindGameObjectsWithTag(k_VRUITag);
            foreach (var vrUI in vrUIs)
            {
                if (vrUI.TryGetComponent(out LazyFollow lazyFollow))
                {
                    float distance = 2.0f; // Distance in front of the camera in meters
                    Vector3 cameraForward = Camera.main.transform.forward;
                    Vector3 cameraPosition = Camera.main.transform.position;
                    lazyFollow.transform.position = cameraPosition + cameraForward * lazyFollow.targetOffset.z + Vector3.up * lazyFollow.targetOffset.y;
                    
                    Vector3 targetDirection = cameraForward;
                    targetDirection.y = 0; // Zero out the y-component to prevent
                    lazyFollow.transform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
        }
    }
}
