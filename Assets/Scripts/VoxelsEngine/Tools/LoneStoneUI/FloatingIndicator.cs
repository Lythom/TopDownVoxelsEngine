using System.Collections.Generic;
using LoneStoneStudio.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

[InfoBox("This component should be anchored center (0.5, 0.5) and have a center anchored itself (0.5, 0.5).")]
public class FloatingIndicator : MonoBehaviour {
    [Header("Configuration")]
    public Transform? Target;

    public Vector2 TargetOffset = new(0, 128);

    public float ArrowOpacityNearCenter = 0;
    public float ArrowOpacityCurveIntensity = 1;
    public float ArrowOpacityCurveDistance = 256;

    public Vector2 SizeNearCenter = new(185, 185);
    public Vector2 SizeFarAway = new(120, 120);

    public float OpacityNearCenter = 1;
    public float OpacityFarAway = 0.75f;
    public float OpacityCurveDistance = 256;

    [Tooltip("In canvas pixels")]
    public Vector2 PaddingMin = new Vector2(100, 300);

    public Vector2 PaddingMax = new Vector2(100, 400);

    [Tooltip("In canvas pixels")]
    public int TargetSize = 1000;

    [Header("Bindings")]
    [Required, ChildGameObjectsOnly]
    public CanvasGroup BadgeCanvasGroup = null!;

    [Required, ChildGameObjectsOnly]
    public CanvasGroup Arrow = null!;

    [Required, ChildGameObjectsOnly]
    public Transform ArrowTransform = null!;

    [Required, ChildGameObjectsOnly]
    public List<GameObject> HideWhenOutside = new();

    private RectTransform _rectTransform = null!;
    private Canvas _canvas = null!;
    private Camera? _mainCamera;

    void Start() {
        _rectTransform = (RectTransform) transform;
        _canvas = _rectTransform.GetComponentInParent<Canvas>();
        _mainCamera = Camera.main;
    }

    void LateUpdate() {
        if (Target == null || _mainCamera == null) return;

        var canvasTransformRect = ((RectTransform) _canvas.transform).rect;
        Vector2 minPosition = canvasTransformRect.min;
        Vector2 maxPosition = canvasTransformRect.max;

        var targetPosition = Target.position;
        var nextObjectivePosition = _canvas.WorldToUISpace(targetPosition, _mainCamera);
        var screenCenter = (maxPosition + minPosition) * 0.5f;
        var towardsCenter = screenCenter - (nextObjectivePosition + TargetOffset);

        var nextPosition = nextObjectivePosition + TargetOffset + towardsCenter.normalized * (TargetSize / _mainCamera.orthographicSize);
        _rectTransform.anchoredPosition = new Vector3(
            x: Mathf.Clamp(nextPosition.x, minPosition.x + PaddingMin.x, maxPosition.x - PaddingMax.x),
            Mathf.Clamp(nextPosition.y, minPosition.y + PaddingMin.y, maxPosition.y - PaddingMax.y),
            _rectTransform.position.z
        );

        ArrowTransform.localEulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.right, nextObjectivePosition + TargetOffset - _rectTransform.anchoredPosition));

        var distanceFromPosition = Vector2.Distance(nextPosition, _rectTransform.anchoredPosition);
        foreach (var toHide in HideWhenOutside) {
            toHide.SmartActive(distanceFromPosition < 1f);
        }

        var arrowFadeOutDistance = Mathf.Max(0.0001f, ArrowOpacityCurveDistance);
        var arrowNormalizedDistanceFromBorder = (distanceFromPosition - arrowFadeOutDistance) * ArrowOpacityCurveIntensity / arrowFadeOutDistance;
        Arrow.alpha = Mathf.Clamp01(Mathf.Lerp(ArrowOpacityNearCenter, 1, arrowNormalizedDistanceFromBorder));

        var fadeOutDistance = Mathf.Max(0.0001f, OpacityCurveDistance);
        var normalizedDistanceFromBorder = (distanceFromPosition - fadeOutDistance) * ArrowOpacityCurveIntensity / fadeOutDistance;
        _rectTransform.sizeDelta = Vector2.Lerp(SizeNearCenter, SizeFarAway, normalizedDistanceFromBorder);
        BadgeCanvasGroup.alpha = Mathf.Lerp(OpacityNearCenter, OpacityFarAway, normalizedDistanceFromBorder);
    }
}