using System;
using MessagePack;

namespace Shared
{
    [Serializable, MessagePackObject]
    public struct Vector2 : IComparable<Vector2>
    {
        [Key(0)] public float X;

        [Key(1)] public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }


        public int CompareTo(Vector2 other)
        {
            return other.X == X && other.Y == Y ? 0 : -1;
        }

        public void Deconstruct(out float x, out float y)
        {
            x = X;
            y = Y;
        }

        public static Vector2 operator *(Vector2 v, float value)
        {
            return new Vector2(v.X * value, v.Y * value);
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

#if UNITY_2020_3_OR_NEWER
        public static implicit operator UnityEngine.Vector2(Vector2 from) => new(from.X, from.Y);
        public static implicit operator Vector2(UnityEngine.Vector2 from) => new(from.x, from.y);
#endif
    }

    [Serializable, MessagePackObject]
    public struct Vector2Int : IComparable<Vector2Int>
    {
        [Key(0)] public int X;

        [Key(1)] public int Y;

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }


        public int CompareTo(Vector2Int other)
        {
            return other.X == X && other.Y == Y ? 0 : -1;
        }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.X + b.X, a.Y + b.Y);
        }

#if UNITY_2020_3_OR_NEWER
        public static implicit operator UnityEngine.Vector2Int(Vector2Int from) => new(from.X, from.Y);
        public static implicit operator Vector2Int(UnityEngine.Vector2Int from) => new(from.x, from.y);
#endif
    }

    [Serializable, MessagePackObject]
    public struct Vector3 : IComparable<Vector3>
    {
        [Key(0)] public float X;

        [Key(1)] public float Y;

        [Key(2)] public float Z;

        public static Vector3 zero = new(0, 0, 0);
        public static Vector3 one = new(1, 1, 1);
        public static Vector3 up = new(0, 1, 0);
        public static Vector3 down = new(0, -1, 0);
        public static Vector3 left = new(-1, 0, 0);
        public static Vector3 right = new(1, 0, 0);
        public static Vector3 forward = new(0, 0, 1);
        public static Vector3 back = new(0, 0, -1);

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public int CompareTo(Vector3 other)
        {
            return other.X == X && other.Y == Y && other.Z == Z ? 0 : -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3)
            {
                Vector3 other = (Vector3) obj;
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + Z.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return !a.Equals(b);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator *(Vector3 a, float scalar)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }

        public static Vector3 operator *(float scalar, Vector3 a)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }


#if UNITY_2020_3_OR_NEWER
        public static implicit operator UnityEngine.Vector3(Vector3 v) => new(v.X, v.Y, v.Z);
        public static implicit operator Vector3(UnityEngine.Vector3 from) => new(from.x, from.y, from.z);
        public static implicit operator Vector3(UnityEngine.Vector3Int from) => new(from.x, from.y, from.z);
#endif
    }

    [Serializable, MessagePackObject]
    public struct Vector3Int : IComparable<Vector3Int>
    {
        [Key(0)] public int X;

        [Key(1)] public int Y;

        [Key(2)] public int Z;

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }


        public int CompareTo(Vector3Int other)
        {
            return other.X == X && other.Y == Y && other.Z == Z ? 0 : -1;
        }

        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static implicit operator Vector3(Vector3Int from) => new(from.X, from.Y, from.Z);

#if UNITY_2020_3_OR_NEWER
        public static implicit operator UnityEngine.Vector3Int(Vector3Int v) => new(v.X, v.Y, v.Z);
        public static implicit operator UnityEngine.Vector3(Vector3Int v) => new(v.X, v.Y, v.Z);
        public static implicit operator Vector3Int(UnityEngine.Vector3Int from) => new(from.x, from.y, from.z);
#endif
    }
}