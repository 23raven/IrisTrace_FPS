using UnityEngine;
using UnityEngine.AI;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Enemies
{
    /// <summary>
    /// Small enemy brain. Uses NavMeshAgent where a baked NavMesh exists and falls back to direct pursuit otherwise.
    /// The fallback avoids a broken prefab in simple prototype scenes; production levels should use NavMesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BasicEnemyAI : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private NavMeshAgent agent;

        private MonoBehaviour targetComponent;
        private ICombatTarget combatTarget;
        private IDamageable targetDamageable;
        private IHealthStatus targetHealth;
        private float nextTargetSearchTime;
        private float nextAttackTime;
        private float nextNavMeshCheckTime;

        public void Configure(EnemyDefinition enemyDefinition, EnemyHealth enemyHealth, NavMeshAgent navMeshAgent)
        {
            definition = enemyDefinition;
            health = enemyHealth;
            agent = navMeshAgent;
        }

        private void Awake()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            // Generated agents are saved disabled to prevent errors in scenes without baked navigation.
            if (agent != null && agent.enabled && !agent.isOnNavMesh)
                agent.enabled = false;
        }

        private void Update()
        {
            if (definition == null || health == null || health.IsDead || definition.Behavior != EnemyBehaviorMode.BasicAI)
                return;

            if (targetComponent == null || targetHealth == null || targetHealth.IsDead)
            {
                if (Time.time >= nextTargetSearchTime)
                {
                    nextTargetSearchTime = Time.time + 0.75f;
                    FindTarget();
                }
            }

            if (targetComponent == null || combatTarget == null || targetDamageable == null || targetHealth == null || targetHealth.IsDead)
            {
                StopMovement();
                return;
            }

            Vector3 flatOffset = targetComponent.transform.position - transform.position;
            flatOffset.y = 0f;
            float distance = flatOffset.magnitude;

            if (distance > definition.DetectionRange)
            {
                StopMovement();
                return;
            }

            FaceTarget(flatOffset);
            if (distance > definition.AttackRange)
            {
                MoveTowardsTarget();
                return;
            }

            StopMovement();
            TryAttack();
        }

        private void FindTarget()
        {
            MonoBehaviour[] candidates = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            float bestDistance = float.MaxValue;

            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate == null || candidate.gameObject == gameObject || !(candidate is ICombatTarget possibleTarget))
                    continue;

                IDamageable damageable = null;
                IHealthStatus healthStatus = null;
                MonoBehaviour[] targetComponents = candidate.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour targetPart in targetComponents)
                {
                    if (damageable == null && targetPart is IDamageable foundDamageable)
                        damageable = foundDamageable;
                    if (healthStatus == null && targetPart is IHealthStatus foundHealth)
                        healthStatus = foundHealth;
                }

                if (damageable == null || healthStatus == null || healthStatus.IsDead)
                    continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                targetComponent = candidate;
                combatTarget = possibleTarget;
                targetDamageable = damageable;
                targetHealth = healthStatus;
            }
        }

        private void MoveTowardsTarget()
        {
            TryEnableNavMeshAgent();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetComponent.transform.position);
                return;
            }

            Vector3 targetPosition = targetComponent.transform.position;
            targetPosition.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, definition.MoveSpeed * Time.deltaTime);
        }

        private void TryEnableNavMeshAgent()
        {
            if (agent == null || agent.enabled || Time.time < nextNavMeshCheckTime)
                return;

            nextNavMeshCheckTime = Time.time + 1f;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.speed = definition.MoveSpeed;
                agent.stoppingDistance = Mathf.Max(0.2f, definition.AttackRange * 0.75f);
            }
        }

        private void TryAttack()
        {
            if (definition.AttackDamage <= 0f || Time.time < nextAttackTime)
                return;

            Vector3 origin = transform.position + Vector3.up * 1.25f;
            Vector3 destination = combatTarget.AimPoint != null
                ? combatTarget.AimPoint.position
                : targetComponent.transform.position + Vector3.up;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform != targetComponent.transform && !hitTransform.IsChildOf(targetComponent.transform))
                    return;
            }

            nextAttackTime = Time.time + definition.AttackCooldown;
            targetDamageable.TakeDamage(definition.AttackDamage);
            Debug.Log($"[{definition.DisplayName}] Hit {targetComponent.name} for {definition.AttackDamage} damage.", targetComponent);
        }

        private void FaceTarget(Vector3 flatOffset)
        {
            if (flatOffset.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(flatOffset.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 540f * Time.deltaTime);
        }

        private void StopMovement()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
    }
}
