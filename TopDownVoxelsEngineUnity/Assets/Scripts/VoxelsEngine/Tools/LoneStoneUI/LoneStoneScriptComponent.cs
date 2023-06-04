using UnityEngine;

namespace LoneStoneStudio.Tools {
    /**
     * Provide a default implementation for IBehaviourComponent.
     * The component does not have to be used in UI
     */
    public class LoneStoneBehaviour : MonoBehaviour, IBehaviourComponent {
        public GameObject GetGameObject() {
            return gameObject;
        }

        public MonoBehaviour GetScript() {
            return this;
        }
    }
}