using System;
using MessagePack;

namespace Shared {

    [MessagePackObject]
    public class BlueprintV0 : IBlueprint {
        [Key(0)]
        public Guid Id;

        [Key(1)]
        public string Name = "";

        [Key(2)]
        public string CreatorId = "";

        [Key(3)]
        public DateTime CreationDate;

        [Key(4)]
        public DateTime LastModifiedDate;

        [Key(5)]
        public Vector3Int Size;

        [Key(6)]
        public CellArrayV0 Cells;

        [Key(7)]
        public BlockPathMapping BlockMapping = null!;

        [Key(8)]
        public int FloorHeight = 0;

        [Key(9)]
        public Symmetries PossibleSymmetries;

        public BlueprintV0() {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
            LastModifiedDate = DateTime.UtcNow;
        }

        [SerializationConstructor]
        public BlueprintV0(
            Guid id,
            string name,
            string creatorId,
            DateTime creationDate,
            DateTime lastModifiedDate,
            Vector3Int size,
            CellArrayV0 cells,
            BlockPathMapping blockMapping,
            int floorHeight,
            Symmetries possibleSymmetries
        ) {
            Id = id;
            Name = name;
            CreatorId = creatorId;
            CreationDate = creationDate;
            LastModifiedDate = lastModifiedDate;
            Size = size;
            Cells = cells;
            BlockMapping = blockMapping;
            FloorHeight = floorHeight;
            PossibleSymmetries = possibleSymmetries;
        }
    }

    public interface IBlueprint {
    }

}