using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using VoxelsEngine;

public class BlockConfigEditor : OdinMenuEditorWindow {
    private List<string> _dirty = new();
    private double _nextAutoSave;

    [MenuItem("DreamBuilder/Block Config Editor")]
    private static void OpenWindow() {
        GetWindow<BlockConfigEditor>().Show();
    }

    protected override void OnBeginDrawEditors() {
        // Draws a toolbar with the name of the currently selected menu item.
        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree?.Config?.SearchToolbarHeight ?? 30);
        {
            if (SirenixEditorGUI.ToolbarButton("Rebuild menu")) {
                Configurator.Instance.BlockRegistry.Reload();
                ForceMenuTreeRebuild();
            }

            if (SirenixEditorGUI.ToolbarButton("Create new")) {
                CreateNew().Forget();
            }

            if (SirenixEditorGUI.ToolbarButton("Save " + (_dirty.Count > 0 ? "*" : ""))) {
                SaveToJson();
                ForceMenuTreeRebuild();
            }

            SirenixEditorGUI.HorizontalLineSeparator();
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
        await UniTask.DelayFrame(1);
        var blockName = EditorInputDialog.Show(
            "Block name", "Name the block.", "", "Create", "Cancel",
            n => Configurator.Instance.BlockRegistry.Get(n) == null,
            "Name must be unique among blocks."
        );
        if (blockName != null) {
            if (!blockName.EndsWith(".json")) blockName += ".json";
            Configurator.Instance.BlockRegistry.SaveToJson(blockName, new BlockConfigJson());
            Configurator.Instance.BlockRegistry.Reload();
            ForceMenuTreeRebuild();
        }
    }

    private async UniTaskVoid DeleteSelection() {
        await UniTask.DelayFrame(1);
        if (MenuTree.Selection.Count <= 0) return;
        var path = MenuTree.Selection[0].Name;
        var shouldRemove = EditorUtility.DisplayDialog("Confirm suppression of " + path,
            $"You're about to delete {path}.\nPlease confirm.",
            $"Delete!", "Cancel");

        if (shouldRemove) {
            Configurator.Instance.BlockRegistry.Remove(path);
            ForceMenuTreeRebuild();
        }
    }

    protected override OdinMenuTree BuildMenuTree() {
        var tree = new OdinMenuTree();
        var registry = Configurator.Instance.BlockRegistry;
        foreach (var (path, blockConfigJson) in registry.Get()) {
            tree.Add(path, blockConfigJson);
        }

        return tree;
    }

    private void SaveToJson() {
        var notifText = "";
        foreach (var selectedItem in MenuTree.MenuItems) {
            if (_dirty.Contains(selectedItem.Name) && selectedItem.Value is BlockConfigJson config) {
                Configurator.Instance.BlockRegistry.SaveToJson(selectedItem.Name, config);
                notifText += $"Saved {selectedItem.Name}\n";
            }
        }

        if (!string.IsNullOrEmpty(notifText)) ShowNotification(new GUIContent(notifText), 0.3);
        _dirty.Clear();
    }
}