using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using InputDevice = UnityEngine.XR.InputDevice;

namespace Unity.Industry.Viewer.VR
{
    // This script manages VR interactions in a Unity project.
    // It handles input actions for single, double, and press activations for both left and right hand controllers.
    // The script manages event subscriptions for these actions and notifies subscribers when actions are performed.
    // It also tracks controller movements and updates subscribers accordingly.
    // The script integrates with Unity's MonoBehaviour for lifecycle management and uses Unity's XR Interaction Toolkit for VR interactions.
    public class VRInteractionController : MonoBehaviour
    {
        [Flags]
        public enum ActionType
        {
            SingleActivate = 1 << 0,
            DoubleActivate = 1 << 1,
            PressActivate = 1 << 2,
        }

        private string k_PinchGameObjectName = "Pinch Point Stabilized";
        
        // Left hand state
        private float m_LeftLastClickTime, m_LeftPressTime;
        private bool m_LeftWaitingForDoubleClick;
        
        // Right hand state  
        private float m_RightLastClickTime, m_RightPressTime;
        private bool m_RightWaitingForDoubleClick;
        private const float k_TapTimeThreshold = 0.3f; // Minimum distance to consider a tap
        private const float k_DoubleClickThreshold = 0.3f; // Maximum time between clicks for double-click
        private const float k_SingleClickDelay = 0.25f; // Delay to wait for potential second click
        private const float k_PressHoldThresholdForHands = 0.8f;
        private const float k_PressReleaseThresholdForHands = 0.5f;
        private readonly WaitForSeconds k_WaitForDoubleClick = new WaitForSeconds(k_SingleClickDelay);
        
        // Event for single activation
        private static event Action<Ray, int> OnSingleActivatePressed;
        private static event Action<Ray, bool, int> OnPressActivatePressed; 
        private static event Action<Ray, int> OnDoubleActivatePressed;
        private static event Action<Ray, int> OnControllerMoved;
        
        // Dictionary to store subscribers for single activation
        private static readonly Dictionary<MonoBehaviour, Action<Ray, int>> SingleActivateSubscribers = new();
        private static readonly Dictionary<MonoBehaviour, Action<Ray, int>> DoubleActivateSubscribers = new();
        private static readonly Dictionary<MonoBehaviour, Action<Ray, int>> ControllerMovedSubscribers = new();
        private static readonly Dictionary<MonoBehaviour, Action<Ray, bool, int>> PressActivateSubscribers = new();
        
        public static VRInteractionController Instance;

        private NearFarInteractor mainLeftRayInteractor;
        private NearFarInteractor mainRightRayInteractor;
        
        private NearFarInteractor leftControllerRayInteractor;
        private NearFarInteractor rightControllerRayInteractor;
        
        private ControlType m_ControlType;
        
        XRInputModalityManager m_InputModalityManager;
        private NearFarInteractor leftHandRayInteractor;
        private NearFarInteractor rightHandRayInteractor;

        private bool leftHandPressOrHold;
        private bool rightHandPressOrHold;
        
        [HideInInspector]
        public ActionType SubscribedActionType;
        
        private Transform m_LeftPinchTransform;
        private Transform m_RightPinchTransform;

        private void Awake()
        {
            Instance = this;
            m_InputModalityManager ??= FindFirstObjectByType<XRInputModalityManager>();
            m_InputModalityManager.motionControllerModeStarted.AddListener(UseMotionController);
            m_InputModalityManager.motionControllerModeEnded.AddListener(DeviceStopped);
            m_InputModalityManager.trackedHandModeStarted.AddListener(UseTrackedHandMode);
            m_InputModalityManager.trackedHandModeEnded.AddListener(DeviceStopped);
        }

        private void OnEnable()
        {
            m_ControlType = GetCurrentInputType();
            UpdateRayInteractors();
            UpdateActionStates();
        }

        // Initialize the script and set up input actions for left and right hand controllers.
        private void Start()
        {
#region Motion Controllers
            leftControllerRayInteractor = m_InputModalityManager.leftController.GetComponentInChildren<NearFarInteractor>();
            rightControllerRayInteractor = m_InputModalityManager.rightController.GetComponentInChildren<NearFarInteractor>();

            leftControllerRayInteractor.activateInput.inputActionReferencePerformed.action.started +=
                OnLeftControllerRayInteractorActivateChanged;
            leftControllerRayInteractor.activateInput.inputActionReferencePerformed.action.canceled +=
                OnLeftControllerRayInteractorActivateChanged;
            
            rightControllerRayInteractor.activateInput.inputActionReferencePerformed.action.started +=
                OnRightControllerRayInteractorActivateChanged;
            rightControllerRayInteractor.activateInput.inputActionReferencePerformed.action.canceled +=
                OnRightControllerRayInteractorActivateChanged;
#endregion

#region Hands
            leftHandRayInteractor = m_InputModalityManager.leftHand.GetComponentInChildren<NearFarInteractor>();
            rightHandRayInteractor = m_InputModalityManager.rightHand.GetComponentInChildren<NearFarInteractor>();

            m_LeftPinchTransform = m_InputModalityManager.leftHand.transform.Find(k_PinchGameObjectName);
            m_RightPinchTransform = m_InputModalityManager.rightHand.transform.Find(k_PinchGameObjectName);
#endregion

            UpdateRayInteractors();
        }

        // Update the controller movement and notify subscribers.
        private void Update()
        {
            if (m_ControlType == ControlType.Hands)
            {
                if (mainLeftRayInteractor.selectInput.TryReadValue(out float leftValue))
                {
                    if (leftValue >= k_PressHoldThresholdForHands && !leftHandPressOrHold)
                    {
                        OnPressStarted(m_LeftPinchTransform, ref leftHandPressOrHold, ref m_LeftPressTime);
                    } else if(leftValue < k_PressReleaseThresholdForHands && leftHandPressOrHold)
                    {
                        OnPressReleased(Handedness.Left, m_LeftPinchTransform, ref leftHandPressOrHold,
                            ref m_LeftWaitingForDoubleClick, ref m_LeftLastClickTime, ref m_LeftPressTime);
                        leftHandPressOrHold = false;
                    }
                }
                else
                {
                    leftHandPressOrHold = false;
                }
                
                if(mainRightRayInteractor.selectInput.TryReadValue(out float rightValue))
                {
                    if (rightValue >= k_PressHoldThresholdForHands && !rightHandPressOrHold)
                    {
                        OnPressStarted(m_RightPinchTransform, ref rightHandPressOrHold, ref m_RightPressTime);
                    } else if(rightValue < k_PressReleaseThresholdForHands && rightHandPressOrHold)
                    {
                        OnPressReleased(Handedness.Right, m_RightPinchTransform, ref rightHandPressOrHold,
                            ref m_RightWaitingForDoubleClick, ref m_RightLastClickTime, ref m_RightPressTime);
                        rightHandPressOrHold = false;
                    }
                }
                else
                {
                    rightHandPressOrHold = false;
                }
            }
            
            if(ControllerMovedSubscribers.Count == 0 || m_ControlType == ControlType.None) return;
            
            var leftRay = m_ControlType == ControlType.MotionControllers
                ? leftControllerRayInteractor.transform
                : m_LeftPinchTransform;
            
            var rightRay = m_ControlType == ControlType.MotionControllers
                ? rightControllerRayInteractor.transform
                : m_RightPinchTransform;
            
            OnControllerMoved?.Invoke(new Ray(leftRay.position, leftRay.forward), leftRay.gameObject.GetInstanceID());
            
            OnControllerMoved?.Invoke(new Ray(rightRay.position, rightRay.forward), rightRay.gameObject.GetInstanceID());
        }

        // Unsubscribe from events and clear dictionaries when the script is destroyed.
        private void OnDestroy()
        {
            Instance = null;
            
            SingleActivateSubscribers.Clear();
            DoubleActivateSubscribers.Clear();
            OnSingleActivatePressed = null;
            OnDoubleActivatePressed = null;
            OnPressActivatePressed = null;
            
            m_InputModalityManager.motionControllerModeStarted.RemoveListener(UseMotionController);
            m_InputModalityManager.motionControllerModeEnded.RemoveListener(DeviceStopped);
            m_InputModalityManager.trackedHandModeStarted.RemoveListener(UseTrackedHandMode);
            m_InputModalityManager.trackedHandModeEnded.RemoveListener(DeviceStopped);
        }

        private void UseTrackedHandMode()
        {
            m_ControlType = ControlType.Hands;
            UpdateRayInteractors();
        }

        private void UseMotionController()
        {
            // Set the control type to MotionControllers and update the ray interactors.
            m_ControlType = ControlType.MotionControllers;
            UpdateRayInteractors();
        }

        private void DeviceStopped()
        {
            // Reset the control type and update the ray interactors when the device stops.
            m_ControlType = ControlType.None;
            UpdateRayInteractors();
        }

        private void UpdateRayInteractors()
        {
            mainLeftRayInteractor = null;
            mainRightRayInteractor = null;
            switch (m_ControlType)
            {
                case ControlType.MotionControllers:
                    mainLeftRayInteractor = leftControllerRayInteractor;
                    mainRightRayInteractor = rightControllerRayInteractor;
                    break;
                case ControlType.Hands:
                    mainLeftRayInteractor = leftHandRayInteractor;
                    mainRightRayInteractor = rightHandRayInteractor;
                    break;
            }
        }

        private void OnRightControllerRayInteractorActivateChanged(InputAction.CallbackContext obj)
        {
            CheckActivateState(obj, Handedness.Right, rightControllerRayInteractor.transform, ref rightHandPressOrHold, ref m_RightWaitingForDoubleClick, ref m_RightLastClickTime, ref m_RightPressTime);
        }

        private void OnLeftControllerRayInteractorActivateChanged(InputAction.CallbackContext obj)
        {
            CheckActivateState(obj, Handedness.Left, leftControllerRayInteractor.transform, ref leftHandPressOrHold, ref m_LeftWaitingForDoubleClick, ref m_LeftLastClickTime, ref m_LeftPressTime);
        }

        private void CheckActivateState(InputAction.CallbackContext callbackContext, Handedness side, Transform rayTransform, 
            ref bool b, ref bool mWaitingForDoubleClick, ref float mLastClickTime, ref float mPressTime)
        {
            if (callbackContext.phase == InputActionPhase.Started)
            {
                OnPressStarted(rayTransform, ref b, ref mPressTime);
            }
            else if (callbackContext.phase == InputActionPhase.Canceled)
            {
                OnPressReleased(side, rayTransform, ref b, ref mWaitingForDoubleClick, ref mLastClickTime, ref mPressTime);
            }
        }

        private void OnPressStarted(Transform rayTransform, ref bool b, ref float mPressTime)
        {
            b = true;
            Ray ray = new Ray(rayTransform.position, rayTransform.forward);
            mPressTime = Time.time;
            if(!SubscribedActionType.HasFlag(ActionType.PressActivate)) return;
            OnPressActivatePressed?.Invoke(ray, b, rayTransform.gameObject.GetInstanceID());
        }

        private void OnPressReleased(Handedness side, Transform rayTransform, ref bool b, ref bool mWaitingForDoubleClick,
            ref float mLastClickTime, ref float mPressTime)
        {
            b = false;
            Ray ray = new Ray(rayTransform.position, rayTransform.forward);
            var currentTime = Time.time;
            var instanceId = rayTransform.gameObject.GetInstanceID();
            if (currentTime - mPressTime < k_TapTimeThreshold)
            {
                if (mWaitingForDoubleClick && (currentTime - mLastClickTime) < k_DoubleClickThreshold)
                {
                    // Double click detected
                    mWaitingForDoubleClick = false;
                    if(!SubscribedActionType.HasFlag(ActionType.DoubleActivate)) return;
                    OnDoubleActivatePressed?.Invoke(ray, instanceId);
                }
                else
                {
                    // Potential single click - start waiting for double click
                    mLastClickTime = currentTime;
                    mWaitingForDoubleClick = true;
                    StartCoroutine(WaitForDoubleClick(side));
                }
            }
            // Always handle press release
            if(!SubscribedActionType.HasFlag(ActionType.PressActivate)) return;
            OnPressActivatePressed?.Invoke(ray, b, instanceId);
        }

        private System.Collections.IEnumerator WaitForDoubleClick(Handedness side)
        {
            yield return k_WaitForDoubleClick;
            Transform controllerTransform = null;
            if (m_ControlType == ControlType.MotionControllers)
            {
                controllerTransform = side == Handedness.Left ? leftControllerRayInteractor.transform : rightControllerRayInteractor.transform;
            } else if (m_ControlType == ControlType.Hands)
            {
                controllerTransform = side == Handedness.Left ? m_LeftPinchTransform : m_RightPinchTransform;
            }
            if(controllerTransform == null) yield break;
            int instanceId = controllerTransform.gameObject.GetInstanceID();
            bool performAction = false;
            switch (side)
            {
                case Handedness.Left:
                    if (m_LeftWaitingForDoubleClick)
                    {
                        // No second click detected within time limit - it's a single click
                        m_LeftWaitingForDoubleClick = false;
                        performAction = true;
                    }
                    break;
                
                case Handedness.Right:
                    if (m_RightWaitingForDoubleClick)
                    {
                        // No second click detected within time limit - it's a single click
                        m_RightWaitingForDoubleClick = false;
                        performAction = true;
                    }
                    break;
            }
            if (performAction && SubscribedActionType.HasFlag(ActionType.SingleActivate))
            {
                Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
                OnSingleActivatePressed?.Invoke(ray, instanceId);
            }
        }
        
        private ControlType GetCurrentInputType()
        {
            #if UNITY_EDITOR
            return ControlType.MotionControllers;
            #endif
            
            // Check left hand first
            var leftType = GetHandInputType(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.HeldInHand);
            if (leftType != ControlType.None)
                return leftType;
    
            // Check right hand
            var rightType = GetHandInputType(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HeldInHand);
            if (rightType != ControlType.None)
                return rightType;
    
            return ControlType.None;
        }

        private static ControlType GetHandInputType(InputDeviceCharacteristics characteristics)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

            foreach (var device in devices)
            {
                if ((device.characteristics & InputDeviceCharacteristics.HandTracking) != 0)
                    return ControlType.Hands;
                if ((device.characteristics & InputDeviceCharacteristics.Controller) != 0)
                    return ControlType.MotionControllers;
            }
            return ControlType.None;
        }
        
        public static void SubscribeSingleActivate(MonoBehaviour subscriber, Action<Ray, int> action)
        {
            if (!SingleActivateSubscribers.TryAdd(subscriber, action)) return;
            OnSingleActivatePressed += action;
            UpdateActionStates();
        }
        
        public static void UnsubscribeSingleActivate(MonoBehaviour subscriber)
        {
            if (!SingleActivateSubscribers.TryGetValue(subscriber, out var action)) return;
            OnSingleActivatePressed -= action;
            SingleActivateSubscribers.Remove(subscriber);
            UpdateActionStates();
        }
        
        public static void SubscribePressActivate(MonoBehaviour subscriber, Action<Ray, bool, int> action)
        {
            if (!PressActivateSubscribers.TryAdd(subscriber, action)) return;
            OnPressActivatePressed += action;
            UpdateActionStates();
        }
        
        public static void UnsubscribePressActivate(MonoBehaviour subscriber)
        {
            if (!PressActivateSubscribers.TryGetValue(subscriber, out var action)) return;
            OnPressActivatePressed -= action;
            PressActivateSubscribers.Remove(subscriber);
            UpdateActionStates();
        }
        
        public static void SubscribeDoubleActivate(MonoBehaviour subscriber, Action<Ray, int> action)
        {
            if (!DoubleActivateSubscribers.TryAdd(subscriber, action)) return;
            OnDoubleActivatePressed += action;
            UpdateActionStates();
        }
        
        public static void UnsubscribeDoubleActivate(MonoBehaviour unsubscriber)
        {
            if (!DoubleActivateSubscribers.TryGetValue(unsubscriber, out var action)) return;
            OnDoubleActivatePressed -= action;
            DoubleActivateSubscribers.Remove(unsubscriber);
            UpdateActionStates();
        }
        
        public static void SubscribeControllerMoved(MonoBehaviour subscriber, Action<Ray, int> action)
        {
            if (!ControllerMovedSubscribers.TryAdd(subscriber, action)) return;
            OnControllerMoved += action;
        }
        
        public static void UnsubscribeControllerMoved(MonoBehaviour unsubscriber)
        {
            if (!ControllerMovedSubscribers.TryGetValue(unsubscriber, out var action)) return;
            OnControllerMoved -= action;
            ControllerMovedSubscribers.Remove(unsubscriber);
        }

        // Update the action states based on the number of subscribers.
        private static void UpdateActionStates()
        {
            switch (SingleActivateSubscribers.Count)
            {
                case > 0:
                {
                    if (!Instance.SubscribedActionType.HasFlag(ActionType.SingleActivate))
                    {
                        Instance.SubscribedActionType |= ActionType.SingleActivate;
                    }
                    break;
                }
                case 0:
                {
                    if (Instance.SubscribedActionType.HasFlag(ActionType.SingleActivate))
                    {
                        Instance.SubscribedActionType &= ~ActionType.SingleActivate;
                    }
                    break;
                }
            }
            
            switch (PressActivateSubscribers.Count)
            {
                case > 0:
                {
                    if (!Instance.SubscribedActionType.HasFlag(ActionType.PressActivate))
                    {
                        Instance.SubscribedActionType |= ActionType.PressActivate;
                    }
                    break;
                }
                case 0:
                {
                    if (Instance.SubscribedActionType.HasFlag(ActionType.PressActivate))
                    {
                        Instance.SubscribedActionType &= ~ActionType.PressActivate;
                    }
                    break;
                }
            }
            
            switch (DoubleActivateSubscribers.Count)
            {
                case > 0:
                {
                    if (!Instance.SubscribedActionType.HasFlag(ActionType.DoubleActivate))
                    {
                        Instance.SubscribedActionType |= ActionType.DoubleActivate;
                    }

                    break;
                }
                case 0:
                {
                    if (Instance.SubscribedActionType.HasFlag(ActionType.DoubleActivate))
                    {
                        Instance.SubscribedActionType &= ~ActionType.DoubleActivate;
                    }

                    break;
                }
            }
        }
    }
}
