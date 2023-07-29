using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LoneStoneStudio.Tools {
    /// <summary>
    /// from SRF.UI.LongPressButton
    /// </summary>
    public class LongPressButton : Button {
        private bool _handled;

        [SerializeField]
        public ButtonClickedEvent OnLongPress = new();

        [SerializeField]
        public UnityEvent<float> OnPressing = new();

        [SerializeField]
        public ButtonClickedEvent OnLongPressRelease = new();

        private bool _pressed;
        private float _pressedTime;
        public float LongPressDuration = 0.9f;
        public Image? LongPressIndicator;

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) {
            base.OnPointerExit(eventData);
            _pressed = false;
        }

        public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) {
            base.OnPointerDown(eventData);

            if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left) {
                return;
            }

            _pressed = true;
            _handled = false;
            _pressedTime = Time.realtimeSinceStartup;
            OnPressing.Invoke(_pressedTime);
        }

        public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) {
            if (!_handled) {
                base.OnPointerUp(eventData);
            } else {
                OnLongPressRelease.Invoke();
            }

            _pressed = false;
        }

        public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) {
            if (!_handled) {
                base.OnPointerClick(eventData);
            }
        }

        private void Update() {
            if (!_pressed) {
                if (LongPressIndicator != null && LongPressIndicator.fillAmount != 0) LongPressIndicator.fillAmount = 0;
                return;
            }

            var timeElapsedSincePointerDown = Time.realtimeSinceStartup - _pressedTime;
            if (LongPressIndicator != null && LongPressDuration > 0) {
                LongPressIndicator.fillAmount = timeElapsedSincePointerDown / LongPressDuration;
            }

            if (timeElapsedSincePointerDown >= LongPressDuration) {
                _pressed = false;
                _handled = true;
                if (LongPressIndicator != null) LongPressIndicator.fillAmount = 0;
                OnLongPress.Invoke();
            }
        }
    }
}