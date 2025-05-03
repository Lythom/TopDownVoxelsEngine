using System.IO;
using System.Text;
using UnityEngine;

public static class RegistryIndexGenerator {
    [UnityEditor.InitializeOnLoadMethod]
    private static void SetupBuildProcessor() {
        UnityEditor.BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
    }

    private static void BuildPlayerHandler(UnityEditor.BuildPlayerOptions options) {
        GenerateIndexes();
        UnityEditor.BuildPipeline.BuildPlayer(options);
    }

    public static void GenerateIndexes() {
        // Generate manifest for each asset directory
        GenerateDirectoryIndex(Path.Combine(Application.streamingAssetsPath, "Blocks"), "*.json");
        GenerateDirectoryIndex(Path.Combine(Application.streamingAssetsPath, "Textures", "Frame"), "*.json");
        GenerateDirectoryIndex(Path.Combine(Application.streamingAssetsPath, "Textures", "Main"), "*.json");
    }

    public static void GenerateDirectoryIndex(string directory, string searchPattern) {
        var index = new StringBuilder();
        var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
        foreach (var f in files) {
            index.Append(f.Replace(directory + Path.DirectorySeparatorChar, ""));
            index.Append("\n");
        }

        File.WriteAllText(Path.Combine(directory, "index.txt"), index.ToString());
    }
}