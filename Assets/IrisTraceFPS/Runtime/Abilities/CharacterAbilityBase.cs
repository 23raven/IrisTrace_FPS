using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>Shared cooldown and activation flow for abilities; custom skills can implement ICharacterAbility directly.</summary>
    public abstract class CharacterAbilityBase : MonoBehaviour, ICharacterAbility
    {
        [SerializeField] private string abilityName = "Ability";
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 5f;

        private float nextAvailableTime;

        public string AbilityName => abilityName;
        public float CooldownRemaining => Application.isPlaying ? Mathf.Max(0f, nextAvailableTime - Time.time) : 0f;

        public void ConfigureCooldown(float seconds)
        {
            cooldownSeconds = Mathf.Max(0.1f, seconds);
        }

        protected void ConfigureName(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                abilityName = value;
        }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled || CooldownRemaining > 0f || !CanActivate())
                return false;

            if (!ActivateAbility())
                return false;

            nextAvailableTime = Time.time + cooldownSeconds;
            Debug.Log($"[{name}] Ability '{abilityName}' activated. Cooldown: {cooldownSeconds:0.##}s.", this);
            return true;
        }

        protected virtual bool CanActivate() => true;
        protected abstract bool ActivateAbility();
    }
}
