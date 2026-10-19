using UnityEngine;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>Simple collision-aware forward dash using Unity's CharacterController.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DashAbility : CharacterAbilityBase
    {
        [SerializeField, Min(0.1f)] private float distance = 6f;
        private CharacterController characterController;

        public void Configure(float dashDistance, float cooldownSeconds)
        {
            distance = Mathf.Max(0.1f, dashDistance);
            ConfigureCooldown(cooldownSeconds);
            ConfigureName("Dash");
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        protected override bool CanActivate()
        {
            return characterController != null && characterController.enabled && characterController.gameObject.activeInHierarchy;
        }

        protected override bool ActivateAbility()
        {
            Vector3 direction = transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return false;

            characterController.Move(direction.normalized * distance);
            return true;
        }
    }
}
