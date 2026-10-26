using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Enemies
{
    /// <summary>
    /// Lightweight floating health bar for damageable enemy targets.
    /// Uses IMGUI to avoid requiring Unity UI/Canvas packages or a renderer-pipeline-specific shader.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyWorldHealthBar : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float verticalOffset = 0.18f;
        [SerializeField, Min(60f)] private float width = 104f;
        [SerializeField, Min(4f)] private float height = 8f;
        [SerializeField] private bool showNumericHealth = true;

        private const float DamageFlashDuration = 0.22f;
        private const float DefaultAnchorHeight = 1.8f;
        private static readonly Color PanelColor = new Color(0.035f, 0.03f, 0.04f, 0.88f);
        private static readonly Color HealthyColor = new Color(0.447f, 0.690f, 0.114f, 1f);
        private static readonly Color DamagedColor = new Color(0.88f, 0.20f, 0.16f, 1f);
        private static readonly Color TextColor = new Color(0.953f, 0.937f, 0.961f, 1f);
        private static readonly Color DamageFlashColor = new Color(1f, 0.82f, 0.28f, 1f);

        private IHealthStatus health;
        private Renderer[] cachedRenderers;
        private Camera cachedCamera;
        private float lastHealth = -1f;
        private float damageFlashRemaining;
        private GUIStyle healthTextStyle;

        private void Awake()
        {
            ResolveHealth();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            cachedCamera = Camera.main;
        }

        private void Update()
        {
            if (health == null)
                ResolveHealth();

            if (health != null)
            {
                float current = health.CurrentHealth;
                if (lastHealth < 0f)
                {
                    lastHealth = current;
                }
                else if (current < lastHealth - 0.01f)
                {
                    // Flash the bar briefly whenever health falls, without changing the
                    // target's material property blocks or interfering with its own damage tint.
                    damageFlashRemaining = DamageFlashDuration;
                    lastHealth = current;
                }
                else
                {
                    lastHealth = current;
                }
            }

            if (damageFlashRemaining > 0f)
                damageFlashRemaining = Mathf.Max(0f, damageFlashRemaining - Time.unscaledDeltaTime);

            if (cachedCamera == null)
                cachedCamera = Camera.main;
        }

        private void OnGUI()
        {
            if (health == null || health.IsDead || cachedCamera == null)
                return;

            Vector3 screenPoint = cachedCamera.WorldToScreenPoint(GetWorldAnchor());
            if (screenPoint.z <= 0f)
                return;

            float barWidth = Mathf.Clamp(width, 60f, 180f);
            float barHeight = Mathf.Clamp(height, 4f, 16f);
            float x = screenPoint.x - barWidth * 0.5f;
            float y = Screen.height - screenPoint.y - barHeight;
            Rect barRect = new Rect(x, y, barWidth, barHeight);

            float ratio = health.MaxHealth > 0f
                ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth)
                : 0f;
            Color fillColor = Color.Lerp(DamagedColor, HealthyColor, ratio);
            Color previousColor = GUI.color;

            if (damageFlashRemaining > 0f)
            {
                float alpha = Mathf.Clamp01(damageFlashRemaining / DamageFlashDuration);
                GUI.color = new Color(DamageFlashColor.r, DamageFlashColor.g, DamageFlashColor.b, alpha);
                GUI.DrawTexture(new Rect(barRect.x - 2f, barRect.y - 2f, barRect.width + 4f, barRect.height + 4f), Texture2D.whiteTexture);
            }

            GUI.color = PanelColor;
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);

            if (ratio > 0f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(barRect.x + 1f, barRect.y + 1f,
                    Mathf.Max(0f, (barRect.width - 2f) * ratio), Mathf.Max(0f, barRect.height - 2f)), Texture2D.whiteTexture);
            }

            if (showNumericHealth)
            {
                EnsureStyles();
                GUI.color = TextColor;
                string text = $"{Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)}";
                GUI.Label(new Rect(x, y - 17f, barWidth, 16f), text, healthTextStyle);
            }

            GUI.color = previousColor;
        }

        private Vector3 GetWorldAnchor()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
                cachedRenderers = GetComponentsInChildren<Renderer>(true);

            bool hasBounds = false;
            Bounds combined = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer candidate in cachedRenderers)
            {
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    combined = candidate.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(candidate.bounds);
                }
            }

            if (hasBounds)
                return new Vector3(combined.center.x, combined.max.y + verticalOffset, combined.center.z);

            return transform.position + Vector3.up * (DefaultAnchorHeight + verticalOffset);
        }

        private void ResolveHealth()
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component == null || component == this)
                    continue;

                if (component is IHealthStatus healthStatus)
                {
                    health = healthStatus;
                    lastHealth = health.CurrentHealth;
                    return;
                }
            }
        }

        private void EnsureStyles()
        {
            if (healthTextStyle == null)
            {
                healthTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Clamp(Screen.height / 90, 9, 13),
                    fontStyle = FontStyle.Bold,
                    clipping = TextClipping.Clip
                };
            }
            else
            {
                healthTextStyle.fontSize = Mathf.Clamp(Screen.height / 90, 9, 13);
            }
        }

        private void OnValidate()
        {
            width = Mathf.Clamp(width, 60f, 180f);
            height = Mathf.Clamp(height, 4f, 16f);
            verticalOffset = Mathf.Max(0f, verticalOffset);
        }
    }
}
