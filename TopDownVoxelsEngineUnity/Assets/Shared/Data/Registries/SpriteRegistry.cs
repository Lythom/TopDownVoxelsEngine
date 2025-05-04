using Cysharp.Threading.Tasks;

namespace Shared {
    public class SpriteRegistry : Registry<string> {
        protected SpriteRegistry(string resourcePath, string searchPattern, ITxtAsset txtAsset) : base(resourcePath, searchPattern, txtAsset) {
        }

        public new static async UniTask<SpriteRegistry> Build(string relativePath, string searchPattern, ITxtAsset txtAsset) {
            var r = new SpriteRegistry(relativePath, searchPattern, txtAsset);
            await r.Load();
            return r;
        }
    }
}