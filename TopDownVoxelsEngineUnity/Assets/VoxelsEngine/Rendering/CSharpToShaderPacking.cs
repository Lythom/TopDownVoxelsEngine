using System;

namespace VoxelsEngine {
    public static class CSharpToShaderPacking {
        // handle values up to 4094
        public static float PackTwo(int a, int b) {
            if (a > 4094 || b > 4094) throw new ApplicationException("Can't handle values greater than 4094");
            return (float) (a * 4095.0 + b);
        }

        // handle values up to 254
        public static float PackThree(int a, int b, int c) {
            if (a > 254 || b > 254 || c > 254) throw new ApplicationException("Can't handle values greater than 254");
            return (float) (a * 255.0 * 255.0 + b * 255.0 + c);
        }

        public static (int, int) UnpackTwo(float f) {
            int a = (int) Math.Floor(f / 4095.0);
            int b = (int) (f - a * 4095.0);
            return (a, b);
        }

        public static (int, int, int) UnpackThree(float f) {
            int a = (int) Math.Floor(f / (255.0 * 255.0));
            int b = (int) ((f - a * 255.0 * 255.0) / 255.0);
            int c = (int) ((f - a * 255.0 * 255.0 - b * 255.0));
            return (a, b, c);
        }
    }
}