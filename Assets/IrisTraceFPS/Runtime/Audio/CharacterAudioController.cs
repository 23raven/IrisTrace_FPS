using UnityEngine;
using IrisTrace.Runtime.Characters;

namespace IrisTrace.Runtime.Audio
{
    /// <summary>Optional first-person character sounds. Every clip is optional.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CharacterAudioController : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private FPSHeroController characterController;
        [SerializeField] private AudioSource audioSource;

        private float nextFootstepTime;
        private bool subscribed;

        public void Configure(CharacterDefinition characterDefinition, FPSHeroController controller, AudioSource source)
        {
            Unsubscribe();
            definition = characterDefinition;
            characterController = controller;
            audioSource = source != null ? source : GetComponent<AudioSource>();
            ConfigureSource();
            Subscribe();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureSource();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Update()
        {
            if (definition == null || characterController == null || audioSource == null ||
                !characterController.IsGrounded || !characterController.IsMoving)
            {
                nextFootstepTime = 0f;
                return;
            }

            if (Time.time < nextFootstepTime)
                return;

            AudioClip clip;
            float interval;
            if (characterController.IsCrouching)
            {
                clip = definition.CrouchFootstepClip != null ? definition.CrouchFootstepClip : definition.WalkFootstepClip;
                interval = definition.CrouchFootstepInterval;
            }
            else if (characterController.IsSprinting)
            {
                clip = definition.SprintFootstepClip != null ? definition.SprintFootstepClip : definition.WalkFootstepClip;
                interval = definition.SprintFootstepInterval;
            }
            else
            {
                clip = definition.WalkFootstepClip;
                interval = definition.WalkFootstepInterval;
            }

            Play(clip);
            nextFootstepTime = Time.time + Mathf.Max(0.1f, interval);
        }

        private void ResolveReferences()
        {
            if (definition == null)
                definition = GetComponent<FPSHeroController>() != null ? GetComponent<FPSHeroController>().Definition : null;
            if (characterController == null)
                characterController = GetComponent<FPSHeroController>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void ConfigureSource()
        {
            if (audioSource == null)
                return;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void Subscribe()
        {
            if (subscribed || characterController == null)
                return;
            characterController.Jumped += OnJumped;
            characterController.Landed += OnLanded;
            characterController.CrouchStarted += OnCrouchStarted;
            characterController.CrouchEnded += OnCrouchEnded;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || characterController == null)
                return;
            characterController.Jumped -= OnJumped;
            characterController.Landed -= OnLanded;
            characterController.CrouchStarted -= OnCrouchStarted;
            characterController.CrouchEnded -= OnCrouchEnded;
            subscribed = false;
        }

        private void OnJumped() => Play(definition != null ? definition.JumpClip : null);
        private void OnLanded() => Play(definition != null ? definition.LandingClip : null);
        private void OnCrouchStarted() => Play(definition != null ? definition.CrouchStartClip : null);
        private void OnCrouchEnded() => Play(definition != null ? definition.CrouchEndClip : null);

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }

        private void OnDisable() => Unsubscribe();
    }
}
