using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Cloud.DataStreaming.Runtime;
using Unity.Cloud.HighPrecision.Runtime;
using UnityEngine;

namespace Unity.Industry.Viewer.VR.FlyMode
{
    public partial class VRMovementController
    {
        [SerializeField, Header("Navigation Mode")]
        private VRNavigationMode m_CurrentNavigationMode = VRNavigationMode.Fly;

        #region Walk Mode Settings

        [Header("Walk Settings")]
        private float m_WalkSpeed = 1f;
        private float m_MaxWalkSpeed = 3f;
        [Tooltip("Height above feet to fire ground detection ray downward.")]
        private float m_GroundedRayLength = 1.5f;
        private float m_StepUpSpeed = 0.8f;
        private float m_MaxFallSpeed = -30f;

        #endregion

        #region Walk Mode State

        public VRNavigationMode CurrentNavigationMode => m_CurrentNavigationMode;

        // Walk mode runtime state
        private bool m_WalkModeEnabled;
        private bool m_IsHovering;
        private bool m_IsGrounded;
        private RaycastResult m_WalkGroundRayResult;
        private Task m_WalkGroundRaycastTask;

        // Movement physics
        private float m_VerticalVelocity;
        private float m_CurrentWalkSpeed;
        private float m_StepUpTime;
        private bool m_GoUpStep;

        // Constants
        private const float k_GroundedEpsilon = 0.05f;
        private const float k_PlayerHeight = 1.7f;

        #endregion

        #region Collision Detection

        public bool CollisionDetection { get; private set; }

        // Collision detection state
        private Task m_CollisionRaycastTask;
        private bool m_CollisionDetected;
        private float m_CollisionDistance;
        private Vector3 m_LastCollisionDirection; // Direction of last collision raycast

        // Collision detection constants
        private const float k_CollisionRaycastDistance = 10f;
        private const float k_CollisionBuffer = 0.5f; // Minimum distance to maintain from obstacles (increased for async latency)
        private const float k_DirectionSimilarityThreshold = 0.5f; // Dot product threshold for "similar" direction

        // Ray grid settings
        [SerializeField, Header("Walk Mode Collision Ray Grid")]
        private int m_CollisionRaysHorizontal = 1; // columns (side-to-side)

        [SerializeField]
        private int m_CollisionRaysVertical = 1; // rows (low to high)

        [SerializeField, Tooltip("Total side-to-side width of the ray grid (metres).")]
        private float m_CollisionBodyWidth = 0.4f;

        [SerializeField, Tooltip("Height above feet for the bottom row of collision rays.")]
        private float m_CollisionBodyHeightMin = 0.4f;

        [SerializeField, Tooltip("Height above feet for the top row of collision rays.")]
        private float m_CollisionBodyHeightMax = 0.8f;

        public void UpdateCollisionDetection(bool value)
        {
            CollisionDetection = value;
        }

        /// <summary>
        /// Reset collision state (call after teleport or mode change).
        /// </summary>
        private void ResetCollisionState()
        {
            m_CollisionDetected = false;
            m_CollisionDistance = k_CollisionRaycastDistance;
            m_LastCollisionDirection = Vector3.zero;
        }

        /// <summary>
        /// Check for collision in the given movement direction.
        /// Returns the safe movement vector (may be zero or reduced if collision detected).
        /// </summary>
        private Vector3 CheckCollisionAndGetSafeMovement(Vector3 currentPosition, Vector3 moveDirection, float moveDistance)
        {
            if (!CollisionDetection || m_StreamingModelController?.Stage == null)
            {
                return moveDirection * moveDistance;
            }

            var normalizedMoveDir = moveDirection.normalized;

            // Check if we have a valid collision result for a similar direction
            bool hasRelevantResult = m_LastCollisionDirection.sqrMagnitude > 0.001f &&
                Vector3.Dot(normalizedMoveDir, m_LastCollisionDirection) > k_DirectionSimilarityThreshold;

            // Fire new raycast if previous completed
            if (m_CollisionRaycastTask == null || m_CollisionRaycastTask.IsCompleted)
            {
                _ = FireCollisionRaycastAsync(currentPosition, moveDirection);
            }

            // If we don't have a relevant result for this direction, allow movement
            // (first frame in new direction - raycast is in flight)
            if (!hasRelevantResult)
            {
                return moveDirection * moveDistance;
            }

            // We have a result for this direction - check if safe to move
            if (m_CollisionDetected)
            {
                // If we're within the buffer zone, stop completely
                if (m_CollisionDistance <= k_CollisionBuffer)
                {
                    return Vector3.zero;
                }

                // Calculate safe distance (leave buffer space)
                var safeDistance = m_CollisionDistance - k_CollisionBuffer;

                // Limit movement to safe distance
                if (moveDistance > safeDistance)
                {
                    return moveDirection * Mathf.Max(0f, safeDistance);
                }
            }

            return moveDirection * moveDistance;
        }

        private async Task FireCollisionRaycastAsync(Vector3 origin, Vector3 direction)
        {
            if (m_StreamingModelController?.Stage == null) return;

            try
            {
                m_CollisionRaycastTask = PerformCollisionRaycastGrid(origin, direction);
                await m_CollisionRaycastTask;
            }
            catch (ObjectDisposedException)
            {
                // Stage disposed during raycast - ignore
            }
            catch (InvalidOperationException)
            {
                // StreamingObjectPool disposed - ignore
            }
            catch (Exception e)
            {
                if (m_StreamingModelController?.Stage != null)
                {
                    Debug.LogError($"[VR Collision] Raycast failed: {e.Message}");
                }
            }
        }

        private async Task PerformCollisionRaycastGrid(Vector3 origin, Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                m_CollisionDetected = false;
                return;
            }

            var normalizedDir = direction.normalized;

            // Compute the right vector perpendicular to movement direction in the horizontal plane
            var rightVec = Vector3.Cross(Vector3.up, normalizedDir);
            if (rightVec.sqrMagnitude < 0.001f)
                rightVec = Vector3.right; // fallback if direction is near-vertical

            rightVec.Normalize();

            int rows = Mathf.Max(1, m_CollisionRaysVertical);
            int cols = Mathf.Max(1, m_CollisionRaysHorizontal);

            // Build and fire all grid rays concurrently
            var tasks = new List<Task<RaycastResult>>(rows * cols);
            var rayOrigins = new List<Vector3>(rows * cols);

            for (int row = 0; row < rows; row++)
            {
                float heightT = rows > 1 ? (float)row / (rows - 1) : 0.5f;
                float height = Mathf.Lerp(m_CollisionBodyHeightMin, m_CollisionBodyHeightMax, heightT);

                for (int col = 0; col < cols; col++)
                {
                    float lateralT = cols > 1 ? (float)col / (cols - 1) - 0.5f : 0f;
                    float lateral = lateralT * m_CollisionBodyWidth;

                    var rayOrigin = origin + Vector3.up * height + rightVec * lateral;
                    var ray = new Ray(rayOrigin, normalizedDir);

                    rayOrigins.Add(rayOrigin);
                    tasks.Add(m_StreamingModelController.Stage.RaycastAsync(
                        (DoubleRay)ray,
                        k_CollisionRaycastDistance,
                        RaycastOptions.ExcludeHiddenInstances
                    ));
                }
            }

            // If either grid dimension is even, the exact body center is not covered
            // by the regular grid. Add one extra center ray so thin obstacles (e.g.
            // a narrow pillar) directly ahead are always detected.
            if (cols % 2 == 0 || rows % 2 == 0)
            {
                float centerHeight = Mathf.Lerp(m_CollisionBodyHeightMin, m_CollisionBodyHeightMax, 0.5f);
                var centerRayOrigin = origin + Vector3.up * centerHeight;
                rayOrigins.Add(centerRayOrigin);
                tasks.Add(m_StreamingModelController.Stage.RaycastAsync(
                    (DoubleRay)new Ray(centerRayOrigin, normalizedDir),
                    k_CollisionRaycastDistance,
                    RaycastOptions.ExcludeHiddenInstances
                ));
            }

            var results = await Task.WhenAll(tasks);

            // Store the direction this grid was fired in
            m_LastCollisionDirection = normalizedDir;

            // Find the closest hit across all rays
            bool anyHit = false;
            float minDistance = k_CollisionRaycastDistance;

            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var rayOrigin = rayOrigins[i];

                if (result.HasIntersected)
                {
                    anyHit = true;
                    float dist = (float)result.Distance;
                    if (dist < minDistance)
                        minDistance = dist;

                    // Visualize: colour by proximity to buffer
                    var hitPoint = rayOrigin + normalizedDir * dist;
                    var rayColor = dist <= k_CollisionBuffer ? Color.red : Color.yellow;
                    Debug.DrawLine(rayOrigin, hitPoint, rayColor, 0.1f);
                }
                else
                {
                    // Visualize: green ? no obstacle in this ray's path
                    Debug.DrawLine(rayOrigin, rayOrigin + normalizedDir * k_CollisionRaycastDistance, Color.green, 0.1f);
                }
            }

            m_CollisionDetected = anyHit;
            m_CollisionDistance = anyHit ? minDistance : k_CollisionRaycastDistance;
        }

        #endregion

        #region Mode Initialization

        /// <summary>
        /// Initialize navigation mode based on Inspector configuration.
        /// Called from OnEnable.
        /// </summary>
        private void InitializeNavigationMode()
        {
            if (m_CurrentNavigationMode == VRNavigationMode.Walk)
            {
                InitializeWalkMode();
            }
            else
            {
                InitializeFlyMode();
            }
        }

        private void InitializeFlyMode()
        {
            // Fly mode uses DynamicMoveProvider for movement
            if (m_DynamicMoveProvider != null)
            {
                m_DynamicMoveProvider.enabled = true;
            }

            // Ensure walk mode state is reset
            m_WalkModeEnabled = false;
            m_IsHovering = true;
            m_IsGrounded = false;
            m_VerticalVelocity = 0f;
        }

        private void InitializeWalkMode()
        {
            m_WalkModeEnabled = true;
            m_IsHovering = true;
            m_IsGrounded = false;
            m_VerticalVelocity = 0f;
            m_CurrentWalkSpeed = m_WalkSpeed;
            m_StepUpTime = 0f;
            m_GoUpStep = false;

            // Disable DynamicMoveProvider - we handle movement ourselves
            if (m_DynamicMoveProvider != null)
            {
                m_DynamicMoveProvider.enabled = false;
            }

            // Fire initial ground detection
            _ = FireGroundRaycastAsync();
        }

        #endregion

        #region Walk Mode Update

        private void UpdateWalkMode()
        {
            if (!m_WalkModeEnabled || m_IsTeleporting) return;

            // Fire ground raycast if previous completed
            if (m_WalkGroundRaycastTask == null || m_WalkGroundRaycastTask.IsCompleted)
            {
                _ = FireGroundRaycastAsync();
            }

            // Handle vertical movement (gravity)
            HandleVerticalMovement();

            // Handle step-up
            HandleWalkStepUp();

            // Handle horizontal movement
            HandleHorizontalMovement();
        }

        private void OnTeleportCompleteWalkMode()
        {
            // Reset walk state after teleport
            m_VerticalVelocity = 0f;
            m_IsGrounded = false;
            m_IsHovering = true;
            m_StepUpTime = 0f;
            m_GoUpStep = false;

            // Reset collision state so we don't get stuck after teleport
            ResetCollisionState();

            // Immediately fire ground detection
            _ = FireGroundRaycastAsync();
        }

        #endregion

        #region Ground Detection

        // Matching desktop WalkModeMoveController pattern exactly
        private const float k_RaycastDistance = 10f;

        private async Task FireGroundRaycastAsync()
        {
            if (m_StreamingModelController?.Stage == null)
            {
                return;
            }

            try
            {
                m_WalkGroundRaycastTask = PerformGroundRaycastAndProcessState();
                await m_WalkGroundRaycastTask;
            }
            catch (ObjectDisposedException)
            {
                // Stage was disposed during raycast (e.g., switching modes or unloading model)
                // This is expected behavior, ignore silently
            }
            catch (InvalidOperationException)
            {
                // StreamingObjectPool disposed - expected when switching modes
                // Ignore silently
            }
            catch (Exception e)
            {
                // Only log unexpected errors
                if (m_WalkModeEnabled && m_StreamingModelController?.Stage != null)
                {
                    Debug.LogError($"[VR Walk] Ground raycast failed: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Get the position to fire the downcast ray from.
        /// Matching desktop WalkModeMoveController.GetDownCastFirePosition() pattern.
        /// </summary>
        private Vector3 GetDownCastFirePosition()
        {
            var currentPos = m_XROrigin.transform.position;
            // XR Origin position is at feet level, so we fire from feet + ground detection distance
            currentPos.y = currentPos.y + m_GroundedRayLength;
            return currentPos;
        }

        private async Task PerformGroundRaycastAndProcessState()
        {
            var currentPosition = m_XROrigin.transform.position;
            var feetY = currentPosition.y;

            // Fire ray from above feet position downward (matching desktop pattern)
            var rayOrigin = GetDownCastFirePosition();
            var ray = new Ray(rayOrigin, Vector3.down);

            // Cast down 10 meters (matching desktop)
            m_WalkGroundRayResult = await m_StreamingModelController.Stage.RaycastAsync(
                (DoubleRay)ray,
                k_RaycastDistance,
                RaycastOptions.ExcludeHiddenInstances
            );

            // Process ground state immediately after raycast completes (matching desktop pattern)
            if (m_WalkGroundRayResult.HasIntersected)
            {
                // Ground detected - matching desktop GetGroundAsync pattern
                m_IsHovering = false;
                m_IsGrounded = feetY <= (float)m_WalkGroundRayResult.Point.y + k_GroundedEpsilon;

                // Check for step-up: ray hit closer than expected = ground is above us
                // (matching desktop: m_GroundRayResult.Distance < m_GroundedRayLength - 0.05f)
                m_GoUpStep = (float)m_WalkGroundRayResult.Distance < m_GroundedRayLength - k_GroundedEpsilon;
                if (!m_GoUpStep)
                {
                    m_StepUpTime = 0f;
                }

            }
            else
            {
                // No ground detected - hover mode (no gravity)
                m_IsHovering = true;
                m_IsGrounded = false;
                m_GoUpStep = false;

            }
        }

        #endregion

        #region Vertical Movement

        private void HandleVerticalMovement()
        {
            // Matching desktop WalkModeMoveController.HandleGround() pattern
            if (m_WalkGroundRayResult.HasIntersected && m_IsGrounded && m_VerticalVelocity <= 0)
            {
                // On ground - no falling
                m_VerticalVelocity = 0f;
            }
            else if (!m_WalkGroundRayResult.HasIntersected && m_VerticalVelocity <= 0)
            {
                // No ground detected - hover (no gravity)
                m_VerticalVelocity = 0f;
            }
            else if (m_IsHovering)
            {
                // Hovering mode - no gravity
                m_VerticalVelocity = 0f;
            }
            else
            {
                // Falling - apply gravity
                m_VerticalVelocity += Physics.gravity.y * Time.deltaTime;
                m_VerticalVelocity = Mathf.Clamp(m_VerticalVelocity, m_MaxFallSpeed, -m_MaxFallSpeed);
            }

            // Apply vertical movement
            if (m_VerticalVelocity != 0f)
            {
                var verticalMovement = new Vector3(0f, m_VerticalVelocity * Time.deltaTime, 0f);
                m_XROrigin.transform.position += verticalMovement;

                // Clamp to ground if we've reached it
                if (m_WalkGroundRayResult.HasIntersected)
                {
                    var groundY = (float)m_WalkGroundRayResult.Point.y;
                    if (m_XROrigin.transform.position.y < groundY)
                    {
                        var pos = m_XROrigin.transform.position;
                        pos.y = groundY;
                        m_XROrigin.transform.position = pos;
                        m_VerticalVelocity = 0f;
                        m_IsGrounded = true;
                    }
                }
            }
        }

        private void HandleWalkStepUp()
        {
            // Matching desktop WalkModeMoveController.HandleStepUp() pattern
            m_StepUpTime += Time.deltaTime * m_StepUpSpeed;

            if (m_GoUpStep && m_WalkGroundRayResult.HasIntersected)
            {
                var stepUpDistance = m_GroundedRayLength - (float)m_WalkGroundRayResult.Distance;
                if (stepUpDistance > 0)
                {
                    var stepAmount = Mathf.Lerp(0f, stepUpDistance, m_StepUpTime);
                    m_XROrigin.transform.position += new Vector3(0f, stepAmount, 0f);
                }
            }

            m_GoUpStep = false;
        }

        #endregion

        #region Horizontal Movement

        private void HandleHorizontalMovement()
        {
            // Read joystick input
            var joystickInput = m_LeftHandJoystickInputReference.action.ReadValue<Vector2>();

            if (joystickInput.sqrMagnitude < 0.01f)
            {
                // No input - reset speed
                m_CurrentWalkSpeed = m_WalkSpeed;
                return;
            }

            // Get head-relative direction
            var headForward = m_XROrigin.Camera.transform.forward;
            headForward.y = 0;
            headForward.Normalize();

            var headRight = m_XROrigin.Camera.transform.right;
            headRight.y = 0;
            headRight.Normalize();

            // Calculate move direction
            var moveDirection = (headForward * joystickInput.y) + (headRight * joystickInput.x);
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            // Project onto ground plane if grounded
            if (m_IsGrounded && m_WalkGroundRayResult.HasIntersected)
            {
                var groundNormal = (Vector3)m_WalkGroundRayResult.Normal;
                moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
            }

            // Handle speed progression (same as fly mode)
            float magnitude = joystickInput.magnitude;
            if (magnitude >= k_JoystickFullPushThreshold)
            {
                float incrementPerSecond = (m_MaxWalkSpeed - m_WalkSpeed) / m_MaxTimeToTravelFullSpeed * MoveSensitivity;
                m_CurrentWalkSpeed = Mathf.MoveTowards(
                    m_CurrentWalkSpeed,
                    m_MaxWalkSpeed,
                    incrementPerSecond * Time.deltaTime
                );
            }
            else
            {
                m_CurrentWalkSpeed = m_WalkSpeed;
            }

            // Calculate intended movement
            var moveDistance = m_CurrentWalkSpeed * MoveSensitivity * Time.deltaTime;

            // Check collision and get safe movement
            var movement = CheckCollisionAndGetSafeMovement(
                m_XROrigin.transform.position,
                moveDirection,
                moveDistance
            );

            // Apply movement
            m_XROrigin.transform.position += movement;
        }

        #endregion
    }
}
