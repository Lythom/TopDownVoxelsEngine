using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using MessagePack;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Windows;
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
        // Valide que exactement trois éléments sont sélectionnés
        return Selection.objects.Length == 3;
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
            var resourcesIdx = albedo.LastIndexOf("/Resources/", StringComparison.Ordinal);
            var slashIdx = albedo.LastIndexOf("/", StringComparison.Ordinal);
            var dotIdx = albedo.LastIndexOf(".", StringComparison.Ordinal);
            var name = albedo.Substring(slashIdx, dotIdx - slashIdx)
                .Replace("albedo", "")
                .Replace("Albedo", "")
                .Replace("_", "");
            var folder = albedo.Substring(0, slashIdx);

            if (isFrame) {
                FrameTextureConfiguration c = new FrameTextureConfiguration();
                c.FrameAlbedoTexture = albedo.Substring(resourcesIdx + 11).Replace(".png", "");
                c.FrameNormalsTexture = normals.Substring(resourcesIdx + 11).Replace(".png", "");
                c.FrameHeightsTexture = heights.Substring(resourcesIdx + 11).Replace(".png", "");
                var confJson = MessagePackSerializer.SerializeToJson(c);
                System.IO.File.WriteAllText($"{folder}/{name}.json", confJson);
                AssetDatabase.ImportAsset($"{folder}/{name}.json");
            } else {
                MainTextureConfiguration c = new MainTextureConfiguration();
                c.MainAlbedoTexture = albedo.Substring(resourcesIdx + 11).Replace(".png", "");
                c.MainNormalsTexture = normals.Substring(resourcesIdx + 11).Replace(".png", "");
                c.MainHeightsTexture = heights.Substring(resourcesIdx + 11).Replace(".png", "");
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