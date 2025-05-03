#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Shared;
using Sirenix.OdinInspector;
using UnityEditor;
using VoxelsEngine;
using VoxelsEngine.Data;
using VoxelsEngine.Tools;

namespace VoxelsEngineEditor.Editor {
    public class AssetsHelper : AssetPostprocessor {
        public static string[]? GetMainTextures() {
            return Configurator.Instance.MainTextureRegistry?.Get().Keys.ToArray();
        }

        [ItemCanBeNull]
        public static ValueDropdownList<string?> GetFrameTextures() {
            var arr = new ValueDropdownList<string?> {{"", "null"}};
            var keyCollection = Configurator.Instance.FrameTextureRegistry?.Get().Keys;
            if (keyCollection is null) return arr;
            foreach (var key in keyCollection) {
                arr.Add(key, key);
            }

            return arr;
        }

        public static string[]? GetSpriteTextures() {
            return Configurator.Instance.SpriteRegistry?.Get().Keys.ToArray();
        }

        public static string[]? GetBlocks() {
            return Configurator.Instance.BlockRegistry?.Get().Keys.ToArray();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            // Configurator.Instance.MainTextureRegistry?.Reload();
            // Configurator.Instance.FrameTextureRegistry?.Reload();
            // Configurator.Instance.SpriteRegistry?.Reload();
            // Configurator.Instance.BlockRegistry?.Reload();
        }
    }
}