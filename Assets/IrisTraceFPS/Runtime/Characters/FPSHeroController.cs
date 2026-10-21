using System;
using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>
    /// Modular FPS controller with acceleration, limited air control, smooth crouching,
    /// ceiling clearance checks, and lifecycle events for optional audio/animation modules.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FPSHeroController : MonoBehaviour, ICombatTarget
    {
        private const float MouseLookScale = 0.08f;
        private const float MovementThresholdSqr = 0.0001f;
        private const float HeightEpsilon = 0.001f;

        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private MonoBehaviour inputSource;

        private readonly Collider[] overlapBuffer = new Collider[32];
        private ICharacterInput input;
        private IWeaponInput weaponInput;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked;
        private bool isCrouching;
        private bool movementEventActive;
        private bool hasStandingPose;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingCameraLocalPosition;

        public Transform AimPoint => playerCamera != null ? playerCamera.transform : transform;
        public bool IsCrouching => isCrouching;
        public bool IsMoving => movementEventActive;
        public Vector3 HorizontalVelocity => horizontalVelocity;

        /// <summary>Raised when the character begins actual horizontal movement.</summary>
        public event Action MovementStarted;
        /// <summary>Raised when actual horizontal movement stops.</summary>
        public event Action MovementStopped;
        /// <summary>Raised when the character begins crouching.</summary>
        public event Action CrouchStarted;
        /// <summary>Raised when the character finishes standing up.</summary>
        public event Action CrouchEnded;
        /// <summary>Raised when a grounded jump is accepted.</summary>
        public event Action Jumped;
        /// <summary>Raised when the character lands after being airborne.</summary>
        public event Action Landed;

        public void Configure(CharacterDefinition characterDefinition, CharacterController controller, Camera viewCamera, MonoBehaviour inputComponent)
        {
            definition = characterDefinition;
            characterController = controller;
            playerCamera = viewCamera;
            inputSource = inputComponent;
            input = inputComponent as ICharacterInput;
            weaponInput = inputComponent as IWeaponInput;
            CaptureStandingPose();
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>(true);
            if (input == null && inputSource != null)
                input = inputSource as ICharacterInput;
            if (weaponInput == null && inputSource != null)
                weaponInput = inputSource as IWeaponInput;

            if (input == null || weaponInput == null)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (input == null && behaviour is ICharacterInput characterInput)
                    {
                        input = characterInput;
                        inputSource = behaviour;
                    }

                    if (weaponInput == null && behaviour is IWeaponInput characterWeaponInput)
                        weaponInput = characterWeaponInput;

                    if (input != null && weaponInput != null)
                        break;
                }
            }

            CaptureStandingPose();
        }

        private void Start() => LockCursor();

        private void Update()
        {
            if (characterController == null || definition == null || input == null)
                return;

            HandleCursor();
            if (cursorLocked)
                HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            if (input.CancelPressed)
            {
                UnlockCursor();
                return;
            }

            // Fire is part of IWeaponInput, not ICharacterInput.
            if (!cursorLocked && weaponInput != null && weaponInput.FirePressed)
                LockCursor();
        }

        private void HandleLook()
        {
            if (playerCamera == null)
                return;

            Vector2 look = input.LookDelta * definition.MouseSensitivity * MouseLookScale;
            transform.Rotate(Vector3.up * look.x);
            cameraPitch = Mathf.Clamp(cameraPitch - look.y, -85f, 85f);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            bool wantsCrouch = definition.CrouchEnabled && input.CrouchHeld;
            UpdateCrouchTransition(wantsCrouch, deltaTime);

            Vector2 inputMove = input.Move;
            Vector3 localDirection = new Vector3(inputMove.x, 0f, inputMove.y);
            if (localDirection.sqrMagnitude > 1f)
                localDirection.Normalize();

            bool movementRequested = localDirection.sqrMagnitude > MovementThresholdSqr;
            bool useCrouchSpeed = isCrouching || wantsCrouch;
            float targetSpeed = useCrouchSpeed
                ? Mathf.Max(0.1f, definition.CrouchSpeed)
                : (input.SprintHeld ? definition.SprintSpeed : definition.WalkSpeed);

            Vector3 desiredHorizontalVelocity = transform.TransformDirection(localDirection) * targetSpeed;
            bool groundedBeforeMove = characterController.isGrounded;
            float airControl = Mathf.Clamp01(definition.AirControl);
            float acceleration = movementRequested
                ? Mathf.Max(0.1f, definition.GroundAcceleration)
                : Mathf.Max(0.1f, definition.GroundDeceleration);

            // In the air, input changes are weaker. With air control = 0, horizontal momentum is preserved.
            if (!groundedBeforeMove)
                acceleration *= airControl;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                desiredHorizontalVelocity,
                acceleration * deltaTime);

            if (groundedBeforeMove && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (!isCrouching && groundedBeforeMove && input.JumpPressed)
            {
                float gravityMagnitude = Mathf.Max(0.01f, -Physics.gravity.y);
                verticalVelocity = Mathf.Sqrt(Mathf.Max(0.1f, definition.JumpHeight) * 2f * gravityMagnitude);
                Jumped?.Invoke();
            }

            verticalVelocity += Physics.gravity.y * deltaTime;
            Vector3 movement = horizontalVelocity;
            movement.y = verticalVelocity;

            Vector3 previousPosition = transform.position;
            characterController.Move(movement * deltaTime);

            bool groundedAfterMove = characterController.isGrounded;
            if (!groundedBeforeMove && groundedAfterMove)
            {
                verticalVelocity = -2f;
                Landed?.Invoke();
            }

            Vector3 actualDelta = transform.position - previousPosition;
            actualDelta.y = 0f;
            UpdateMovementEvents(actualDelta.sqrMagnitude > MovementThresholdSqr, movementRequested);
        }

        private void UpdateMovementEvents(bool movedThisFrame, bool movementRequested)
        {
            // Do not emit MovementStarted while the controller is being pushed solely by residual velocity.
            bool shouldBeActive = movedThisFrame && (movementRequested || movementEventActive);
            if (shouldBeActive == movementEventActive)
                return;

            movementEventActive = shouldBeActive;
            if (movementEventActive)
                MovementStarted?.Invoke();
            else
                MovementStopped?.Invoke();
        }

        private void CaptureStandingPose()
        {
            if (characterController == null)
                return;

            standingHeight = characterController.height;
            standingCenter = characterController.center;
            if (playerCamera != null)
                standingCameraLocalPosition = playerCamera.transform.localPosition;
            hasStandingPose = true;
            isCrouching = false;
        }

        private void UpdateCrouchTransition(bool wantsCrouch, float deltaTime)
        {
            if (!hasStandingPose || characterController == null || !characterController.enabled)
                return;

            float minimumHeight = characterController.radius * 2f + 0.02f;
            float crouchedHeight = Mathf.Clamp(definition.CrouchedHeight, minimumHeight, standingHeight - 0.05f);
            float targetHeight = wantsCrouch ? crouchedHeight : standingHeight;
            float currentHeight = characterController.height;
            float transitionDistance = Mathf.Max(0.05f, standingHeight - crouchedHeight);
            float duration = Mathf.Max(0.05f, definition.CrouchTransitionDuration);
            float transitionSpeed = transitionDistance / duration;
            float nextHeight = Mathf.MoveTowards(currentHeight, targetHeight, transitionSpeed * deltaTime);

            // Shrinking is always safe. Growing is allowed only after checking the complete proposed capsule.
            if (nextHeight > currentHeight + HeightEpsilon && !CanOccupyHeight(nextHeight))
                nextHeight = currentHeight;

            if (Mathf.Abs(nextHeight - currentHeight) > HeightEpsilon)
                ApplyCharacterHeight(nextHeight);

            bool crouchingNow = characterController.height < standingHeight - 0.01f;
            if (crouchingNow == isCrouching)
                return;

            isCrouching = crouchingNow;
            if (isCrouching)
                CrouchStarted?.Invoke();
            else
                CrouchEnded?.Invoke();
        }

        private void ApplyCharacterHeight(float height)
        {
            float clampedHeight = Mathf.Clamp(height, characterController.radius * 2f + 0.02f, standingHeight);
            characterController.height = clampedHeight;

            // Keep the capsule's feet at the same world height as it transitions.
            Vector3 center = standingCenter;
            center.y -= (standingHeight - clampedHeight) * 0.5f;
            characterController.center = center;

            if (playerCamera != null)
            {
                Vector3 cameraPosition = standingCameraLocalPosition;
                cameraPosition.y -= standingHeight - clampedHeight;
                playerCamera.transform.localPosition = cameraPosition;
            }
        }

        private bool CanOccupyHeight(float targetHeight)
        {
            if (characterController == null || !characterController.enabled || !hasStandingPose)
                return true;

            Vector3 scale = transform.lossyScale;
            float verticalScale = Mathf.Abs(scale.y);
            float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float worldHeight = targetHeight * verticalScale;
            float worldRadius = characterController.radius * horizontalScale;
            float worldSkin = characterController.skinWidth * horizontalScale;
            float radius = Mathf.Max(0.01f, worldRadius - worldSkin * 0.5f);
            float halfHeight = Mathf.Max(worldHeight * 0.5f, radius);

            // Use the standing center as the reference so the feet remain anchored during resizing.
            Vector3 worldCenter = transform.TransformPoint(standingCenter);
            Vector3 up = transform.up;
            float verticalInset = Mathf.Max(0.01f, worldSkin * 0.25f);
            Vector3 lowerSphere = worldCenter + up * (-halfHeight + radius + verticalInset);
            Vector3 upperSphere = worldCenter + up * (halfHeight - radius - verticalInset);
            int count = Physics.OverlapCapsuleNonAlloc(
                lowerSphere, upperSphere, radius, overlapBuffer, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            // Fail closed if the buffer fills rather than letting the character expand into geometry.
            if (count >= overlapBuffer.Length)
                return false;

            for (int i = 0; i < count; i++)
            {
                Collider hit = overlapBuffer[i];
                if (hit == null)
                    continue;

                Transform hitTransform = hit.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                return false;
            }

            return true;
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorLocked = false;
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                UnlockCursor();

            if (movementEventActive)
            {
                movementEventActive = false;
                MovementStopped?.Invoke();
            }
        }
    }
}
