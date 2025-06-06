using Shared;
using UnityEditor;
using VoxelsEngine;

public class BlockConfigEditor : JsonConfigEditor<BlockConfigJson> {

    [MenuItem("DreamBuilder/PlayerTool JSON Editor")]
    private static void OpenWindow() {
        GetWindow<BlockConfigEditor>().Show();
    }

    protected override Registry<BlockConfigJson>? GetRegistry() {
        return Configurator.Instance.BlockRegistry;
    }
}