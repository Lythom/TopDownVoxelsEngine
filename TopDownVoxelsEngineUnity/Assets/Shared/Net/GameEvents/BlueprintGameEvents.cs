using System;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class SaveBlueprintEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterId;

        [Key(2)]
        public readonly string Name;

        [Key(3)]
        public readonly Vector3Int AnchorPosition;

        [Key(4)]
        public readonly Vector3Int Size;

        [SerializationConstructor]
        public SaveBlueprintEvent(uint id, ushort characterId, string name, Vector3Int anchorPosition, Vector3Int size) {
            Id = id;
            CharacterId = characterId;
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
        public readonly ushort CharacterId;

        [Key(2)]
        public readonly int Page;

        [Key(3)]
        public readonly int PageSize;

        [SerializationConstructor]
        public LoadBlueprintListEvent(uint id, ushort characterId, int page, int pageSize) {
            Id = id;
            CharacterId = characterId;
            Page = page;
            PageSize = pageSize;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintListResponseEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterId;

        [Key(2)]
        public readonly BlueprintMetadataV0[] Blueprints;

        [Key(3)]
        public readonly int TotalCount;

        [SerializationConstructor]
        public LoadBlueprintListResponseEvent(uint id, ushort characterId, BlueprintMetadataV0[] blueprints, int totalCount) {
            Id = id;
            CharacterId = characterId;
            Blueprints = blueprints;
            TotalCount = totalCount;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterId;

        [Key(2)]
        public readonly Guid BlueprintId;

        [SerializationConstructor]
        public LoadBlueprintEvent(uint id, ushort characterId, Guid blueprintId) {
            Id = id;
            CharacterId = characterId;
            BlueprintId = blueprintId;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintResponseEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterId;

        [Key(2)]
        public readonly BlueprintV0 Blueprint;

        [SerializationConstructor]
        public LoadBlueprintResponseEvent(uint id, ushort characterId, BlueprintV0 blueprint) {
            Id = id;
            CharacterId = characterId;
            Blueprint = blueprint;
        }
    }

    [MessagePackObject]
    public class PlaceBlueprintEvent : INetworkMessage {
        [Key(0)]
        public readonly uint Id;

        [Key(1)]
        public readonly ushort CharacterId;

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
            ushort characterId,
            Guid blueprintId,
            Vector3Int position,
            byte rotation,
            Symmetries flipOperations
        ) {
            Id = id;
            CharacterId = characterId;
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
        public readonly ushort CharacterId;

        [Key(2)]
        public readonly Vector3Int Position;

        [Key(3)]
        public readonly CellArrayV0 Cells;

        [SerializationConstructor]
        public BlueprintUpdateEvent(uint id, ushort characterId, Vector3Int position, CellArrayV0 cells) {
            Id = id;
            CharacterId = characterId;
            Position = position;
            Cells = cells;
        }
    }
}