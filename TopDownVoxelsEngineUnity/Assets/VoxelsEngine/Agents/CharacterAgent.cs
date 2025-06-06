using LoneStoneStudio.Tools;
using Shared;
using Shared.Signals;
using Sirenix.OdinInspector;
using TinkState;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class CharacterAgent : ConnectedBehaviour, ICharacterSpeed {
        // TODO: sync CharacterAgents and characters (use sync prefab list ?)

        [Required]
        public FaceController FaceController = null!;

        [Required]
        public Animator Animator = null!;


        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Altitude = Animator.StringToHash("Altitude");

        public Signal<ushort> CharacterId = new(0);
        private Observable<Character?>? _character;
        private Vector3 _lastPosition;
        private Vector3 _calculatedPosition;
        public float VisualSnappingStrength = 0.28f;

        private void Awake() {
            var position = transform.position;
            transform.position = new Vector3(position.x, 10, position.z);
        }

        private void UpdateAnimation(Vector3 movement, bool isInAir) {
            Animator.SetFloat(Velocity, movement.x * movement.x + movement.z * movement.z);
            Animator.SetFloat(Altitude, isInAir ? 1 : -0.01f);
            FaceController.CurrentFace = movement.magnitude > 0.001f ? FaceController.Faces.Angry : FaceController.Faces.SmileBlink;
        }

        protected override void OnSetup(GameState state, Selectors clientEngineSelectors) {
            _character = Observable.Auto(() => state.Characters.TryGetValue(CharacterId.Value, out var value) ? value : null);
            Observable.AutoRun(() => {
                this.SmartActive(_character.Value?.Level.Value != null && Selectors.CurrentLevel.Value == _character.Value?.Level.Value);
            });
        }

        /// <summary>
        /// In the update, read the controls.
        /// Currently, the client is in charge of calculating the speed, so there is no limitation to speeding or teleporting cheats.
        /// </summary>
        private void Update() {
            if (_character?.Value is null) return;
            var levelId = _character.Value.Level.Value;
            if (levelId == null || !ClientEngine.State.Levels.ContainsKey(levelId)) return;
            if (!ClientEngine.State.Levels.TryGetValue(levelId, out var level)) return;

            // update or calculate display position
            var pos = (Vector3) _character.Value.Position;
            var vel = (Vector3) _character.Value.Velocity * Time.deltaTime;
            if (_lastPosition != pos) {
                _calculatedPosition = pos;
            } else {
                _calculatedPosition += vel;
            }

            // interpolate rendering
            transform.position = Vector3.Lerp(transform.position, _calculatedPosition, VisualSnappingStrength * 50 * Time.deltaTime);
            Vector3 currentRotation = transform.eulerAngles;
            currentRotation.y = Mathf.LerpAngle(currentRotation.y, Character.UncompressAngle(_character.Value.Angle), VisualSnappingStrength * 50 * Time.deltaTime);
            transform.eulerAngles = currentRotation;
            UpdateAnimation(vel, _character.Value.IsInAir);
        }

        public float CurrentSpeed => _character?.Value == null ? 0 : ((Vector3) _character.Value.Velocity).magnitude / 5f;
    }
}