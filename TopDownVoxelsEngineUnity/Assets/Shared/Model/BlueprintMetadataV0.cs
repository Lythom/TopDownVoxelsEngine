using System;
using MessagePack;

namespace Shared {
    [MessagePackObject]
    public struct BlueprintMetadataV0 {
        [Key(0)]
        public Guid Id;

        [Key(1)]
        public string Name;

        [Key(2)]
        public string CreatorId;

        [Key(3)]
        public DateTime CreationDate;

        [Key(4)]
        public DateTime LastModifiedDate;

        [Key(5)]
        public Vector3Int Size;

        [Key(6)]
        public int FloorHeight;

        [Key(7)]
        public Symmetries PossibleSymmetries;

        [SerializationConstructor]
        public BlueprintMetadataV0(
            Guid id,
            string name,
            string creatorId,
            DateTime creationDate,
            DateTime lastModifiedDate,
            Vector3Int size,
            int floorHeight,
            Symmetries possibleSymmetries
        ) {
            Id = id;
            Name = name;
            CreatorId = creatorId;
            CreationDate = creationDate;
            LastModifiedDate = lastModifiedDate;
            Size = size;
            FloorHeight = floorHeight;
            PossibleSymmetries = possibleSymmetries;
        }


        public static BlueprintMetadataV0 FromBlueprint(BlueprintV0 blueprint)
        {
            return new BlueprintMetadataV0(
                blueprint.Id,
                blueprint.Name,
                blueprint.CreatorId,
                blueprint.CreationDate,
                blueprint.LastModifiedDate,
                blueprint.Size,
                blueprint.FloorHeight,
                blueprint.PossibleSymmetries
            );
        }

    }
}