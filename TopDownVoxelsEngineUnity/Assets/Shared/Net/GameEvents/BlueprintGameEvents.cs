using System;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class SaveBlueprintEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly string Name;

        [Key(3)]
        public readonly Vector3Int AnchorPosition;

        [Key(4)]
        public readonly Vector3Int Size;

        [SerializationConstructor]
        public SaveBlueprintEvent(uint id, ushort characterShortId, string name, Vector3Int anchorPosition, Vector3Int size) {
            Id = id;
            CharacterShortId = characterShortId;
            Name = name;
            AnchorPosition = anchorPosition;
            Size = size;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintListEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly int Page;

        [Key(3)]
        public readonly int PageSize;

        [SerializationConstructor]
        public LoadBlueprintListEvent(uint id, ushort characterShortId, int page, int pageSize) {
            Id = id;
            CharacterShortId = characterShortId;
            Page = page;
            PageSize = pageSize;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintListResponseEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly BlueprintMetadataV0[] Blueprints;

        [Key(3)]
        public readonly int TotalCount;

        [SerializationConstructor]
        public LoadBlueprintListResponseEvent(uint id, ushort characterShortId, BlueprintMetadataV0[] blueprints, int totalCount) {
            Id = id;
            CharacterShortId = characterShortId;
            Blueprints = blueprints;
            TotalCount = totalCount;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly Guid BlueprintId;

        [SerializationConstructor]
        public LoadBlueprintEvent(uint id, ushort characterShortId, Guid blueprintId) {
            Id = id;
            CharacterShortId = characterShortId;
            BlueprintId = blueprintId;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintResponseEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly BlueprintV0 Blueprint;

        [SerializationConstructor]
        public LoadBlueprintResponseEvent(uint id, ushort characterShortId, BlueprintV0 blueprint) {
            Id = id;
            CharacterShortId = characterShortId;
            Blueprint = blueprint;
        }
    }

    [MessagePackObject]
    public class PlaceBlueprintEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly Guid BlueprintId;

        [Key(3)]
        public readonly Vector3Int Position;

        [Key(4)]
        public readonly byte Rotation; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°

        [Key(5)]
        public readonly Symmetries FlipOperations;

        [SerializationConstructor]
        public PlaceBlueprintEvent(
            uint id,
            ushort characterShortId,
            Guid blueprintId,
            Vector3Int position,
            byte rotation,
            Symmetries flipOperations
        ) {
            Id = id;
            CharacterShortId = characterShortId;
            BlueprintId = blueprintId;
            Position = position;
            Rotation = rotation;
            FlipOperations = flipOperations;
        }
    }

    [MessagePackObject]
    public class BlueprintUpdateEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly Vector3Int Position;

        [Key(3)]
        public readonly CellArrayV0 Cells;

        [SerializationConstructor]
        public BlueprintUpdateEvent(uint id, ushort characterShortId, Vector3Int position, CellArrayV0 cells) {
            Id = id;
            CharacterShortId = characterShortId;
            Position = position;
            Cells = cells;
        }
    }
}