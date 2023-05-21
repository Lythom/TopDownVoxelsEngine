using UnityEngine;

namespace LoneStoneStudio.Tools {
    public class AutoDisable : MonoBehaviour {
        private void Awake() {
            gameObject.SetActive(false);
        }
    }
}