using UnityEngine;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>Built-in ability presets available in the first IrisTrace FPS release.</summary>
    public enum AbilityKind
    {
        None = 0,
        Dash = 1,
        Heal = 2
    }

    /// <summary>Saved configuration for a generated IrisTrace FPS character.</summary>
    [CreateAssetMenu(menuName = "IrisTrace FPS/Character Definition", fileName = "NewCharacter")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField, HideInInspector] private string primaryWeaponId;
        [SerializeField] private string displayName = "New FPS Hero";
        [SerializeField] private Sprite portraitIcon;

        [Header("Vitals")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;
        [SerializeField, Range(0.1f, 5f)] private float gravityMultiplier = 1.8f;
        [SerializeField, Range(1f, 4f)] private float fallGravityMultiplier = 2.2f;
        [SerializeField, Range(5f, 100f)] private float maximumFallSpeed = 35f;
        [SerializeField, Min(0.1f)] private float groundAcceleration = 18f;
        [SerializeField, Min(0.1f)] private float groundDeceleration = 22f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.35f;
        [SerializeField] private bool crouchEnabled = true;
        [SerializeField, Min(0.1f)] private float crouchSpeed = 2.5f;
        [SerializeField, Min(0.65f)] private float crouchedHeight = 1.1f;
        [SerializeField, Min(0.05f)] private float crouchTransitionDuration = 0.18f;

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
        [SerializeField] private string crouchBinding = "<Keyboard>/leftCtrl";
        [SerializeField] private string fireBinding = "<Mouse>/leftButton";
        [SerializeField] private string reloadBinding = "<Keyboard>/r";

        [Header("Ability Slot 1")]
        [SerializeField] private AbilityKind ability1Type = AbilityKind.Dash;
        [SerializeField] private string ability1Binding = "<Keyboard>/q";
        [SerializeField, Min(0.1f)] private float ability1Cooldown = 5f;
        [SerializeField, Min(0.1f)] private float ability1Value = 6f;

        [Header("Ability Slot 2")]
        [SerializeField] private AbilityKind ability2Type = AbilityKind.Heal;
        [SerializeField] private string ability2Binding = "<Keyboard>/e";
        [SerializeField, Min(0.1f)] private float ability2Cooldown = 8f;
        [SerializeField, Min(0.1f)] private float ability2Value = 25f;

        public string Id => id;
        public string PrimaryWeaponId => primaryWeaponId;
        public string DisplayName => displayName;
        public Sprite PortraitIcon => portraitIcon;
        public float MaxHealth => maxHealth;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float JumpHeight => jumpHeight;
        // Fallbacks keep characters saved before these fields existed playable.
        public float GravityMultiplier => gravityMultiplier > 0f ? gravityMultiplier : 1.8f;
        public float FallGravityMultiplier => fallGravityMultiplier > 0f ? fallGravityMultiplier : 2.2f;
        public float MaximumFallSpeed => maximumFallSpeed > 0f ? maximumFallSpeed : 35f;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirControl => airControl;
        public bool CrouchEnabled => crouchEnabled;
        public float CrouchSpeed => crouchSpeed;
        public float CrouchedHeight => crouchedHeight;
        public float CrouchTransitionDuration => crouchTransitionDuration;
        public float MouseSensitivity => mouseSensitivity;
        public float CameraFieldOfView => cameraFieldOfView;
        public string MoveForwardBinding => moveForwardBinding;
        public string MoveBackwardBinding => moveBackwardBinding;
        public string MoveLeftBinding => moveLeftBinding;
        public string MoveRightBinding => moveRightBinding;
        public string JumpBinding => jumpBinding;
        public string SprintBinding => sprintBinding;
        public string CrouchBinding => crouchBinding;
        public string FireBinding => fireBinding;
        public string ReloadBinding => reloadBinding;
        public AbilityKind Ability1Type => ability1Type;
        public string Ability1Binding => ability1Binding;
        public float Ability1Cooldown => ability1Cooldown;
        public float Ability1Value => ability1Value;
        public AbilityKind Ability2Type => ability2Type;
        public string Ability2Binding => ability2Binding;
        public float Ability2Cooldown => ability2Cooldown;
        public float Ability2Value => ability2Value;
    }
}
