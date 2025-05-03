using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Shared;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace VoxelsEngine.Tools {
    public interface IStreamAssets : ITxtAsset {
        /// <summary>
        /// Loads a texture from streaming assets asynchronously
        /// </summary>
        /// <param name="relativePath">Relative path to the texture within streaming assets</param>
        /// <returns>Loaded Texture2D object</returns>
        /// <exception cref="Exception">Thrown when texture cannot be loaded</exception>
        UniTask<Texture2D> LoadTexture2DAsync(string relativePath);

        /// <summary>
        /// Combines the provided path segments with the streaming assets path
        /// </summary>
        /// <param name="relativePath">Path segments to combine</param>
        /// <returns>Full path to the resource</returns>
        string GetPath(params string[] relativePath);
    }

    public class StreamAssetsFilesAdapter : IStreamAssets {
        public UniTask<string> LoadTxtAsync(string path) => StreamAssetsFiles.LoadTxtAsync(path);
        public UniTask<Texture2D> LoadTexture2DAsync(string relativePath) => StreamAssetsFiles.LoadTexture2DAsync(relativePath);
        public string GetPath(params string[] relativePath) => StreamAssetsFiles.GetPath(relativePath);
    }

    public static class StreamAssetsFiles {
        public static async UniTask<string> LoadTxtAsync(string path) {
            return await File.ReadAllTextAsync(GetPath(path));
        }

        /// <summary>
        /// Textures are expected to be in the StreamingAssets folder
        /// </summary>
        /// <param name="relativePath">path from the streamingAssetsPath folder</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async UniTask<Texture2D> LoadTexture2DAsync(string relativePath) {
            var path = GetPath(relativePath);
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentNullException(nameof(relativePath));
            if (!File.Exists(path))
                throw new FileNotFoundException($"Image not found at path: {path}", path);

            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            try {
                tex.LoadImage(await File.ReadAllBytesAsync(path));
                return tex;
            } catch (Exception e) {
                Object.Destroy(tex); // Prevent memory leak
                throw new Exception($"Failed to load texture at {path}", e);
            }
        }

        public static string GetPath(params string[] paths) {
            string combinedPath = Path.Combine(paths);
            return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, combinedPath));
        }
    }

    public class StreamAssetsWeb : IStreamAssets {
        public async UniTask<string> LoadTxtAsync(string path) {
            string fullPath = GetPath(path);
            using var request = UnityWebRequest.Get(fullPath);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                throw new Exception($"Failed to load text file at {fullPath}: {request.error}");
            }

            return request.downloadHandler.text;
        }

        public async UniTask<Texture2D> LoadTexture2DAsync(string relativePath) {
            string fullPath = GetPath(relativePath);
            using var request = UnityWebRequestTexture.GetTexture(fullPath);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                throw new Exception($"Failed to load texture at {fullPath}: {request.error}");
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        public string GetPath(params string[] relativePath) {
            // In WebGL, StreamingAssets are served from a URL
            // COMMENT: Path.Combine adapted to webgl context?
            return $"{Application.streamingAssetsPath}/{Path.Combine(relativePath)}";
        }
    }

    public static class StreamAssetsFetcherFactory {
        public static IStreamAssets Create() {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                return new StreamAssetsWeb();
            }

            return new StreamAssetsFilesAdapter();
        }
    }
}