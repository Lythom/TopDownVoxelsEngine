using System;

namespace VoxelsEngine {
    public static class CSharpToShaderPacking {
        public static float Pack(int a, int b) {
            return (float) (a * 4095.0 + b);
        }

        public static (int, int) Unpack(float f) {
            int a = (int) Math.Floor(f / 4095.0);
            int b = (int) (f - a * 4095.0);
            return (a, b);
        }
    }
}