using System;
using System.Runtime.CompilerServices;

namespace Shared {
    [Serializable]
    public enum Direction : byte {
        North,
        South,
        West,
        East,
        Up,
        Down
    }

    [Serializable]
    [Flags]
    public enum DirectionFlags : byte {
        None = 0b00000000,
        All = 0b00111111,
        North = 1 << 0,
        South = 1 << 1,
        West = 1 << 2,
        East = 1 << 3,
        Up = 1 << 4,
        Down = 1 << 5,
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