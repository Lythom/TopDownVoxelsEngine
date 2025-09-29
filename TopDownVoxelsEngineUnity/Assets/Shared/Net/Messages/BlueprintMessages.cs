using System;
using System.Runtime.CompilerServices;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {

    public static class BlueprintMessages {
        [Unit("0=0deg | 1=90deg | 2=180deg | 3=270deg")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetRotationCompressed(int rotation) => rotation switch {
            90 => 1,
            180 => 2,
            270 => 3,
            _ => 0
        };

        [Unit("Degrees")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRotationAsDegrees(byte compressedRotation) => compressedRotation switch {
            1 => 90,
            2 => 180,
            3 => 270,
            _ => 0
        };
    }

    [MessagePackObject]
    public class SaveBlueprintCommand : INetworkMessage {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly string Name;

        [Key(3)]
        public readonly short AnchorX;

        [Key(4)]
        public readonly short AnchorY;

        [Key(5)]
        public readonly short AnchorZ;

        [Key(6)]
        public readonly short SizeX;

        [Key(7)]
        public readonly short SizeY;

        [Key(8)]
        public readonly short SizeZ;

        [SerializationConstructor]
        public SaveBlueprintCommand(int id, ushort characterShortId, string name, short x, short y, short z, short sizeX, short sizeY, short sizeZ) {
            Id = id;
            CharacterShortId = characterShortId;
            Name = name;
            AnchorX = x;
            AnchorY = y;
            AnchorZ = z;
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintListQuery : INetworkMessage, INetworkQuery<LoadBlueprintListResponse> {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly int Page;

        [Key(3)]
        public readonly int PageSize;

        [SerializationConstructor]
        public LoadBlueprintListQuery(int id, ushort characterShortId, int page, int pageSize) {
            Id = id;
            CharacterShortId = characterShortId;
            Page = page;
            PageSize = pageSize;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintListResponse : INetworkMessage {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly BlueprintMetadataV0[] Blueprints;

        [Key(3)]
        public readonly int TotalCount;

        [SerializationConstructor]
        public LoadBlueprintListResponse(int id, ushort characterShortId, BlueprintMetadataV0[] blueprints, int totalCount) {
            Id = id;
            CharacterShortId = characterShortId;
            Blueprints = blueprints;
            TotalCount = totalCount;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintQuery : INetworkMessage, INetworkQuery<LoadBlueprintResponse> {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly Guid BlueprintId;

        [SerializationConstructor]
        public LoadBlueprintQuery(int id, ushort characterShortId, Guid blueprintId) {
            Id = id;
            CharacterShortId = characterShortId;
            BlueprintId = blueprintId;
        }
    }

    [MessagePackObject]
    public class LoadBlueprintResponse : INetworkMessage {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly BlueprintV0 Blueprint;

        [SerializationConstructor]
        public LoadBlueprintResponse(int id, ushort characterShortId, BlueprintV0 blueprint) {
            Id = id;
            CharacterShortId = characterShortId;
            Blueprint = blueprint;
        }
    }

    [MessagePackObject]
    public class PlaceBlueprintCommand : INetworkMessage {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly Guid BlueprintId;

        [Key(3)]
        public readonly short X;

        [Key(4)]
        public readonly short Y;

        [Key(5)]
        public readonly short Z;

        [Key(6)]
        [Unit("0 = 0deg | 1 = 90deg | 2 = 180deg | 3 = 270deg")]
        public readonly byte Rotation;

        [Key(7)]
        public readonly Symmetries FlipOperations;

        [SerializationConstructor]
        public PlaceBlueprintCommand(
            int id,
            ushort characterShortId,
            Guid blueprintId,
            short x,
            short y,
            short z,
            byte rotation,
            Symmetries flipOperations
        ) {
            Id = id;
            CharacterShortId = characterShortId;
            BlueprintId = blueprintId;
            X = x;
            Y = y;
            Z = z;
            Rotation = rotation;
            FlipOperations = flipOperations;
        }
    }

    public class ConfigureBlueprintGameEvent : GameEvent {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public readonly ushort CharacterShortId;

        [Key(2)]
        public readonly Guid? BlueprintId;

        [Key(3)]
        public readonly short AnchorX;

        [Key(4)]
        public readonly short AnchorY;

        [Key(5)]
        public readonly short AnchorZ;

        [Key(6)]
        public readonly short SizeX;

        [Key(7)]
        public readonly short SizeY;

        [Key(8)]
        public readonly short SizeZ;

        [Key(9)]
        public readonly byte Rotation; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°

        [Key(10)]
        public readonly Symmetries FlipOperations;

        public ConfigureBlueprintGameEvent(int id, ushort characterShortId, Guid? blueprintId, short anchorX, short anchorY, short anchorZ, short sizeX, short sizeY, short sizeZ, byte rotation, Symmetries flipOperations) {
            Id = id;
            CharacterShortId = characterShortId;
            BlueprintId = blueprintId;
            AnchorX = anchorX;
            AnchorY = anchorY;
            AnchorZ = anchorZ;
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            Rotation = rotation;
            FlipOperations = flipOperations;
        }

        public override int GetId() => Id;

        protected override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            var character = gameState.Characters[CharacterShortId];
            character.ActiveBlueprintId.Value = BlueprintId;
            character.BlueprintAnchorPosition.Value = new(AnchorX, AnchorY, AnchorZ);
            character.BlueprintSize.Value = new(SizeX, SizeY, SizeZ);
            character.BlueprintRotation.Value = Rotation;
            character.BlueprintFlip.Value = FlipOperations;
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterShortId)) throw new ApplicationException("Character must exists");
        }
    }
}