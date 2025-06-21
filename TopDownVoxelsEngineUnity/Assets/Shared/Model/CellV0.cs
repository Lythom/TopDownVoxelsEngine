using System;
using MessagePack;

namespace Shared {

    [MessagePackObject()]
    public struct CellV0 {
        [Key(0)]
        public ushort Block;

        [Key(1)]
        public byte DamageLevel;

        [SerializationConstructor]
        public CellV0(ushort block, byte damageLevel) {
            Block = block;
            DamageLevel = damageLevel;
        }

        public CellV0(ushort idx) {
            Block = idx;
            DamageLevel = 0;
        }
    }

    [Union(0, typeof(CellArrayV0))]
    public interface ICellArray {
    }

    [MessagePackObject]
    public struct CellArrayV0 : ICellArray {
        [Key(0)]
        public CellV0[,,] Cells;

        [IgnoreMember]
        public bool IsGenerated;

        [SerializationConstructor]
        public CellArrayV0(CellV0[,,] cells) {
            Cells = cells;
            IsGenerated = true;
        }
        
        public CellArrayV0(CellV0[,,] cells, bool isGenerated) {
            Cells = cells;
            IsGenerated = isGenerated;
        }


        public static byte[] Serialize(CellV0[,,] cells) {
            return MessagePackSerializer.Serialize(new CellArrayV0(cells));
        }

        public static CellArrayV0 DeserializeUpdatedOrDefault(byte[]? rawData) {
            if (rawData != null) {
                ICellArray obj;
                try {
                    obj = MessagePackSerializer.Deserialize<ICellArray>(rawData);
                } catch (Exception e) {
                    try {
                        var arr = MessagePackSerializer.Deserialize<CellV0[,,]>(rawData);
                        obj = new CellArrayV0(arr);
                    } catch (Exception e2) {
                        Console.WriteLine("[ICellArray] Failed to deserialize: ICellArray and CellV0[,,].");
                        return new(new CellV0[Chunk.Size, Chunk.Height, Chunk.Size], false);
                    }
                }

                if (obj is CellArrayV0 v0) return v0;
            }

            return new(new CellV0[Chunk.Size, Chunk.Height, Chunk.Size], false);
        }
    }
}