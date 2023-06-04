using UnityEngine;

namespace LoneStoneStudio.Tools {
    /**
     * Provide a default implementation for IBehaviourComponent.
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