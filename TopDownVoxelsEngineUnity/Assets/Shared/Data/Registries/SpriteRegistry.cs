using System.Collections.Generic;
using System.IO;

namespace Shared {
    public class SpriteRegistry {
        private Dictionary<string, string>? _spriteTextures = null;
        private readonly string _searchPattern;
        private readonly string _resourcePath;

        public SpriteRegistry(string resourcePath, string searchPattern) {
            _searchPattern = searchPattern;
            _resourcePath = resourcePath;
        }

        public Dictionary<string, string> Get() {
            if (_spriteTextures != null) return _spriteTextures;
            var files = Directory.GetFiles(_resourcePath, _searchPattern, SearchOption.AllDirectories);
            _spriteTextures = new Dictionary<string, string>();
            foreach (var filePath in files) {
                var assetPath = filePath.Replace(_resourcePath + Path.DirectorySeparatorChar, "");
                _spriteTextures[assetPath] = filePath;
            }

            return _spriteTextures;
        }
        
        public string? Get(string path) {
            var r = Get();
            if (r.TryGetValue(path, out var v)) return v;
            return null;
        }

        public void Reload() {
            _spriteTextures = null;
        }
    }
}