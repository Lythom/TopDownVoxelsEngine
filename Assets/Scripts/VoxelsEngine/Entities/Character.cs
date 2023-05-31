using System;
using Cysharp.Threading.Tasks;
using Popcron;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using VoxelsEngine.Tools;

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
        private readonly Cooldown _placeCooldown = new(0.2f);
        private readonly Cooldown _jumpCooldown = new(0.5f);
        private float _jumpChargeStart;
        private float _spawnedTime;
        private Camera _cam;

        private void Awake() {
            _controls = new Controls();
            _spawnedTime = Time.time;
            _cam = Camera.main;
            var position = transform.position;
            transform.position = new Vector3(position.x, 10, position.z);
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
            Vector3 movement = CameraTransform.rotation * move;
            movement.y = 0;
            movement = movement.normalized * (Speed * Time.deltaTime);

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
                var forwardXPosFeet = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, -0.8f, 0)).WorldToCell();
                var forwardXPosHead = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, 0.8f, 0)).WorldToCell();
                var cellFeet = Level.GetCellAt(forwardXPosFeet);
                var cellHead = Level.GetCellAt(forwardXPosHead);
                if (!cellFeet.IsAir() || !cellHead.IsAir()) {
                    Acceleration.x = 0;
                }
            }

            if (Mathf.Abs(movement.z) > 0) {
                var forwardZPosFeet = (pos + new Vector3(0, -0.8f, Mathf.Sign(movement.z) * 0.6f)).WorldToCell();
                var forwardZPosHead = (pos + new Vector3(0, 0.8f, Mathf.Sign(movement.z) * 0.6f)).WorldToCell();
                var cellFeet = Level.GetCellAt(forwardZPosFeet);
                var cellHead = Level.GetCellAt(forwardZPosHead);
                if (!cellFeet.IsAir() || !cellHead.IsAir()) {
                    Acceleration.z = 0;
                }
            }


            var groundPosition = (pos + Vector3.down).WorldToCell();
            BCubeDrawer.Cube(
                groundPosition,
                Quaternion.identity,
                Vector3.one,
                Color.gray
            );
            var groundCell = Level.GetCellAt(groundPosition);

            if (Time.time - _spawnedTime > 1) {
                if (!groundCell.HasValue || groundCell.Value.BlockDef == BlockDefId.Air) {
                    // fall if no ground under
                    Acceleration.y -= Gravity * Time.deltaTime;
                    if (Acceleration.y < -0.9f) Acceleration.y = -0.9f;
                } else if (Acceleration.y < 0) {
                    Acceleration.y = 0;
                }
            }


            // var facingPosition = (pos + t.rotation * Vector3.forward * 1.5f).WorldToCell();
            // var targetPosition = facingPosition;
            var targetPosition = GetCollidedBlockPosition(Level, _cam.ScreenPointToRay(Input.mousePosition));


            //     var c = Level.GetCellAt(facingPosition);

            if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                if (SelectedItem.Value > 0) SelectedItem.Value--;
            } else if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                if ((int) SelectedItem.Value < Enum.GetNames(typeof(BlockDefId)).Length) SelectedItem.Value++;
            }

            if (targetPosition != null) {
                BCubeDrawer.Cube(
                    targetPosition.Value,
                    Quaternion.identity,
                    Vector3.one
                );

                if (_controls.Gameplay.Place.IsPressed()) {
                    var succeeded = _placeCooldown.TryPerform() && Level.SetCellAt(targetPosition.Value, SelectedItem.Value);
                    if (succeeded) {
                        var (chX, chZ) = LevelTools.GetChunkPosition(targetPosition.Value);
                        Level.UpdateChunk(chX, chZ);
                    }
                }
            }

            if (_controls.Gameplay.Jump.WasPressedThisFrame() && _jumpCooldown.TryPerform() && !groundCell.IsAir()) {
                _jumpChargeStart = Time.time;
                Acceleration.y = JumpForce;
            }

            if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = Time.time - _jumpChargeStart;
                Acceleration.y += JumpChargeIntensity * Time.deltaTime * (1 - Mathf.Clamp01(jumpCharge * 2));
            }


            pos += Acceleration;
            t.position = pos;
        }

        private Plane _wkPlane;

        private Vector3? GetCollidedBlockPosition(LevelGenerator level, Ray mouseRay) {
            for (int y = ChunkData.Size - 1; y >= 0; y--) {
                _wkPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, y + 0.5f, 0));
                if (_wkPlane.Raycast(mouseRay, out var enter)) {
                    Vector3 position = mouseRay.GetPoint(enter);
                    var bottomCell = level.GetCellAt(position + Vector3.down * 0.5f);
                    if (!bottomCell.IsAir()) {
                        // Debug.Log((position + Vector3.down * 0.5f) + "/" + (position + Vector3.down * 0.5f).WorldToCell());
                        return (position + Vector3.up * 0.5f).WorldToCell();
                    }
                }
            }

            return null;
        }
    }
}