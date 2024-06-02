using System.Runtime.CompilerServices;
using LoneStoneStudio.Tools;
using MessagePack;

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

        public readonly Reactive<string?> Level = new(null);
        public readonly Reactive<ToolId> SelectedTool = new(ToolId.None);
        public readonly Reactive<BlockId> SelectedBlock = new(1);
        public readonly Reactive<TemplateId> SelectedTemplate = new(TemplateId.None);
        public readonly Reactive<byte> ToolRemoveBlockLevel = new(0);
        public readonly Reactive<byte> ToolAddBlockLevel = new(0);
        public readonly Reactive<byte> ToolAddFurnitureLevel = new(0);
        public readonly Reactive<byte> ToolReplaceBlockLevel = new(0);
        public readonly ReactiveDictionary<BlockId, int> BlocsInventory = new();
        public readonly ReactiveList<TemplateId> KnownTemplates = new();

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

        public Character(
            string name,
            Vector3 position,
            Vector3 velocity,
            byte angle,
            Reactive<string?>? level,
            Reactive<ToolId>? selectedTool,
            Reactive<BlockId>? selectedBlock,
            Reactive<TemplateId>? selectedTemplate,
            Reactive<byte>? toolRemoveBlockLevel,
            Reactive<byte>? toolAddBlockLevel,
            Reactive<byte>? toolAddFurnitureLevel,
            Reactive<byte>? toolReplaceBlockLevel,
            ReactiveDictionary<BlockId, int>? blocsInventory,
            ReactiveList<TemplateId>? knownTemplates
        ) {
            Name = name;
            Position = position;
            Velocity = velocity;
            Angle = angle;
            Level.Value = level?.Value;
            SelectedTool.Value = selectedTool ?? ToolId.None;
            SelectedBlock.Value = selectedBlock ?? BlockId.Air;
            SelectedTemplate.Value = selectedTemplate ?? TemplateId.None;
            ToolRemoveBlockLevel.Value = toolRemoveBlockLevel ?? (byte) 0;
            ToolAddBlockLevel.Value = toolAddBlockLevel ?? (byte) 0;
            ToolAddFurnitureLevel.Value = toolAddFurnitureLevel ?? (byte) 0;
            ToolReplaceBlockLevel.Value = toolReplaceBlockLevel ?? (byte) 0;
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
}