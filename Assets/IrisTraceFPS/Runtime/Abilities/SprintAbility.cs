using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>
    /// Hold-to-sprint ability. Movement speed is configured by CharacterDefinition.SprintSpeed;
    /// this component only publishes whether the ability is currently active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SprintAbility : MonoBehaviour, IHoldCharacterAbility, ISprintState
    {
        public string AbilityName => "Sprint";
        public float CooldownRemaining => 0f;
        public bool IsActive => IsSprinting;
        public bool IsSprinting { get; private set; }

        public bool TryActivate()
        {
            // Sprint is hold-driven. It is activated by SetInputHeld, not a one-shot call.
            return false;
        }

        public void SetInputHeld(bool held)
        {
            IsSprinting = held && isActiveAndEnabled;
        }

        private void OnDisable()
        {
            IsSprinting = false;
        }
    }
}
