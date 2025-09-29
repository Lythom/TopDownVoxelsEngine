using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LoneStoneStudio.Tools;
using MessagePack;
using Shared.Signals;

namespace Shared {
    public static class CharacterUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CompressAngle(float yAngle) => (byte) M.RoundToInt(yAngle * 255 / 360);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UncompressAngle(byte angle) => angle * 360 / 255f;
    }

    [Union(0, typeof(CharacterV0))]
    public interface ICharacter {
    }

    [MessagePackObject(true)]
    public class CharacterV0 : ICharacter, IUpdatable<CharacterV0> {
        public string Name;

        // Calculated by the tick
        public Vector3 Position;

        // Input from the CharacterAgent (via local or via network)
        public Vector3 Velocity;

        public bool IsInAir;

        // 0 is forward on the z axis. Clockwise = positive, CounterClockwise = negative
        // Input from the CharacterAgent (via local or via network)
        public byte Angle = 0;

        public readonly Signal<string?> Level = new(null);
        public readonly Signal<byte> SelectedTool = new(0);
        public readonly Signal<BlockId> SelectedBlock = new(1);
        public readonly Signal<TemplateId> SelectedTemplate = new(TemplateId.None);
        public readonly Signal<byte> ToolRemoveBlockLevel = new(0);
        public readonly Signal<byte> ToolAddBlockLevel = new(0);
        public readonly Signal<byte> ToolAddFurnitureLevel = new(0);
        public readonly Signal<byte> ToolReplaceBlockLevel = new(0);
        public readonly SignalDictionary<BlockId, int> BlocsInventory = new();
        public readonly SignalList<TemplateId> KnownTemplates = new();

        // Blueprint anchor data
        public readonly Signal<Vector3Int?> BlueprintAnchorPosition = new(new());
        public readonly Signal<Vector3Int> BlueprintSize = new(new Vector3Int(5, 5, 5));
        [Unit("Degrees")] public readonly Signal<int> BlueprintRotation = new(0);
        public readonly Signal<Symmetries> BlueprintFlip = new(0);
        public readonly Signal<Guid?> ActiveBlueprintId = new(null);
        public readonly Signal<BlueprintMode> BlueprintMode = new(Shared.BlueprintMode.Save);

        public CharacterV0(string name, Vector3 position, string? levelName) {
            Name = name;
            Position = position;
            Velocity = Vector3.zero;
            Angle = 0;
            Level.Value = levelName;
        }

        [SerializationConstructor]
        public CharacterV0(
            string name,
            Vector3 position,
            Vector3 velocity,
            byte angle,
            Signal<string?>? level,
            Signal<byte>? selectedTool,
            Signal<BlockId>? selectedBlock,
            Signal<TemplateId>? selectedTemplate,
            Signal<byte>? toolRemoveBlockLevel,
            Signal<byte>? toolAddBlockLevel,
            Signal<byte>? toolAddFurnitureLevel,
            Signal<byte>? toolReplaceBlockLevel,
            SignalDictionary<BlockId, int>? blocsInventory,
            SignalList<TemplateId>? knownTemplates,
            Signal<Vector3Int?>? blueprintAnchorPosition,
            Signal<Vector3Int>? blueprintSize,
            Signal<int>? blueprintRotation,
            Signal<Symmetries>? blueprintFlip,
            Signal<Guid?>? activeBlueprintId,
            Signal<BlueprintMode>? blueprintMode
        ) {
            Name = name;
            Position = position;
            Velocity = velocity;
            Angle = angle;
            Level.Value = level?.Value;
            SelectedTool.Value = selectedTool?.Value ?? 0;
            SelectedBlock.Value = selectedBlock?.Value ?? BlockId.Air;
            SelectedTemplate.Value = selectedTemplate?.Value ?? TemplateId.None;
            ToolRemoveBlockLevel.Value = toolRemoveBlockLevel?.Value ?? 0;
            ToolAddBlockLevel.Value = toolAddBlockLevel?.Value ?? 0;
            ToolAddFurnitureLevel.Value = toolAddFurnitureLevel?.Value ?? 0;
            ToolReplaceBlockLevel.Value = toolReplaceBlockLevel?.Value ?? 0;
            if (blocsInventory != null) BlocsInventory.SynchronizeToTarget(blocsInventory);
            if (knownTemplates != null) KnownTemplates.SynchronizeToTarget(knownTemplates);
            BlueprintAnchorPosition.Value = blueprintAnchorPosition?.Value ?? null;
            BlueprintSize.Value = blueprintSize?.Value ?? new Vector3Int(5, 5, 5);
            BlueprintRotation.Value = blueprintRotation?.Value ?? 0;
            BlueprintFlip.Value = blueprintFlip?.Value ?? 0;
            ActiveBlueprintId.Value = activeBlueprintId?.Value ?? null;
            BlueprintMode.Value = blueprintMode?.Value ?? Shared.BlueprintMode.Save;
        }

        public void UpdateValue(CharacterV0 nextState) {
            Name = nextState.Name;
            Position = nextState.Position;
            Velocity = nextState.Velocity;
            Angle = nextState.Angle;
            Level.Value = nextState.Level.Value;
            SelectedTool.Value = nextState.SelectedTool.Value;
            SelectedBlock.Value = nextState.SelectedBlock.Value;
            SelectedTemplate.Value = nextState.SelectedTemplate.Value;
            ToolRemoveBlockLevel.Value = nextState.ToolRemoveBlockLevel.Value;
            ToolAddBlockLevel.Value = nextState.ToolAddBlockLevel.Value;
            ToolAddFurnitureLevel.Value = nextState.ToolAddFurnitureLevel.Value;
            ToolReplaceBlockLevel.Value = nextState.ToolReplaceBlockLevel.Value;
            BlocsInventory.SynchronizeToTarget(nextState.BlocsInventory);
            KnownTemplates.SynchronizeToTarget(nextState.KnownTemplates);
            BlueprintAnchorPosition.Value = nextState.BlueprintAnchorPosition.Value;
            BlueprintSize.Value = nextState.BlueprintSize.Value;
            BlueprintRotation.Value = nextState.BlueprintRotation.Value;
            BlueprintFlip.Value = nextState.BlueprintFlip.Value;
            ActiveBlueprintId.Value = nextState.ActiveBlueprintId.Value;
            BlueprintMode.Value = nextState.BlueprintMode.Value;
        }

        public static byte[] Serialize(ICharacter state) {
            return MessagePackSerializer.Serialize(state);
        }

        public static bool TryDeserializeUpdatedOrDefault(byte[]? rawData, out CharacterV0? character) {
            character = null;
            if (rawData != null) {
                try {
                    var obj = MessagePackSerializer.Deserialize<ICharacter>(rawData);
                    if (obj is CharacterV0 v0) {
                        character = v0;
                        return true;
                    }
                } catch (Exception) {
                    return false;
                }
            }

            return false;
        }
    }

    public class CharacterEqualityComparer : IEqualityComparer<CharacterV0?> {
        public static readonly CharacterEqualityComparer Instance = new();

        public bool Equals(CharacterV0? x, CharacterV0? y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(CharacterV0? obj) {
            return obj?.Name.GetHashCode() ?? -1;
        }
    }
}