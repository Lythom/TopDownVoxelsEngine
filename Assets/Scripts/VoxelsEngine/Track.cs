using DG.Tweening;
using UnityEngine;

namespace VoxelsEngine {
    public class Track : MonoBehaviour {
        public GameObject? Target; // The object to follow
        public float OrbitDistance = 50f;
        public float TrackingDistance = 10.0f; // The tracking distance
        public float LerpFactor = 0.1f; // The lerp factor (for smooth follow)
        public float LerpFactorDuringRotations = 0.5f; // The lerp factor (for smooth follow)
        public float RotationDuration = 0.2f;

        private bool _isRotating = false;

        private void Update() {
            if (Target == null || _isRotating) return;

            if (Input.GetKeyDown(KeyCode.Q)) {
                RotateCamera(90);
            } else if (Input.GetKeyDown(KeyCode.E)) {
                RotateCamera(-90);
            }
        }

        private void LateUpdate() {
            if (Target == null) return; // Do nothing if target is null

            var t = transform;

            // Calculer le vecteur direction de la caméra vers le joueur
            var targetPosition = Target.transform.position;
            Vector3 directionToPlayer = (targetPosition - t.position).normalized;

            // Créez un vecteur direction qui pointe dans la direction "avant" du quaternion de rotation, 
            // puis multipliez par la distance pour obtenir le vecteur déplacement.
            Vector3 offset = t.rotation * Vector3.forward * OrbitDistance;
            var targetCenterPosition = targetPosition - offset;

            if (_isRotating) {
                // during rotation, track closely
                t.position = Vector3.Lerp(t.position, targetCenterPosition, LerpFactorDuringRotations);
            } else {
                // normal situation, track smoothly
                var distanceToTarget = Vector3.Distance(targetCenterPosition, t.position);

                if (distanceToTarget > TrackingDistance) {
                    var targetEdgePosition = Vector3.Lerp(t.position, targetCenterPosition, (distanceToTarget - TrackingDistance) / distanceToTarget);
                    // Use Lerp for a smooth follow
                    if (distanceToTarget > TrackingDistance * 1.9f) {
                        // far from target, configured Lerp
                        t.position = Vector3.Lerp(t.position, targetEdgePosition, LerpFactor);
                    } else {
                        // far to target, snap
                        t.position = targetEdgePosition;
                    }
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