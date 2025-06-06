using UnityEditor;
using UnityEngine;

public class StreamingAssetsPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets, 
        string[] deletedAssets, 
        string[] movedAssets, 
        string[] movedFromAssetPaths)
    {
        bool shouldGenerateIndexes = false;
        // Check imported assets
        foreach (string asset in importedAssets) {
            if (asset.StartsWith("Assets/StreamingAssets/")) {
                shouldGenerateIndexes = true;
                break;
            }
        }
        // Check deleted assets
        if (!shouldGenerateIndexes) {
            foreach (string asset in deletedAssets) {
                if (asset.StartsWith("Assets/StreamingAssets/")) {
                    shouldGenerateIndexes = true;
                    break;
                }
            }
        }
        // Check moved assets
        if (!shouldGenerateIndexes) {
            foreach (string asset in movedAssets) {
                if (asset.StartsWith("Assets/StreamingAssets/")) {
                    shouldGenerateIndexes = true;
                    break;
                }
            }
        }
        if (shouldGenerateIndexes) {
            RegistryIndexGenerator.GenerateIndexes();
            Debug.Log("Indexes regenerated due to StreamingAssets changes");
        }
    }
}