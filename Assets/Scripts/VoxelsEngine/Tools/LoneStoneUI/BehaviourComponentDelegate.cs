using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LoneStoneStudio.Tools {
    /**
     * Sometimes you want a third party script to be used by the code generation (ie. Image, Button),
     * this component act as a proxy for it for the code generation.
     */
    public class BehaviourComponentDelegate : MonoBehaviour, IBehaviourComponent {
        [Required]
        public MonoBehaviour Reference = null!;

        public GameObject GetGameObject() {
            return gameObject;
        }

        public MonoBehaviour GetScript() {
            if (Reference == null) throw new ApplicationException("Reference missing in " + transform.parent.gameObject.name + "/" + gameObject.name);
            return Reference;
        }

#if UNITY_EDITOR
        [OnInspectorGUI]
        public void GenerateButtons() {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Reference != null) return;
            GUILayout.Label("Possible references:");
            foreach (var c in GetComponents<MonoBehaviour>()) {
                if (c != null && c is not BehaviourComponentDelegate && GUILayout.Button("Use " + c.GetType().Name)) {
                    Reference = c;
                }
            }
        }
#endif
    }
}