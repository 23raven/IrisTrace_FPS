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
        [Header("First-Person Viewmodel Mount")]
        [SerializeField] private Vector3 viewModelMountLocalPosition = new Vector3(0.24f, -0.30f, 0.56f);
        [SerializeField] private Vector3 viewModelMountLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 viewModelMountLocalScale = new Vector3(0.72f, 0.72f, 0.72f);

        [Header("First-Person Viewmodel Asset")]
        [SerializeField] private GameObject viewModelPrefab;
        [SerializeField] private Vector3 viewModelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 viewModelLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 viewModelLocalScale = Vector3.one;

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
        public Vector3 ViewModelMountLocalPosition => viewModelMountLocalPosition == Vector3.zero
            ? new Vector3(0.24f, -0.30f, 0.56f) : viewModelMountLocalPosition;
        public Vector3 ViewModelMountLocalEuler => viewModelMountLocalEuler;
        public Vector3 ViewModelMountLocalScale => SafeMountScale(viewModelMountLocalScale);
        public GameObject ViewModelPrefab => viewModelPrefab;
        public Vector3 ViewModelLocalPosition => viewModelLocalPosition;
        public Vector3 ViewModelLocalEuler => viewModelLocalEuler;
        public Vector3 ViewModelLocalScale => SafeScale(viewModelLocalScale);
        public AudioClip FireClip => fireClip;

        private static Vector3 SafeMountScale(Vector3 value)
        {
            if (Mathf.Abs(value.x) < 0.001f || Mathf.Abs(value.y) < 0.001f || Mathf.Abs(value.z) < 0.001f)
                return new Vector3(0.72f, 0.72f, 0.72f);
            return value;
        }

        private static Vector3 SafeScale(Vector3 value)
        {
            if (Mathf.Abs(value.x) < 0.001f || Mathf.Abs(value.y) < 0.001f || Mathf.Abs(value.z) < 0.001f)
                return Vector3.one;
            return value;
        }
        public AudioClip ReloadClip => reloadClip;
        public AudioClip DryFireClip => dryFireClip;
    }
}
