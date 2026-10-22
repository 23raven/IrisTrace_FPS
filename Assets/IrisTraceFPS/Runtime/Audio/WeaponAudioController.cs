using UnityEngine;
using IrisTrace.Runtime.Weapons;

namespace IrisTrace.Runtime.Audio
{
    /// <summary>Plays optional weapon clips in response to weapon events without coupling audio into weapon logic.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class WeaponAudioController : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private AudioSource audioSource;
        private bool subscribed;

        public void Configure(WeaponDefinition weaponDefinition, HitscanWeapon weaponComponent, AudioSource source)
        {
            Unsubscribe();
            definition = weaponDefinition;
            weapon = weaponComponent;
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

        private void ResolveReferences()
        {
            if (weapon == null)
                weapon = GetComponent<HitscanWeapon>();
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
            if (subscribed || weapon == null)
                return;
            weapon.ShotFired += OnShotFired;
            weapon.DryFired += OnDryFired;
            weapon.ReloadStarted += OnReloadStarted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || weapon == null)
                return;
            weapon.ShotFired -= OnShotFired;
            weapon.DryFired -= OnDryFired;
            weapon.ReloadStarted -= OnReloadStarted;
            subscribed = false;
        }

        private void OnShotFired() => Play(definition != null ? definition.FireClip : null);
        private void OnDryFired() => Play(definition != null ? definition.DryFireClip : null);
        private void OnReloadStarted() => Play(definition != null ? definition.ReloadClip : null);

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }

        private void OnDisable() => Unsubscribe();
    }
}
