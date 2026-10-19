using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.UI
{
    /// <summary>
    /// Minimal dependency-free prototype HUD. It displays telemetry through contracts,
    /// so it does not reference concrete character or weapon implementations.
    /// Replace or extend this view without changing gameplay modules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FPSRuntimeHUD : MonoBehaviour
    {
        private IHealthStatus health;
        private IWeaponTelemetry weapon;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;

        private static readonly Color Accent = new Color(0.447f, 0.690f, 0.114f, 1f);
        private static readonly Color Panel = new Color(0.051f, 0.039f, 0.043f, 0.82f);
        private static readonly Color MainText = new Color(0.953f, 0.937f, 0.961f, 1f);

        private void Awake()
        {
            FindTelemetry();
        }

        private void FindTelemetry()
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (health == null && behaviour is IHealthStatus healthStatus)
                    health = healthStatus;
                if (weapon == null && behaviour is IWeaponTelemetry weaponTelemetry)
                    weapon = weaponTelemetry;
                if (health != null && weapon != null)
                    break;
            }
        }

        private void OnGUI()
        {
            if (health == null || weapon == null)
                FindTelemetry();

            EnsureStyles();
            Color previousColor = GUI.color;

            float panelWidth = Mathf.Clamp(Screen.width * 0.23f, 190f, 270f);
            float panelHeight = 56f;
            float margin = Mathf.Clamp(Screen.height * 0.025f, 14f, 28f);
            float y = Screen.height - panelHeight - margin;

            GUI.color = Panel;
            GUI.DrawTexture(new Rect(margin, y, panelWidth, panelHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - panelWidth - margin, y, panelWidth, panelHeight), Texture2D.whiteTexture);

            GUI.color = Accent;
            string healthText = health != null
                ? $"HP  {Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)}"
                : "HP  --";
            GUI.Label(new Rect(margin + 14f, y + 8f, panelWidth - 28f, 24f), healthText, labelStyle);

            GUI.color = MainText;
            string ammoText = weapon != null
                ? $"{weapon.CurrentAmmoInMagazine}  /  {weapon.ReserveAmmo}"
                : "--  /  --";
            GUI.Label(new Rect(Screen.width - panelWidth - margin + 14f, y + 8f, panelWidth - 28f, 24f), ammoText, labelStyle);

            GUI.color = weapon != null && weapon.IsReloading ? Accent : MainText;
            string secondaryText = weapon == null ? "WEAPON UNAVAILABLE" : weapon.IsReloading ? "RELOADING…" : weapon.WeaponDisplayName.ToUpperInvariant();
            GUI.Label(new Rect(Screen.width - panelWidth - margin + 14f, y + 31f, panelWidth - 28f, 18f), secondaryText, smallStyle);

            DrawCrosshair();
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Screen.height / 42, 15, 24),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
            }
            labelStyle.fontSize = Mathf.Clamp(Screen.height / 42, 15, 24);

            if (smallStyle == null)
            {
                smallStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Screen.height / 75, 10, 14),
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
            }
            smallStyle.fontSize = Mathf.Clamp(Screen.height / 75, 10, 14);
        }

        private static void DrawCrosshair()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            const float thickness = 2f;
            const float gap = 5f;
            const float length = 7f;

            GUI.color = Accent;
            GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - gap - length, thickness, length), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY + gap, thickness, length), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - gap - length, centerY - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        }
    }
}
