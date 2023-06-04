using UnityEngine;

namespace LoneStoneStudio.Tools {
    /**
     * Put this interface on any component that should be compatible with Lonestone component code generation.
     * You can also directly inherit your component from LoneStoneUIComponent.
     */
    public interface IBehaviourComponent {
        GameObject GetGameObject();
        MonoBehaviour GetScript();
    }
}