using UnityEngine;

namespace IrisTrace.Runtime.Enemies
{
    public enum EnemyBehaviorMode
    {
        StationaryTarget,
        BasicAI
    }

    public enum EnemyAttackMode
    {
        Melee,
        Ranged
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
        [SerializeField] private EnemyAttackMode attackMode = EnemyAttackMode.Melee;
        [SerializeField, Range(0f, 100f)] private float accuracy = 75f;

        [Header("Collision & Hit Zones")]
        [SerializeField, Min(0.8f)] private float capsuleHeight = 2.4f;
        [SerializeField, Min(0.05f)] private float capsuleRadius = 0.45f;
        [SerializeField, Min(0f)] private float headDamageMultiplier = 2f;
        [SerializeField, Min(0f)] private float bodyDamageMultiplier = 1f;
        [SerializeField, Min(0f)] private float legsDamageMultiplier = 0.75f;

        public string Id => id;
        public string DisplayName => displayName;
        public EnemyBehaviorMode Behavior => behavior;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public EnemyAttackMode AttackMode => attackMode;
        public float Accuracy => Mathf.Clamp(accuracy, 0f, 100f);
        public float CapsuleHeight => Mathf.Max(0.8f, capsuleHeight);
        public float CapsuleRadius => Mathf.Clamp(capsuleRadius, 0.05f, CapsuleHeight * 0.45f);
        public float HeadDamageMultiplier => Mathf.Max(0f, headDamageMultiplier);
        public float BodyDamageMultiplier => Mathf.Max(0f, bodyDamageMultiplier);
        public float LegsDamageMultiplier => Mathf.Max(0f, legsDamageMultiplier);

        public void Configure(
            string uniqueId,
            string name,
            EnemyBehaviorMode behaviorMode,
            float health,
            float speed,
            float detectRange,
            float attackDistance,
            float damage,
            float cooldown,
            float height = 2.4f,
            float radius = 0.45f,
            float headMultiplier = 2f,
            float bodyMultiplier = 1f,
            float legsMultiplier = 0.75f,
            EnemyAttackMode attackMode = EnemyAttackMode.Melee,
            float accuracy = 75f)
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
            this.attackMode = attackMode;
            this.accuracy = Mathf.Clamp(accuracy, 0f, 100f);
            capsuleHeight = Mathf.Max(0.8f, height);
            capsuleRadius = Mathf.Clamp(radius, 0.05f, capsuleHeight * 0.45f);
            headDamageMultiplier = Mathf.Max(0f, headMultiplier);
            bodyDamageMultiplier = Mathf.Max(0f, bodyMultiplier);
            legsDamageMultiplier = Mathf.Max(0f, legsMultiplier);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            attackRange = Mathf.Max(0.5f, attackRange);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
            accuracy = Mathf.Clamp(accuracy, 0f, 100f);
            capsuleHeight = Mathf.Max(0.8f, capsuleHeight);
            capsuleRadius = Mathf.Clamp(capsuleRadius, 0.05f, capsuleHeight * 0.45f);
            headDamageMultiplier = Mathf.Max(0f, headDamageMultiplier);
            bodyDamageMultiplier = Mathf.Max(0f, bodyDamageMultiplier);
            legsDamageMultiplier = Mathf.Max(0f, legsDamageMultiplier);
        }
    }
}
