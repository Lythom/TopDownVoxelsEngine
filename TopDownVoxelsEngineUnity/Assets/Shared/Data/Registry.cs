using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using UnityEngine;

namespace Shared {
    /**
     * Maps config files with the corresponding data
     */
    public class Registry<T> where T : class {
        private Dictionary<string, T>? _data = null;
        private bool _isLoaded = false;
        public bool IsLoaded => _isLoaded;

        private readonly string _resourcePath;
        private readonly string _searchPattern;
        private readonly ITxtAsset _txtAsset;

        public string ResourcePath => _resourcePath;

        protected Registry(string resourcePath, string searchPattern, ITxtAsset txtAsset) {
            _searchPattern = searchPattern;
            _txtAsset = txtAsset;
            _resourcePath = resourcePath;
        }

        public static async UniTask<Registry<T>> Build(string relativePath, string searchPattern, ITxtAsset txtAsset) {
            var r = new Registry<T>(relativePath, searchPattern, txtAsset);
            await r.Load();
            return r;
        }

        protected async UniTask Load() {
            _data = new Dictionary<string, T>();

            Logr.Log($"Loading registry from {Path.Combine(ResourcePath, "index.txt")}");
            var indexContent = await _txtAsset.LoadTxtAsync(Path.Combine(ResourcePath, "index.txt"));
            var files = indexContent.Split('\n');
            foreach (var file in files) {
                if (string.IsNullOrWhiteSpace(file)) continue;
                var assetPath = file.Replace(ResourcePath + Path.DirectorySeparatorChar, "");
                var relativePath = Path.Combine(ResourcePath, file);
                if (file.EndsWith(".json")) {
                    Logr.Log($"Found file {file}. Loading {Path.Combine(ResourcePath, "index.txt")}");
                    var fetchTxtAsync = await _txtAsset.LoadTxtAsync(relativePath);
                    _data[assetPath] = MessagePackSerializer.Deserialize<T>(MessagePackSerializer.ConvertFromJson(fetchTxtAsync));
                } else if (relativePath is T filePath) {
                    Logr.Log($"Found file {file}. Adding raw reference to registry.");
                    _data[assetPath] = filePath;
                }
            }

            _isLoaded = true;
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
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, ResourcePath, path), jsonText);
                Get()[path] = obj;
                return true;
            } catch (Exception e) {
                Logr.LogException(e);
                return false;
            }
        }


        public void Remove(string path) {
            File.Delete(Path.Combine(Application.streamingAssetsPath, ResourcePath, path));
            Get().Remove(path);
        }
#endif
    }
}