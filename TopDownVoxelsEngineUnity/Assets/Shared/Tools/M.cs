namespace Shared
{
    public static class M
    {
        public static int RoundToInt(float value) => (int) System.Math.Round(value);
        public static int FloorToInt(float value) => (int) System.Math.Floor(value);

        public static Vector3Int RoundToInt(Vector3 value) => new(RoundToInt(value.X), RoundToInt(value.Y), RoundToInt(value.Z));
    }
}