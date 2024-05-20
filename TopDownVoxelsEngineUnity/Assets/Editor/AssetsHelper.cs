using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace VoxelsEngineEditor.Editor {
    public class AssetsHelper : AssetPostprocessor {
        [CanBeNull]
        private static ValueDropdownList<string> _allMainTextures = null;

        [CanBeNull]
        private static ValueDropdownList<string> _allFrameTextures = null;

        private static void RefreshTextures() {
            var mtextures = Resources.LoadAll<TextAsset>("Textures/Main");
            _allMainTextures = new ValueDropdownList<string>();
            foreach (var textAsset in mtextures) {
                _allMainTextures.Add(textAsset.name, textAsset.text);
            }

            var ftextures = Resources.LoadAll<TextAsset>("Textures/Frame");
            _allFrameTextures = new ValueDropdownList<string>();
            foreach (var textAsset in ftextures) {
                _allFrameTextures.Add(textAsset.name, textAsset.text);
            }
        }

        public static ValueDropdownList<string> GetMainTextures() {
            RefreshTextures();
            return _allMainTextures;
        }

        public static ValueDropdownList<string> GetFrameTextures() {
            RefreshTextures();
            return _allFrameTextures;
        }

        void OnPostprocessTexture(Texture2D texture) {
            _allMainTextures = null;
            _allFrameTextures = null;
        }
    }
}