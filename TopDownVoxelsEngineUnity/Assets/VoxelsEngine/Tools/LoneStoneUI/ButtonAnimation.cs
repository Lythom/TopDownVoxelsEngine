using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    private Tweener? _tweener;
    private Button? _button = null;
    public float FullScale = 1f;

    private void Awake() {
        OnEnable();
    }

    private void OnEnable() {
        if (_button == null) _button = GetComponent<Button>();
    }

    // The ButtonAnimation can work without buttons too, so _button == null is ok
    private bool Interactable => enabled && (_button == null || _button.interactable && _button.enabled);

    public void OnPointerDown(PointerEventData eventData) {
        if (Interactable) {
            _tweener?.Kill();
            _tweener = transform.DOScale(0.9f, 0.1f);
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (Interactable) {
            _tweener?.Kill();
            _tweener = transform.DOScale(FullScale, 0.1f);
        }
    }
}