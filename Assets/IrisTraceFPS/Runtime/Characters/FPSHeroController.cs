using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>Minimal FPS movement/controller; consumes an input contract, not a concrete input package.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FPSHeroController : MonoBehaviour
    {
        private const float MouseLookScale = 0.08f;

        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private MonoBehaviour inputSource;

        private ICharacterInput input;
        private IWeaponInput weaponInput;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked;

        public void Configure(CharacterDefinition characterDefinition, CharacterController controller, Camera viewCamera, MonoBehaviour inputComponent)
        {
            definition = characterDefinition;
            characterController = controller;
            playerCamera = viewCamera;
            inputSource = inputComponent;
            input = inputComponent as ICharacterInput;
            weaponInput = inputComponent as IWeaponInput;
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

            // The fire action belongs to IWeaponInput, not ICharacterInput.
            // Keep the contracts separate while allowing the same input adapter to provide both.
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
            Vector2 inputMove = input.Move;
            Vector3 direction = new Vector3(inputMove.x, 0f, inputMove.y);
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            float speed = input.SprintHeld ? definition.SprintSpeed : definition.WalkSpeed;
            Vector3 velocity = transform.TransformDirection(direction) * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (characterController.isGrounded && input.JumpPressed)
                verticalVelocity = Mathf.Sqrt(definition.JumpHeight * -2f * Physics.gravity.y);

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
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
