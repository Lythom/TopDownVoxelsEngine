using System.IO;
using Cysharp.Threading.Tasks;
using Shared;

namespace Server;

public class AssetTxtFetcher : ITxtAsset {
    public async UniTask<string> LoadTxtAsync(string path) {
        return await File.ReadAllTextAsync(path);
    }
}