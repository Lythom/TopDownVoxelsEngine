using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public class Character {
        // Calculated by the tick
        public Vector3 Position;

        // Input from the CharacterAgent (via local or via network)
        public Vector3 Velocity;

        // 0 is forward on the z axis. Clockwise = positive, CounterClockwise = negative
        // Input from the CharacterAgent (via local or via network)
        public byte Angle = 0;

        public string Level = "Lobby";
        public ToolId SelectedTool;
        public BlockId SelectedBlock;
        public TemplateId SelectedTemplate;
        public byte ToolRemoveBlockLevel;
        public byte ToolAddBlockLevel;
        public byte ToolAddFurnitureLevel;
        public byte ToolReplaceBlockLevel;
        public Dictionary<BlockId, int> BlocsInventory = new();
        public List<TemplateId> KnownTemplates = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CompressAngle(float yAngle) => (byte) M.RoundToInt(yAngle * 255 / 360);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UncompressAngle(byte angle) => angle * 360 / 255f;
    }
}