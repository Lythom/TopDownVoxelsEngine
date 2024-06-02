using System;
using System.IO;
using UnityEngine;

namespace VoxelsEngine.Tools {
    public static class StreamAssets {
        public static Texture2D FromRelativePath(string relativePath) {
            var path = GetPath(relativePath);
            if (!File.Exists(path)) throw new Exception("No image at " + path);
            Texture2D tex = new Texture2D(1, 1);
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            return tex;
        }

        public static string GetPath(params string[] paths) {
            string combinedPath = Path.Combine(paths);
            return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, combinedPath));
        }
        public static string RelativePath(string path) {
            return path.Replace(Application.streamingAssetsPath + Path.DirectorySeparatorChar, "");
        }
    }
}