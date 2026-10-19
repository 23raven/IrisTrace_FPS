using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IrisTrace.Editor.UI
{
    internal static class IrisTraceTheme
    {
        private const string PreferenceKey = "IrisTrace.Theme.DarkMode";
        public static event Action ThemeChanged;

        public static bool IsDark => EditorPrefs.GetBool(PreferenceKey, true);

        public static Color Background => IsDark ? Hex(0x0D0A0B) : Hex(0xF3EFF5);
        public static Color Surface => IsDark ? new Color32(69, 73, 85, 52) : new Color32(243, 239, 245, 255);
        public static Color SurfaceStrong => IsDark ? Hex(0x454955) : new Color32(69, 73, 85, 24);
        public static Color Border => IsDark ? new Color32(69, 73, 85, 190) : new Color32(69, 73, 85, 75);
        public static Color PrimaryText => IsDark ? Hex(0xF3EFF5) : Hex(0x0D0A0B);
        public static Color SecondaryText => IsDark ? new Color32(243, 239, 245, 178) : Hex(0x454955);
        public static Color Accent => IsDark ? Hex(0x72B01D) : Hex(0x3F7D20);
        public static Color AccentHover => IsDark ? Hex(0x3F7D20) : Hex(0x72B01D);
        public static Color AccentText => IsDark ? Hex(0x0D0A0B) : Hex(0xF3EFF5);

        public static void Toggle()
        {
            EditorPrefs.SetBool(PreferenceKey, !IsDark);
            EditorApplication.delayCall += () => ThemeChanged?.Invoke();
        }

        public static void ApplyBackground(VisualElement element)
        {
            element.style.backgroundColor = Background;
        }

        private static Color Hex(uint value)
        {
            byte r = (byte)((value >> 16) & 0xFF);
            byte g = (byte)((value >> 8) & 0xFF);
            byte b = (byte)(value & 0xFF);
            return new Color32(r, g, b, 255);
        }
    }
}
