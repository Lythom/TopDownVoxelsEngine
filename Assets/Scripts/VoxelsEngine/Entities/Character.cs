using System;
using Cysharp.Threading.Tasks;
using Popcron;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelsEngine {
    public class Character : MonoBehaviour {
        public float Speed = 5.0f;
        public Vector3 Acceleration = new(0, 0, 0);

        public float JumpForce = 0.2f;
        public float JumpChargeIntensity = 1f;
        public float Gravity = 0.2f;

        [FormerlySerializedAs("SelectedTool")]
        [FormerlySerializedAs("CurrentBlock")]
        public AsyncReactiveProperty<BlockDefId> SelectedItem = new(BlockDefId.Dirt);

        [Required]
        public LevelGenerator Level = null!;

        [Required]
        public Transform CameraTransform = null!;

        private Controls _controls;
        private float _jumpChargeStart;
        private float _spawnedTime;

        private void Awake() {
            _controls = new Controls();
            _spawnedTime = Time.time;
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
            Vector3 movement = CameraTransform.rotation * move * (Speed * Time.deltaTime);
            movement.y = 0;

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
                var forwardXPos = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, 0, 0)).Snapped();
                var cell = Level.GetCellAt(forwardXPos);
                if (cell.HasValue && cell.Value.BlockDef != BlockDefId.Air) {
                    Acceleration.x = 0;
                }
            }

            if (Mathf.Abs(movement.z) > 0) {
                var forwardZPos = (pos + new Vector3(0, 0, Mathf.Sign(movement.z) * 0.6f)).Snapped();
                var cell = Level.GetCellAt(forwardZPos);
                if (!cell.HasValue || cell.Value.BlockDef != BlockDefId.Air) {
                    Acceleration.z = 0;
                }
            }


            var groundPosition = (pos + Vector3.down).Snapped();
            BCubeDrawer.Cube(
                groundPosition,
                Quaternion.identity,
                Vector3.one,
                Color.gray
            );
            if (Time.time - _spawnedTime > 3) {
                var groundCell = Level.GetCellAt(groundPosition);
                if (!groundCell.HasValue || groundCell.Value.BlockDef == BlockDefId.Air) {
                    // fall if no ground under
                    Acceleration.y -= Gravity * Time.deltaTime;
                    if (Acceleration.y < -0.9f) Acceleration.y = -0.9f;
                } else if (Acceleration.y < 0) {
                    Acceleration.y = 0;
                }
            }


            var facingPosition = (pos + t.rotation * Vector3.forward * 1.5f).Snapped();
            //     var c = Level.GetCellAt(facingPosition);
            BCubeDrawer.Cube(
                facingPosition,
                Quaternion.identity,
                Vector3.one
            );

            if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                if (SelectedItem.Value > 0) SelectedItem.Value--;
            } else if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                if ((int) SelectedItem.Value < Enum.GetNames(typeof(BlockDefId)).Length) SelectedItem.Value++;
            }

            if (_controls.Gameplay.Place.WasPressedThisFrame()) {
                var succeeded = Level.SetCellAt(facingPosition, SelectedItem.Value);
                if (succeeded) {
                    var (chX, chZ) = LevelTools.GetChunkPosition(facingPosition);
                    Level.UpdateChunk(chX, chZ);
                }
            }

            if (_controls.Gameplay.Jump.WasPressedThisFrame()) {
                _jumpChargeStart = Time.time;
                Acceleration.y = JumpForce;
            }

            if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = Time.time - _jumpChargeStart;
                Acceleration.y += JumpChargeIntensity * Time.deltaTime * (1 - Mathf.Clamp01(jumpCharge));
            }


            pos += Acceleration;
            t.position = pos;
        }
    }
}