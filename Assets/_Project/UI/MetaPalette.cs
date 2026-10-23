using UnityEngine;

namespace Game.UI
{
    public static class MetaPalette
    {
        public static readonly Color Void = Hex(0x0c0a0b);
        public static readonly Color Stone = Hex(0x161213);
        public static readonly Color Ash = Hex(0x221c1d);
        public static readonly Color Line = Hex(0x3a2f2f);
        public static readonly Color Bone = Hex(0xeadfcd);
        public static readonly Color Dim = Hex(0x9d8f82);
        public static readonly Color Faint = Hex(0x6b5f57);
        public static readonly Color Blood = Hex(0xc3262f);
        public static readonly Color BloodDeep = Hex(0x6e1117);
        public static readonly Color Ember = Hex(0xe7843a);
        public static readonly Color Gold = Hex(0xd8b25a);
        public static readonly Color Memory = Hex(0x7cd0e0);
        public static readonly Color Magic = Hex(0xa98bff);
        public static readonly Color GemRed = Hex(0xe0414b);
        public static readonly Color GemBlue = Hex(0x4f8dff);
        public static readonly Color GemGreen = Hex(0x43c47c);

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;

            return color;
        }

        public static string Tag(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);
        }
    }
}
