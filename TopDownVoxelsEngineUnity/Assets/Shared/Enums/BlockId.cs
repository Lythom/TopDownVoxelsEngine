using System;
using MessagePack;
using Sirenix.OdinInspector;

namespace Shared {
    [MessagePackObject, HideReferenceObjectPicker]
    public readonly struct BlockId : IComparable {
        [Key(0)]
        public readonly ushort Id;
        
        public static BlockId Air = new(0);


        [SerializationConstructor]
        public BlockId(ushort id) {
            Id = id;
        }

        public BlockId(int idx) {
            Id = (ushort) idx;
        }

        public static implicit operator ushort(BlockId value) => value.Id;
        public static implicit operator BlockId(ushort b) => new(b);
        public static implicit operator BlockId(int b) => new(b);

        public override string ToString() => $"BlockId({Id})";

        public int CompareTo(object obj) {
            if (obj is BlockId idx) return Id.CompareTo(idx.Id);
            return 1;
        }

        public static bool operator ==(BlockId c1, BlockId c2) {
            return c1.Equals(c2);
        }

        public static bool operator !=(BlockId c1, BlockId c2) {
            return !c1.Equals(c2);
        }

        public bool Equals(BlockId other) {
            return Id == other.Id;
        }

        public override bool Equals(object? obj) {
            return obj is BlockId other && Equals(other);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }
    }
}