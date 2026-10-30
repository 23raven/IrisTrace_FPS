using UnityEngine;
using UnityEngine.AI;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Enemies
{
    /// <summary>
    /// Lightweight enemy brain. Uses NavMesh when available; otherwise moves in a straight line
    /// while respecting simple obstacle checks. Complex pathfinding requires a baked NavMesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BasicEnemyAI : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField, Min(0.1f)] private float targetSearchInterval = 0.65f;
        [SerializeField, Min(0.05f)] private float destinationRefreshInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float fallbackObstacleProbeDistance = 0.25f;

        private MonoBehaviour targetComponent;
        private Transform targetRoot;
        private ICombatTarget combatTarget;
        private IDamageable targetDamageable;
        private IHealthStatus targetHealth;
        private float nextTargetSearchTime;
        private float nextAttackTime;
        private float nextAttackCheckTime;
        private float nextNavMeshCheckTime;
        private float nextDestinationRefreshTime;
        private Vector3 lastDestination;
        private LineRenderer shotTracer;
        private Material shotTracerMaterial;
        private float tracerDisableTime;
        private readonly RaycastHit[] attackHits = new RaycastHit[32];

        public void Configure(EnemyDefinition enemyDefinition, EnemyHealth enemyHealth, NavMeshAgent navMeshAgent)
        {
            definition = enemyDefinition;
            health = enemyHealth;
            agent = navMeshAgent;
            if (muzzlePoint == null)
                muzzlePoint = transform.Find("Muzzle");
        }

        private void Awake()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
            if (muzzlePoint == null)
                muzzlePoint = transform.Find("Muzzle");

            // Generated NavMeshAgents start disabled so prefabs can be used in scenes without a NavMesh.
            if (agent != null && agent.enabled && !agent.isOnNavMesh)
                agent.enabled = false;
        }

        private void Update()
        {
            if (shotTracer != null && shotTracer.enabled && Time.time >= tracerDisableTime)
                shotTracer.enabled = false;

            if (definition == null || health == null || health.IsDead || definition.Behavior != EnemyBehaviorMode.BasicAI)
            {
                StopMovement();
                return;
            }

            if (!HasValidTarget() && Time.time >= nextTargetSearchTime)
            {
                nextTargetSearchTime = Time.time + Mathf.Max(0.1f, targetSearchInterval);
                FindTarget();
            }

            if (!HasValidTarget())
            {
                StopMovement();
                return;
            }

            Vector3 flatOffset = targetRoot.position - transform.position;
            flatOffset.y = 0f;
            float distance = flatOffset.magnitude;

            if (distance > definition.DetectionRange)
            {
                StopMovement();
                ClearTarget();
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

        private bool HasValidTarget()
        {
            return targetComponent != null && targetRoot != null && combatTarget != null &&
                   targetDamageable != null && targetHealth != null && !targetHealth.IsDead;
        }

        private void FindTarget()
        {
            ClearTarget();
            MonoBehaviour[] candidates = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            float bestDistance = definition.DetectionRange * definition.DetectionRange;

            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || !(candidate is ICombatTarget possibleTarget))
                    continue;

                Transform candidateTransform = candidate.transform;
                if (candidateTransform == transform || candidateTransform.IsChildOf(transform) || transform.IsChildOf(candidateTransform))
                    continue;

                if (!TryResolveTarget(candidate, possibleTarget, out Transform resolvedRoot,
                        out IDamageable damageable, out IHealthStatus healthStatus))
                    continue;

                if (healthStatus.IsDead)
                    continue;

                Vector3 offset = resolvedRoot.position - transform.position;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                targetComponent = candidate;
                targetRoot = resolvedRoot;
                combatTarget = possibleTarget;
                targetDamageable = damageable;
                targetHealth = healthStatus;
            }
        }

        private static bool TryResolveTarget(
            MonoBehaviour candidate,
            ICombatTarget possibleTarget,
            out Transform resolvedRoot,
            out IDamageable damageable,
            out IHealthStatus healthStatus)
        {
            resolvedRoot = candidate.transform;
            damageable = null;
            healthStatus = null;

            // If the input/aim component lives on a child, use the highest parent that also
            // represents the same combat target. This keeps movement and collider checks consistent.
            for (Transform parent = candidate.transform.parent; parent != null; parent = parent.parent)
            {
                if (HasCombatTarget(parent))
                    resolvedRoot = parent;
            }

            MonoBehaviour[] descendants = resolvedRoot.GetComponentsInChildren<MonoBehaviour>(true);
            FindHealthContracts(descendants, ref damageable, ref healthStatus);

            // Health may intentionally be attached to a parent object that does not implement ICombatTarget.
            for (Transform parent = resolvedRoot.parent; parent != null; parent = parent.parent)
            {
                FindHealthContracts(parent.GetComponents<MonoBehaviour>(), ref damageable, ref healthStatus);
                if (damageable != null && healthStatus != null)
                    break;
            }

            return damageable != null && healthStatus != null;
        }

        private static bool HasCombatTarget(Transform candidate)
        {
            MonoBehaviour[] components = candidate.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
                if (component != null && component.isActiveAndEnabled && component is ICombatTarget)
                    return true;
            return false;
        }

        private static void FindHealthContracts(
            MonoBehaviour[] components,
            ref IDamageable damageable,
            ref IHealthStatus healthStatus)
        {
            foreach (MonoBehaviour component in components)
            {
                if (component == null || !component.isActiveAndEnabled)
                    continue;
                if (damageable == null && component is IDamageable foundDamageable)
                    damageable = foundDamageable;
                if (healthStatus == null && component is IHealthStatus foundHealth)
                    healthStatus = foundHealth;
                if (damageable != null && healthStatus != null)
                    return;
            }
        }

        private void MoveTowardsTarget()
        {
            if (TryEnableNavMeshAgent())
            {
                agent.isStopped = false;
                bool destinationMoved = (targetRoot.position - lastDestination).sqrMagnitude > 0.36f;
                if (Time.time >= nextDestinationRefreshTime || destinationMoved || !agent.hasPath)
                {
                    nextDestinationRefreshTime = Time.time + Mathf.Max(0.05f, destinationRefreshInterval);
                    if (agent.SetDestination(targetRoot.position))
                        lastDestination = targetRoot.position;
                }
                return;
            }

            // No NavMesh: provide predictable direct pursuit, but don't walk through an obstacle.
            Vector3 offset = targetRoot.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
                return;

            Vector3 direction = offset.normalized;
            float step = Mathf.Min(definition.MoveSpeed * Time.deltaTime,
                Mathf.Max(0f, offset.magnitude - definition.AttackRange * 0.8f));
            if (step <= 0f || IsFallbackMovementBlocked(direction, step + fallbackObstacleProbeDistance))
                return;

            transform.position += direction * step;
        }

        private bool TryEnableNavMeshAgent()
        {
            if (agent == null)
                return false;

            if (agent.enabled && agent.isOnNavMesh)
                return true;

            if (agent.enabled && !agent.isOnNavMesh)
                agent.enabled = false;

            if (Time.time < nextNavMeshCheckTime)
                return false;

            nextNavMeshCheckTime = Time.time + 1f;
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return false;

            // Put the root on the sampled point before enabling the agent; this avoids an
            // enabled-but-unregistered agent when the generated prefab was slightly off the NavMesh.
            transform.position = hit.position;
            agent.enabled = true;
            if (!agent.isOnNavMesh)
            {
                agent.enabled = false;
                return false;
            }

            agent.Warp(hit.position);
            agent.speed = definition.MoveSpeed;
            agent.stoppingDistance = Mathf.Max(0.2f, definition.AttackRange * 0.75f);
            return true;
        }

        private bool IsFallbackMovementBlocked(Vector3 direction, float distance)
        {
            Vector3 origin = transform.position + Vector3.up;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                return false;

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return false;
            if (targetRoot != null && (hitTransform == targetRoot || hitTransform.IsChildOf(targetRoot)))
                return false;
            return true;
        }

        private void TryAttack()
        {
            if (definition.AttackMode == EnemyAttackMode.Ranged)
                TryRangedAttack();
            else
                TryMeleeAttack();
        }

        private void TryMeleeAttack()
        {
            if (definition.AttackDamage <= 0f || Time.time < nextAttackTime || Time.time < nextAttackCheckTime)
                return;

            Vector3 origin = transform.position + Vector3.up * Mathf.Min(definition.CapsuleHeight * 0.65f, 1.25f);
            Vector3 destination = combatTarget.AimPoint != null
                ? combatTarget.AimPoint.position
                : targetRoot.position + Vector3.up;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return;

            if (!HasLineOfSight(origin, direction.normalized, distance))
            {
                // Don't raycast every frame when a wall blocks the target.
                nextAttackCheckTime = Time.time + 0.15f;
                return;
            }

            nextAttackTime = Time.time + definition.AttackCooldown;
            targetDamageable.TakeDamage(definition.AttackDamage);
            Debug.Log($"[{definition.DisplayName}] Hit {targetRoot.name} for {definition.AttackDamage} damage.", targetRoot);
        }

        private void TryRangedAttack()
        {
            if (definition.AttackDamage <= 0f || Time.time < nextAttackTime || Time.time < nextAttackCheckTime)
                return;

            Vector3 origin = muzzlePoint != null
                ? muzzlePoint.position
                : transform.position + Vector3.up * Mathf.Min(definition.CapsuleHeight * 0.72f, 1.6f);
            Vector3 destination = combatTarget != null && combatTarget.AimPoint != null
                ? combatTarget.AimPoint.position
                : targetRoot.position + Vector3.up;
            Vector3 toTarget = destination - origin;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return;

            float maxDistance = Mathf.Max(definition.AttackRange + 1f, toTarget.magnitude + 1f);
            float spreadAngle = Mathf.Lerp(12f, 0.15f, definition.Accuracy / 100f);
            Vector2 spread = Random.insideUnitCircle * spreadAngle;
            Vector3 direction = Quaternion.LookRotation(toTarget.normalized) *
                                Quaternion.Euler(spread.y, spread.x, 0f) * Vector3.forward;

            Physics.SyncTransforms();
            int hitCount = Physics.RaycastNonAlloc(origin, direction, attackHits, maxDistance, ~0,
                QueryTriggerInteraction.Ignore);

            bool hasExternalHit = false;
            RaycastHit closestHit = default;
            for (int i = 0; i < hitCount; i++)
            {
                Collider candidate = attackHits[i].collider;
                if (candidate == null || IsOwnCollider(candidate))
                    continue;

                if (!hasExternalHit || attackHits[i].distance < closestHit.distance)
                {
                    closestHit = attackHits[i];
                    hasExternalHit = true;
                }
            }

            bool hitTarget = !hasExternalHit;
            Vector3 tracerEnd = origin + direction * maxDistance;
            if (hasExternalHit)
            {
                tracerEnd = closestHit.point;
                Transform hitTransform = closestHit.collider.transform;
                hitTarget = targetRoot != null &&
                            (hitTransform == targetRoot || hitTransform.IsChildOf(targetRoot));
            }

            nextAttackTime = Time.time + definition.AttackCooldown;
            ShowShotTracer(origin, tracerEnd);

            if (!hitTarget)
            {
                nextAttackCheckTime = Time.time + 0.05f;
                return;
            }

            targetDamageable.TakeDamage(definition.AttackDamage);
            Debug.Log($"[{definition.DisplayName}] Shot {targetRoot.name} for {definition.AttackDamage} damage.", targetRoot);
        }

        private bool IsOwnCollider(Collider candidate)
        {
            Transform candidateTransform = candidate.transform;
            return candidateTransform == transform || candidateTransform.IsChildOf(transform);
        }

        private void ShowShotTracer(Vector3 start, Vector3 end)
        {
            EnsureShotTracer();
            if (shotTracer == null)
                return;

            shotTracer.SetPosition(0, start);
            shotTracer.SetPosition(1, end);
            shotTracer.enabled = true;
            tracerDisableTime = Time.time + 0.075f;
        }

        private void EnsureShotTracer()
        {
            if (shotTracer != null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("Unlit/Color");
            if (shader == null)
                return;

            GameObject tracerObject = new GameObject("RuntimeShotTracer");
            tracerObject.transform.SetParent(transform, false);
            shotTracer = tracerObject.AddComponent<LineRenderer>();
            shotTracerMaterial = new Material(shader);
            shotTracer.material = shotTracerMaterial;
            shotTracer.useWorldSpace = true;
            shotTracer.alignment = LineAlignment.View;
            shotTracer.positionCount = 2;
            shotTracer.startWidth = 0.025f;
            shotTracer.endWidth = 0.006f;
            shotTracer.startColor = new Color(1f, 0.18f, 0.08f, 0.95f);
            shotTracer.endColor = new Color(1f, 0.52f, 0.12f, 0.35f);
            shotTracer.numCapVertices = 2;
            shotTracer.enabled = false;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 direction, float distance)
        {
            int hitCount = Physics.RaycastNonAlloc(origin, direction, attackHits, distance, ~0,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            Transform closestTransform = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = attackHits[i].collider;
                if (collider == null)
                    continue;

                Transform hitTransform = collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                if (attackHits[i].distance < closestDistance)
                {
                    closestDistance = attackHits[i].distance;
                    closestTransform = hitTransform;
                }
            }

            // If no collider is in the ray, treat the path as clear. This allows custom targets
            // without colliders while still respecting walls and other blocking geometry.
            if (closestTransform == null)
                return true;

            return targetRoot != null &&
                   (closestTransform == targetRoot || closestTransform.IsChildOf(targetRoot));
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
                if (agent.hasPath)
                    agent.ResetPath();
            }
            nextDestinationRefreshTime = 0f;
        }

        private void ClearTarget()
        {
            targetComponent = null;
            targetRoot = null;
            combatTarget = null;
            targetDamageable = null;
            targetHealth = null;
        }

        private void OnDisable()
        {
            StopMovement();
            if (shotTracer != null)
                shotTracer.enabled = false;
        }

        private void OnDestroy()
        {
            if (shotTracerMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(shotTracerMaterial);
                else
                    DestroyImmediate(shotTracerMaterial);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null)
                return;

            Gizmos.color = new Color(0.45f, 0.69f, 0.11f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, definition.DetectionRange);
            Gizmos.color = new Color(0.85f, 0.25f, 0.18f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, definition.AttackRange);
        }
    }
}
