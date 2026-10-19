using UnityEngine;
using UnityEngine.UIElements;

namespace IrisTrace.Editor.UI
{
    /// <summary>Single fixed IrisTrace editor palette.</summary>
    internal static class IrisTraceTheme
    {
        public static Color Background => Hex(0x383838);
        public static Color Surface => Hex(0x25282B);
        public static Color SurfaceStrong => Hex(0x383B40);
        public static Color Border => new Color32(69, 73, 85, 205);
        public static Color PrimaryText => Hex(0xF3EFF5);
        public static Color SecondaryText => new Color32(243, 239, 245, 178);
        public static Color Accent => Hex(0x72B01D);
        public static Color AccentHover => Hex(0x3F7D20);
        public static Color AccentText => Hex(0x0D0A0B);

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
