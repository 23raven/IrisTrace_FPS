using UnityEngine;

namespace IrisTrace.Runtime.Weapons
{
    /// <summary>
    /// Adds procedural recoil, reload motion, subtle camera-look sway and grounded movement bob.
    /// Only the weapon mount is animated; camera aim and hitscan direction remain unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponViewmodelMotion : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private HitscanWeapon weapon;

        private Transform cameraTransform;
        private CharacterController characterController;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 recoilPositionOffset;
        private Vector3 recoilEulerOffset;
        private Vector3 lookSwayPositionOffset;
        private Vector3 lookSwayEulerOffset;
        private Vector3 bobPositionOffset;
        private Vector3 bobEulerOffset;
        private float reloadBlend;
        private float reloadBlendTarget;
        private float bobPhase;
        private Vector3 previousCameraEuler;
        private bool hasBasePose;
        private bool hasCameraSample;

        public void Configure(WeaponDefinition weaponDefinition, HitscanWeapon weaponSource)
        {
            Unsubscribe();
            definition = weaponDefinition;
            weapon = weaponSource;
            ResolveContext();
            CaptureBasePose();
            ResetCameraSample();
            Subscribe();
        }

        private void Awake()
        {
            ResolveContext();
            CaptureBasePose();
            if (weapon == null)
                weapon = GetComponent<HitscanWeapon>();
            ResetCameraSample();
            Subscribe();
        }

        private void OnEnable()
        {
            ResolveContext();
            ResetCameraSample();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            reloadBlendTarget = 0f;
            reloadBlend = 0f;
            recoilPositionOffset = Vector3.zero;
            recoilEulerOffset = Vector3.zero;
            lookSwayPositionOffset = Vector3.zero;
            lookSwayEulerOffset = Vector3.zero;
            bobPositionOffset = Vector3.zero;
            bobEulerOffset = Vector3.zero;
            RestoreBasePose();
        }

        private void Update()
        {
            if (!hasBasePose)
                CaptureBasePose();
            if (!hasBasePose)
                return;

            ResolveContext();
            float deltaTime = Time.deltaTime;
            WeaponDefinition currentDefinition = definition;
            float recoverySpeed = currentDefinition != null ? currentDefinition.RecoilRecoverySpeed : 12f;
            float reloadSpeed = currentDefinition != null ? currentDefinition.ReloadMotionSpeed : 8f;

            float recoilRecovery = ExpBlend(recoverySpeed, deltaTime);
            recoilPositionOffset = Vector3.Lerp(recoilPositionOffset, Vector3.zero, recoilRecovery);
            recoilEulerOffset = Vector3.Lerp(recoilEulerOffset, Vector3.zero, recoilRecovery);
            reloadBlend = Mathf.MoveTowards(reloadBlend, reloadBlendTarget, reloadSpeed * deltaTime);

            UpdateLookSway(currentDefinition, deltaTime);
            UpdateMovementBob(currentDefinition, deltaTime);

            Vector3 position = baseLocalPosition + recoilPositionOffset + lookSwayPositionOffset + bobPositionOffset;
            Vector3 euler = recoilEulerOffset + lookSwayEulerOffset + bobEulerOffset;
            if (currentDefinition != null)
            {
                position += currentDefinition.ReloadDipLocalPosition * reloadBlend;
                euler += currentDefinition.ReloadDipLocalEuler * reloadBlend;
            }

            transform.localPosition = position;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(euler);
        }

        private void UpdateLookSway(WeaponDefinition currentDefinition, float deltaTime)
        {
            Vector3 targetPosition = Vector3.zero;
            Vector3 targetEuler = Vector3.zero;
            float smooth = currentDefinition != null ? currentDefinition.LookSwaySmooth : 12f;

            if (currentDefinition != null && currentDefinition.EnableLookSway && cameraTransform != null)
            {
                Vector3 currentEuler = cameraTransform.eulerAngles;
                if (!hasCameraSample)
                {
                    previousCameraEuler = currentEuler;
                    hasCameraSample = true;
                }

                // Clamp per-frame angular movement so a fast turn or frame hitch cannot fling the weapon.
                float yawDelta = Mathf.Clamp(Mathf.DeltaAngle(previousCameraEuler.y, currentEuler.y), -3f, 3f);
                float pitchDelta = Mathf.Clamp(Mathf.DeltaAngle(previousCameraEuler.x, currentEuler.x), -3f, 3f);
                previousCameraEuler = currentEuler;

                Vector3 positionAmount = currentDefinition.LookSwayPositionAmount;
                Vector3 rotationAmount = currentDefinition.LookSwayRotationAmount;
                targetPosition = new Vector3(
                    -yawDelta * positionAmount.x,
                    -pitchDelta * positionAmount.y,
                    -Mathf.Abs(yawDelta) * positionAmount.z);
                targetEuler = new Vector3(
                    -pitchDelta * rotationAmount.x,
                    yawDelta * rotationAmount.y,
                    -yawDelta * rotationAmount.z);
                targetPosition = Vector3.ClampMagnitude(targetPosition, 0.04f);
                targetEuler = Vector3.ClampMagnitude(targetEuler, 4.5f);
            }
            else if (cameraTransform != null)
            {
                previousCameraEuler = cameraTransform.eulerAngles;
                hasCameraSample = true;
            }

            float blend = ExpBlend(smooth, deltaTime);
            lookSwayPositionOffset = Vector3.Lerp(lookSwayPositionOffset, targetPosition, blend);
            lookSwayEulerOffset = Vector3.Lerp(lookSwayEulerOffset, targetEuler, blend);
        }

        private void UpdateMovementBob(WeaponDefinition currentDefinition, float deltaTime)
        {
            Vector3 targetPosition = Vector3.zero;
            Vector3 targetEuler = Vector3.zero;
            float smooth = currentDefinition != null ? currentDefinition.MovementBobSmooth : 10f;

            if (currentDefinition != null && currentDefinition.EnableMovementBobbing &&
                characterController != null && characterController.enabled && characterController.isGrounded)
            {
                Vector3 velocity = characterController.velocity;
                velocity.y = 0f;
                float speed = velocity.magnitude;
                float movementWeight = Mathf.Clamp01(speed / 4f);
                if (movementWeight > 0.015f)
                {
                    bobPhase += deltaTime * currentDefinition.MovementBobFrequency * Mathf.Lerp(0.75f, 1.2f, movementWeight);
                    float horizontalWave = Mathf.Sin(bobPhase);
                    float verticalWave = Mathf.Abs(Mathf.Sin(bobPhase * 2f)) - 0.5f;
                    Vector3 positionAmount = currentDefinition.MovementBobPositionAmount;
                    Vector3 rotationAmount = currentDefinition.MovementBobRotationAmount;
                    targetPosition = new Vector3(
                        horizontalWave * positionAmount.x,
                        verticalWave * positionAmount.y,
                        Mathf.Abs(horizontalWave) * -positionAmount.z) * movementWeight;
                    targetEuler = new Vector3(
                        Mathf.Abs(verticalWave) * rotationAmount.x,
                        horizontalWave * rotationAmount.y,
                        -horizontalWave * rotationAmount.z) * movementWeight;
                }
            }

            float blend = ExpBlend(smooth, deltaTime);
            bobPositionOffset = Vector3.Lerp(bobPositionOffset, targetPosition, blend);
            bobEulerOffset = Vector3.Lerp(bobEulerOffset, targetEuler, blend);
        }

        private void ResolveContext()
        {
            if (cameraTransform == null)
            {
                Camera parentCamera = GetComponentInParent<Camera>();
                if (parentCamera == null)
                    parentCamera = Camera.main;
                if (parentCamera != null)
                    cameraTransform = parentCamera.transform;
            }

            if (characterController == null)
                characterController = GetComponentInParent<CharacterController>();
        }

        private void ResetCameraSample()
        {
            if (cameraTransform == null)
                return;
            previousCameraEuler = cameraTransform.eulerAngles;
            hasCameraSample = true;
        }

        private static float ExpBlend(float speed, float deltaTime)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0.1f, speed) * deltaTime);
        }

        private void CaptureBasePose()
        {
            if (hasBasePose)
                return;
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            hasBasePose = true;
        }

        private void RestoreBasePose()
        {
            if (!hasBasePose)
                return;
            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
        }

        private void Subscribe()
        {
            if (weapon == null)
                return;
            weapon.ShotFired -= HandleShotFired;
            weapon.ReloadStarted -= HandleReloadStarted;
            weapon.ReloadCompleted -= HandleReloadCompleted;
            weapon.ShotFired += HandleShotFired;
            weapon.ReloadStarted += HandleReloadStarted;
            weapon.ReloadCompleted += HandleReloadCompleted;
        }

        private void Unsubscribe()
        {
            if (weapon == null)
                return;
            weapon.ShotFired -= HandleShotFired;
            weapon.ReloadStarted -= HandleReloadStarted;
            weapon.ReloadCompleted -= HandleReloadCompleted;
        }

        private void HandleShotFired()
        {
            if (definition == null)
                return;

            recoilPositionOffset += definition.RecoilLocalPosition;
            recoilPositionOffset = Vector3.ClampMagnitude(recoilPositionOffset, 0.14f);
            recoilEulerOffset += definition.RecoilLocalEuler;
            recoilEulerOffset.x = Mathf.Clamp(recoilEulerOffset.x, -12f, 12f);
            recoilEulerOffset.y = Mathf.Clamp(recoilEulerOffset.y, -6f, 6f);
            recoilEulerOffset.z = Mathf.Clamp(recoilEulerOffset.z, -6f, 6f);
        }

        private void HandleReloadStarted()
        {
            reloadBlendTarget = 1f;
        }

        private void HandleReloadCompleted()
        {
            reloadBlendTarget = 0f;
        }
    }
}
