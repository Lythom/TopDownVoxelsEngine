using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using VoxelsEngine;

public class PlayerToolEditor : OdinMenuEditorWindow {
    private readonly List<string> _dirty = new();
    private double _nextAutoSave;

    [MenuItem("DreamBuilder/Player Tool Editor")]
    private static void OpenWindow() {
        GetWindow<PlayerToolEditor>().Show();
    }

    protected override void OnBeginDrawEditors() {
        // Draws a toolbar with the name of the currently selected menu item.
        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree?.Config?.SearchToolbarHeight ?? 30);
        {
            if (SirenixEditorGUI.ToolbarButton("Rebuild menu")) {
                if (Configurator.Instance.PlayerToolRegistry == null) return;
                Configurator.Instance.PlayerToolRegistry.Reload().ContinueWith(ForceMenuTreeRebuild);
            }

            if (SirenixEditorGUI.ToolbarButton("Update indexes")) {
                RegistryIndexGenerator.GenerateIndexes();
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
        if (Configurator.Instance.PlayerToolRegistry is null) return;
        await UniTask.DelayFrame(1);
        var toolName = EditorInputDialog.Show(
            "Player Tool", "Name the tool.", "", "Create", "Cancel",
            n => Configurator.Instance.PlayerToolRegistry.Get(n) == null,
            "Name must be unique among tools."
        );
        if (toolName != null) {
            if (!toolName.EndsWith(".json")) toolName += ".json";
            var newTool = new PlayerToolJson { 
                Name = toolName.Replace(".json", ""),
                ToolSpritePath = "",
                SortOrder = 0
            };
            Configurator.Instance.PlayerToolRegistry.Editor_SaveToJson(toolName, newTool);
            await Configurator.Instance.PlayerToolRegistry.Reload();
            ForceMenuTreeRebuild();
        }
    }

    private async UniTaskVoid DeleteSelection() {
        if (Configurator.Instance.PlayerToolRegistry is null) return;
        await UniTask.DelayFrame(1);
        if (MenuTree.Selection.Count <= 0) return;
        var path = MenuTree.Selection[0].Name;
        var shouldRemove = EditorUtility.DisplayDialog("Confirm suppression of " + path,
            $"You're about to delete {path}.\nPlease confirm.",
            $"Delete!", "Cancel");

        if (shouldRemove) {
            Configurator.Instance.PlayerToolRegistry.Remove(path);
            ForceMenuTreeRebuild();
        }
    }

    protected override OdinMenuTree BuildMenuTree() {
        var tree = new OdinMenuTree();
        var registry = Configurator.Instance.PlayerToolRegistry;
        if (registry is null) return tree;
        foreach (var (path, playerToolJson) in registry.Get()) {
            tree.Add(path, playerToolJson);
        }

        return tree;
    }

    private void SaveToJson() {
        if (Configurator.Instance.PlayerToolRegistry is null) return;
        var notifText = "";
        foreach (var selectedItem in MenuTree.MenuItems) {
            if (_dirty.Contains(selectedItem.Name) && selectedItem.Value is PlayerToolJson config) {
                Configurator.Instance.PlayerToolRegistry.Editor_SaveToJson(selectedItem.Name, config);
                notifText += $"Saved {selectedItem.Name}\n";
            }
        }

        if (!string.IsNullOrEmpty(notifText)) ShowNotification(new GUIContent(notifText), 0.3);
        _dirty.Clear();
    }
}
