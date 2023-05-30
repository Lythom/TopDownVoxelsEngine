using System;
using Cysharp.Threading.Tasks;
using Popcron;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class Character : MonoBehaviour {
        public float Speed = 5.0f;
        public Vector3 Acceleration = new(0, 0, 0);

        public AsyncReactiveProperty<BlockDefId> CurrentBlock = new(BlockDefId.Dirt);

        [Required]
        public LevelGenerator Level = null!;

        [Required]
        public Transform CameraTransform = null!;

        private Controls _controls;

        private void Awake() {
            _controls = new Controls();
        }

        public void OnEnable() {
            _controls.Enable();
        }

        public void OnDisable() {
            _controls.Disable();
        }

        private void Update() {
            if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                if (CurrentBlock.Value > 0) CurrentBlock.Value--;
            } else if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                if ((int) CurrentBlock.Value < Enum.GetNames(typeof(BlockDefId)).Length) CurrentBlock.Value++;
            }

            // Get the input on the x and z axis
            Vector2 moveInput = _controls.Gameplay.Move.ReadValue<Vector2>();

            // Create a new vector of the direction we want to move in. 
            // We assume Y movement (upwards) is 0 as we are moving only on X and Z axis
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 movement = CameraTransform.rotation * move * Speed * Time.deltaTime;

            // If we have some input
            if (move != Vector3.zero) {
                // Create a quaternion (rotation) based on looking down the vector from the player to the camera.
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);

                // Smoothly interpolate between current rotation and target rotation
                transform.rotation = targetRotation; //Quaternion.Slerp(transform.rotation, targetRotation, 5.0f * Time.deltaTime);
            }

            // Apply the movement to the player's rigidbody using the camera's rotation
            Acceleration.x = movement.x;
            Acceleration.z = movement.z;

            var t = transform;
            var pos = t.position;
            pos += Acceleration;
            t.position = pos;

            var facingPosition = pos + t.rotation * Vector3.forward * 1.5f;
            // var cell = Level.GetCellAt(facingPosition, out _, out _, out _);
            BCubeDrawer.Cube(
                new Vector3(Mathf.Round(facingPosition.x), Mathf.Round(facingPosition.y), Mathf.Round(facingPosition.z)),
                Quaternion.identity,
                Vector3.one
            );
        }
    }
}