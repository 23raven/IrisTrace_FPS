using UnityEngine;

namespace IrisTrace.Runtime.Enemies
{
    public enum EnemyBehaviorMode
    {
        StationaryTarget,
        BasicAI
    }

    /// <summary>Reusable configuration used to generate an IrisTrace enemy prefab.</summary>
    [CreateAssetMenu(menuName = "IrisTrace FPS/Enemy Definition", fileName = "NewEnemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField] private string displayName = "New Enemy";
        [SerializeField] private EnemyBehaviorMode behavior = EnemyBehaviorMode.StationaryTarget;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float detectionRange = 14f;
        [SerializeField, Min(0.5f)] private float attackRange = 2.2f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.25f;

        public string Id => id;
        public string DisplayName => displayName;
        public EnemyBehaviorMode Behavior => behavior;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;

        public void Configure(
            string uniqueId,
            string name,
            EnemyBehaviorMode behaviorMode,
            float health,
            float speed,
            float detectRange,
            float attackDistance,
            float damage,
            float cooldown)
        {
            id = uniqueId;
            displayName = string.IsNullOrWhiteSpace(name) ? "New Enemy" : name.Trim();
            behavior = behaviorMode;
            maxHealth = Mathf.Max(1f, health);
            moveSpeed = Mathf.Max(0.1f, speed);
            detectionRange = Mathf.Max(0.1f, detectRange);
            attackRange = Mathf.Max(0.5f, attackDistance);
            attackDamage = Mathf.Max(0f, damage);
            attackCooldown = Mathf.Max(0.1f, cooldown);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            attackRange = Mathf.Max(0.5f, attackRange);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
        }
    }
}
