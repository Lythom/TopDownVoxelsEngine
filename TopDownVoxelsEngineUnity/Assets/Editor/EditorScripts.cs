using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using MessagePack;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using VoxelsEngine.Data;
using Debug = UnityEngine.Debug;

public class EditorScripts : MonoBehaviour {
    private const string ScriptBuildPath = "Temp/PlayerScriptBuildTest";

    [MenuItem("Tools/Generate MessagePack resolvers", priority = -2000)]
    public static async void InstantCodeGen() {
        Debug.Log("Generating MessagePack Files");
        try {
            var log = await InvokeProcessStartAsync("dotnet", "mpc -i . -o ./VoxelsEngine/MessagePackGenerated");
            if (log.Contains("Fail")) {
                Debug.LogError(log);
            } else {
                Debug.Log(log);
            }
        } catch (Exception e) {
            Debug.LogException(e);
        } finally {
            await UniTask.SwitchToMainThread();
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/Check Scripts", priority = -2000)]
    private static void CheckScripts() {
        var buildSettings = new ScriptCompilationSettings() {
            group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
            target = EditorUserBuildSettings.activeBuildTarget,
            options = ScriptCompilationOptions.None
        };

        var results = PlayerBuildInterface.CompilePlayerScripts(buildSettings, ScriptBuildPath);

        if ((results.assemblies == null || results.assemblies.Any() == false) && results.typeDB == null) {
            Debug.LogError("Build failed");
        } else {
            Debug.Log("CheckScripts: no errors.");
        }
    }


    // Ajoute une entrée au menu contextuel de la vue Projet
    [MenuItem("Assets/CreateTextureConfig", true)]
    private static bool ValidateCreateTextureConfigAction() {
        // Valide qu'exactement trois éléments sont sélectionnés
        return Selection.objects.Length == 3;
    }

    // Ajoute une entrée au menu contextuel de la vue Projet
    [MenuItem("Assets/Create Block", true)]
    private static bool ValidateCreateBlock() {
        // Valide qu'exactement quatre éléments sont sélectionnés
        return Selection.objects.Length == 4;
    }

    [MenuItem("Assets/Create Block")]
    private static void CreateBlock() {
        string? baseColor = null;
        string? ambientOcclusion = null;
        string? normal = null;
        string? height = null;

        // Ensure that four textures are selected
        if (Selection.objects.Length == 4) {
            var files = Selection.objects.Select(AssetDatabase.GetAssetPath).ToList();

            // Identify each texture type based on their file names
            foreach (var selectedObject in files) {
                if (selectedObject.ToLower().Contains("_basecolor") || selectedObject.ToLower().Contains("_color")) baseColor = selectedObject;
                else if (selectedObject.ToLower().Contains("_ambientocclusion")) ambientOcclusion = selectedObject;
                else if (selectedObject.ToLower().Contains("_normal")) normal = selectedObject;
                else if (selectedObject.ToLower().Contains("_height")) height = selectedObject;
                else throw new Exception($"Could not figure out texture type for {selectedObject}");
            }

            // Verify all required textures are found
            if (baseColor == null) throw new Exception($"Couldn't find base color texture in {string.Join(",", files)}.");
            if (ambientOcclusion == null) throw new Exception($"Couldn't find ambient occlusion texture in {string.Join(",", files)}.");
            if (normal == null) throw new Exception($"Couldn't find normal texture in {string.Join(",", files)}.");
            if (height == null) throw new Exception($"Couldn't find height texture in {string.Join(",", files)}.");

            // Extract texture name from base color texture path
            var slashIdx = baseColor.LastIndexOf("/", StringComparison.Ordinal);
            var dotIdx = baseColor.LastIndexOf(".", StringComparison.Ordinal);
            var fullName = baseColor.Substring(slashIdx + 1, dotIdx - slashIdx - 1);
            var name = fullName.Replace("_baseColor", "");

            // Create output directory
            var outputFolder = $"Assets/StreamingAssets/Textures/Main/{name}";
            if (!System.IO.Directory.Exists(outputFolder)) {
                System.IO.Directory.CreateDirectory(outputFolder);
            }

            // Load source textures
            var baseColorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor);
            var aoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ambientOcclusion);
            var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
            var heightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(height);

            if (baseColorTexture == null || aoTexture == null || normalTexture == null || heightTexture == null) {
                throw new Exception("Failed to load one or more textures");
            }

            // Process and save output textures

            // 1. Combine baseColor and ambientOcclusion to create albedo texture
            var albedoTexture = new Texture2D(baseColorTexture.width, baseColorTexture.height, TextureFormat.RGBA32, false);
            var baseColorPixels = baseColorTexture.GetPixels();
            var aoPixels = aoTexture.GetPixels();
            var albedoPixels = new Color[baseColorPixels.Length];


            for (int i = 0; i < baseColorPixels.Length; i++) {
                // Multiply base color by ambient occlusion
                float occlusion = aoPixels[i].r;
                albedoPixels[i] = new Color(
                    baseColorPixels[i].r * occlusion,
                    baseColorPixels[i].g * occlusion,
                    baseColorPixels[i].b * occlusion,
                    baseColorPixels[i].a
                );
            }

            albedoTexture.SetPixels(albedoPixels);
            albedoTexture.Apply();

            // Save processed textures
            var albedoPath = $"{outputFolder}/{name}_albedo.png";
            var normalPath = $"{outputFolder}/{name}_normal.png";
            var heightPath = $"{outputFolder}/{name}_height.png";

            // Convert textures to PNG and save
            System.IO.File.WriteAllBytes(albedoPath, albedoTexture.EncodeToPNG());
            System.IO.File.WriteAllBytes(normalPath, normalTexture.EncodeToPNG());
            System.IO.File.WriteAllBytes(heightPath, heightTexture.EncodeToPNG());

            // Import saved assets to make them available in the project
            AssetDatabase.ImportAsset(albedoPath);
            AssetDatabase.ImportAsset(normalPath);
            AssetDatabase.ImportAsset(heightPath);

            // Create texture config JSON
            var rootFolder = "Assets/StreamingAssets/";
            var rootFolderLength = rootFolder.Length;

            MainTextureJson textureConfig = new MainTextureJson();
            textureConfig.MainAlbedoTexture = albedoPath.Substring(rootFolderLength);
            textureConfig.MainNormalsTexture = normalPath.Substring(rootFolderLength);
            textureConfig.MainHeightsTexture = heightPath.Substring(rootFolderLength);

            var textureConfigJson = MessagePackSerializer.SerializeToJson(textureConfig);
            var textureConfigPath = $"{outputFolder}/{name}.json";
            System.IO.File.WriteAllText(textureConfigPath, textureConfigJson);
            AssetDatabase.ImportAsset(textureConfigPath);

            // Create block config JSON
            var blocksFolder = "Assets/StreamingAssets/Blocks";
            if (!System.IO.Directory.Exists(blocksFolder)) {
                System.IO.Directory.CreateDirectory(blocksFolder);
            }

            BlockConfigJson blockConfig = new BlockConfigJson();
            blockConfig.Sides.Add(new BlockSideJson {MainTextureConfig = $"{name}\\{name}.json"});

            var blockConfigJson = MessagePackSerializer.SerializeToJson(blockConfig);
            var blockConfigPath = $"{blocksFolder}/{name}.json";
            System.IO.File.WriteAllText(blockConfigPath, blockConfigJson);
            AssetDatabase.ImportAsset(blockConfigPath);

            // Refresh AssetDatabase and update indexes
            AssetDatabase.Refresh();
            RegistryIndexGenerator.GenerateIndexes();

            Debug.Log($"Block {name} created successfully!");
        } else {
            Debug.LogWarning("Please select exactly four texture files (baseColor, ambientOcclusion, normal, height).");
        }
    }

    [MenuItem("Assets/CreateTextureConfig")]
    private static void CreateTextureConfigAction() {
        bool isFrame = false;
        string? albedo = null;
        string? normals = null;
        string? heights = null;

        // Assurez-vous que trois éléments sont sélectionnés
        if (Selection.objects.Length == 3) {
            var files = Selection.objects.Select(AssetDatabase.GetAssetPath).ToList();
            // Exécutez votre fonction arbitraire sur les fichiers sélectionnés
            foreach (var selectedObject in files) {
                if (selectedObject.Contains("/Frame/")) isFrame = true;
                if (selectedObject.ToLower().Contains("normal")) normals = selectedObject;
                else if (selectedObject.ToLower().Contains("height")) heights = selectedObject;
                else if (albedo == null) albedo = selectedObject;
                else throw new Exception($"Could figure out if {selectedObject} is albedo, normals or heights");
            }

            if (albedo == null) throw new Exception($"Couldn't find albedo in {string.Join(",", files)}.");
            if (normals == null) throw new Exception($"Couldn't find normals in {string.Join(",", files)}.");
            if (heights == null) throw new Exception($"Couldn't find heights in {string.Join(",", files)}.");
            var rootFolder = "Assets/StreamingAssets/";
            var rootFolderLength = rootFolder.Length;
            var rootFolderIdx = albedo.LastIndexOf(rootFolder, StringComparison.Ordinal);
            var slashIdx = albedo.LastIndexOf("/", StringComparison.Ordinal);
            var dotIdx = albedo.LastIndexOf(".", StringComparison.Ordinal);
            var name = albedo.Substring(slashIdx, dotIdx - slashIdx)
                .Replace("albedo", "")
                .Replace("Albedo", "")
                .Replace("basecolor", "")
                .Replace("_", "");
            var folder = albedo.Substring(0, slashIdx);

            if (isFrame) {
                FrameTextureJson c = new FrameTextureJson();
                c.FrameAlbedoTexture = albedo.Substring(rootFolderIdx + rootFolderLength);
                c.FrameNormalsTexture = normals.Substring(rootFolderIdx + rootFolderLength);
                c.FrameHeightsTexture = heights.Substring(rootFolderIdx + rootFolderLength);
                var confJson = MessagePackSerializer.SerializeToJson(c);
                System.IO.File.WriteAllText($"{folder}/{name}.json", confJson);
                AssetDatabase.ImportAsset($"{folder}/{name}.json");
            } else {
                MainTextureJson c = new MainTextureJson();
                c.MainAlbedoTexture = albedo.Substring(rootFolderIdx + rootFolderLength);
                c.MainNormalsTexture = normals.Substring(rootFolderIdx + rootFolderLength);
                c.MainHeightsTexture = heights.Substring(rootFolderIdx + rootFolderLength);
                var confJson = MessagePackSerializer.SerializeToJson(c);
                System.IO.File.WriteAllText($"{folder}/{name}.json", confJson);
                AssetDatabase.ImportAsset($"{folder}/{name}.json");
            }
        } else {
            Debug.LogWarning("Please select exactly three items.");
        }
    }


    public static async UniTask<string> InvokeProcessStartAsync(string fileName, string arguments) {
        var psi = new ProcessStartInfo() {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = Application.dataPath
        };

        Process? p;
        StringBuilder logs = new StringBuilder();
        try {
            p = Process.Start(psi);
            if (p == null) throw new ApplicationException("Could not start process " + psi);
            var log = await p.StandardOutput.ReadToEndAsync();
            logs.Append(log);
        } catch (Exception ex) {
            return await UniTask.FromException<string>(ex);
        }

        var tcs = new UniTaskCompletionSource<string>();
        p.EnableRaisingEvents = true;
        p.Exited += (_, _) => {
            logs.Append(p.StandardOutput.ReadToEnd());
            p.Dispose();
            p = null;

            tcs.TrySetResult(logs.ToString());
        };
        return await tcs.Task;
    }
}