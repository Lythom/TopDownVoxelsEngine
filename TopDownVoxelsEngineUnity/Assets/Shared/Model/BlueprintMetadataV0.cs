using System;
using MessagePack;

namespace Shared {
    [MessagePackObject]
    public class BlueprintMetadataV0
    {
        [Key(0)]
        public Guid Id { get; set; }

        [Key(1)]
        public string Name { get; set; } = "";

        [Key(2)]
        public string CreatorId { get; set; } = "";

        [Key(3)]
        public DateTime CreationDate { get; set; }

        [Key(4)]
        public DateTime LastModifiedDate { get; set; }

        [Key(5)]
        public Vector3Int Size { get; set; }

        [Key(6)]
        public int FloorHeight { get; set; }

        [Key(7)]
        public Symmetries PossibleSymmetries { get; set; }
    }
}