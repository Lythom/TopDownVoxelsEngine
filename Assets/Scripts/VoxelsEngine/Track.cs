using UnityEngine;

namespace VoxelsEngine {
    public class Track : MonoBehaviour {
        public GameObject? Target; // The object to follow
        public float TrackingDistance = 10.0f; // The tracking distance
        public float LerpFactor = 0.1f; // The lerp factor (for smooth follow)
        public Vector3 Offset;

        private void LateUpdate() {
            if (Target == null) return; // Do nothing if target is null

            Vector3 targetCenterPosition = Target.transform.position + Offset;
            var distanceToTarget = Vector3.Distance(targetCenterPosition, transform.position);

            if (distanceToTarget > TrackingDistance) {
                var targetEdgePosition = Vector3.Lerp(transform.position, targetCenterPosition, (distanceToTarget - TrackingDistance) / distanceToTarget);
                // Use Lerp for a smooth follow
                if (distanceToTarget > TrackingDistance * 1.9f) {
                    // far from target, configured Lerp
                    transform.position = Vector3.Lerp(transform.position, targetEdgePosition, LerpFactor);
                } else {
                    // far to target, snap
                    transform.position = targetEdgePosition;
                }
            }
        }
    }
}