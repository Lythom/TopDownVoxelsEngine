using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LoneStoneStudio.Tools;
using MessagePack;
using Shared.Signals;
using TinkState;

namespace Shared {
    [MessagePackObject(true)]
    public class Character : IUpdatable<Character> {
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
        public readonly Signal<ToolId> SelectedTool = new(ToolId.None);
        public readonly Signal<BlockId> SelectedBlock = new(1);
        public readonly Signal<TemplateId> SelectedTemplate = new(TemplateId.None);
        public readonly Signal<byte> ToolRemoveBlockLevel = new(0);
        public readonly Signal<byte> ToolAddBlockLevel = new(0);
        public readonly Signal<byte> ToolAddFurnitureLevel = new(0);
        public readonly Signal<byte> ToolReplaceBlockLevel = new(0);
        public readonly SignalDictionary<BlockId, int> BlocsInventory = new();
        public readonly SignalList<TemplateId> KnownTemplates = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CompressAngle(float yAngle) => (byte) M.RoundToInt(yAngle * 255 / 360);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UncompressAngle(byte angle) => angle * 360 / 255f;

        public Character(string name, Vector3 position, string? levelName) {
            Name = name;
            Position = position;
            Velocity = Vector3.zero;
            Angle = 0;
            Level.Value = levelName;
        }

        [SerializationConstructor]
        public Character(
            string name,
            Vector3 position,
            Vector3 velocity,
            byte angle,
            Signal<string?>? level,
            Signal<ToolId>? selectedTool,
            Signal<BlockId>? selectedBlock,
            Signal<TemplateId>? selectedTemplate,
            Signal<byte>? toolRemoveBlockLevel,
            Signal<byte>? toolAddBlockLevel,
            Signal<byte>? toolAddFurnitureLevel,
            Signal<byte>? toolReplaceBlockLevel,
            SignalDictionary<BlockId, int>? blocsInventory,
            SignalList<TemplateId>? knownTemplates
        ) {
            Name = name;
            Position = position;
            Velocity = velocity;
            Angle = angle;
            Level.Value = level?.Value;
            SelectedTool.Value = selectedTool?.Value ?? ToolId.None;
            SelectedBlock.Value = selectedBlock?.Value ?? BlockId.Air;
            SelectedTemplate.Value = selectedTemplate?.Value ?? TemplateId.None;
            ToolRemoveBlockLevel.Value = toolRemoveBlockLevel?.Value ?? 0;
            ToolAddBlockLevel.Value = toolAddBlockLevel?.Value ?? 0;
            ToolAddFurnitureLevel.Value = toolAddFurnitureLevel?.Value ?? 0;
            ToolReplaceBlockLevel.Value = toolReplaceBlockLevel?.Value ?? 0;
            if (blocsInventory != null) BlocsInventory.SynchronizeToTarget(blocsInventory);
            if (knownTemplates != null) KnownTemplates.SynchronizeToTarget(knownTemplates);
        }

        public void UpdateValue(Character nextState) {
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
        }
    }

    public class CharacterEqualityComparer : IEqualityComparer<Character?> {

        public static readonly CharacterEqualityComparer Instance = new();

        public bool Equals(Character? x, Character? y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(Character? obj) {
            return obj?.Name.GetHashCode() ?? -1;
        }
    }
}