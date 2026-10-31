using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Interactions
{
    [Serializable]
    public sealed class TriggerColliderEvent : UnityEvent<Collider> { }

    /// <summary>
    /// Reusable trigger volume with a stable identifier, filtering and both code/Inspector events.
    /// A kinematic Rigidbody is kept on the trigger so it can report overlaps with CharacterControllers
    /// and ordinary colliders without changing the player's movement architecture.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    [AddComponentMenu("IrisTrace FPS/Interactions/Trigger")]
    public sealed class IrisTraceTrigger : MonoBehaviour
    {
        [SerializeField] private string triggerId = "Trigger_01";
        [SerializeField] private string displayName = "Trigger";
        [SerializeField] private LayerMask acceptedLayers = ~0;
        [SerializeField] private string requiredTag = string.Empty;
        [SerializeField] private bool requireCombatTarget;
        [SerializeField] private TriggerColliderEvent onActorEntered = new TriggerColliderEvent();
        [SerializeField] private TriggerColliderEvent onActorExited = new TriggerColliderEvent();

        private readonly Dictionary<int, HashSet<int>> actorColliders = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, int> colliderToActor = new Dictionary<int, int>();
        private BoxCollider triggerCollider;
        private Rigidbody triggerBody;

        public string TriggerId => triggerId;
        public string DisplayName => displayName;
        public event Action<Collider> ActorEntered;
        public event Action<Collider> ActorExited;

        public void Configure(string id, string label = null, LayerMask? layers = null, string tag = null,
            bool onlyCombatTargets = false)
        {
            triggerId = string.IsNullOrWhiteSpace(id) ? "Trigger_01" : id.Trim();
            if (!string.IsNullOrWhiteSpace(label))
                displayName = label.Trim();
            if (layers.HasValue)
                acceptedLayers = layers.Value;
            if (tag != null)
                requiredTag = tag.Trim();
            requireCombatTarget = onlyCombatTargets;
        }

        public void SetRequireCombatTarget(bool value)
        {
            requireCombatTarget = value;
        }

        /// <summary>Clears tracked overlaps, for example when a pickup temporarily disables its trigger.</summary>
        public void ClearOccupants()
        {
            actorColliders.Clear();
            colliderToActor.Clear();
        }

        private void Reset()
        {
            EnsureComponents();
            ConfigurePhysicsBody();
            triggerCollider.isTrigger = true;
            if (string.IsNullOrWhiteSpace(triggerId))
                triggerId = gameObject.name;
        }

        private void Awake()
        {
            EnsureComponents();
            ConfigurePhysicsBody();
            triggerCollider.isTrigger = true;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(triggerId))
                triggerId = "Trigger_01";
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = "Trigger";
            requiredTag = requiredTag == null ? string.Empty : requiredTag.Trim();

            EnsureComponents();
            ConfigurePhysicsBody();
            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }

        private void EnsureComponents()
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<BoxCollider>();
            if (triggerBody == null)
                triggerBody = GetComponent<Rigidbody>();
        }

        private void ConfigurePhysicsBody()
        {
            if (triggerBody == null)
                return;

            // Trigger volumes are overlap detectors, not physics-driven objects.
            triggerBody.isKinematic = true;
            triggerBody.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !TryResolveActorRoot(other, out Transform actorRoot))
                return;

            int colliderId = other.GetInstanceID();
            if (colliderToActor.ContainsKey(colliderId))
                return;

            int actorId = actorRoot.GetInstanceID();
            colliderToActor.Add(colliderId, actorId);

            if (!actorColliders.TryGetValue(actorId, out HashSet<int> colliders))
            {
                colliders = new HashSet<int>();
                actorColliders.Add(actorId, colliders);
            }

            bool firstCollider = colliders.Count == 0;
            colliders.Add(colliderId);
            if (firstCollider)
                RaiseActorEntered(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null)
                return;

            int colliderId = other.GetInstanceID();
            if (!colliderToActor.TryGetValue(colliderId, out int actorId))
                return;

            colliderToActor.Remove(colliderId);
            if (!actorColliders.TryGetValue(actorId, out HashSet<int> colliders))
                return;

            colliders.Remove(colliderId);
            if (colliders.Count == 0)
            {
                actorColliders.Remove(actorId);
                RaiseActorExited(other);
            }
        }

        private bool TryResolveActorRoot(Collider other, out Transform actorRoot)
        {
            actorRoot = null;
            if ((acceptedLayers.value & (1 << other.gameObject.layer)) == 0)
                return false;

            if (requireCombatTarget)
            {
                for (Transform current = other.transform; current != null; current = current.parent)
                {
                    MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour component in components)
                    {
                        if (component != null && component.isActiveAndEnabled && component is ICombatTarget)
                        {
                            actorRoot = current;
                            break;
                        }
                    }
                    if (actorRoot != null)
                        break;
                }

                if (actorRoot == null)
                    return false;
            }
            else
            {
                actorRoot = other.attachedRigidbody != null
                    ? other.attachedRigidbody.transform
                    : other.transform.root;
            }

            if (!string.IsNullOrEmpty(requiredTag) && actorRoot.gameObject.tag != requiredTag)
            {
                actorRoot = null;
                return false;
            }

            return true;
        }

        private void RaiseActorEntered(Collider other)
        {
            ActorEntered?.Invoke(other);
            onActorEntered?.Invoke(other);
        }

        private void RaiseActorExited(Collider other)
        {
            ActorExited?.Invoke(other);
            onActorExited?.Invoke(other);
        }

        private void OnDisable()
        {
            ClearOccupants();
        }
    }
}
