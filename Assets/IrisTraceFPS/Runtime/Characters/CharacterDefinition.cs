using UnityEngine;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>Saved configuration for a generated IrisTrace FPS character.</summary>
    [CreateAssetMenu(menuName = "IrisTrace FPS/Character Definition", fileName = "NewCharacter")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField] private string displayName = "New FPS Hero";

        [Header("Vitals")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;

        [Header("Camera")]
        [SerializeField, Min(0.1f)] private float mouseSensitivity = 1f;
        [SerializeField, Range(60f, 120f)] private float cameraFieldOfView = 90f;

        [Header("Input System bindings")]
        [SerializeField] private string moveForwardBinding = "<Keyboard>/w";
        [SerializeField] private string moveBackwardBinding = "<Keyboard>/s";
        [SerializeField] private string moveLeftBinding = "<Keyboard>/a";
        [SerializeField] private string moveRightBinding = "<Keyboard>/d";
        [SerializeField] private string jumpBinding = "<Keyboard>/space";
        [SerializeField] private string sprintBinding = "<Keyboard>/leftShift";
        [SerializeField] private string fireBinding = "<Mouse>/leftButton";
        [SerializeField] private string reloadBinding = "<Keyboard>/r";

        public string Id => id;
        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float JumpHeight => jumpHeight;
        public float MouseSensitivity => mouseSensitivity;
        public float CameraFieldOfView => cameraFieldOfView;
        public string MoveForwardBinding => moveForwardBinding;
        public string MoveBackwardBinding => moveBackwardBinding;
        public string MoveLeftBinding => moveLeftBinding;
        public string MoveRightBinding => moveRightBinding;
        public string JumpBinding => jumpBinding;
        public string SprintBinding => sprintBinding;
        public string FireBinding => fireBinding;
        public string ReloadBinding => reloadBinding;
    }
}
