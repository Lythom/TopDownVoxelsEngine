using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;

namespace Shared {
    public interface ITxtAsset {
        /// <summary>
        /// Loads a text file from streaming assets asynchronously
        /// </summary>
        /// <param name="path">Relative path to the text file within streaming assets</param>
        /// <returns>Content of the text file</returns>
        /// <exception cref="Exception">Thrown when file cannot be loaded</exception>
        UniTask<string> LoadTxtAsync(string path);
    }

    /**
     * Maps config files with the corresponding data
     */
    public class Registry<T> where T : class, new() {
        public Dictionary<string, T>? _data = null;

        private readonly string _resourcePath;
        private readonly string _searchPattern;
        private readonly ITxtAsset _txtAsset;

        public string ResourcePath => _resourcePath;

        private Registry(string resourcePath, string searchPattern, ITxtAsset txtAsset) {
            _searchPattern = searchPattern;
            _txtAsset = txtAsset;
            _resourcePath = resourcePath;
        }

        public static async UniTask<Registry<T>> Build(string resourcePath, string searchPattern, ITxtAsset txtAsset) {
            var r = new Registry<T>(resourcePath, searchPattern, txtAsset);
            await r.Load();
            return r;
        }

        private async UniTask Load() {
            _data = new Dictionary<string, T>();

            var indexContent = await _txtAsset.LoadTxtAsync(Path.Combine(ResourcePath, "index.txt"));
            var files = indexContent.Split('\n');
            foreach (var file in files) {
                if (string.IsNullOrWhiteSpace(file)) continue;
                // Logr.Log($"Found {jsonFile}");
                var assetPath = file.Replace(ResourcePath + Path.DirectorySeparatorChar, "");
                var fetchTxtAsync = await _txtAsset.LoadTxtAsync(Path.Combine(ResourcePath, file));
                _data[assetPath] = MessagePackSerializer.Deserialize<T>(MessagePackSerializer.ConvertFromJson(fetchTxtAsync));
            }
        }

        public Dictionary<string, T> Get() {
            if (_data != null) return _data;
            throw new InvalidOperationException("Registry not loaded");
        }

        public T? Get(string path) {
            var r = Get();
            if (r.TryGetValue(path, out var v)) return v;
            return null;
        }

        public async UniTask Reload() {
            await Load();
        }

#if UNITY_EDITOR
        public bool Editor_SaveToJson(string path, T obj) {
            try {
                string jsonText = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(obj));
                File.WriteAllText(Path.Combine(ResourcePath, path), jsonText);
                Get()[path] = obj;
                return true;
            } catch (Exception e) {
                Logr.LogException(e);
                return false;
            }
        }


        public void Remove(string path) {
            File.Delete(Path.Combine(ResourcePath, path));
            Get().Remove(path);
        }
#endif
    }
}