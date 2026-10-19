using UnityEngine;
using UnityEngine.InputSystem;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Input
{
    /// <summary>
    /// New Input System adapter. Bindings are stored as Input System control paths
    /// for keyboard keys and mouse buttons.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class IrisTraceInputReader : MonoBehaviour, ICharacterInput, IWeaponInput
    {
        [Header("Movement bindings")]
        [SerializeField] private string moveForward = "<Keyboard>/w";
        [SerializeField] private string moveBackward = "<Keyboard>/s";
        [SerializeField] private string moveLeft = "<Keyboard>/a";
        [SerializeField] private string moveRight = "<Keyboard>/d";
        [SerializeField] private string jump = "<Keyboard>/space";
        [SerializeField] private string sprint = "<Keyboard>/leftShift";

        [Header("Weapon bindings")]
        [SerializeField] private string fire = "<Mouse>/leftButton";
        [SerializeField] private string reload = "<Keyboard>/r";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction fireAction;
        private InputAction reloadAction;
        private InputAction cancelAction;

        public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookDelta => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => jumpAction != null && jumpAction.WasPressedThisFrame();
        public bool SprintHeld => sprintAction != null && sprintAction.IsPressed();
        public bool CancelPressed => cancelAction != null && cancelAction.WasPressedThisFrame();
        public bool FirePressed => fireAction != null && fireAction.WasPressedThisFrame();
        public bool FireHeld => fireAction != null && fireAction.IsPressed();
        public bool FireReleased => fireAction != null && fireAction.WasReleasedThisFrame();
        public bool ReloadPressed => reloadAction != null && reloadAction.WasPressedThisFrame();

        public void Configure(
            string moveForwardPath,
            string moveBackwardPath,
            string moveLeftPath,
            string moveRightPath,
            string jumpPath,
            string sprintPath,
            string firePath,
            string reloadPath)
        {
            moveForward = SafePath(moveForwardPath, "<Keyboard>/w");
            moveBackward = SafePath(moveBackwardPath, "<Keyboard>/s");
            moveLeft = SafePath(moveLeftPath, "<Keyboard>/a");
            moveRight = SafePath(moveRightPath, "<Keyboard>/d");
            jump = SafePath(jumpPath, "<Keyboard>/space");
            sprint = SafePath(sprintPath, "<Keyboard>/leftShift");
            fire = SafePath(firePath, "<Mouse>/leftButton");
            reload = SafePath(reloadPath, "<Keyboard>/r");

            if (Application.isPlaying && isActiveAndEnabled)
                CreateActions();
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
            sprintAction = new InputAction("Sprint", InputActionType.Button, SafePath(sprint, "<Keyboard>/leftShift"));
            fireAction = new InputAction("Fire", InputActionType.Button, SafePath(fire, "<Mouse>/leftButton"));
            reloadAction = new InputAction("Reload", InputActionType.Button, SafePath(reload, "<Keyboard>/r"));
            cancelAction = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");

            moveAction.Enable();
            lookAction.Enable();
            jumpAction.Enable();
            sprintAction.Enable();
            fireAction.Enable();
            reloadAction.Enable();
            cancelAction.Enable();
        }

        private void DisposeActions()
        {
            DisposeAction(ref moveAction);
            DisposeAction(ref lookAction);
            DisposeAction(ref jumpAction);
            DisposeAction(ref sprintAction);
            DisposeAction(ref fireAction);
            DisposeAction(ref reloadAction);
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
