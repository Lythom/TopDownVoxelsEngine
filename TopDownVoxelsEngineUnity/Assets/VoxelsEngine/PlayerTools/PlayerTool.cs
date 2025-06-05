using System;
using MessagePack;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class PlayerTool {
        public string Name = "";

        [Required]
        public Texture2D Sprite = null!;

        public PlayerTool(string name, Texture2D sprite) {
            Name = name;
            Sprite = sprite;
        }
    }

    [Serializable, MessagePackObject(true)]
    public class PlayerToolJson {
        public string Name;
        [ValueDropdown("@AssetsHelper.GetSpriteTextures()")]
        public string ToolSpritePath;
        public int SortOrder;
    }

}