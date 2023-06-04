using System.Runtime.CompilerServices;

namespace Shared {
    public enum Direction {
        North,
        South,
        West,
        East,
        Up,
        Down
    }

    public static class DirectionTools {
        private static readonly (int x, int y, int z)[] Offsets = {
            (0, 0, 1),
            (0, 0, -1),
            (-1, 0, 0),
            (1, 0, 0),
            (0, 1, 0),
            (0, -1, 0),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int x, int y, int z) GetOffset(this Direction dir) {
            return Offsets[(int) dir];
        }
    }
}