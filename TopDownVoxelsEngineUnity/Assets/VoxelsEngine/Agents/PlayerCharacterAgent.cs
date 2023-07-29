using System;
using Popcron;
using Shared;
using Shared.Net;
using UnityEngine;
using VoxelsEngine.Tools;
using VoxelsEngine.VoxelsEngine.Tools;
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

        private void Start() {
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
                (collidingBlockPos, facingCursorPos) = mouseRay.GetBlocksOnPlane(_draggingPlane.Value);
            } else {
                Plane? plane;
                (collidingBlockPos, facingCursorPos, plane) = mouseRay.GetCollidedBlockPosition(level, pos, PlacementRadius);
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
            transform.position = UnityEngine.Vector3.Lerp(transform.position, _character.Position, VisualSnappingStrength * 50 * Time.deltaTime);

            // _character.Position is updated in the TickEvent
            if (_character.Position.Y < -20) {
                _character.Position.Y = 20f;
            }
        }

        /// <summary>
        /// In fixed update, update the display
        /// </summary>
        private void FixedUpdate() {
            // optimistic update
            if (_character == null) return;
        }
    }
}