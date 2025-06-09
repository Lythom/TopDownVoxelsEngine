using Shared;
using UnityEditor;
using VoxelsEngine;

public class PlayerToolEditor : JsonConfigEditor<PlayerToolJson> {

    [MenuItem("💭 DreamBuilder/PlayerTool JSON Editor")]
    private static void OpenWindow() {
        GetWindow<PlayerToolEditor>().Show();
    }

    protected override Registry<PlayerToolJson>? GetRegistry() {
        return Configurator.Instance.PlayerToolRegistry;
    }
}