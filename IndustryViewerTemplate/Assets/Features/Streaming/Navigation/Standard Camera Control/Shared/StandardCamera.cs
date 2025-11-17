using System;
using UnityEngine;
using Unity.Cloud.DataStreaming.Runtime;
using Unity.Cloud.HighPrecision.Runtime;
using Unity.Mathematics;
using Unity.Industry.Viewer.Streaming;
using System.Collections;

namespace Unity.Industry.Viewer.Navigation.StandardCameraControl.Shared
{
    public class StandardCamera : MonoBehaviour
    {
        public const string JoystickEnable = "JoystickEnable";
        public const float DefaultBounds = 10000f;
        
        public Camera Camera => m_Camera;
        
        private float m_MinSpeed = 1.0f;
        private float m_MaxSpeed = 10.0f;
        private float m_Acceleration = 3.0f;
        private float m_WaitingDeceleration = 4.0f;
        private float m_MovingSpeed;
        [SerializeField]
        private float maxSpeedCeiling = 9f;
        [SerializeField] [Range(0,1)]
        private float minSpeedPercentOfMaxSpeed = 0.75f;

        protected Vector3 m_MovingDirection;

        private Vector3 m_PrevPosition;
        private Quaternion m_PrevRotation;
        
        [SerializeField]
        protected Camera m_Camera;
        
        protected Vector3 m_DesiredLookAt;
        protected Vector3 m_DesiredPosition;
        protected Quaternion m_DesiredRotation;
        protected Vector2 m_DesiredRotationEuler;
        private bool m_IsSphericalMovement;
        
        [SerializeField]
        protected CameraSettings m_Settings;
        
        public CameraUtility Utility { get; protected set; }
        
        private bool m_ShouldPauseCameraControl;
        
        private Coroutine m_TranslationCoroutine;

        public Transform Transform
        {
            get
            {
                if (m_Camera == null)
                {
                    m_Camera = Camera.main;
                }
                return m_Camera.transform;
            }
        } 

        protected virtual void Awake()
        {
            Utility ??= new CameraUtility(m_Camera);

            m_DesiredLookAt = m_Settings.initialLookAt;
            m_DesiredPosition = m_Camera.transform.position;
            m_DesiredRotation = m_Camera.transform.rotation;
            m_DesiredRotationEuler = m_DesiredRotation.eulerAngles;

            m_IsSphericalMovement = false;
        }
        
        protected virtual void Start()
        {
            NavigationController.PauseCameraControl += PauseCameraControl;
        }
        
        void Update()
        {
            if(m_ShouldPauseCameraControl) return;
            // skip this update and snap cam transform if modified by an external source
            if (CheckExternalCameraMovement())
                return;
            
            var delta = Time.unscaledDeltaTime;

            if (m_MovingDirection != Vector3.zero)
            {
                var offset = m_DesiredRotation * m_MovingDirection * m_MovingSpeed * delta;

                m_DesiredPosition += offset;
                m_DesiredLookAt += offset;

                m_MovingSpeed = Mathf.Clamp(m_MovingSpeed + m_Acceleration * delta, m_MinSpeed, m_MaxSpeed);
            }
            else
            {
                if (delta < 0.1f) // Should be based on UINavigationControllerSettings' inputLagSkipThreshold, but it's not in the scope.  Using default value for now.
                {
                    m_MovingSpeed = Mathf.Clamp(m_MovingSpeed - m_WaitingDeceleration * delta, m_MinSpeed, m_MaxSpeed);
                }
                else
                {
                    m_MovingSpeed = 0;
                }
            }

            var rotation = Quaternion.Lerp(m_Camera.transform.rotation, m_DesiredRotation, Mathf.Clamp(delta / m_Settings.rotationElasticity, 0.0f, 1.0f));
            m_Camera.transform.rotation = m_PrevRotation = rotation;

            Vector3 position;
            if (m_IsSphericalMovement)
            {
                position = m_DesiredLookAt + rotation * Vector3.back * GetDistanceFromLookAt();
            }
            else
            {
                position = Vector3.Lerp(m_Camera.transform.position, m_DesiredPosition, Mathf.Clamp(delta / m_Settings.positionElasticity, 0.0f, 1.0f));
            }

            m_Camera.transform.position = m_PrevPosition = position;
        }
        
        protected virtual void OnDestroy()
        {
            Utility = null;
            NavigationController.PauseCameraControl -= PauseCameraControl;
        }

        public virtual void MoveInLocalDirection(Vector3 unitDir) { }

        public virtual void Rotate(Vector2 angleOffset) { }

        private void PauseCameraControl(bool pause)
        {
            m_ShouldPauseCameraControl = pause;
        }

        protected void UpdateSphericalMovement(bool isSphericalMovement)
        {
            m_IsSphericalMovement = isSphericalMovement;
        }
        
        public float GetDistanceFromLookAt()
        {
            return (m_DesiredLookAt - m_DesiredPosition).magnitude;
        }
        
        /// <summary>
        /// Adjusts the camera speed settings to optimally navigate an area contained within the desired <see cref="Bounds"/>.
        /// </summary>
        /// <param name="bb">The reference <see cref="Bounds"/> to navigate around/within.</param>
        public void SetCameraSpeedSettings(DoubleBounds bb)
        {
            var maxDistanceToMove = (float)math.length(bb.Size);
            //m_MinSpeed =  Mathf.Clamp(maxDistanceToMove / m_Settings.maxTimeToTravelMinSpeed * m_Settings.minSpeedScaling, clampedMinSpeed, m_MinSpeed);
            //m_MinSpeed = maxDistanceToMove / m_Settings.maxTimeToTravelMinSpeed * m_Settings.minSpeedScaling;
            m_MaxSpeed = Mathf.Min(maxDistanceToMove / maxSpeedCeiling, maxSpeedCeiling) * m_Settings.maxSpeedScaling;
            m_MinSpeed = m_MaxSpeed * minSpeedPercentOfMaxSpeed * m_Settings.minSpeedScaling;
            m_Acceleration = (m_MaxSpeed - m_MinSpeed) / m_Settings.maxTimeToAccelerate * m_Settings.accelerationScaling;
            m_WaitingDeceleration = m_Acceleration * m_Settings.waitingDecelerationScaling;
        }
        
        /// <summary>
        /// Resets the camera tracking to the desired values.
        /// </summary>
        /// <param name="pos">The desired position.</param>
        /// <param name="lookAt">The desired lookAt direction.</param>
        public void ResetTracking(Vector3 pos, Vector3 lookAt)
        {
            var rotation = Quaternion.LookRotation(lookAt - pos, Vector3.up);
            
            pos = new Vector3(
                Mathf.Clamp(pos.x, -StandardCamera.DefaultBounds, StandardCamera.DefaultBounds),
                Mathf.Clamp(pos.y, -StandardCamera.DefaultBounds, StandardCamera.DefaultBounds),
                Mathf.Clamp(pos.z, -StandardCamera.DefaultBounds, StandardCamera.DefaultBounds)
            );
            
            m_Camera.transform.SetPositionAndRotation(pos, rotation);
            m_DesiredPosition = pos;
            m_DesiredRotation = rotation;
            m_DesiredRotationEuler = rotation.eulerAngles;
            m_DesiredLookAt = lookAt;
        }
        
        bool CheckExternalCameraMovement()
        {
            var camTransform = m_Camera.transform;
            if (m_PrevPosition == camTransform.position && m_PrevRotation == camTransform.rotation)
                return false;

            m_DesiredPosition = m_PrevPosition = camTransform.position;
            m_DesiredRotation = m_PrevRotation = camTransform.rotation;
            m_DesiredRotationEuler = m_DesiredRotation.eulerAngles;

            return true;
        }

        public void FollowPresenter(GameObject presenterObject, GameObject cameraObject)
        {
            if (m_TranslationCoroutine != null)
            {
                StopCoroutine(m_TranslationCoroutine);
            }
            cameraObject.transform.SetPositionAndRotation(presenterObject.transform.position,
                presenterObject.transform.rotation);
        }

        public void TranslateTo(GameObject objectToTranslate, Vector3 targetPosition,
            Quaternion targetRotation)
        {
            if (m_TranslationCoroutine != null)
            {
                StopCoroutine(m_TranslationCoroutine);
            }

            m_TranslationCoroutine = StartCoroutine(SmoothTranslateTo());

            IEnumerator SmoothTranslateTo(float duration = 1.0f)
            {
                NavigationController.PauseCameraControl?.Invoke(true);
                Vector3 startPosition = objectToTranslate.transform.position;
                Quaternion startRotation = objectToTranslate.transform.rotation;
                float elapsedTime = 0;

                while (elapsedTime < duration)
                {
                    var finalPos = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
                    var finalRot = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / duration);
                    objectToTranslate.transform.SetPositionAndRotation(finalPos, finalRot);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                objectToTranslate.transform.SetPositionAndRotation(targetPosition, targetRotation);
                NavigationController.PauseCameraControl?.Invoke(false);
            }
        }
    }
}
