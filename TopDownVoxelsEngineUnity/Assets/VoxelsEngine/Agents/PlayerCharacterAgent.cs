using System;
using LoneStoneStudio.Tools;
using Popcron;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine.Tools;
using VoxelsEngine.VoxelsEngine.Tools;
using Ray = UnityEngine.Ray;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector3Int = Shared.Vector3Int;

namespace VoxelsEngine {
    public class PlayerCharacterAgent : ConnectedBehaviour {
        public ushort CharacterId = 0;

        [ShowInInspector]
        public float DeltaTime => Time.deltaTime;

        [Required]
        public FaceController FaceController = null!;

        [Required]
        public Animator Animator = null!;

        [RequiredInScene]
        public Transform CameraTransform = null!;

        [Title("Game feel configuration")]
        public float VisualSnappingStrength = 0.3f;

        public float WallCollisionProximity = 0.6f;

        [Title("Zoom Configuration")]
        public float CameraHeightOffset = 2.5f;

        public float RotationSensivityX = 1.4f;
        public float RotationSensivityZ = 0.5f;

        // Define a new variable for the zoom level
        public float ZoomLevel = 10f;
        public float MinZoomLevel = 2f;
        public float MaxZoomLevel = 10f;
        public float MinZAngle = -4f;
        public float MaxBaseZAngle = 3f;
        public float MaxRatioZAngle = 1f;
        public float ZoomSensitivity = 10f;
        public float CameraZoomTiltStrength = 0.5f;
        public Vector3 CameraLookOffset = new(0, 2.5f, 0);

        [Title("Controls Configuration")]
        public int PlacementRadius = 6;

        public float Speed = 5.0f;

        private Controls _controls = null!;
        private Camera _cam = null!;
        private float _jumpChargeStart;

        public float JumpForce = 0.2f;
        public float JumpChargeIntensity = 1f;
        public float Gravity = 0.4f;

        private readonly Cooldown _jumpCooldown = new(0.05f);
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _originalOffset;
        private float _currentAngleX;
        private float _currentAngleZ;
        private bool _isRotating;
        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Altitude = Animator.StringToHash("Altitude");
        private Vector3 _vel;

        private Character? _character;

        private Vector3 _nextPosition;
        private string? _levelId;
        private bool _isPlacing;
        private Plane? _draggingPlane;
        private bool _initialized;

        void Awake() {
            _controls = new Controls();
        }

        protected override void OnEnable() {
            base.OnEnable();
            _controls.Enable();
            _vel = Vector3.zero;
        }

        public void Init(Camera cam, Vector3 position) {
            transform.position = position;
            CameraTransform = cam.transform;
            _cam = cam;
            _position = position;
            _originalOffset = CameraTransform.position - position;
            _initialized = true;
        }

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.LocalPlayerStateSelector, p => {
                _character = p;
                if (p == null) return;
                _position = p.Position;
                _rotation = Quaternion.Euler(0, Character.UncompressAngle(p.Angle), 0);
            });
            Subscribe(state.Selectors.LocalPlayerLevelIdSelector, lId => _levelId = lId);
        }

        public void OnDisable() {
            _controls.Disable();
        }

        void Update() {
            if (!_initialized) return;
            if (_character == null) return;
            if (_levelId == null || !ClientEngine.State.Levels.ContainsKey(_levelId)) return;
            if (!ClientEngine.State.Levels.TryGetValue(_levelId, out var level)) return;

            var selectedTool = _character.SelectedTool.Value;
            var selectedBlock = _character.SelectedBlock.Value;

            UpdateTools(selectedTool, selectedBlock);
            var groundPosition = LevelTools.WorldToCell(_position + Vector3.down * 0.001f);
            var groundCell = level.TryGetExistingCell(groundPosition);
            var isInAir = groundCell.IsAir();
            var mouseRay = _cam.ScreenPointToRay(Input.mousePosition);
            var (collidingBlockPos, facingCursorPos) = GetMouseTargets(level, mouseRay);
            UpdateAction(level, collidingBlockPos, facingCursorPos, selectedTool, selectedBlock);
            UpdateCamera();
            Vector3 movement;
            (_vel, movement) = UpdateMove(level, _vel, isInAir, groundPosition.Y + 0.5f);
            UpdateAnimation(movement, isInAir);
            transform.position = Vector3.Lerp(transform.position, _position, VisualSnappingStrength * 10 * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, VisualSnappingStrength * 10 * Time.deltaTime);

            // Wild override of state for client side prediction
            // Child, don't do that at home…
            _character.Velocity = _vel;
            _character.Angle = Character.CompressAngle(transform.eulerAngles.y);

            BCubeDrawer.Cube(
                groundPosition,
                Quaternion.identity,
                Vector3.one,
                Color.gray
            );
        }

        private void UpdateAnimation(Vector3 movement, bool isInAir) {
            Animator.SetFloat(Velocity, movement.x * movement.x + movement.z * movement.z);
            Animator.SetFloat(Altitude, isInAir ? 1 : -0.01f);
            FaceController.CurrentFace = movement.magnitude > 0.001f ? FaceController.Faces.Angry : FaceController.Faces.SmileBlink;
        }

        private (Vector3Int? collidingBlockPos, Vector3Int? facingCursorPos) GetMouseTargets(LevelMap level, Ray mouseRay) {
            Vector3Int? collidingBlockPos;
            Vector3Int? facingCursorPos;
            if (_isPlacing && _draggingPlane.HasValue) {
                (collidingBlockPos, facingCursorPos) = mouseRay.GetBlocksOnPlane(_draggingPlane.Value);
            } else {
                Plane? plane;
                (collidingBlockPos, facingCursorPos, plane) = mouseRay.GetCollidedBlockPosition(level, _position, PlacementRadius);
                if (plane.HasValue) _draggingPlane = plane.Value;
            }

            return (collidingBlockPos, facingCursorPos);
        }

        private void UpdateAction(LevelMap level, Vector3Int? collidingBlockPos, Vector3Int? facingCursorPos, ToolId selectedTool, BlockId selectedBlock) {
            if (facingCursorPos != null && collidingBlockPos != null) {
                var target = selectedTool switch {
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
                    var blockToSet = selectedTool switch {
                        ToolId.PlaceBlock => selectedBlock,
                        ToolId.ExchangeBlock => selectedBlock,
                        _ => BlockId.Air
                    };
                    switch (selectedTool) {
                        case ToolId.PlaceBlock:
                        case ToolId.ExchangeBlock:
                        case ToolId.RemoveBlock:
                            _isPlacing = true;
                            var succeeded = level.CanSet(target, blockToSet);
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
        }

        private void UpdateTools(ToolId selectedTool, BlockId selectedBlock) {
            Vector2 scrollDelta = _controls.Gameplay.SelectTool.ReadValue<Vector2>();
            if (scrollDelta.y > 0) {
                ToolId nextToolId = (ToolId) M.Mod((int) selectedTool + 1, Enum.GetNames(typeof(ToolId)).Length);
                SendBlindMessageOptimistic(new ChangeToolGameEvent(0, CharacterId, nextToolId));
            } else if (scrollDelta.y < 0) {
                ToolId prevToolId = (ToolId) M.Mod((int) selectedTool - 1, Enum.GetNames(typeof(ToolId)).Length);
                SendBlindMessageOptimistic(new ChangeToolGameEvent(0, CharacterId, prevToolId));
            }

            if (_controls.Gameplay.SelectNextItem.WasPressedThisFrame()) {
                BlockId nextBlockId = (BlockId) M.Mod((int) selectedBlock + 1, Enum.GetNames(typeof(BlockId)).Length);
                if (nextBlockId == BlockId.Air) nextBlockId++;
                SendBlindMessageOptimistic(new ChangeBlockGameEvent(0, CharacterId, nextBlockId));
            } else if (_controls.Gameplay.SelectPrevItem.WasPressedThisFrame()) {
                var length = Enum.GetNames(typeof(BlockId)).Length;
                BlockId prevBlockId = (BlockId) M.Mod((int) selectedBlock - 1, length);
                if (prevBlockId == BlockId.Air) prevBlockId = (BlockId) (length - 1);
                SendBlindMessageOptimistic(new ChangeBlockGameEvent(0, CharacterId, prevBlockId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="vel"></param>
        /// <param name="isInAir"></param>
        /// <returns>Velocity and Horizontal direction in which the character moves.</returns>
        private (Vector3, Vector3) UpdateMove(LevelMap level, Vector3 vel, bool isInAir, float groundY) {
            Vector2 moveInput = _controls.Gameplay.Move.ReadValue<Vector2>();
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 moveDirection = (CameraTransform.rotation * move).WithY(0).normalized;

            // If we have some input
            if (moveDirection != Vector3.zero) {
                // Create a quaternion (rotation) based on looking down the vector from the player to the camera.
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);

                // Smoothly interpolate between current rotation and target rotation
                _rotation = targetRotation; //Quaternion.Slerp(transform.rotation, targetRotation, 5.0f * Time.deltaTime);
            }

            // Update Velocity
            vel.x = moveDirection.x * Speed;
            vel.z = moveDirection.z * Speed;
            if (isInAir) {
                vel.y -= Gravity * Time.deltaTime;
            } else {
                vel.y = 0;
                _position.y = groundY;
            }

            if (_controls.Gameplay.Jump.WasPressedThisFrame() && _jumpCooldown.TryPerform() && !isInAir) {
                _jumpChargeStart = Time.time;
                vel.y = JumpForce;
            } else if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = (Time.time - _jumpChargeStart) * 2;
                vel.y += JumpChargeIntensity * Time.deltaTime * (1 - Mathf.Clamp01(jumpCharge));
            }

            if (Mathf.Abs(vel.x) > 0) {
                var forwardXPosFeet = _position + new Vector3(Mathf.Sign(vel.x) * WallCollisionProximity, 0.5f, 0);
                var forwardXPosStomach = _position + new Vector3(Mathf.Sign(vel.x) * WallCollisionProximity, 1.5f, 0);
                var forwardXPosHead = _position + new Vector3(Mathf.Sign(vel.x) * WallCollisionProximity, 2.5f, 0);
                var cellFeet = level.TryGetExistingCell(LevelTools.WorldToCell(forwardXPosFeet));
                var cellStomach = level.TryGetExistingCell(LevelTools.WorldToCell(forwardXPosStomach));
                var cellHead = level.TryGetExistingCell(LevelTools.WorldToCell(forwardXPosHead));
                if (!cellFeet.IsAir() || !cellStomach.IsAir() || !cellHead.IsAir()) {
                    vel.x = 0;
                }
            }

            if (Mathf.Abs(vel.z) > 0) {
                var forwardZPosFeet = _position + new Vector3(0, 0.5f, Mathf.Sign(vel.z) * WallCollisionProximity);
                var forwardZPosStomach = _position + new Vector3(0, 1.5f, Mathf.Sign(vel.z) * WallCollisionProximity);
                var forwardZPosHead = _position + new Vector3(0, 2.5f, Mathf.Sign(vel.z) * WallCollisionProximity);
                var cellFeet = level.TryGetExistingCell(LevelTools.WorldToCell(forwardZPosFeet));
                var cellStomach = level.TryGetExistingCell(LevelTools.WorldToCell(forwardZPosStomach));
                var cellHead = level.TryGetExistingCell(LevelTools.WorldToCell(forwardZPosHead));
                if (!cellFeet.IsAir() || !cellStomach.IsAir() || !cellHead.IsAir()) {
                    vel.z = 0;
                }
            }


            _position += vel * Time.deltaTime;

            if (_position.y < -20) {
                _position.y = 20f;
            }

            return (vel, moveDirection);
        }

        private void UpdateCamera() {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            ZoomLevel -= scroll * ZoomSensitivity;
            // You might want to limit the zoom level to some minimum and maximum values
            ZoomLevel = Mathf.Clamp(ZoomLevel, MinZoomLevel, MaxZoomLevel);
            if (Input.GetMouseButton(1)) {
                float mouseDeltaX = Input.GetAxis("Mouse X");
                float mouseDeltaY = Input.GetAxis("Mouse Y");
                _currentAngleX += mouseDeltaX * RotationSensivityX;
                _currentAngleZ = Mathf.Clamp(_currentAngleZ - (mouseDeltaY + scroll * 2) * RotationSensivityZ, -MinZAngle, MaxBaseZAngle + ZoomLevel * MaxRatioZAngle);
            }

            // Here we adjust _originalOffset.z based on ZoomLevel
            _originalOffset = Quaternion.Euler(0, _currentAngleX, 0) * new Vector3(0, CameraHeightOffset + _currentAngleZ * 2, -ZoomLevel - _currentAngleZ * CameraZoomTiltStrength).normalized * ZoomLevel;

            CameraTransform.position = transform.position + _originalOffset;
            CameraTransform.LookAt(transform.position + CameraLookOffset);
        }
    }
}