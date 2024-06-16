using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Shared;
using UnityEngine;

namespace VoxelsEngine.Tools {
    public static class StreamAssets {
        public static Texture2D FromRelativePath(string relativePath) {
            var path = GetPath(relativePath);
            if (!File.Exists(path)) throw new Exception("No image at " + path);
            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            return tex;
        }
        public static Texture2D FromAbsolutePath(string absolutePath) {
            var path = GetPath(absolutePath);
            if (!File.Exists(path)) throw new Exception("No image at " + path);
            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            return tex;
        }
        public static async UniTask ToAbsolutePath(Texture2D t, string absolutePath) {
            var path = GetPath(absolutePath);
            if (File.Exists(path)) Logr.LogError("Overriding at " + path);
            await File.WriteAllBytesAsync(absolutePath, t.EncodeToPNG());
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