using System;
using System.Collections.Generic;
using System.IO;
using MessagePack;

namespace Shared {
    /**
     * Maps config files with the corresponding data
     */
    public class Registry<T> where T : class, new() {
        public Dictionary<string, T>? _data = null;

        private readonly string _resourcePath;
        private readonly string _searchPattern;

        public Registry(string resourcePath, string searchPattern) {
            _searchPattern = searchPattern;
            _resourcePath = resourcePath;
        }

        public Dictionary<string, T> Get() {
            if (_data != null) return _data;
            Logr.Log($"Loading assets {typeof(T)} in {_resourcePath}{Path.DirectorySeparatorChar}{_searchPattern}.");
            _data = new Dictionary<string, T>();
            var jsonFiles = Directory.GetFiles(_resourcePath, _searchPattern, SearchOption.AllDirectories);
            foreach (var jsonFile in jsonFiles) {
                // Logr.Log($"Found {jsonFile}");
                var assetPath = jsonFile.Replace(_resourcePath + Path.DirectorySeparatorChar, "");
                _data[assetPath] = MessagePackSerializer.Deserialize<T>(MessagePackSerializer.ConvertFromJson(File.ReadAllText(jsonFile)));
            }

            return _data;
        }

        public T? Get(string path) {
            var r = Get();
            if (r.TryGetValue(path, out var v)) return v;
            return null;
        }

        public void Reload() {
            _data = null;
        }

        public bool SaveToJson(string path, T obj) {
            try {
                string jsonText = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(obj));
                File.WriteAllText(Path.Combine(_resourcePath, path), jsonText);
                Get()[path] = obj;
                return true;
            } catch (Exception e) {
                Logr.LogException(e);
                return false;
            }
        }

        public void Remove(string path) {
            File.Delete(Path.Combine(_resourcePath, path));
            Get().Remove(path);
        }
    }
}