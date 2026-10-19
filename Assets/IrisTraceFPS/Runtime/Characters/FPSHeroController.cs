using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>Modular FPS controller with optional hold-to-crouch and ceiling clearance checks.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FPSHeroController : MonoBehaviour, ICombatTarget
    {
        private const float MouseLookScale = 0.08f;

        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private MonoBehaviour inputSource;

        private readonly Collider[] overlapBuffer = new Collider[32];
        private ICharacterInput input;
        private IWeaponInput weaponInput;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked;
        private bool isCrouching;
        private bool hasStandingPose;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingCameraLocalPosition;

        public Transform AimPoint => playerCamera != null ? playerCamera.transform : transform;
        public bool IsCrouching => isCrouching;

        public void Configure(CharacterDefinition characterDefinition, CharacterController controller, Camera viewCamera, MonoBehaviour inputComponent)
        {
            definition = characterDefinition;
            characterController = controller;
            playerCamera = viewCamera;
            inputSource = inputComponent;
            input = inputComponent as ICharacterInput;
            weaponInput = inputComponent as IWeaponInput;
            CaptureStandingPose();
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
            bool wantsCrouch = definition.CrouchEnabled && input.CrouchHeld;

            // Do not let the player stand up into a ceiling or other blocking collider.
            // If blocked, remain crouched and retry until the space is clear.
            if (isCrouching && !wantsCrouch && !CanStandUp())
                wantsCrouch = true;

            SetCrouching(wantsCrouch);

            Vector2 inputMove = input.Move;
            Vector3 direction = new Vector3(inputMove.x, 0f, inputMove.y);
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            float speed = isCrouching
                ? Mathf.Max(0.1f, definition.CrouchSpeed)
                : (input.SprintHeld ? definition.SprintSpeed : definition.WalkSpeed);
            Vector3 velocity = transform.TransformDirection(direction) * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (!isCrouching && characterController.isGrounded && input.JumpPressed)
                verticalVelocity = Mathf.Sqrt(definition.JumpHeight * -2f * Physics.gravity.y);

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
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

        private void SetCrouching(bool crouch)
        {
            if (!hasStandingPose || characterController == null || isCrouching == crouch)
                return;

            isCrouching = crouch;
            float targetHeight = standingHeight;
            if (crouch && definition != null && definition.CrouchEnabled)
            {
                float minimumHeight = characterController.radius * 2f + 0.02f;
                targetHeight = Mathf.Clamp(definition.CrouchedHeight, minimumHeight, standingHeight - 0.05f);
            }

            // Move the capsule center with the height change so its feet remain at the same world height.
            characterController.height = targetHeight;
            Vector3 center = standingCenter;
            center.y -= (standingHeight - targetHeight) * 0.5f;
            characterController.center = center;

            if (playerCamera != null)
            {
                Vector3 cameraPosition = standingCameraLocalPosition;
                // Keep the camera's distance from the top of the standing capsule.
                cameraPosition.y -= standingHeight - targetHeight;
                playerCamera.transform.localPosition = cameraPosition;
            }
        }

        private bool CanStandUp()
        {
            if (characterController == null || !characterController.enabled || !hasStandingPose)
                return true;

            Vector3 scale = transform.lossyScale;
            float verticalScale = Mathf.Abs(scale.y);
            float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float worldHeight = standingHeight * verticalScale;
            float worldRadius = characterController.radius * horizontalScale;
            float worldSkin = characterController.skinWidth * horizontalScale;
            float radius = Mathf.Max(0.01f, worldRadius - worldSkin * 0.5f);
            float halfHeight = Mathf.Max(worldHeight * 0.5f, radius);

            Vector3 worldCenter = transform.TransformPoint(standingCenter);
            Vector3 up = transform.up;
            // Inset the test volume slightly so the floor contact itself does not block standing.
            float verticalInset = Mathf.Max(0.01f, worldSkin * 0.25f);
            Vector3 lowerSphere = worldCenter + up * (-halfHeight + radius + verticalInset);
            Vector3 upperSphere = worldCenter + up * (halfHeight - radius - verticalInset);
            int count = Physics.OverlapCapsuleNonAlloc(lowerSphere, upperSphere, radius, overlapBuffer,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);

            // If the buffer fills, fail closed rather than risk standing through geometry.
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
        }
    }
}
