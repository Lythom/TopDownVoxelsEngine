﻿using System;
using Cysharp.Threading.Tasks;
using Popcron;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelsEngine.Tools;

namespace VoxelsEngine {
    public class Character : MonoBehaviour {
        public float Speed = 5.0f;
        public Vector3 Acceleration = new(0, 0, 0);

        public float JumpForce = 0.2f;
        public float JumpChargeIntensity = 1f;
        public float Gravity = 0.2f;

        public int PlacementRadius = 4;

        [FormerlySerializedAs("SelectedTool")]
        [FormerlySerializedAs("CurrentBlock")]
        public AsyncReactiveProperty<BlockDefId> SelectedItem = new(BlockDefId.Dirt);

        [Required]
        public LevelGenerator Level = null!;

        [Required]
        public Transform CameraTransform = null!;

        private Controls _controls = null!;
        private readonly Cooldown _placeCooldown = new(0.1f);
        private readonly Cooldown _jumpCooldown = new(0.5f);
        private float _jumpChargeStart;
        private float _spawnedTime;
        private Camera _cam = null!;

        private Plane _wkPlane;
        private Plane? _draggingPlane = null;
        private bool _isPlacing = false;

        private void Awake() {
            _controls = new Controls();
            _spawnedTime = Time.time;
            if (Camera.main == null) throw new ApplicationException("No camera found");
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
                if ((int) SelectedItem.Value < Enum.GetNames(typeof(BlockDefId)).Length) SelectedItem.Value++;
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
                Acceleration.y = JumpForce;
            }

            if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = Time.time - _jumpChargeStart;
                Acceleration.y += JumpChargeIntensity * Time.deltaTime * (1 - Mathf.Clamp01(jumpCharge * 2));
            }


            pos += Acceleration;
            t.position = pos;
        }

        private (Vector3Int?, Vector3Int?) GetBlocksOnPlane(Ray mouseRay, Plane plane) {
            Vector3Int? facingCursorPos = null;
            Vector3Int? collidingBlockPos = null;
            var axis = plane.normal;
            int dir = Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (plane.Raycast(mouseRay, out var enter)) {
                Vector3 collisionPos = mouseRay.GetPoint(enter);
                collidingBlockPos = (collisionPos + axis * (0.5f * dir)).WorldToCell();
                facingCursorPos = (collisionPos - axis * (0.5f * dir)).WorldToCell();
            }

            return (collidingBlockPos, facingCursorPos);
        }


        private (Vector3Int? collidingBlock, Vector3Int? facingBloc0k, Plane? plane) GetCollidedBlockPosition(LevelGenerator level, Ray mouseRay, Vector3 position, int radius = 4) {
            var (up, upC, upF, upPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.up, Mathf.RoundToInt(position.y), radius);
            var (right, rightC, rightF, rightPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.right, Mathf.RoundToInt(position.x), radius);
            var (forward, forwardC, forwardF, forwardPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.forward, Mathf.RoundToInt(position.z), radius);
            var min = Mathf.Min(up, right, forward);
            if (up > 0 && up == min) return (upC, upF, upPlane);
            if (right > 0 && right == min) return (rightC, rightF, rightPlane);
            if (forward > 0 && forward == min) return (forwardC, forwardF, forwardPlane);
            return (null, null, null);
        }

        /// <summary>
        /// Determines the block position that a ray collides with in a given level.
        /// </summary>
        /// <param name="level">The level in which to check for collisions.</param>
        /// <param name="mouseRay">The ray to check for collisions.</param>
        /// <param name="axis">Should be the positive vector indicating the Plane orientation. ie. Vector3.up, Vector3.right or Vector3.forward.</param>
        /// <param name="playerPosition">The position of the player in the level.</param>
        /// <param name="radius">The radius within which to check for collisions. Default is 4.</param>
        /// <returns>The distance at which the ray enters the block, the position of the block that the ray collides with, and the position of the block that faces the colliding face.</returns>
        private (float rayEnter, Vector3Int? collidingBlock, Vector3Int? facingBlock, Plane? p) GetCollidedBlockPositionOnPlane(LevelGenerator level, Ray mouseRay, Vector3 axis, int playerPosition, int radius = 4) {
            // Define the start and stop positions for the collision check based on the player's position and the specified radius.
            // the goal is to start with blocks that are closer to the camera (=mouse ray origin)
            var start = playerPosition - radius;
            var stop = playerPosition + radius;

            // Determine the direction of the ray along the specified axis.
            int dir = Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (dir < 0) (start, stop) = (stop, start);

            for (int pos = start; pos != stop; pos += dir) {
                _wkPlane.SetNormalAndPosition(axis, axis * (pos + dir * 0.5f));
                if (_wkPlane.Raycast(mouseRay, out var enter)) {
                    Vector3 position = mouseRay.GetPoint(enter);
                    // Determine the position inside the block that the ray intersects.
                    var insidePosition = (position + axis * (0.5f * dir)).WorldToCell();
                    var inside = level.GetCellAt(insidePosition);
                    if (!inside.IsAir()) {
                        return (enter, insidePosition, (position - axis * (0.5f * dir)).WorldToCell(), _wkPlane);
                    }
                }
            }

            return (float.MaxValue, null, null, null);
        }
    }
}