using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Interactions
{
    public enum PickupItemType
    {
        Health,
        Ammo,
        Custom
    }

    [System.Serializable]
    public sealed class PickupGameObjectEvent : UnityEvent<GameObject> { }

    /// <summary>
    /// Simple automatic pickup. Health and Ammo use optional capability interfaces;
    /// Custom items delegate their effect to a component implementing IPickupEffect.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IrisTraceTrigger))]
    [AddComponentMenu("IrisTrace FPS/Interactions/Pickup Item")]
    public sealed class PickupItem : MonoBehaviour
    {
        [SerializeField] private string itemName = "Health Pickup";
        [SerializeField] private PickupItemType itemType = PickupItemType.Health;
        [SerializeField, Min(0f)] private float amount = 25f;
        [SerializeField, Min(0f)] private float respawnDelay;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private PickupGameObjectEvent onPickedUp = new PickupGameObjectEvent();

        private IrisTraceTrigger trigger;
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private bool[] rendererInitialStates;
        private bool[] colliderInitialStates;
        private bool componentsCached;
        private bool available = true;
        private Coroutine respawnRoutine;

        public string ItemName => itemName;
        public PickupItemType ItemType => itemType;
        public float Amount => amount;
        public event Action<GameObject> PickedUp;

        public void Configure(string label, PickupItemType type, float value, float respawnSeconds = 0f,
            bool destroyAfterPickup = true)
        {
            itemName = string.IsNullOrWhiteSpace(label) ? "Pickup" : label.Trim();
            itemType = type;
            amount = Mathf.Max(0f, value);
            respawnDelay = Mathf.Max(0f, respawnSeconds);
            destroyOnPickup = destroyAfterPickup;
        }

        private void Reset()
        {
            trigger = GetComponent<IrisTraceTrigger>();
            if (trigger != null)
                trigger.SetRequireCombatTarget(true);
        }

        private void Awake()
        {
            trigger = GetComponent<IrisTraceTrigger>();
            if (trigger != null)
                trigger.SetRequireCombatTarget(true);
            CacheComponents();
        }

        private void OnEnable()
        {
            if (trigger == null)
                trigger = GetComponent<IrisTraceTrigger>();
            if (trigger != null)
                trigger.ActorEntered += HandleActorEntered;
        }

        private void OnDisable()
        {
            if (trigger != null)
                trigger.ActorEntered -= HandleActorEntered;

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }

        private void HandleActorEntered(Collider other)
        {
            if (!available || other == null)
                return;

            GameObject receiver = FindCombatTarget(other);
            if (receiver == null || !TryApply(receiver))
                return;

            available = false;
            PickedUp?.Invoke(receiver);
            onPickedUp?.Invoke(receiver);

            if (respawnDelay > 0f)
            {
                SetVisible(false);
                respawnRoutine = StartCoroutine(RespawnRoutine());
            }
            else if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                SetVisible(false);
            }
        }

        private bool TryApply(GameObject receiver)
        {
            List<MonoBehaviour> behaviours = CollectReceiverBehaviours(receiver);

            switch (itemType)
            {
                case PickupItemType.Health:
                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        if (!(behaviour is IHealable healable))
                            continue;

                        float previousHealth = healable.CurrentHealth;
                        healable.Heal(amount);
                        if (healable.CurrentHealth > previousHealth + 0.001f)
                            return true;
                    }
                    return false;

                case PickupItemType.Ammo:
                    int ammoAmount = Mathf.RoundToInt(amount);
                    if (ammoAmount <= 0)
                        return false;

                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        if (!(behaviour is IAmmoReceiver ammoReceiver))
                            continue;

                        if (ammoReceiver.AddReserveAmmo(ammoAmount) > 0)
                            return true;
                    }
                    return false;

                case PickupItemType.Custom:
                    MonoBehaviour[] pickupBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (MonoBehaviour behaviour in pickupBehaviours)
                    {
                        if (behaviour is IPickupEffect customEffect && customEffect.TryApply(receiver, amount))
                            return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private static GameObject FindCombatTarget(Collider other)
        {
            for (Transform current = other.transform; current != null; current = current.parent)
            {
                MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour component in components)
                {
                    if (component != null && component.isActiveAndEnabled && component is ICombatTarget)
                        return current.gameObject;
                }
            }
            return null;
        }

        private static List<MonoBehaviour> CollectReceiverBehaviours(GameObject receiver)
        {
            List<MonoBehaviour> result = new List<MonoBehaviour>();
            HashSet<int> seen = new HashSet<int>();

            for (Transform current = receiver.transform; current != null; current = current.parent)
                AddBehaviours(current.GetComponents<MonoBehaviour>(), result, seen);

            AddBehaviours(receiver.GetComponentsInChildren<MonoBehaviour>(true), result, seen);
            return result;
        }

        private static void AddBehaviours(MonoBehaviour[] source, List<MonoBehaviour> destination, HashSet<int> seen)
        {
            foreach (MonoBehaviour behaviour in source)
            {
                if (behaviour == null || !behaviour.isActiveAndEnabled)
                    continue;
                if (seen.Add(behaviour.GetInstanceID()))
                    destination.Add(behaviour);
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            SetVisible(true);
            available = true;
            respawnRoutine = null;
        }

        private void CacheComponents()
        {
            if (componentsCached)
                return;

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            cachedColliders = GetComponentsInChildren<Collider>(true);
            rendererInitialStates = new bool[cachedRenderers.Length];
            colliderInitialStates = new bool[cachedColliders.Length];

            for (int i = 0; i < cachedRenderers.Length; i++)
                rendererInitialStates[i] = cachedRenderers[i] != null && cachedRenderers[i].enabled;
            for (int i = 0; i < cachedColliders.Length; i++)
                colliderInitialStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;

            componentsCached = true;
        }

        private void SetVisible(bool visible)
        {
            CacheComponents();
            if (!visible && trigger != null)
                trigger.ClearOccupants();

            for (int i = 0; i < cachedRenderers.Length; i++)
                if (cachedRenderers[i] != null)
                    cachedRenderers[i].enabled = visible && rendererInitialStates[i];
            for (int i = 0; i < cachedColliders.Length; i++)
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = visible && colliderInitialStates[i];
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemName))
                itemName = itemType == PickupItemType.Ammo ? "Ammo Pickup" : "Health Pickup";
            amount = Mathf.Max(0f, amount);
            respawnDelay = Mathf.Max(0f, respawnDelay);
        }
    }
}
