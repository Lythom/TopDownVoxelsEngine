using Shared;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class PlayerTool {
        public string Name = "";
        public int SortOrder;
        public PlacementMode Placement;
        public PlayerToolPurpose Purpose;

        [Required]
        public Texture2D Sprite = null!;

        public PlayerTool(string name, int sortOrder, PlacementMode placement, PlayerToolPurpose purpose, Texture2D sprite) {
            Name = name;
            SortOrder = sortOrder;
            Placement = placement;
            Purpose = purpose;
            Sprite = sprite;
        }
    }

}