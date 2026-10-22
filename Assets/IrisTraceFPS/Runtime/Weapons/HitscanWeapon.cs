using System;
using System.Collections;
using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Weapons
{
    /// <summary>Minimal configurable raycast weapon with a magazine and timed reload.</summary>
    [DisallowMultipleComponent]
    public sealed class HitscanWeapon : MonoBehaviour, IWeapon, IWeaponTelemetry
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform ownerRoot;
        [SerializeField] private LayerMask hitMask = ~0;

        private int currentAmmoInMagazine;
        private int reserveAmmo;
        private float nextShotTime;
        private float nextDryFireTime;
        private bool isFiring;
        private Coroutine reloadRoutine;

        public int CurrentAmmoInMagazine => currentAmmoInMagazine;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => reloadRoutine != null;
        public string WeaponDisplayName => definition != null ? definition.DisplayName : "WEAPON";

        /// <summary>Raised after ammo is consumed for a valid shot attempt.</summary>
        public event Action ShotFired;
        public event Action DryFired;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;

        public void Configure(WeaponDefinition weaponDefinition, Camera cameraForShots, Transform weaponOwnerRoot = null)
        {
            definition = weaponDefinition;
            viewCamera = cameraForShots;
            ownerRoot = weaponOwnerRoot;
            ResolveOwnerRoot();
            ResetAmmo();
        }

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = GetComponentInParent<Camera>();

            ResolveOwnerRoot();
            ResetAmmo();
        }

        private void Update()
        {
            if (isFiring && definition != null && definition.Automatic && !IsReloading)
                TryFire();
        }

        public void BeginFire()
        {
            if (definition == null || IsReloading)
                return;

            isFiring = true;
            TryFire();
        }

        public void EndFire()
        {
            isFiring = false;
        }

        public void Reload()
        {
            if (!isActiveAndEnabled || definition == null || IsReloading)
                return;
            if (currentAmmoInMagazine >= definition.MagazineSize || reserveAmmo <= 0)
                return;

            isFiring = false;
            ReloadStarted?.Invoke();
            reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        private void TryFire()
        {
            if (definition == null || viewCamera == null || IsReloading)
                return;
            if (Time.time < nextShotTime)
                return;

            // If the magazine is already empty (for example, after loading an old prefab),
            // start reloading on the next fire attempt rather than silently doing nothing.
            if (currentAmmoInMagazine <= 0)
            {
                if (reserveAmmo > 0)
                {
                    Reload();
                }
                else if (Time.time >= nextDryFireTime)
                {
                    // Rate-limit dry-fire feedback so holding the trigger cannot spam audio/events.
                    nextDryFireTime = Time.time + 0.25f;
                    DryFired?.Invoke();
                }
                return;
            }

            currentAmmoInMagazine--;
            nextShotTime = Time.time + (1f / Mathf.Max(0.1f, definition.ShotsPerSecond));
            ShotFired?.Invoke();

            // Automatically reload after the last round, when reserve ammo is available.
            // Start this before the raycast so misses still trigger the reload.
            if (currentAmmoInMagazine <= 0 && reserveAmmo > 0)
                Reload();

            Vector3 origin = viewCamera.transform.position;
            Vector3 direction = viewCamera.transform.forward;
            Debug.DrawRay(origin, direction * definition.Range, Color.green, 0.15f);

            // Ignore every collider belonging to the shooter and resolve the nearest external hit.
            // This keeps the weapon from damaging its own character while preserving wall occlusion.
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                definition.Range,
                hitMask,
                QueryTriggerInteraction.Ignore);

            bool foundExternalHit = false;
            RaycastHit closestHit = default;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider candidate = hits[i].collider;
                if (candidate == null || IsOwnerCollider(candidate))
                    continue;

                if (!foundExternalHit || hits[i].distance < closestHit.distance)
                {
                    closestHit = hits[i];
                    foundExternalHit = true;
                }
            }

            if (!foundExternalHit)
                return;

            IDamageable damageable = FindDamageable(closestHit.collider);
            if (damageable == null)
                return;

            damageable.TakeDamage(definition.Damage);
            string targetName = damageable is Component targetComponent
                ? targetComponent.gameObject.name
                : closestHit.collider.gameObject.name;
            Debug.Log($"[{definition.DisplayName}] Hit {targetName} for {definition.Damage} damage.", closestHit.collider);
        }

        private IEnumerator ReloadRoutine()
        {
            yield return new WaitForSeconds(definition.ReloadDuration);

            int missingAmmo = definition.MagazineSize - currentAmmoInMagazine;
            int ammoToLoad = Mathf.Min(missingAmmo, reserveAmmo);
            currentAmmoInMagazine += ammoToLoad;
            reserveAmmo -= ammoToLoad;
            reloadRoutine = null;
            ReloadCompleted?.Invoke();
        }

        private void OnDisable()
        {
            isFiring = false;
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
        }

        private void ResetAmmo()
        {
            if (definition == null)
            {
                currentAmmoInMagazine = 0;
                reserveAmmo = 0;
                return;
            }

            currentAmmoInMagazine = definition.MagazineSize;
            reserveAmmo = definition.StartingReserveAmmo;
        }

        private void ResolveOwnerRoot()
        {
            if (ownerRoot != null)
                return;

            // Generated heroes use CharacterController. This fallback also handles prefabs
            // generated before ownerRoot was added to the weapon configuration.
            CharacterController ownerController = viewCamera != null
                ? viewCamera.GetComponentInParent<CharacterController>()
                : GetComponentInParent<CharacterController>();

            ownerRoot = ownerController != null ? ownerController.transform : transform.root;
        }

        private bool IsOwnerCollider(Collider candidate)
        {
            if (ownerRoot == null || candidate == null)
                return false;

            Transform candidateTransform = candidate.transform;
            return candidateTransform == ownerRoot || candidateTransform.IsChildOf(ownerRoot);
        }

        private static IDamageable FindDamageable(Collider hitCollider)
        {
            MonoBehaviour[] components = hitCollider.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IDamageable damageable)
                    return damageable;
            }

            return null;
        }
    }
}
