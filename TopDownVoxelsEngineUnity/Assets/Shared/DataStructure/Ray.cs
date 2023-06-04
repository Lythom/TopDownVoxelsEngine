using System;
using MessagePack;

namespace Shared {
    [Serializable, MessagePackObject]
    public struct Ray : IComparable<Ray> {
        [Key(0)]
        public Vector3 Origin;

        [Key(1)]
        public Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction) {
            Origin = origin;
            Direction = direction;
        }

        public int CompareTo(Ray other) {
            var originComparison = Origin.CompareTo(other.Origin);
            if (originComparison != 0) return originComparison;
            return Direction.CompareTo(other.Direction);
        }

#if UNITY_2020_3_OR_NEWER
        public static implicit operator UnityEngine.Ray(Ray from) => new(from.Origin, from.Direction);
        public static implicit operator Ray(UnityEngine.Ray from) => new(from.origin, from.direction);
#endif
    }
}