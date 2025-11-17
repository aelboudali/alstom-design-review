using System;
using Unity.Cloud.DataStreaming.Runtime;
using UnityEngine;
using Unity.Cloud.HighPrecision.Runtime;
using Unity.Industry.Viewer.Streaming;
using Unity.XR.CoreUtils;
using Unity.Industry.Viewer.Navigation.StandardCameraControl.Shared;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using InputDevice = UnityEngine.XR.InputDevice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using System.Collections;
using Unity.Mathematics;

namespace Unity.Industry.Viewer.VR.FlyMode
{
    public class VRMovementController : NavigationOption
    {
        private const string k_RightHandTeleportInteractorTag = "VR-RightHandTeleport Interactor";
        
        private const float k_PinchThreshold = 0.8f; // Threshold for pinch detection
        private const float k_PinchReleaseThreshold = 0.5f; // Threshold for pinch release detection
        private const float k_PinchMovementThreshold = 0.2f; // Minimum movement distance to trigger pinch movement
        
        XROrigin m_XROrigin;
        DynamicMoveProvider m_DynamicMoveProvider;
        
        DoubleBounds? m_Bounds;

        private CameraUtility Utility;
        
        [SerializeField]
        protected CameraSettings m_Settings;
        
        public float MoveSensitivity { get; private set; }
        
        private CameraUtility m_CameraUtility;
        
        XRInputModalityManager m_InputModalityManager;

        private ControlType m_CurrentControlType; 
        
        static List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();
        
        XRFingerShape m_LeftHandPinchShape;
        XRFingerShape m_RightHandPinchShape;
        
        bool m_LeftHandPinchActive;
        bool m_RightHandPinchActive;
        
        private Vector3 m_leftHandPinchStartPosition;
        private Vector3 m_rightHandPinchStartPosition;
        
        private LocomotionMediator m_LocomotionMediator;

        #region Fly Mode

        [SerializeField, Header("Fly Settings")]
        private InputActionReference m_LeftHandJoystickInputReference;
        private float m_OriginalFlySpeed;
        private float m_MaxFlySpeed;
        private const float k_JoystickFullPushThreshold = 0.9f; 
        [SerializeField]
        private float m_MaxTimeToTravelFullSpeed = 3f;
        private float m_PinchMovementSpeed = 0.1f;

        #endregion

        #region Teleport
        
        public Gradient InvalidGradient => _invalidGradient;
        [SerializeField, Header("Teleport Settings")]
        private Gradient _invalidGradient;
        public Gradient ValidGradient => _validGradient;
        [SerializeField]
        private Gradient _validGradient;
        
        [SerializeField]
        private GameObject m_DirectionTeleportReticle;
        public GameObject DirectionTeleportReticle => m_DirectionTeleportReticle;
        
        private CustomTeleportHandler m_CustomTeleportHandler;
        public StreamingModelController StreamingModelController => m_StreamingModelController;
        private StreamingModelController m_StreamingModelController;
        
        [SerializeField]
        private InputActionReference m_TeleportCancelActionReference;
        public InputActionReference TeleportCancelActionReference => m_TeleportCancelActionReference;

        [SerializeField] private InputActionReference m_TeleportDirectionInputReference;
        public InputActionReference TeleportDirectionInputReference => m_TeleportDirectionInputReference;
        
        private Coroutine m_TeleportCoroutine;
        private bool m_IsTeleporting;
        
        [SerializeField, Tooltip("The delay after releasing the teleport button to start the teleportation.")]
        float m_TeleportMoveDelay = 0.375f;

        #endregion

        private void Awake()
        {
            m_LocomotionMediator = FindFirstObjectByType<LocomotionMediator>(FindObjectsInactive.Include);
            m_InputModalityManager ??= FindFirstObjectByType<XRInputModalityManager>();
            m_StreamingModelController = FindFirstObjectByType<StreamingModelController>();
            
            m_XROrigin = FindFirstObjectByType<XROrigin>();
            m_DynamicMoveProvider = FindFirstObjectByType<DynamicMoveProvider>();
            m_OriginalFlySpeed = m_DynamicMoveProvider.moveSpeed;
            Utility ??= new CameraUtility(Camera.main);
        }

        private void Start()
        {
            MoveSensitivity = 1f;
            m_InputModalityManager.motionControllerModeStarted.AddListener(UseMotionController);
            m_InputModalityManager.motionControllerModeEnded.AddListener(MotionControllerTrackingStopped);
            m_InputModalityManager.trackedHandModeStarted.AddListener(UseTrackedHandMode);
            m_InputModalityManager.trackedHandModeEnded.AddListener(HandsTrackingStopped);
        }

        private void OnEnable()
        {
            m_LocomotionMediator?.gameObject.SetActive(true);
            
            m_CurrentControlType = GetCurrentInputType();
            
            if(m_CurrentControlType == ControlType.Hands)
            {
                EnablePinchMovement();
            }
            else
            {
                InitializeHandController();
            }
        }

        private void Update()
        {
            if (m_Bounds.HasValue)
            {
                var newBounds = StreamingUtils.ReturnBounds(m_Bounds.Value);
                Utility?.SetClipPlane(newBounds);
                Camera.main.nearClipPlane = 0.01f;
            }
            
            if (m_CurrentControlType == ControlType.MotionControllers)
            {
                var leftJoystickValue = m_LeftHandJoystickInputReference.action.ReadValue<Vector2>();
                float magnitude = leftJoystickValue.magnitude; 
                bool isAtFarEnd = magnitude >= k_JoystickFullPushThreshold;

                if (isAtFarEnd)
                {
                    float incrementPerSecond = (m_MaxFlySpeed - m_OriginalFlySpeed) / m_MaxTimeToTravelFullSpeed * MoveSensitivity;
                    m_DynamicMoveProvider.moveSpeed = Mathf.MoveTowards(
                        m_DynamicMoveProvider.moveSpeed,
                        m_MaxFlySpeed,
                        incrementPerSecond * Time.deltaTime
                    );
                }
                else
                {
                    m_DynamicMoveProvider.moveSpeed = m_OriginalFlySpeed;
                }
                return;
            }
            
            if (!TryGetSubsystem(out var subsystem))
                return;
            
            m_LeftHandPinchShape = subsystem.leftHand.CalculateFingerShape(XRHandFingerID.Index, XRFingerShapeTypes.Pinch);
            m_RightHandPinchShape = subsystem.rightHand.CalculateFingerShape(XRHandFingerID.Index, XRFingerShapeTypes.Pinch);
            if (m_LeftHandPinchShape.TryGetPinch(out float leftPinch))
            {
                if (leftPinch >= k_PinchThreshold && !m_LeftHandPinchActive)
                {
                    m_LeftHandPinchActive = true;
                    m_leftHandPinchStartPosition = subsystem.leftHand.rootPose.position;
                } else if (leftPinch <= k_PinchReleaseThreshold && m_LeftHandPinchActive)
                {
                    m_LeftHandPinchActive = false;
                }
            }
            else
            {
                m_LeftHandPinchActive = false;
            }

            if (m_RightHandPinchShape.TryGetPinch(out float rightPinch))
            {
                if (rightPinch >= k_PinchThreshold && !m_RightHandPinchActive)
                {
                    m_RightHandPinchActive = true;
                    m_rightHandPinchStartPosition = subsystem.rightHand.rootPose.position;
                } else if (rightPinch <= k_PinchReleaseThreshold && m_RightHandPinchActive)
                {
                    m_RightHandPinchActive = false;
                }
            } 
            else
            {
                m_RightHandPinchActive = false;
            }

            if (m_LeftHandPinchActive && m_RightHandPinchActive)
            {
                var leftHandPosition = subsystem.leftHand.rootPose.position;
                var rightHandPosition = subsystem.rightHand.rootPose.position;
                var pinchDistance = Vector3.Distance(leftHandPosition, rightHandPosition);
                var originDistance = Vector3.Distance(m_leftHandPinchStartPosition, m_rightHandPinchStartPosition);
                var zoomIn = originDistance < pinchDistance;
                var difference = Mathf.Abs(pinchDistance - originDistance);
                if (m_XROrigin != null && difference > k_PinchMovementThreshold)
                {
                    float incrementPerSecond = (m_MaxFlySpeed - m_PinchMovementSpeed) / m_MaxTimeToTravelFullSpeed * MoveSensitivity * 0.1f;
                    m_PinchMovementSpeed = Mathf.MoveTowards(
                        m_PinchMovementSpeed,
                        m_MaxFlySpeed,
                        incrementPerSecond * Time.deltaTime);
                    m_XROrigin.MoveCameraToWorldLocation(m_XROrigin.Camera.transform.position + (zoomIn? m_XROrigin.Camera.transform.forward * m_PinchMovementSpeed : -m_XROrigin.Camera.transform.forward * m_PinchMovementSpeed));
                }
                else
                {
                    m_PinchMovementSpeed = 0.1f;
                }
            }
        }

        private void OnDisable()
        {
            m_IsTeleporting = false;
            if (m_TeleportCoroutine != null)
            {
                StopCoroutine(m_TeleportCoroutine);
            }
        }

        private void OnDestroy()
        {
            m_InputModalityManager.motionControllerModeStarted.RemoveListener(UseMotionController);
            m_InputModalityManager.motionControllerModeEnded.RemoveListener(MotionControllerTrackingStopped);
            m_InputModalityManager.trackedHandModeStarted.RemoveListener(UseTrackedHandMode);
            m_InputModalityManager.trackedHandModeEnded.RemoveListener(HandsTrackingStopped);
            if(m_CustomTeleportHandler != null)
            {
                Destroy(m_CustomTeleportHandler);
            }
        }

        public void TeleportStart(Vector3 position, Quaternion rotation)
        {
            if(m_IsTeleporting) return;
            m_TeleportCoroutine = StartCoroutine(StartTeleport());
            return;

            IEnumerator StartTeleport()
            {
                m_IsTeleporting = true;
                m_LocomotionMediator.gameObject.SetActive(false);
                yield return new WaitForSeconds(m_TeleportMoveDelay);
                
                var xrRigTransform = m_XROrigin.transform;
                var currentPosition = xrRigTransform.position;
                
                var mainCamera = m_XROrigin.Camera;
                if (mainCamera != null)
                {
                    var cameraToRigOffset = currentPosition - mainCamera.transform.position;
                    cameraToRigOffset = rotation * Quaternion.Inverse(xrRigTransform.rotation) * cameraToRigOffset;
                    cameraToRigOffset.y = 0;
                    position += cameraToRigOffset;
                }
                
                const float kTargetDuration = 0.05f;
                var currentDuration = 0f;
                
                m_XROrigin.MatchOriginUpCameraForward(Vector3.up, rotation * Vector3.forward);
                
                while (currentDuration < kTargetDuration)
                {
                    currentDuration += Time.unscaledDeltaTime;
                    currentPosition = Vector3.Lerp(currentPosition, position, currentDuration / kTargetDuration);
                    xrRigTransform.position = currentPosition;
                    yield return null;
                }
                
                xrRigTransform.position = position;
                m_IsTeleporting = false;
                m_LocomotionMediator.gameObject.SetActive(true);
            }
        }
        
        private void StartHandSubsystem()
        {
            if (TryGetSubsystem(out var subsystem))
            {
                if (!subsystem.running)
                {
                    subsystem.Start();
                    Debug.Log("Hand subsystem started");
                }
                else
                {
                    Debug.Log("Hand subsystem already running");
                }
            }
            else
            {
                Debug.LogWarning("No hand subsystem found");
            }
        }

        private void EnablePinchMovement()
        {
            StartHandSubsystem();
        }

        private void HandsTrackingStopped()
        {

        }

        private void MotionControllerTrackingStopped()
        {
            
        }

        private void UseTrackedHandMode()
        {
            EnablePinchMovement();
        }

        private void UseMotionController()
        {
            InitializeHandController();
        }

        private void InitializeHandController()
        {
            if (m_CustomTeleportHandler == null)
            {
                var allRightHandChildren = m_InputModalityManager.rightController.GetComponentsInChildren<Transform>(true);
                var rightHandTeleportInteractor = allRightHandChildren.First(x => x.gameObject.CompareTag(k_RightHandTeleportInteractorTag));
                if (rightHandTeleportInteractor != null)
                {
                    m_CustomTeleportHandler = rightHandTeleportInteractor.gameObject.GetComponent<CustomTeleportHandler>();
                    if (m_CustomTeleportHandler == null)
                    {
                        m_CustomTeleportHandler = rightHandTeleportInteractor.gameObject.AddComponent<CustomTeleportHandler>();
                    }

                    m_CustomTeleportHandler.Init(this);
                }
            }
            
        }

        public override void Initialize()
        {
            navigationCamera = Camera.main;
            StreamingModelController.AddObserver += AddObserver;
            StreamingModelController.BoundsUpdated += OnBoundsUpdated;
        }

        private void AddObserver(Camera observerCamera)
        {
            m_CameraUtility ??= new CameraUtility(observerCamera);
        }

        public override void Uninitialize()
        {
            StreamingModelController.AddObserver -= AddObserver;
            StreamingModelController.BoundsUpdated -= OnBoundsUpdated;
        }
        
        public override void SetDefaultView()
        {
            if(m_Bounds == null) return;
            SetView(m_Bounds);
        }

        public override void FocusToPoint(DoubleBounds bounds)
        {
            Vector3 direction = (m_XROrigin.gameObject.transform.position - ((Bounds)bounds).center).normalized; // Step 1: Direction
            Vector3 result = ((Bounds)bounds).center + direction * 5f; // Step 2: Move along direction
            
            m_XROrigin.MoveCameraToWorldLocation(result);
                
            m_XROrigin.MatchOriginUpCameraForward(Vector3.up, direction);
        }

        public override void TranslateTo(Vector3 position, Quaternion rotation)
        {
            if(m_XROrigin == null) return;
            m_XROrigin.MoveCameraToWorldLocation(position);
            m_XROrigin.MatchOriginUpCameraForward(Vector3.up, rotation * Vector3.forward);
        }

        public override void FollowPresenter(GameObject presenterObject)
        {
            // This method is not applicable for VR movement controller.
            return;
        }

        private void OnBoundsUpdated(DoubleBounds bounds, bool skipCameraUpdate)
        {
            if(m_XROrigin == null) return;
            
            m_Bounds = new DoubleBounds(bounds.Center, bounds.Size * 1.5f);
            
            if (!skipCameraUpdate)
            {
                SetView(bounds);
            }
           
            
            if(m_DynamicMoveProvider == null) return;
            
            CalculateMaxSpeed();
        }

        private void CalculateMaxSpeed()
        {
            if(m_Bounds == null) return;
            var maxDistanceToMove = (float)math.length(m_Bounds.Value.Extents) / 2;
            //Cap at 100 to avoid too high speeds
            m_MaxFlySpeed = Mathf.Min(200, Mathf.Max(m_OriginalFlySpeed, maxDistanceToMove / m_Settings.maxTimeToTravelFullSpeed * m_Settings.maxSpeedScaling));
        }
        
        private void SetView(DoubleBounds? bounds)
        {
            var pitch = 20.0f;
            float fillRatio = 0.9f;
            var fieldOfView = navigationCamera.fieldOfView;
            var aspectRatio = navigationCamera.aspect;
            var nearClipPlane = navigationCamera.nearClipPlane;
            var farClipPlane = navigationCamera.farClipPlane;
                
            var desiredEuler = new Vector3(pitch, 0, 0);
            var distanceFromCenter = GetDistanceFromCenterToFit(bounds.Value, fillRatio, fieldOfView, aspectRatio);
            
            if (distanceFromCenter > farClipPlane)
            {
                distanceFromCenter = (farClipPlane + nearClipPlane) / 2 ;
            }

            var center = new Vector3((float) bounds.Value.Center.x, (float) bounds.Value.Center.y, (float) bounds.Value.Center.z);
                
            var position = center - distanceFromCenter * (Quaternion.Euler(desiredEuler) * Vector3.forward);

            if (NavigationController.StartingPosition.HasValue)
            {
                position = NavigationController.StartingPosition.Value;
            }
            
            position = new Vector3(
                Mathf.Clamp(position.x, -StandardCamera.DefaultBounds, StandardCamera.DefaultBounds),
                Mathf.Clamp(position.y, -StandardCamera.DefaultBounds, StandardCamera.DefaultBounds),
                Mathf.Clamp(position.z, -StandardCamera.DefaultBounds, StandardCamera.DefaultBounds)
            );
            
            var faceDirection = center - new Vector3(position.x, center.y, position.z); 
                
            m_XROrigin.MoveCameraToWorldLocation(position);
                
            m_XROrigin.MatchOriginUpCameraForward(Vector3.up, faceDirection);
            
            float GetDistanceFromCenterToFit(DoubleBounds bb, float fillRatio, float fovY, float aspectRatio)
            {
                var fovX = GetHorizontalFov(fovY, aspectRatio);
                var distanceToFitXAxisInView = GetDistanceFromCenter(bb, (float)bb.Extents.x, fovX, fillRatio);
                var distanceToFitYAxisInView = GetDistanceFromCenter(bb, (float)bb.Extents.y, fovY, fillRatio);
                return Mathf.Max(distanceToFitXAxisInView, distanceToFitYAxisInView);
            }
            
            float GetHorizontalFov(float fovY, float aspectRatio)
            {
                var ratio = Mathf.Tan(Mathf.Deg2Rad * (fovY / 2.0f));
                return Mathf.Rad2Deg * Mathf.Atan(ratio * aspectRatio) * 2.0f;
            }
            
            float GetDistanceFromCenter(DoubleBounds bb, float opposite, float fov, float fillRatio)
            {
                var lookAt = bb.Center;

                var angle = fov / 2.0f;
                var ratio = Mathf.Tan(Mathf.Deg2Rad * angle);
                var adjacent = opposite / ratio;
                var distanceFromLookAt = lookAt.z - bb.Min.z + adjacent / fillRatio;

                return (float)distanceFromLookAt;
            }
        }

        public void SetHomeView()
        {
            SetView(m_Bounds);
        }
        
        public void UpdateMoveSensitivity(float value)
        {
            MoveSensitivity = Mathf.Clamp(value, 0.1f, 5.0f);
            //CalculateSpeed();
        }

        public override void OnNavigationOptionEnable()
        {
            StreamingModelController.AddObserver?.Invoke(Camera.main);
        }

        public override void OnNavigationOptionDisable()
        {
            
        }

        public override bool IsSupported()
        {
            return true;
        }

        public override GameObject GetNavigationGameObject()
        {
            return null;
        }
        
        private ControlType GetCurrentInputType()
        {
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
        
        private ControlType GetHandInputType(InputDeviceCharacteristics characteristics)
        {
            #if UNITY_EDITOR
            return ControlType.MotionControllers;
            #endif
            
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
        
        static bool TryGetSubsystem(out XRHandSubsystem system)
        {
            system = null;

            if (s_SubsystemsReuse.Count == 0)
                SubsystemManager.GetSubsystems(s_SubsystemsReuse);

            if (s_SubsystemsReuse.Count > 0)
            {
                system = s_SubsystemsReuse[0];
                return true;
            }
            return false;
        }
    }
}
