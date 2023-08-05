using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shared {
    [Serializable]
    public enum Direction : byte {
        None,
        North,
        South,
        West,
        East,
        Up,
        Down
    }

    [Serializable]
    [Flags]
    public enum DirectionFlag : byte {
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
            (0, 0, 1), // North, etc.
            (0, 0, -1),
            (-1, 0, 0),
            (1, 0, 0),
            (0, 1, 0),
            (0, -1, 0),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int x, int y, int z) GetOffset(this Direction dir) {
            return Offsets[(int) dir - 1];
        }

        public static bool HasFlagFast(this DirectionFlag value, DirectionFlag flag) {
            return (value & flag) != 0;
        }

        public static bool HasFlagFast(this DirectionFlag value, Direction id) {
            return (value & ToFlag(id)) != 0;
        }

        public static DirectionFlag ToFlag(this Direction id) {
            if (id == 0) return 0;
            return (DirectionFlag) (1 << (int) id - 1);
        }

        public static List<Direction> ToList(this DirectionFlag flags) {
            var list = new List<Direction>();
            foreach (Direction value in Enum.GetValues(typeof(Direction))) {
                if (flags.HasFlagFast(value)) list.Add(value);
            }

            return list;
        }

        public static IEnumerable<Direction> AsEnumerable(this DirectionFlag flags) {
            foreach (Direction value in Enum.GetValues(typeof(Direction))) {
                if (flags.HasFlagFast(value)) yield return value;
            }
        }
    }
}