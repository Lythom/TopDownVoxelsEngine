using UnityEditor;

[InitializeOnLoad]
public class ReloadAssetsOnPlayMode {
    static ReloadAssetsOnPlayMode() {
        EditorApplication.playModeStateChanged += OnPlayModeState;
    }

    private static void OnPlayModeState(PlayModeStateChange state) {
        if (state == PlayModeStateChange.ExitingEditMode) AssetDatabase.Refresh();
    }
}