using UnityEngine;
using UnityEngine.InputSystem;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Input
{
    /// <summary>
    /// New Input System adapter. Runtime gameplay modules consume interfaces,
    /// not this concrete input implementation.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class IrisTraceInputReader : MonoBehaviour, ICharacterInput, IWeaponInput, IAbilityInput, IAbilityHoldInput
    {
        [Header("Movement bindings")]
        [SerializeField] private string moveForward = "<Keyboard>/w";
        [SerializeField] private string moveBackward = "<Keyboard>/s";
        [SerializeField] private string moveLeft = "<Keyboard>/a";
        [SerializeField] private string moveRight = "<Keyboard>/d";
        [SerializeField] private string jump = "<Keyboard>/space";
        [SerializeField] private string crouch = "<Keyboard>/leftCtrl";

        [Header("Weapon bindings")]
        [SerializeField] private string fire = "<Mouse>/leftButton";
        [SerializeField] private string reload = "<Keyboard>/r";

        [Header("Ability bindings")]
        [SerializeField] private string ability1 = "<Keyboard>/q";
        [SerializeField] private string ability2 = "<Keyboard>/e";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction crouchAction;
        private InputAction fireAction;
        private InputAction reloadAction;
        private InputAction ability1Action;
        private InputAction ability2Action;
        private InputAction cancelAction;

        public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookDelta => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => jumpAction != null && jumpAction.WasPressedThisFrame();
        public bool CrouchHeld => crouchAction != null && crouchAction.IsPressed();
        public bool CancelPressed => cancelAction != null && cancelAction.WasPressedThisFrame();
        public bool FirePressed => fireAction != null && fireAction.WasPressedThisFrame();
        public bool FireHeld => fireAction != null && fireAction.IsPressed();
        public bool FireReleased => fireAction != null && fireAction.WasReleasedThisFrame();
        public bool ReloadPressed => reloadAction != null && reloadAction.WasPressedThisFrame();
        public bool Ability1Pressed => ability1Action != null && ability1Action.WasPressedThisFrame();
        public bool Ability2Pressed => ability2Action != null && ability2Action.WasPressedThisFrame();
        public bool Ability1Held => ability1Action != null && ability1Action.IsPressed();
        public bool Ability2Held => ability2Action != null && ability2Action.IsPressed();

        // Compatibility overload for older integrations. The legacy sprint path is intentionally ignored;
        // sprint is now configured as a hold-to-activate ability in slot 1 or slot 2.
        public void Configure(
            string moveForwardPath,
            string moveBackwardPath,
            string moveLeftPath,
            string moveRightPath,
            string jumpPath,
            string legacySprintPath,
            string firePath,
            string reloadPath)
        {
            Configure(moveForwardPath, moveBackwardPath, moveLeftPath, moveRightPath,
                jumpPath, firePath, reloadPath,
                "<Keyboard>/q", "<Keyboard>/e", "<Keyboard>/leftCtrl");
        }

        // Primary configuration: no Sprint binding in the default control set.
        public void Configure(
            string moveForwardPath,
            string moveBackwardPath,
            string moveLeftPath,
            string moveRightPath,
            string jumpPath,
            string firePath,
            string reloadPath,
            string ability1Path,
            string ability2Path,
            string crouchPath)
        {
            moveForward = SafePath(moveForwardPath, "<Keyboard>/w");
            moveBackward = SafePath(moveBackwardPath, "<Keyboard>/s");
            moveLeft = SafePath(moveLeftPath, "<Keyboard>/a");
            moveRight = SafePath(moveRightPath, "<Keyboard>/d");
            jump = SafePath(jumpPath, "<Keyboard>/space");
            crouch = SafePath(crouchPath, "<Keyboard>/leftCtrl");
            fire = SafePath(firePath, "<Mouse>/leftButton");
            reload = SafePath(reloadPath, "<Keyboard>/r");
            ability1 = SafePath(ability1Path, "<Keyboard>/q");
            ability2 = SafePath(ability2Path, "<Keyboard>/e");

            if (Application.isPlaying && isActiveAndEnabled)
                CreateActions();
        }

        // Compatibility overload for projects which serialized the previous 11-argument configuration.
        public void Configure(
            string moveForwardPath,
            string moveBackwardPath,
            string moveLeftPath,
            string moveRightPath,
            string jumpPath,
            string legacySprintPath,
            string firePath,
            string reloadPath,
            string ability1Path,
            string ability2Path,
            string crouchPath)
        {
            Configure(moveForwardPath, moveBackwardPath, moveLeftPath, moveRightPath,
                jumpPath, firePath, reloadPath, ability1Path, ability2Path, crouchPath);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                CreateActions();
        }

        private void OnDisable() => DisposeActions();

        private void CreateActions()
        {
            DisposeActions();

            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", SafePath(moveForward, "<Keyboard>/w"))
                .With("Down", SafePath(moveBackward, "<Keyboard>/s"))
                .With("Left", SafePath(moveLeft, "<Keyboard>/a"))
                .With("Right", SafePath(moveRight, "<Keyboard>/d"));

            lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlType: "Vector2");
            jumpAction = new InputAction("Jump", InputActionType.Button, SafePath(jump, "<Keyboard>/space"));
            crouchAction = new InputAction("Crouch", InputActionType.Button, SafePath(crouch, "<Keyboard>/leftCtrl"));
            fireAction = new InputAction("Fire", InputActionType.Button, SafePath(fire, "<Mouse>/leftButton"));
            reloadAction = new InputAction("Reload", InputActionType.Button, SafePath(reload, "<Keyboard>/r"));
            ability1Action = new InputAction("Ability1", InputActionType.Button, SafePath(ability1, "<Keyboard>/q"));
            ability2Action = new InputAction("Ability2", InputActionType.Button, SafePath(ability2, "<Keyboard>/e"));
            cancelAction = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");

            moveAction.Enable();
            lookAction.Enable();
            jumpAction.Enable();
            crouchAction.Enable();
            fireAction.Enable();
            reloadAction.Enable();
            ability1Action.Enable();
            ability2Action.Enable();
            cancelAction.Enable();
        }

        private void DisposeActions()
        {
            DisposeAction(ref moveAction);
            DisposeAction(ref lookAction);
            DisposeAction(ref jumpAction);
            DisposeAction(ref crouchAction);
            DisposeAction(ref fireAction);
            DisposeAction(ref reloadAction);
            DisposeAction(ref ability1Action);
            DisposeAction(ref ability2Action);
            DisposeAction(ref cancelAction);
        }

        private static void DisposeAction(ref InputAction action)
        {
            if (action == null)
                return;

            action.Disable();
            action.Dispose();
            action = null;
        }

        private static string SafePath(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
