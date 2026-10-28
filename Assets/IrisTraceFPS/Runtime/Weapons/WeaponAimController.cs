using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Weapons
{
    /// <summary>
    /// Applies an optional weapon-specific aim profile. It moves a dedicated parent pivot,
    /// leaving WeaponViewmodelMotion free to animate the child weapon mount independently.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponAimController : MonoBehaviour, IAimState
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform aimPivot;
        [SerializeField] private GameObject weaponVisualRoot;
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private MonoBehaviour inputSource;

        private IAimInput aimInput;
        private WeaponAimProfile currentProfile;
        private float baseFieldOfView = 90f;
        private Vector3 basePivotPosition;
        private Quaternion basePivotRotation = Quaternion.identity;
        private bool baseVisualActive = true;
        private bool initialized;
        private bool isAiming;

        public bool IsAiming => isAiming;
        public float LookSensitivityMultiplier => isAiming && currentProfile != null
            ? currentProfile.AimSensitivityMultiplier
            : 1f;

        public void Configure(Camera viewCamera, Transform pivot, GameObject visuals,
            WeaponDefinition definition, MonoBehaviour inputComponent)
        {
            playerCamera = viewCamera;
            aimPivot = pivot;
            weaponVisualRoot = visuals;
            weaponDefinition = definition;
            inputSource = inputComponent;
            aimInput = inputComponent as IAimInput;
            if (Application.isPlaying)
                CaptureBaseState();
        }

        private void Awake()
        {
            ResolveInput();
            CaptureBaseState();
        }

        private void OnEnable()
        {
            ResolveInput();
            if (Application.isPlaying)
                CaptureBaseState();
        }

        private void Update()
        {
            ResolveInput();
            if (!initialized)
                CaptureBaseState();

            currentProfile = weaponDefinition != null ? weaponDefinition.AimProfile : null;
            bool profileAllowsAim = currentProfile != null && currentProfile.Mode != WeaponAimMode.Disabled;
            isAiming = profileAllowsAim && aimInput != null && aimInput.AimHeld;

            float speed = currentProfile != null ? currentProfile.TransitionSpeed : 12f;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.1f, speed) * Time.deltaTime);

            if (playerCamera != null)
            {
                float targetFov = isAiming && currentProfile != null
                    ? currentProfile.TargetFieldOfView
                    : baseFieldOfView;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, blend);
            }

            if (aimPivot != null)
            {
                Vector3 targetPosition = basePivotPosition;
                Quaternion targetRotation = basePivotRotation;
                if (isAiming && currentProfile != null && currentProfile.Mode == WeaponAimMode.AimDownSights)
                {
                    targetPosition += currentProfile.AimPivotPositionOffset;
                    targetRotation = basePivotRotation * Quaternion.Euler(currentProfile.AimPivotEulerOffset);
                }
                aimPivot.localPosition = Vector3.Lerp(aimPivot.localPosition, targetPosition, blend);
                aimPivot.localRotation = Quaternion.Slerp(aimPivot.localRotation, targetRotation, blend);
            }

            if (weaponVisualRoot != null)
            {
                bool hideForScope = isAiming && currentProfile != null &&
                    currentProfile.Mode == WeaponAimMode.ScopeZoom && currentProfile.HideViewmodelWhileScoped;
                if (weaponVisualRoot.activeSelf == hideForScope)
                    weaponVisualRoot.SetActive(!hideForScope);
            }
        }

        private void OnGUI()
        {
            if (!isAiming || currentProfile == null || currentProfile.Mode != WeaponAimMode.ScopeZoom)
                return;

            Texture2D overlay = currentProfile.ScopeOverlayTexture;
            if (overlay == null)
                return;

            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlay, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void OnDisable()
        {
            isAiming = false;
            if (playerCamera != null)
                playerCamera.fieldOfView = baseFieldOfView;
            if (aimPivot != null)
            {
                aimPivot.localPosition = basePivotPosition;
                aimPivot.localRotation = basePivotRotation;
            }
            if (weaponVisualRoot != null)
                weaponVisualRoot.SetActive(baseVisualActive);
        }

        private void ResolveInput()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>(true);
            if (inputSource == null || aimInput == null)
            {
                MonoBehaviour[] components = GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour component in components)
                {
                    if (component is IAimInput input)
                    {
                        inputSource = component;
                        aimInput = input;
                        break;
                    }
                }
            }
        }

        private void CaptureBaseState()
        {
            if (initialized)
                return;
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera != null)
                baseFieldOfView = playerCamera.fieldOfView;
            if (aimPivot != null)
            {
                basePivotPosition = aimPivot.localPosition;
                basePivotRotation = aimPivot.localRotation;
            }
            if (weaponVisualRoot != null)
                baseVisualActive = weaponVisualRoot.activeSelf;
            initialized = true;
        }
    }
}
