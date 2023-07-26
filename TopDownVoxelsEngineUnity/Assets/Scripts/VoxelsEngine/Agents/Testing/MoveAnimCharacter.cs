using System;
using LoneStoneStudio.Tools;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VoxelsEngine.Tools;

namespace VoxelsEngine {
    public class MoveAnimCharacter : MonoBehaviour {

        [ShowInInspector]
        public float DeltaTime => Time.deltaTime;
        public float FixedDeltaTime => Time.deltaTime;
        
        [ShowInInspector]
        public Vector3 Vel => _vel;
        
        [Required]
        public FaceController FaceController = null!;

        public Animator Animator;

        [RequiredInScene]
        public Transform CameraTransform = null!;

        public float VisualSnappingStrength = 0.3f;

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

        // Define zoom sensitivity
        public float ZoomSensitivity = 10f;

        public float CameraZoomTiltStrength = 0.5f;


        public float Speed = 5.0f;

        private Controls _controls = null!;
        private Camera _cam = null!;
        private float _jumpChargeStart;

        public float JumpForce = 0.2f;
        public float JumpChargeIntensity = 1f;
        public float Gravity = 0.4f;

        private readonly Cooldown _jumpCooldown = new(0.5f);
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _originalOffset;
        private float _currentAngleX;
        private float _currentAngleZ;
        private bool _isRotating;
        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Altitude = Animator.StringToHash("Altitude");
        private Vector3 _vel;

        // Start is called before the first frame update
        void Awake() {
            _controls = new Controls();
            _cam = CameraTransform.GetComponent<Camera>();
            _position = transform.position;
            _originalOffset = CameraTransform.position - transform.position;
        }

        protected void OnEnable() {
            _controls.Enable();
            _vel = Vector3.zero;
        }

        public void OnDisable() {
            _controls.Disable();
        }
        void Update() {
            HandleCamera();
            var movement = HandleMove();
            Animator.SetFloat(Velocity, movement.x*movement.x + movement.z*movement.z);
            Animator.SetFloat(Altitude, _position.y);
            FaceController.CurrentFace = movement.magnitude > 0.001f ? FaceController.FACES.Angry : FaceController.FACES.Smile_Blink;
            transform.position = Vector3.Lerp(transform.position, _position, VisualSnappingStrength * 10 * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, VisualSnappingStrength * 10 * Time.deltaTime);
        }

        private Vector3 HandleMove() {
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

            if (_position.y > 0) {
                _vel.y -= Gravity * Time.deltaTime;
            } else {
                _vel.y = 0;
            }
            
            if (_controls.Gameplay.Jump.WasPressedThisFrame() && _jumpCooldown.TryPerform() && _position.y <= 0) {
                _jumpChargeStart = Time.time;
                _vel.y = JumpForce;
            }
            else if (_controls.Gameplay.Jump.IsPressed()) {
                var jumpCharge = (Time.time - _jumpChargeStart) * 2;
                _vel.y += JumpChargeIntensity * Time.deltaTime * (1 - Mathf.Clamp01(jumpCharge));
            }

            _position += moveDirection * (Speed * Time.deltaTime) + new Vector3(0, _vel.y * Time.deltaTime, 0);
           
            if (_position.y <= 0.001f) {
                _position.y = 0;
            }

            return moveDirection;
        }

        private void HandleCamera() {
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

            var offset = Vector3.up * 2.5f;
            CameraTransform.position = transform.position + _originalOffset + offset;
            CameraTransform.LookAt(transform.position + offset);
        }
    }
}