using System.Runtime.CompilerServices;

namespace Tools
{
    public static class MortonCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Encode(uint x, uint y)
        {
            x = (x | x << 16) & 0x0000FFFF;
            x = (x | x << 8) & 0x00FF00FF;
            x = (x | x << 4) & 0x0F0F0F0F;
            x = (x | x << 2) & 0x33333333;
            x = (x | x << 1) & 0x55555555;

            y = (y | y << 16) & 0x0000FFFF;
            y = (y | y << 8) & 0x00FF00FF;
            y = (y | y << 4) & 0x0F0F0F0F;
            y = (y | y << 2) & 0x33333333;
            y = (y | y << 1) & 0x55555555;


            return x | (y << 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Decode(uint morton, out uint x, out uint y)
        {
            x = morton & 0x55555555;
            x = (x ^ (x >> 1)) & 0x33333333;
            x = (x ^ (x >> 2)) & 0x0F0F0F0F;
            x = (x ^ (x >> 4)) & 0x00FF00FF;
            x = (x ^ (x >> 8)) & 0x0000FFFF;

            y = (morton >> 1) & 0x55555555;
            y = (y ^ (y >> 1)) & 0x33333333;
            y = (y ^ (y >> 2)) & 0x0F0F0F0F;
            y = (y ^ (y >> 4)) & 0x00FF00FF;
            y = (y ^ (y >> 8)) & 0x0000FFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Encode(uint x, uint y, uint z)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;

            y = (y | (y << 16)) & 0x030000FF;
            y = (y | (y << 8)) & 0x0300F00F;
            y = (y | (y << 4)) & 0x030C30C3;
            y = (y | (y << 2)) & 0x09249249;

            z = (z | (z << 16)) & 0x030000FF;
            z = (z | (z << 8)) & 0x0300F00F;
            z = (z | (z << 4)) & 0x030C30C3;
            z = (z | (z << 2)) & 0x09249249;

            return x | (y << 1) | (z << 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Decode(uint morton, out uint x, out uint y, out uint z)
        {
            x = morton & 0x9249249;
            x = (x ^ (x >> 2)) & 0x30c30c3;
            x = (x ^ (x >> 4)) & 0x0300f00f;
            x = (x ^ (x >> 8)) & 0x30000ff;
            x = (x ^ (x >> 16)) & 0x000003ff;

            y = (morton >> 1) & 0x9249249;
            y = (y ^ (y >> 2)) & 0x30c30c3;
            y = (y ^ (y >> 4)) & 0x0300f00f;
            y = (y ^ (y >> 8)) & 0x30000ff;
            y = (y ^ (y >> 16)) & 0x000003ff;

            z = (morton >> 2) & 0x9249249;
            z = (z ^ (z >> 2)) & 0x30c30c3;
            z = (z ^ (z >> 4)) & 0x0300f00f;
            z = (z ^ (z >> 8)) & 0x30000ff;
            z = (z ^ (z >> 16)) & 0x000003ff;
        }
    }
}