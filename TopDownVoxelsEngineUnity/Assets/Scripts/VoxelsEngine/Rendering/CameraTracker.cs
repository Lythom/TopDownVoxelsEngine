using DG.Tweening;
using UnityEngine;

namespace VoxelsEngine {
    public class CameraTracker : MonoBehaviour {
        public GameObject? Target; // The object to follow
        public float OrbitDistance = 50f;
        public float TrackingDistance = 1.0f; // The tracking distance
        public float MaxTrackingDistance = 3.0f; // The tracking distance
        public float LerpFactor = 0.1f; // The lerp factor (for smooth follow)
        public float LerpFactorDuringRotations = 0.5f; // The lerp factor (for smooth follow)
        public float RotationDuration = 0.2f;
        public float RotationAngle = 45f;

        public float ForwardAnticipationDistance = 2f;

        private bool _isRotating = false;

        private void Update() {
            if (Target == null || _isRotating) return;

            if (Input.GetKeyDown(KeyCode.Q)) {
                RotateCamera(RotationAngle);
            } else if (Input.GetKeyDown(KeyCode.E)) {
                RotateCamera(-RotationAngle);
            }
        }

        private void LateUpdate() {
            if (Target == null) return; // Do nothing if target is null

            var t = transform;
            var pos = t.position;

            // Calculer le vecteur direction de la caméra vers le joueur
            var targetTransform = Target.transform;

            var targetPosition = targetTransform.position + targetTransform.rotation * Vector3.forward * ForwardAnticipationDistance;

            // Créez un vecteur direction qui pointe dans la direction "avant" du quaternion de rotation, 
            // puis multipliez par la distance pour obtenir le vecteur déplacement.
            Vector3 offset = t.rotation * Vector3.forward * OrbitDistance;
            var targetCenterPosition = targetPosition - offset;

            if (_isRotating) {
                // during rotation, track closely
                t.position = Vector3.Lerp(pos, targetCenterPosition, LerpFactorDuringRotations);
            } else {
                // normal situation, track smoothly
                var distanceToTarget = Vector3.Distance(targetCenterPosition, t.position);
                var targetEdgePosition = Vector3.Lerp(pos, targetCenterPosition, (distanceToTarget - TrackingDistance) / distanceToTarget);
                var targetMaxEdgePosition = Vector3.Lerp(pos, targetCenterPosition, (distanceToTarget - (MaxTrackingDistance - 0.1f)) / distanceToTarget);

                if (distanceToTarget <= MaxTrackingDistance) {
                    // Use Lerp for a smooth follow
                    if (distanceToTarget > TrackingDistance) {
                        // far from target, configured Lerp
                        t.position = Vector3.Lerp(pos, targetEdgePosition, LerpFactor);
                    } else {
                        // inside tracking deadzone, no move
                    }
                } else {
                    // too far, snap to edge
                    t.position = targetMaxEdgePosition;
                }
            }
        }

        private void RotateCamera(float angle) {
            var rotation = transform.rotation;
            _isRotating = true;
            transform.DORotate(
                    new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y + angle, rotation.eulerAngles.z),
                    RotationDuration
                )
                .SetEase(Ease.OutQuart)
                .OnComplete(() => _isRotating = false);
        }
    }
}