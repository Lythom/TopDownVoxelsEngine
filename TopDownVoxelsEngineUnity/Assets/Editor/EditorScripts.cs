using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EditorScripts : MonoBehaviour {
    private const string ScriptBuildPath = "Temp/PlayerScriptBuildTest";

    [MenuItem("Tools/Generate MessagePack resolvers", priority = -2000)]
    public static async void InstantCodeGen() {
        Debug.Log("Generating MessagePack Files");
        try {
            var log = await InvokeProcessStartAsync("dotnet", "mpc -i . -o ./Scripts/VoxelsEngine/MessagePackGenerated");
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