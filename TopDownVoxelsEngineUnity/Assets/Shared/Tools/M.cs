using System;

namespace Shared
{
    public static class M
    {
        public static int RoundToInt(float value) => (int) Math.Round(value, MidpointRounding.AwayFromZero);
        public static int FloorToInt(float value) => (int) Math.Floor(value);

        public static Vector3Int RoundToInt(Vector3 value) => new(RoundToInt(value.X), RoundToInt(value.Y), RoundToInt(value.Z));
    }
}