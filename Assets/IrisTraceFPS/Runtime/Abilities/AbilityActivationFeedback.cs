using UnityEngine;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>
    /// Lightweight feedback for successful ability activations. It can flash the screen edge
    /// and optionally spawn a per-slot VFX prefab without coupling abilities to the HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityActivationFeedback : MonoBehaviour
    {
        [SerializeField] private AbilityInputHandler inputHandler;
        [SerializeField] private GameObject ability1VfxPrefab;
        [SerializeField] private GameObject ability2VfxPrefab;
        [SerializeField] private Transform effectAnchor;
        [SerializeField, Min(0.05f)] private float flashDuration = 0.2f;
        [SerializeField, Range(0f, 0.4f)] private float flashOpacity = 0.14f;
        [SerializeField, Min(0.1f)] private float fallbackVfxLifetime = 2.5f;

        private float flashRemaining;
        private Color flashColor = new Color(0.35f, 0.8f, 1f, 1f);
        private bool subscribed;

        public void Configure(AbilityInputHandler handler)
        {
            if (inputHandler != handler)
                Unsubscribe();

            inputHandler = handler;
            Subscribe();
        }

        private void Awake()
        {
            if (inputHandler == null)
                inputHandler = GetComponent<AbilityInputHandler>();
        }

        private void OnEnable()
        {
            if (inputHandler == null)
                inputHandler = GetComponent<AbilityInputHandler>();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (flashRemaining > 0f)
                flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || inputHandler == null || subscribed)
                return;

            inputHandler.AbilityActivated += HandleAbilityActivated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (inputHandler != null)
                inputHandler.AbilityActivated -= HandleAbilityActivated;
            subscribed = false;
        }

        private void HandleAbilityActivated(int slot, string abilityName)
        {
            flashColor = ResolveColor(abilityName);
            flashRemaining = Mathf.Max(0.05f, flashDuration);
            SpawnVfx(slot);
        }

        private void SpawnVfx(int slot)
        {
            GameObject prefab = slot == 1 ? ability1VfxPrefab : slot == 2 ? ability2VfxPrefab : null;
            if (prefab == null)
                return;

            Transform anchor = effectAnchor != null ? effectAnchor : transform;
            GameObject instance = Instantiate(prefab, anchor.position, anchor.rotation);
            instance.name = prefab.name + " (Ability VFX)";

            float lifetime = Mathf.Max(0.1f, fallbackVfxLifetime);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                if (!main.loop)
                    lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            }

            Destroy(instance, Mathf.Clamp(lifetime, 0.1f, 30f));
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || flashRemaining <= 0f || Screen.width <= 0 || Screen.height <= 0)
                return;

            float duration = Mathf.Max(0.05f, flashDuration);
            float alpha = flashOpacity * Mathf.Clamp01(flashRemaining / duration);
            if (alpha <= 0.001f)
                return;

            Color previous = GUI.color;
            GUI.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            float edge = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.022f, 10f, 28f);
            Texture texture = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, edge), texture);
            GUI.DrawTexture(new Rect(0f, Screen.height - edge, Screen.width, edge), texture);
            GUI.DrawTexture(new Rect(0f, edge, edge, Screen.height - 2f * edge), texture);
            GUI.DrawTexture(new Rect(Screen.width - edge, edge, edge, Screen.height - 2f * edge), texture);
            GUI.color = previous;
        }

        private static Color ResolveColor(string abilityName)
        {
            string value = string.IsNullOrWhiteSpace(abilityName) ? string.Empty : abilityName.ToLowerInvariant();
            if (value.Contains("heal") || value.Contains("medic"))
                return new Color(0.25f, 0.95f, 0.45f, 1f);
            if (value.Contains("dash") || value.Contains("blink"))
                return new Color(0.25f, 0.8f, 1f, 1f);
            if (value.Contains("sprint") || value.Contains("speed"))
                return new Color(0.55f, 0.85f, 0.2f, 1f);
            return new Color(0.45f, 0.69f, 0.11f, 1f);
        }
    }
}
