using System;
using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Enemies
{
    /// <summary>Reusable health and death feedback for generated enemy prefabs.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable, IHealthStatus
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private bool componentsCached;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0f;
        public event Action<float, float> HealthChanged;
        public event Action Died;

        public void Configure(float health)
        {
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
        }

        private void Awake()
        {
            CacheComponents();
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            UpdateVisuals();
            if (IsDead)
                DisableCombat();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            UpdateVisuals();
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (IsDead)
            {
                DisableCombat();
                Debug.Log($"Enemy '{name}' defeated.", this);
                Died?.Invoke();
            }
            else
            {
                Debug.Log($"Enemy '{name}': {Mathf.CeilToInt(currentHealth)} HP remaining.", this);
            }
        }

        private void CacheComponents()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            cachedColliders = GetComponentsInChildren<Collider>(true);
            componentsCached = true;
        }

        private void UpdateVisuals()
        {
            if (!componentsCached)
                CacheComponents();

            float ratio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            Color healthy = new Color(0.447f, 0.690f, 0.114f, 1f);
            Color damaged = new Color(0.82f, 0.22f, 0.16f, 1f);
            Color dead = new Color(0.27f, 0.29f, 0.33f, 1f);
            Color color = IsDead ? dead : Color.Lerp(damaged, healthy, ratio);
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (Renderer cachedRenderer in cachedRenderers)
            {
                if (cachedRenderer == null)
                    continue;

                cachedRenderer.GetPropertyBlock(block);
                Material material = cachedRenderer.sharedMaterial;
                if (material != null && material.HasProperty(BaseColorId))
                    block.SetColor(BaseColorId, color);
                else
                    block.SetColor(ColorId, color);
                cachedRenderer.SetPropertyBlock(block);
            }
        }

        private void DisableCombat()
        {
            if (!componentsCached)
                CacheComponents();

            foreach (Collider cachedCollider in cachedColliders)
                if (cachedCollider != null)
                    cachedCollider.enabled = false;

            BasicEnemyAI ai = GetComponent<BasicEnemyAI>();
            if (ai != null)
                ai.enabled = false;

            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
    }
}
