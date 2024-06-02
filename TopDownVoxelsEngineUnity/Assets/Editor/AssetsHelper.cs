using System.Linq;
using Shared;
using UnityEditor;
using VoxelsEngine.Data;
using VoxelsEngine.Tools;

namespace VoxelsEngineEditor.Editor {
    public class AssetsHelper : AssetPostprocessor {
        public static readonly Registry<MainTextureJson> MainTextureRegistry = new(StreamAssets.GetPath("Textures", "Main"), "*.json");
        public static readonly Registry<FrameTextureJson> FrameTextureRegistry = new(StreamAssets.GetPath("Textures", "Frame"), "*.json");
        public static readonly SpriteRegistry SpriteRegistry = new(StreamAssets.GetPath("Sprites"), "*.png");
        public static readonly Registry<BlockConfigJson> BlockRegistry = new(StreamAssets.GetPath("Blocks"), "*.json");

        public static string[] GetMainTextures() {
            return MainTextureRegistry.Get().Keys.ToArray();
        }

        public static string[] GetFrameTextures() {
            return FrameTextureRegistry.Get().Keys.ToArray();
        }

        public static string[] GetSpriteTextures() {
            return SpriteRegistry.Get().Keys.ToArray();
        }

        public static string[] GetBlocks() {
            return BlockRegistry.Get().Keys.ToArray();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            MainTextureRegistry.Reload();
            FrameTextureRegistry.Reload();
            SpriteRegistry.Reload();
            BlockRegistry.Reload();
        }
    }
}