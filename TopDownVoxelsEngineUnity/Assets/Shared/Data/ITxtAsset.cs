using System;
using Cysharp.Threading.Tasks;

namespace Shared {
    public interface ITxtAsset {
        /// <summary>
        /// Loads a text file from streaming assets asynchronously
        /// </summary>
        /// <param name="relativePath">Relative path to the text file within streaming assets</param>
        /// <returns>Content of the text file</returns>
        /// <exception cref="Exception">Thrown when file cannot be loaded</exception>
        UniTask<string> LoadTxtAsync(string relativePath);
    }
}