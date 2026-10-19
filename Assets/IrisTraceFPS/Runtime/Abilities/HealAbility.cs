using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>Restores health through the IHealable contract without depending on CharacterHealth.</summary>
    [DisallowMultipleComponent]
    public sealed class HealAbility : CharacterAbilityBase
    {
        [SerializeField, Min(0.1f)] private float healAmount = 25f;
        private IHealable healable;

        public void Configure(float amount, float cooldownSeconds)
        {
            healAmount = Mathf.Max(0.1f, amount);
            ConfigureCooldown(cooldownSeconds);
            ConfigureName("Heal");
        }

        private void Awake()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IHealable health)
                {
                    healable = health;
                    break;
                }
            }
        }

        protected override bool CanActivate()
        {
            return healable != null && !healable.IsDead && healable.CurrentHealth < healable.MaxHealth;
        }

        protected override bool ActivateAbility()
        {
            if (healable == null)
                return false;

            float before = healable.CurrentHealth;
            healable.Heal(healAmount);
            return healable.CurrentHealth > before;
        }
    }
}
