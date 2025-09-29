using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Shared;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using VoxelsEngine;

public abstract class JsonConfigEditor<T> : OdinMenuEditorWindow where T : class, new() {
    private readonly List<string> _dirty = new();
    private double _nextAutoSave;

    protected abstract Registry<T>? GetRegistry();

    protected override void OnBeginDrawEditors() {
        // Draws a toolbar with the name of the currently selected menu item.
        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree?.Config?.SearchToolbarHeight ?? 30);
        {
            if (SirenixEditorGUI.ToolbarButton("Rebuild menu")) {
                if (Configurator.Instance.BlockRegistry == null) return;
                GetRegistry()?.Reload().ContinueWith(ForceMenuTreeRebuild);
            }

            if (SirenixEditorGUI.ToolbarButton("Update indexes")) {
                RegistryIndexGenerator.GenerateIndexes();
                Configurator.Instance.MainTextureRegistry?.Reload();
                Configurator.Instance.FrameTextureRegistry?.Reload();
                Configurator.Instance.SpriteRegistry?.Reload();
                Configurator.Instance.BlockRegistry?.Reload();
            }

            if (SirenixEditorGUI.ToolbarButton("Create new")) {
                CreateNew().Forget();
            }

            if (SirenixEditorGUI.ToolbarButton("Save " + (_dirty.Count > 0 ? "*" : ""))) {
                SaveToJson();
                ForceMenuTreeRebuild();
            }

            SirenixEditorGUI.HorizontalLineSeparator();
            if (SirenixEditorGUI.ToolbarButton("Edit Code")) {
                var script = MonoScript.FromScriptableObject(this);
                AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
            }

            if (SirenixEditorGUI.ToolbarButton("Remove")) {
                DeleteSelection().Forget();
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }

    // ↓↓ Auto Save stuff ↓↓
    private new void SetDirty() {
        if (MenuTree.Selection.Count <= 0) return;
        _dirty.Add(MenuTree.Selection[0].Name);
        _nextAutoSave = EditorApplication.timeSinceStartup + 7f;
    }

    protected override void DrawEditors() {
        EditorGUI.BeginChangeCheck();
        base.DrawEditors();
        if (EditorGUI.EndChangeCheck()) {
            SetDirty();
        }
    }


    private void Update() {
        if (_dirty.Count > 0 && _nextAutoSave > 0 && _nextAutoSave < EditorApplication.timeSinceStartup) {
            SaveToJson();
            ForceMenuTreeRebuild();
        }
    }

    private async UniTaskVoid CreateNew() {
        var registry = GetRegistry();
        if (registry is null) return;
        await UniTask.DelayFrame(1);
        var blockName = EditorInputDialog.Show(
            "Json name", "Name the block.", "", "Create", "Cancel",
            n => registry.Get(n) == null,
            "Name must be unique among blocks."
        );
        if (blockName != null) {
            if (!blockName.EndsWith(".json")) blockName += ".json";
            registry.Editor_SaveToJson(blockName, new T());
            await registry.Reload();
            ForceMenuTreeRebuild();
        }
    }

    private async UniTaskVoid DeleteSelection() {
        var registry = GetRegistry();
        if (registry is null) return;
        await UniTask.DelayFrame(1);
        if (MenuTree.Selection.Count <= 0) return;
        var path = MenuTree.Selection[0].Name;
        var shouldRemove = EditorUtility.DisplayDialog("Confirm suppression of " + path,
            $"You're about to delete {path}.\nPlease confirm.",
            $"Delete!", "Cancel");

        if (shouldRemove) {
            registry.Remove(path);
            ForceMenuTreeRebuild();
        }
    }

    protected override OdinMenuTree BuildMenuTree() {
        var tree = new OdinMenuTree();
        var registry = GetRegistry();
        if (registry is null) return tree;
        foreach (var (path, blockConfigJson) in registry.Get()) {
            tree.Add(path, blockConfigJson);
        }

        return tree;
    }

    private void SaveToJson() {
        var registry = GetRegistry();
        if (registry is null) return;
        var notifText = "";
        foreach (var selectedItem in MenuTree.MenuItems) {
            if (_dirty.Contains(selectedItem.Name) && selectedItem.Value is T config) {
                registry?.Editor_SaveToJson(selectedItem.Name, config);
                notifText += $"Saved {selectedItem.Name}\n";
            }
        }

        if (!string.IsNullOrEmpty(notifText)) ShowNotification(new GUIContent(notifText), 0.3);
        _dirty.Clear();
    }
}