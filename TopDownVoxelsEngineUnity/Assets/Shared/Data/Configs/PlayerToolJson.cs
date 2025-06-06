using System;
using MessagePack;
using Shared;
using Sirenix.OdinInspector;

namespace VoxelsEngine {
    [Serializable, MessagePackObject(true)]
    public class PlayerToolJson {
        public string Name = "";
        public int SortOrder;
        public PlacementMode Placement = PlacementMode.FacingBlock;
        public PlayerToolPurpose Purpose;

        [ValueDropdown("@AssetsHelper.GetSpriteTextures()")]
        public string ToolSpritePath = "";

        
    }
}