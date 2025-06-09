using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxelsEngine.UI {
    public class KeyInputAlt : MonoBehaviour {

        [Required]
        public InputActionReference AltFunctionInputActionRef = null!;

        [Required]
        public Transform Target = null!;

        public float RotationOffsetOnAlt = 180;
        public Vector3 ScaleTargetOnAlt = Vector3.one;

        private float _initialRotation;
        private Vector3 _initialScale;

        public void Start() {
            AltFunctionInputActionRef.action.performed += HandleAltFunctionPerformed;
            AltFunctionInputActionRef.action.canceled += HandleAltFunctionPerformed;
            _initialRotation = Target.eulerAngles.z;
            _initialScale = Target.localScale;
        }

        private void HandleAltFunctionPerformed(InputAction.CallbackContext obj) {
            DOTween.Kill(this);
            Target.DORotate(new Vector3(
                    Target.eulerAngles.x,
                    Target.eulerAngles.y,
                    _initialRotation + (AltFunctionInputActionRef.action.IsPressed() ? RotationOffsetOnAlt : 0)
                ),
                0.1f,
                RotateMode.FastBeyond360
            ).SetTarget(this);
            Target.DOScale(AltFunctionInputActionRef.action.IsPressed() ? ScaleTargetOnAlt : _initialScale, 0.1f).SetTarget(this);
        }
    }
}