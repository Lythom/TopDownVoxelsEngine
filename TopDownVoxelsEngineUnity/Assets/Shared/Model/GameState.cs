using System.Collections.Generic;

namespace Shared
{
    public class LocalState
    {
        public int CurrentPlayerId;
    }

    public class GameState
    {
        public List<Character> Characters = new();
        public List<NPC> NPCs = new();
        public List<LevelData> Levels = new();
    }

    public class Character
    {
        public Vector3 Position;

        public Vector3 Velocity;

        // 0 is forward on the z axis. Clockwise = positive, CounterClockwise = negative
        public byte Angle = 0;
        public ToolId SelectedTool;
        public BlockId SelectedBlock;
        public TemplateId SelectedTemplate;
        public byte ToolRemoveBlockLevel;
        public byte ToolAddBlockLevel;
        public byte ToolAddFurnitureLevel;
        public byte ToolReplaceBlockLevel;
        public Dictionary<BlockId, int> BlocsInventory = new();
        public List<TemplateId> KnownTemplates = new();
    }

    // ReSharper disable once InconsistentNaming
    public class NPC
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public byte Angle;
    }
}