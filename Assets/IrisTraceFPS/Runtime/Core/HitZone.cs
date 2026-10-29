using UnityEngine;

namespace IrisTrace.Runtime.Core
{
    public enum HitZoneType
    {
        Head,
        Body,
        Legs,
        Custom
    }

    /// <summary>
    /// Marks a collider as a damage zone. Attach it to the same GameObject as a trigger collider,
    /// or to a parent of that collider. A multiplier of 2 applies double weapon damage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitZone : MonoBehaviour
    {
        [SerializeField] private HitZoneType zoneType = HitZoneType.Body;
        [SerializeField] private string customZoneName = "Custom";
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        public HitZoneType ZoneType => zoneType;
        public string DisplayName => zoneType == HitZoneType.Custom
            ? (string.IsNullOrWhiteSpace(customZoneName) ? "Custom" : customZoneName.Trim())
            : zoneType.ToString();
        public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);

        public void Configure(HitZoneType type, float multiplier, string customName = null)
        {
            zoneType = type;
            damageMultiplier = Mathf.Max(0f, multiplier);
            if (type == HitZoneType.Custom && !string.IsNullOrWhiteSpace(customName))
                customZoneName = customName.Trim();
        }

        private void OnValidate()
        {
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            if (string.IsNullOrWhiteSpace(customZoneName))
                customZoneName = "Custom";
        }
    }
}
