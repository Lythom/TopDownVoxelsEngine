using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Shared;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using VoxelsEngine.Tools;

public class TilesetGeneratorEditor : OdinEditorWindow {
    private List<string> _dirty = new();
    private double _nextAutoSave;

    [MenuItem("DreamBuilder/TilesetGenerator")]
    private static void OpenWindow() {
        GetWindow<TilesetGeneratorEditor>().Show();
    }

    [Sirenix.OdinInspector.FilePath(Extensions = ".png", AbsolutePath = true, RequireExistingPath = true), OnValueChanged("Refresh")]
    public string InputPath;

    [ReadOnly]
    public int tileSize = -1;

    public async UniTaskVoid Refresh() {
        if (!File.Exists(InputPath)) return;
        Input = await StreamAssetsFiles.LoadTexture2DAsync(InputPath);

        tileSize = Input.height / 3;

        Output = new Texture2D(tileSize * 12, tileSize * 5, TextureFormat.ARGB32, false);
        for (int x = 0; x < tileSize * 12; x += tileSize) {
            for (int y = 0; y < tileSize * 5; y += tileSize) {
                Graphics.CopyTexture(Input, 0, 0, 0, tileSize * 2, tileSize, tileSize, Output, 0, 0, x, y);
            }
        }

        ToAbsolutePath(Output, InputPath.Replace(".png", "_out.png")).Forget();
    }

    public static async UniTask ToAbsolutePath(Texture2D t, string absolutePath) {
        if (File.Exists(absolutePath)) Logr.LogError("Overriding at " + absolutePath);
        await File.WriteAllBytesAsync(absolutePath, t.EncodeToPNG());
    }

    [PreviewField(Height = 512, Alignment = ObjectFieldAlignment.Left, FilterMode = FilterMode.Bilinear), ReadOnly, HideLabel]
    public Texture2D Input;

    [PreviewField(Height = 512, Alignment = ObjectFieldAlignment.Left, FilterMode = FilterMode.Bilinear), ReadOnly, HideLabel]
    public Texture2D Output;
}