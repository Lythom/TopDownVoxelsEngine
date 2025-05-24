using System.Runtime.CompilerServices;

namespace Shared {
    public static class M {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(float value) => (int) (value + 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorToInt(float value) => (int) value;

        public static Vector3Int RoundToInt(Vector3 value) => new(RoundToInt(value.X), RoundToInt(value.Y), RoundToInt(value.Z));

        /// <summary>
        /// Modulo function that return a correct "offset" when reading into negative values instead of mirroring the rest of the division.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="modulo"></param>
        /// <returns></returns>
        public static uint Mod(int value, int modulo) {
            int r = value % modulo;
            return (uint) (r < 0 ? r + modulo : r);
        }
    }
}