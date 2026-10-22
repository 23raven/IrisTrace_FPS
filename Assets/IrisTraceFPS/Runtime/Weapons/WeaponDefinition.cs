using UnityEngine;

namespace IrisTrace.Runtime.Weapons
{
    /// <summary>Data-only weapon configuration. Runtime behaviour lives in a separate component.</summary>
    [CreateAssetMenu(menuName = "IrisTrace FPS/Weapon Definition", fileName = "NewWeapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField] private string displayName = "Primary Weapon";
        [SerializeField, Min(0.1f)] private float damage = 25f;
        [SerializeField, Min(0.1f)] private float shotsPerSecond = 6f;
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(0)] private int reserveAmmo = 48;
        [SerializeField, Min(0.1f)] private float reloadDuration = 1.4f;
        [SerializeField, Min(1f)] private float range = 100f;
        [SerializeField] private bool automatic = true;
        [Header("Optional Audio")]
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip dryFireClip;

        public string Id => id;
        public string DisplayName => displayName;
        public float Damage => damage;
        public float ShotsPerSecond => shotsPerSecond;
        public int MagazineSize => magazineSize;
        public int StartingReserveAmmo => reserveAmmo;
        public float ReloadDuration => reloadDuration;
        public float Range => range;
        public bool Automatic => automatic;
        public AudioClip FireClip => fireClip;
        public AudioClip ReloadClip => reloadClip;
        public AudioClip DryFireClip => dryFireClip;
    }
}
