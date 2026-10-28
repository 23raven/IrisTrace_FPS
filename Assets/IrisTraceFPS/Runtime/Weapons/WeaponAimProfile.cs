using UnityEngine;

namespace IrisTrace.Runtime.Weapons
{
    /// <summary>Optional aiming behavior for a weapon. No profile means RMB aiming is disabled.</summary>
    public enum WeaponAimMode
    {
        Disabled = 0,
        AimDownSights = 1,
        ScopeZoom = 2
    }

    [CreateAssetMenu(menuName = "IrisTrace FPS/Weapon Aim Profile", fileName = "NewWeaponAimProfile")]
    public sealed class WeaponAimProfile : ScriptableObject
    {
        [SerializeField] private WeaponAimMode mode = WeaponAimMode.AimDownSights;
        [SerializeField, Range(10f, 89f)] private float aimFieldOfView = 55f;
        [SerializeField, Range(5f, 60f)] private float scopeFieldOfView = 22f;
        [SerializeField, Min(0.1f)] private float transitionSpeed = 12f;
        [SerializeField, Range(0.05f, 2f)] private float aimSensitivityMultiplier = 0.65f;
        [SerializeField] private Vector3 aimPivotPositionOffset = new Vector3(-0.16f, 0.14f, 0.02f);
        [SerializeField] private Vector3 aimPivotEulerOffset = Vector3.zero;
        [SerializeField] private bool hideViewmodelWhileScoped = true;
        [SerializeField] private Texture2D scopeOverlayTexture;

        public WeaponAimMode Mode => mode;
        public float AimFieldOfView => Mathf.Clamp(aimFieldOfView, 10f, 120f);
        public float ScopeFieldOfView => Mathf.Clamp(scopeFieldOfView, 5f, 60f);
        public float TransitionSpeed => Mathf.Max(0.1f, transitionSpeed);
        public float AimSensitivityMultiplier => Mathf.Clamp(aimSensitivityMultiplier, 0.05f, 2f);
        public Vector3 AimPivotPositionOffset => aimPivotPositionOffset;
        public Vector3 AimPivotEulerOffset => aimPivotEulerOffset;
        public bool HideViewmodelWhileScoped => hideViewmodelWhileScoped;
        public Texture2D ScopeOverlayTexture => scopeOverlayTexture;

        public float TargetFieldOfView => mode == WeaponAimMode.ScopeZoom
            ? ScopeFieldOfView
            : AimFieldOfView;
    }
}
