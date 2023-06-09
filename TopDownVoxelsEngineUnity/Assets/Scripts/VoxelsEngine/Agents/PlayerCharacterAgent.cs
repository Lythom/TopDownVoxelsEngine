﻿using System;
using LoneStoneStudio.Tools;
using Popcron;
using Shared;
using Shared.Net;
using UnityEngine;
using VoxelsEngine.Tools;
using Ray = UnityEngine.Ray;
using Vector2 = UnityEngine.Vector2;
using Vector3 = Shared.Vector3;
using Vector3Int = Shared.Vector3Int;

namespace VoxelsEngine {
    public class PlayerCharacterAgent : ConnectedBehaviour {
        public ushort CharacterId = 0;
        public float Speed = 5.0f;

        public float JumpForce = 0.2f;
        public float JumpChargeIntensity = 1f;

        public int PlacementRadius = 4;

        // visual is slightly delayed because FPS rate might be a bit faster than update rate
        // this is to bring smoother visual
        public float VisualSnappingStrength = 0.3f;


        [RequiredInScene]
        public Transform CameraTransform = null!;

        private Controls _controls = null!;
        private readonly Cooldown _placeCooldown = new(0.02f);
        private readonly Cooldown _jumpCooldown = new(0.5f);
        private float _jumpChargeStart;
        private float _spawnedTime;
        private Camera _cam = null!;

        private Plane _wkPlane;
        private Plane? _draggingPlane = null;
        private bool _isPlacing = false;
        private Character? _character;

        private Vector3 _nextPosition;
        private string? _levelId;

        private void Awake() {
            _controls = new Controls();
            _spawnedTime = Time.time;
            if (Camera.main == null) throw new ApplicationException("No camera found");
            _cam = Camera.main;
            var position = transform.position;
            transform.position = new Vector3(position.x, 10, position.z);
        }

        protected override void OnEnable() {
            base.OnEnable();
            _controls.Enable();
        }

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.LocalPlayerStateSelector, p => _character = p);
            Subscribe(state.Selectors.LocalPlayerLevelIdSelector, lId => _levelId = lId);
        }

        public void OnDisable() {
            _controls.Disable();
        }

        /// <summary>
        /// In update, read the controls.
        /// Currently the client is in charge of calculating the speed, so there is no limitation to speeding or teleporting cheats.
        /// </summary>
        private void Update() {
            if (_character == null) return;
            if (_levelId == null || !ClientEngine.State.Levels.ContainsKey(_levelId)) return;
            if (!ClientEngine.State.Levels.TryGetValue(_levelId, out var level)) return;

            Vector3 pos = _character.Position;

            // Get the input on the x and z axis
            Vector2 moveInput = _controls.Gameplay.Move.ReadValue<Vector2>();

            // Create a new vector of the direction we want to move in. 
            // We assume Y movement (upwards) is 0 as we are moving only on X and Z axis
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            UnityEngine.Vector3 movement = CameraTransform.rotation * move;
            movement.y = 0;
            movement = movement.normalized * (Speed * Time.fixedDeltaTime);

            // If we have some input
            if (move != Vector3.zero) {
                // Create a quaternion (rotation) based on looking down the vector from the player to the camera.
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);

                // Smoothly interpolate between current rotation and target rotation
                transform.rotation = targetRotation; //Quaternion.Slerp(transform.rotation, targetRotation, 5.0f * Time.deltaTime);
            }

            // Apply the movement to the player's rigidbody using the camera's rotation
            Vector3 velocity = _character.Velocity;
            velocity.X = movement.x;
            velocity.Z = movement.z;

            // check collisions
            if (Mathf.Abs(movement.x) > 0) {
                var forwardXPosFeet = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, -0.8f, 0)).WorldToCell();
                var forwardXPosHead = (pos + new Vector3(Mathf.Sign(movement.x) * 0.6f, 0.8f, 0)).WorldToCell();
                var cellFeet = level.TryGetExistingCell(forwardXPosFeet);
                var cellHead = level.TryGetExistingCell(forwardXPosHead);
                if (!cellFeet.IsAir() || !cellHead.IsAir()) {
                    velocity.X = 0;
                }
            }

            if (Mathf.Abs(movement.z) > 0) {
                var forwardZPosFeet = (pos + new Vector3(0, -0.8f, Mathf.Sign(movement.z) * 0.6f)).WorldToCell();
                var forwardZPosHead = (pos + new Vector3(0, 0.8f, Mathf.Sign(movement.z) * 0.6f)).WorldToCell();
                var cellFeet = level.TryGetExistingCell(forwardZPosFeet);
                var cellHead = level.TryGetExistingCell(forwardZPosHead);
                if (!cellFeet.IsAir() || !cellHead.IsAir()) {
                    velocity.Z = 0;
                }
            }

            var groundPosition = (pos + Vector3.down).WorldToCell();
            BCubeDrawer.Cube(
                groundPosition,
                Quaternion.identity,
                Vector3.one,
                Color.gray
            );
            var groundCell = level.TryGetExistingCell(groundPosition);

            Vector3Int? collidingBlockPos;
            Vector3Int? facingCursorPos;
            var mouseRay = _cam.ScreenPointToRay(Input.mousePosition);
            if (_isPlacing && _draggingPlane.HasValue) {
                (collidingBlockPos, facingCursorPos) = GetBlocksOnPlane(mouseRay, _draggingPlane.Value);
            } else {
                Plane? plane;
                (collidingBlockPos, facingCursorPos, plane) = GetCollidedBlockPosition(level, mouseRay, pos, PlacementRadius);
                if (plane.HasValue) _draggingPlane = plane.Value;
            }

            if (facingCursorPos != null && collidingBlockPos != null) {
                var target = _character.SelectedTool.Value switch {
                    ToolId.PlaceBlock => facingCursorPos.Value,
                    ToolId.PlaceFurniture => facingCursorPos.Value,
                    _ => collidingBlockPos.Value
                };
                BCubeDrawer.Cube(
                    target,
                    Quaternion.identity,
                    Vector3.one
                );

                if (_controls.Gameplay.Place.IsPressed()) {
                    var blockToSet = _character.SelectedTool.Value switch {
                        ToolId.PlaceBlock => _character.SelectedBlock.Value,
                        ToolId.ExchangeBlock => _character.SelectedBlock.Value,
                        _ => BlockId.Air
                    };
                    switch (_character.SelectedTool.Value) {
                        case ToolId.PlaceBlock:
                        case ToolId.ExchangeBlock:
                        case ToolId.RemoveBlock:
                            _isPlacing = true;
                            var succeeded = _placeCooldown.TryPerform() && level.CanSet(target, blockToSet);
                            if (succeeded) {
                                var (x, y, z) = target;
                                SendBlindMessageOptimistic(new PlaceBlocksGameEvent(0, CharacterId, (short) x, (short) y, (short) z, blockToSet));
                            }

                            break;
                    }
                }

                if (_controls.Gameplay.Place.WasReleasedThisFrame()) {
                    _isPlacing = false;
                }
            }

            if (_controls.Gameplay.Jump.WasPressedThisFrame() && _jumpCooldown.TryPerform() && !groundCell.IsAir()) {
                _jumpChargeStart = Time.time;
                velocity.Y = JumpForce;
            } else if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = Time.time - _jumpChargeStart;
                velocity.Y += JumpChargeIntensity * Time.fixedDeltaTime * (1 - Mathf.Clamp01(jumpCharge * jumpCharge));
            }

            Vector2 scrollDelta = _controls.Gameplay.SelectTool.ReadValue<Vector2>();
            if (scrollDelta.y > 0) {
                ToolId nextToolId = (ToolId) M.Mod((int) _character.SelectedTool.Value + 1, Enum.GetNames(typeof(ToolId)).Length);
                SendBlindMessageOptimistic(new ChangeToolGameEvent(0, CharacterId, nextToolId));
            } else if (scrollDelta.y < 0) {
                ToolId prevToolId = (ToolId) M.Mod((int) _character.SelectedTool.Value - 1, Enum.GetNames(typeof(ToolId)).Length);
                SendBlindMessageOptimistic(new ChangeToolGameEvent(0, CharacterId, prevToolId));
            }

            if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                BlockId nextBlockId = (BlockId) M.Mod((int) _character.SelectedBlock.Value + 1, Enum.GetNames(typeof(BlockId)).Length);
                if (nextBlockId == BlockId.Air) nextBlockId++;
                SendBlindMessageOptimistic(new ChangeBlockGameEvent(0, CharacterId, nextBlockId));
            } else if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                var length = Enum.GetNames(typeof(BlockId)).Length;
                BlockId prevBlockId = (BlockId) M.Mod((int) _character.SelectedBlock.Value - 1, length);
                if (prevBlockId == BlockId.Air) prevBlockId = (BlockId) (length - 1);
                SendBlindMessageOptimistic(new ChangeBlockGameEvent(0, CharacterId, prevBlockId));
            }


            // Wild override of local state for client side prediction
            _character.Velocity = velocity;
            _character.Angle = Character.CompressAngle(transform.eulerAngles.y);

            if (velocity.X != 0 || velocity.Z != 0) transform.rotation = Quaternion.LookRotation(new UnityEngine.Vector3(velocity.X, 0, velocity.Z), UnityEngine.Vector3.up);
            // apply a lerp transition for smooth animation
            // _character.Position is updated in the TickEvent
            if (_character.Position.Y < -20) {
                _character.Position.Y = 20f;
            }

            transform.position = UnityEngine.Vector3.Lerp(transform.position, _character.Position, VisualSnappingStrength * 50 * Time.deltaTime);
        }

        /// <summary>
        /// In fixed update, update the display
        /// </summary>
        private void FixedUpdate() {
            // optimistic update
            if (_character == null) return;
        }

        private (Vector3Int?, Vector3Int?) GetBlocksOnPlane(Ray mouseRay, Plane plane) {
            Vector3Int? facingCursorPos = null;
            Vector3Int? collidingBlockPos = null;
            var axis = plane.normal;
            int dir = UnityEngine.Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (plane.Raycast(mouseRay, out var enter)) {
                UnityEngine.Vector3 collisionPos = mouseRay.GetPoint(enter);
                collidingBlockPos = LevelTools.WorldToCell((collisionPos + axis * (0.5f * dir)));
                facingCursorPos = LevelTools.WorldToCell((collisionPos - axis * (0.5f * dir)));
            }

            return (collidingBlockPos, facingCursorPos);
        }


        private (Vector3Int? collidingBlock, Vector3Int? facingBloc0k, Plane? plane) GetCollidedBlockPosition(
            LevelMap level,
            Ray mouseRay,
            Vector3 position,
            int radius = 4
        ) {
            var (up, upC, upF, upPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.up, Mathf.RoundToInt(position.Y), radius);
            var (right, rightC, rightF, rightPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.right, Mathf.RoundToInt(position.X), radius);
            var (forward, forwardC, forwardF, forwardPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.forward, Mathf.RoundToInt(position.Z), radius);
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
        private (float rayEnter, Vector3Int? collidingBlock, Vector3Int? facingBlock, Plane? p) GetCollidedBlockPositionOnPlane(
            LevelMap level,
            Ray mouseRay,
            Vector3 axis,
            int playerPosition,
            int radius = 4
        ) {
            // Define the start and stop positions for the collision check based on the player's position and the specified radius.
            // the goal is to start with blocks that are closer to the camera (=mouse ray origin)
            var start = playerPosition - radius;
            var stop = playerPosition + radius;

            // Determine the direction of the ray along the specified axis.
            int dir = UnityEngine.Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (dir < 0) (start, stop) = (stop, start);

            for (int pos = start; pos != stop; pos += dir) {
                _wkPlane.SetNormalAndPosition(axis, axis * (pos + dir * 0.5f));
                if (_wkPlane.Raycast(mouseRay, out var enter)) {
                    Vector3 position = mouseRay.GetPoint(enter);
                    // Determine the position inside the block that the ray intersects.
                    var insidePosition = (position + axis * (0.5f * dir)).WorldToCell();
                    var inside = level.TryGetExistingCell(insidePosition);
                    if (!inside.IsAir()) {
                        return (enter, insidePosition, (position - axis * (0.5f * dir)).WorldToCell(), _wkPlane);
                    }
                }
            }

            return (float.MaxValue, null, null, null);
        }
    }
}