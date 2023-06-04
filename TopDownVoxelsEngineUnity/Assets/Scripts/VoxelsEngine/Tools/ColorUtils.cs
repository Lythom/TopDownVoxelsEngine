using UnityEngine;

namespace LoneStoneStudio.Tools {
    public static class ColorUtils {
        ///The color with alpha
        public static Color WithAlpha(this Color color, float alpha) {
            color.a = alpha;
            return color;
        }

        public static string ToHex(this Color32 color) {
            return color.r.ToString("x2") + color.g.ToString("x2") + color.b.ToString("x2");
        }

        public static string ToHex(this Color color) {
            Color32 c = color;
            return c.ToHex();
        }
    }
}