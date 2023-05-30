using System;
using Cysharp.Threading.Tasks;
using Popcron;
using Sirenix.OdinInspector;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

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
            var t = transform;
            var pos = t.position;

            // Get the input on the x and z axis
            Vector2 moveInput = _controls.Gameplay.Move.ReadValue<Vector2>();

            // Create a new vector of the direction we want to move in. 
            // We assume Y movement (upwards) is 0 as we are moving only on X and Z axis
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 movement = Quaternion.Euler(0, CameraTransform.rotation.y, 0) * move * (Speed * Time.deltaTime);

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

            // check collisions
            if (Mathf.Abs(movement.x) > 0) {
                var forwardXPos = (pos + new Vector3(Mathf.Sign(movement.x), 0, 0)).Snapped();
                BCubeDrawer.Cube(
                    forwardXPos,
                    Quaternion.identity,
                    Vector3.one,
                    Color.yellow
                );
                var cell = Level.GetCellAt(forwardXPos);
                if (!cell.HasValue || cell.Value.BlockDef != BlockDefId.Air) {
                    Acceleration.x = 0;
                }
            }

            if (Mathf.Abs(movement.z) > 0) {
                var forwardZPos = (pos + new Vector3(0, 0, Mathf.Sign(movement.z))).Snapped();
                BCubeDrawer.Cube(
                    forwardZPos,
                    Quaternion.identity,
                    Vector3.one,
                    new Color(1f, 0.77f, 0.1f)
                );
                var cell = Level.GetCellAt(forwardZPos);
                if (!cell.HasValue || cell.Value.BlockDef != BlockDefId.Air) {
                    Acceleration.z = 0;
                }
            }

            pos += Acceleration;
            t.position = pos;

            var facingPosition = (pos + t.rotation * Vector3.forward * 1.5f).Snapped();
       //     var c = Level.GetCellAt(facingPosition);
            BCubeDrawer.Cube(
                facingPosition,
                Quaternion.identity,
                Vector3.one
            );

            if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                if (CurrentBlock.Value > 0) CurrentBlock.Value--;
            } else if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                if ((int) CurrentBlock.Value < Enum.GetNames(typeof(BlockDefId)).Length) CurrentBlock.Value++;
            }

            if (_controls.Gameplay.Place.WasPressedThisFrame()) {
            }
        }
    }
}