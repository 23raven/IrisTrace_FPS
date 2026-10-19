using System;
using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Characters
{
    /// <summary>
    /// Small reusable health component shared by heroes and future enemies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHealth : MonoBehaviour, IDamageable, IHealthStatus
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public void Initialize(float startingMaxHealth)
        {
            maxHealth = Mathf.Max(1f, startingMaxHealth);
            currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
                Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
