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
        [SerializeField] private LayerMask hitMask = ~0;

        private int currentAmmoInMagazine;
        private int reserveAmmo;
        private float nextShotTime;
        private bool isFiring;
        private Coroutine reloadRoutine;

        public int CurrentAmmoInMagazine => currentAmmoInMagazine;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => reloadRoutine != null;
        public string WeaponDisplayName => definition != null ? definition.DisplayName : "WEAPON";

        public void Configure(WeaponDefinition weaponDefinition, Camera cameraForShots)
        {
            definition = weaponDefinition;
            viewCamera = cameraForShots;
            ResetAmmo();
        }

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = GetComponentInParent<Camera>();

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
            reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        private void TryFire()
        {
            if (definition == null || viewCamera == null || IsReloading)
                return;
            if (Time.time < nextShotTime || currentAmmoInMagazine <= 0)
                return;

            currentAmmoInMagazine--;
            nextShotTime = Time.time + (1f / Mathf.Max(0.1f, definition.ShotsPerSecond));

            Vector3 origin = viewCamera.transform.position;
            Vector3 direction = viewCamera.transform.forward;
            Debug.DrawRay(origin, direction * definition.Range, Color.green, 0.15f);

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, definition.Range, hitMask, QueryTriggerInteraction.Ignore))
                return;

            IDamageable damageable = FindDamageable(hit.collider);
            if (damageable != null)
            {
                damageable.TakeDamage(definition.Damage);
                Debug.Log($"[{definition.DisplayName}] Hit {hit.collider.name} for {definition.Damage} damage.", hit.collider);
            }
        }

        private IEnumerator ReloadRoutine()
        {
            yield return new WaitForSeconds(definition.ReloadDuration);

            int missingAmmo = definition.MagazineSize - currentAmmoInMagazine;
            int ammoToLoad = Mathf.Min(missingAmmo, reserveAmmo);
            currentAmmoInMagazine += ammoToLoad;
            reserveAmmo -= ammoToLoad;
            reloadRoutine = null;
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
