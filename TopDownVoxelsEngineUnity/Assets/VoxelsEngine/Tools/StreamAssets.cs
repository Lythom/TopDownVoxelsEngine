using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace VoxelsEngine.Tools {
    public static class StreamAssets {
        
        
        public static async UniTask<Texture2D> FromRelativePath(string relativePath) {
            var path = GetPath(relativePath);
            return await LoadTextureFromPath(path);
        }

        public static async UniTask<Texture2D> FromAbsolutePath(string absolutePath) {
            var path = GetPath(absolutePath);
            return await LoadTextureFromPath(path);
        }

        private static async UniTask<Texture2D> LoadTextureFromPath(string path) {
            string uri = Path.Combine(Application.streamingAssetsPath, path);

#if UNITY_WEBGL
            // In WebGL, we need to use the full URL
            uri = "StreamingAssets/" + path;
#endif

            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uri)) {
                await webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success) {
                    throw new Exception($"Failed to load texture at {path}: {webRequest.error}");
                }

                return DownloadHandlerTexture.GetContent(webRequest);
            }
        }

        public static async UniTask ToAbsolutePath(Texture2D t, string absolutePath) {
#if !UNITY_WEBGL
            var path = GetPath(absolutePath);
            if (File.Exists(path)) Logr.LogError("Overriding at " + path);
            await File.WriteAllBytesAsync(absolutePath, t.EncodeToPNG());
#else
            Debug.LogWarning("Saving files is not supported in WebGL builds");
#endif
        }

        public static string GetPath(params string[] paths) {
            string combinedPath = Path.Combine(paths);
#if !UNITY_WEBGL
            return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, combinedPath));
#else
            return combinedPath.Replace('\\', '/');
#endif
        }

        public static string RelativePath(string path) {
#if !UNITY_WEBGL
            return path.Replace(Application.streamingAssetsPath + Path.DirectorySeparatorChar, "");
#else
            return path.Replace("StreamingAssets/", "").Replace('\\', '/');
#endif
        }
    }
}