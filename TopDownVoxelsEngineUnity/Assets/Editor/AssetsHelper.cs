using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VoxelsEngineEditor.Editor {
    public class AssetsHelper : AssetPostprocessor {
        private static List<string> _allMainTextures;
        private static List<string> _allFrameTextures;

        private static void RefreshTextures() {
            var mtextures = Resources.LoadAll<Texture2D>("Textures/Main");
            var ftextures = Resources.LoadAll<Texture2D>("Textures/Frame");
            _allMainTextures = mtextures.Select(t => t.name).Prepend("").ToList();
            _allFrameTextures = ftextures.Select(t => t.name).Prepend("").ToList();
        }

        public static List<string> GetMainTextures() {
            if (_allMainTextures == null) RefreshTextures();
            return _allMainTextures;
        }

        public static List<string> GetFrameTextures() {
            if (_allMainTextures == null) RefreshTextures();
            return _allFrameTextures;
        }

        void OnPostprocessTexture(Texture2D texture) {
            _allMainTextures = null;
        }
    }
}