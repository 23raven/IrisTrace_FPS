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
        private IAbilitySlotProvider abilitySlots;
        private ICharacterAbility ability1;
        private ICharacterAbility ability2;
        private string ability1Binding = "<Keyboard>/q";
        private string ability2Binding = "<Keyboard>/e";
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle abilityNameStyle;

        private static readonly Color Accent = new Color(0.447f, 0.690f, 0.114f, 1f);
        private static readonly Color Warning = new Color(0.92f, 0.34f, 0.25f, 1f);
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
                if (abilitySlots == null && behaviour is IAbilitySlotProvider slotProvider)
                    abilitySlots = slotProvider;
                if (health != null && weapon != null && abilitySlots != null)
                    break;
            }

            RefreshAbilitySlots();
        }

        private void OnGUI()
        {
            if (health == null || weapon == null || abilitySlots == null)
                FindTelemetry();

            // Ability components can finish their Awake after this HUD's Awake. Refresh the slot
            // references continuously so their display does not remain blank due to script order.
            RefreshAbilitySlots();
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

            bool outOfAmmo = weapon != null && !weapon.IsReloading &&
                weapon.CurrentAmmoInMagazine <= 0 && weapon.ReserveAmmo <= 0;
            bool magazineEmpty = weapon != null && !weapon.IsReloading &&
                weapon.CurrentAmmoInMagazine <= 0 && weapon.ReserveAmmo > 0;
            GUI.color = weapon != null && weapon.IsReloading ? Accent : (outOfAmmo || magazineEmpty) ? Warning : MainText;
            string secondaryText = weapon == null ? "WEAPON UNAVAILABLE" :
                weapon.IsReloading ? "RELOADING…" :
                outOfAmmo ? "OUT OF AMMO" :
                magazineEmpty ? "MAGAZINE EMPTY" :
                weapon.WeaponDisplayName.ToUpperInvariant();
            GUI.Label(new Rect(Screen.width - panelWidth - margin + 14f, y + 31f, panelWidth - 28f, 18f), secondaryText, smallStyle);

            DrawAbilitySlots(y, panelWidth, margin);
            DrawCrosshair();
            GUI.color = previousColor;
        }

        private void RefreshAbilitySlots()
        {
            ability1 = abilitySlots != null ? abilitySlots.AbilitySlot1 : null;
            ability2 = abilitySlots != null ? abilitySlots.AbilitySlot2 : null;
            if (abilitySlots != null)
            {
                ability1Binding = abilitySlots.AbilitySlot1Binding;
                ability2Binding = abilitySlots.AbilitySlot2Binding;
            }
        }

        private void DrawAbilitySlots(float baseY, float sidePanelWidth, float margin)
        {
            float gap = 8f;
            float slotWidth = Mathf.Clamp((Screen.width - 2f * (sidePanelWidth + margin) - gap) * 0.5f, 112f, 160f);
            float totalWidth = slotWidth * 2f + gap;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float slotHeight = 40f;
            float y = baseY - slotHeight - 4f;
            DrawAbilitySlot(new Rect(startX, y, slotWidth, slotHeight), ability1, ability1Binding, 1);
            DrawAbilitySlot(new Rect(startX + slotWidth + gap, y, slotWidth, slotHeight), ability2, ability2Binding, 2);
        }

        private void DrawAbilitySlot(Rect rect, ICharacterAbility ability, string bindingPath, int slotNumber)
        {
            GUI.color = Panel;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            string binding = FormatBinding(bindingPath, slotNumber);
            string abilityName = ability != null && !string.IsNullOrWhiteSpace(ability.AbilityName)
                ? ability.AbilityName.ToUpperInvariant()
                : $"SLOT {slotNumber}";
            string title = $"{binding}  {abilityName}";
            float remaining = ability != null ? ability.CooldownRemaining : 0f;
            bool coolingDown = ability != null && remaining > 0.05f;
            IHoldCharacterAbility holdAbility = ability as IHoldCharacterAbility;

            GUI.color = MainText;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, 15f), title, abilityNameStyle);

            GUI.color = ability == null ? MainText : coolingDown ? Warning : Accent;
            string status = ability == null ? "NOT ASSIGNED" :
                holdAbility != null ? (holdAbility.IsActive ? "ACTIVE · HOLD" : "HOLD TO ACTIVATE") :
                coolingDown ? $"{remaining:0.0}s" : "READY";
            GUI.Label(new Rect(rect.x + 8f, rect.y + 20f, rect.width - 16f, 15f), status, smallStyle);
        }

        private static string FormatBinding(string path, int slotNumber)
        {
            if (string.IsNullOrWhiteSpace(path))
                return $"SLOT {slotNumber}";

            int slash = path.LastIndexOf('/');
            string control = slash >= 0 && slash < path.Length - 1 ? path.Substring(slash + 1) : path;
            switch (control.ToLowerInvariant())
            {
                case "leftbutton": return "LMB";
                case "rightbutton": return "RMB";
                case "middlebutton": return "MMB";
                case "leftctrl": return "LCTRL";
                case "rightctrl": return "RCTRL";
                case "leftshift": return "LSHIFT";
                case "rightshift": return "RSHIFT";
                case "space": return "SPACE";
                default: return control.ToUpperInvariant();
            }
        }

        private void EnsureStyles()
        {
            if (abilityNameStyle == null)
            {
                abilityNameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Screen.height / 95, 10, 13),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
            }
            abilityNameStyle.fontSize = Mathf.Clamp(Screen.height / 95, 10, 13);

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
