using UnityEngine;

namespace VoxelsEngine {
    public class Track : MonoBehaviour {
        public Transform? target;
        public Vector3 offset;
        public float trackingDistance = 1f;
        public float stiffness = 0.05f;

        // Update is called once per frame
        void Update() {
            if (target == null) return;

            Vector3 targetPos = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                target.position.z + offset.z
            );
            Vector3 moveOffset = Vector3.zero;
            if (Vector2.Distance(target.position, transform.position) > trackingDistance) {
                var position = transform.position;
                moveOffset = (targetPos - new Vector3(position.x, position.y, position.z)) * stiffness;
            }

            transform.position += moveOffset;
        }
    }
}