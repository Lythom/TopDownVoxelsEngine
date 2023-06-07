using System;
using log4net.Core;
using Popcron;
using Shared;
using UnityEngine;
using VoxelsEngine.Tools;
using Vector3 = UnityEngine.Vector3;
using Vector3Int = UnityEngine.Vector3Int;

namespace VoxelsEngine.InputManagers {
    public class CharacterInputs : MonoBehaviour {
        private Controls _controls = null!;
        private readonly Cooldown _placeCooldown = new(0.1f);
        private readonly Cooldown _jumpCooldown = new(0.5f);
        private float _jumpChargeStart;
        private float _spawnedTime;
        private Camera _cam = null!;

        private void Awake() {
            _controls = new Controls();
            _spawnedTime = Time.time;
            if (Camera.main == null) throw new ApplicationException("No camera found");
            _cam = Camera.main;
        }


        public void OnEnable() {
            _controls.Enable();
        }

        public void OnDisable() {
            _controls.Disable();
        }

        private void Update() {
            var t = transform;
            Vector3 pos = t.position;

            // Get the input on the x and z axis
            UnityEngine.Vector2 moveInput = _controls.Gameplay.Move.ReadValue<UnityEngine.Vector2>();

            // Create a new vector of the direction we want to move in. 
            // We assume Y movement (upwards) is 0 as we are moving only on X and Z axis
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            UnityEngine.Vector3 movement = CameraTransform.rotation * move;
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
            Acceleration.X = movement.x;
            Acceleration.Z = movement.z;

            // check collisions
            if (Mathf.Abs(movement.x) > 0) {
                var forwardXPosFeet = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, -0.8f, 0)).WorldToCell();
                var forwardXPosHead = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, 0.8f, 0)).WorldToCell();
                var cellFeet = Level.GetCellAt(forwardXPosFeet);
                var cellHead = Level.GetCellAt(forwardXPosHead);
                if (!cellFeet.IsAir() || !cellHead.IsAir()) {
                    Acceleration.X = 0;
                }
            }

            if (Mathf.Abs(movement.z) > 0) {
                var forwardZPosFeet = (pos + new Vector3(0, -0.8f, Mathf.Sign(movement.z) * 0.6f)).WorldToCell();
                var forwardZPosHead = (pos + new Vector3(0, 0.8f, Mathf.Sign(movement.z) * 0.6f)).WorldToCell();
                var cellFeet = Level.GetCellAt(forwardZPosFeet);
                var cellHead = Level.GetCellAt(forwardZPosHead);
                if (!cellFeet.IsAir() || !cellHead.IsAir()) {
                    Acceleration.Z = 0;
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
                if (!groundCell.HasValue || groundCell.Value.Block == BlockId.Air) {
                    // fall if no ground under
                    Acceleration.Y -= Gravity * Time.deltaTime;
                    if (Acceleration.Y < -0.9f) Acceleration.Y = -0.9f;
                } else if (Acceleration.Y < 0) {
                    Acceleration.Y = 0;
                }
            }

            Vector3Int? collidingBlockPos;
            Vector3Int? facingCursorPos;
            var mouseRay = _cam.ScreenPointToRay(Input.mousePosition);
            if (_isPlacing && _draggingPlane.HasValue) {
                (collidingBlockPos, facingCursorPos) = GetBlocksOnPlane(mouseRay, _draggingPlane.Value);
            } else {
                Plane? plane;
                (collidingBlockPos, facingCursorPos, plane) = GetCollidedBlockPosition(Level, mouseRay, pos, PlacementRadius);
                if (plane.HasValue) _draggingPlane = plane.Value;
            }

            if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                if (SelectedItem.Value > 0) SelectedItem.Value--;
            } else if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                if ((int) SelectedItem.Value < Enum.GetNames(typeof(BlockId)).Length) SelectedItem.Value++;
            }

            if (facingCursorPos != null) {
                BCubeDrawer.Cube(
                    facingCursorPos.Value,
                    Quaternion.identity,
                    Vector3.one
                );

                if (_controls.Gameplay.Place.IsPressed()) {
                    _isPlacing = true;
                    var succeeded = _placeCooldown.TryPerform() && Level.SetCellAt(facingCursorPos.Value, SelectedItem.Value);
                    if (succeeded) {
                        var (chX, chZ) = LevelTools.GetChunkPosition(facingCursorPos.Value);
                        Level.UpdateChunk(chX, chZ);
                    }
                }

                if (_controls.Gameplay.Place.WasReleasedThisFrame()) {
                    _isPlacing = false;
                }
            }

            if (_controls.Gameplay.Jump.WasPressedThisFrame() && _jumpCooldown.TryPerform() && !groundCell.IsAir()) {
                _jumpChargeStart = Time.time;
                Acceleration.Y = JumpForce;
            }

            if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = Time.time - _jumpChargeStart;
                Acceleration.Y += JumpChargeIntensity * Time.deltaTime * (1 - Mathf.Clamp01(jumpCharge * 2));
            }


            pos += Acceleration;
            t.position = pos;
        }
    }
}