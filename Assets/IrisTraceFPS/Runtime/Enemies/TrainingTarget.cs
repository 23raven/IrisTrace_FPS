using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Enemies
{
    /// <summary>Simple stationary damageable target used to validate weapon and health workflows.</summary>
    [DisallowMultipleComponent]
    public sealed class TrainingTarget : MonoBehaviour, IDamageable, IHealthStatus
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        private Renderer[] targetRenderers;
        private Collider[] targetColliders;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0f;

        public void Configure(float startingHealth)
        {
            maxHealth = Mathf.Max(1f, startingHealth);
            currentHealth = maxHealth;
            UpdateVisuals();
        }

        private void Awake()
        {
            CacheComponents();
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            UpdateVisuals();
            if (IsDead)
                DisableTargetColliders();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            UpdateVisuals();

            if (IsDead)
            {
                DisableTargetColliders();
                Debug.Log($"Training target '{name}' destroyed.", this);
            }
            else
            {
                Debug.Log($"Training target '{name}': {Mathf.CeilToInt(currentHealth)} HP remaining.", this);
            }
        }

        public void ResetTarget()
        {
            currentHealth = maxHealth;
            if (targetColliders == null)
                CacheComponents();
            foreach (Collider targetCollider in targetColliders)
                if (targetCollider != null) targetCollider.enabled = true;
            UpdateVisuals();
        }

        private void CacheComponents()
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
            targetColliders = GetComponentsInChildren<Collider>(true);
        }

        private void DisableTargetColliders()
        {
            if (targetColliders == null)
                CacheComponents();
            foreach (Collider targetCollider in targetColliders)
                if (targetCollider != null) targetCollider.enabled = false;
        }

        private void UpdateVisuals()
        {
            if (targetRenderers == null)
                CacheComponents();

            float healthRatio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            Color healthy = new Color(0.447f, 0.690f, 0.114f, 1f);
            Color damaged = new Color(0.82f, 0.22f, 0.16f, 1f);
            Color dead = new Color(0.27f, 0.29f, 0.33f, 1f);
            Color targetColor = IsDead ? dead : Color.Lerp(damaged, healthy, healthRatio);
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                    continue;

                targetRenderer.GetPropertyBlock(block);
                Material material = targetRenderer.sharedMaterial;
                if (material != null && material.HasProperty(BaseColorId))
                    block.SetColor(BaseColorId, targetColor);
                else
                    block.SetColor(ColorId, targetColor);
                targetRenderer.SetPropertyBlock(block);
            }
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
    }
}
