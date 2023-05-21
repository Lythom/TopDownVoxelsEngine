using UnityEngine;

namespace VoxelsEngine {
    public class Character : MonoBehaviour {
        public float Speed = 5.0f;
        public Vector3 Acceleration = new(0, 0, 0);
        public Transform CameraTransform;

        private void Update() {
            // Get the input on the x and z axis
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // Create a new vector of the direction we want to move in. 
            // We assume Y movement (upwards) is 0 as we are moving only on X and Z axis
            Vector3 move = new Vector3(moveX, 0, moveZ);

            // If we have some input
            if (move != Vector3.zero) {
                // Create a quaternion (rotation) based on looking down the vector from the player to the camera.
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);

                // Smoothly interpolate between current rotation and target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5.0f * Time.deltaTime);
            }

            // Apply the movement to the player's rigidbody using the camera's rotation
            Vector3 movement = CameraTransform.rotation * move * Speed * Time.deltaTime;
            Acceleration.x = movement.x;
            Acceleration.z = movement.z;

            transform.position += Acceleration;
        }
    }
}