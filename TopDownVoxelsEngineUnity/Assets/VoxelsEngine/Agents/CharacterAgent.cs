using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class CharacterAgent : ConnectedBehaviour {
        // TODO: sync CharacterAgents and characters (use sync prefab list ?)

        [Required]
        public FaceController FaceController = null!;

        [Required]
        public Animator Animator = null!;


        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Altitude = Animator.StringToHash("Altitude");

        public Reactive<ushort> CharacterId = new(0);
        private Character? _character;
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

        protected override void OnSetup(GameState state) {
            var playerStateSelector = ReactiveHelpers.CreateSelector(
                state.Characters,
                CharacterId,
                (characters, shortId) => characters.Dictionary.TryGetValue(shortId, out var value) ? value : null,
                null,
                ResetToken
            );
            Reactive<string?> levelIdSelector = new(null);
            levelIdSelector.BindNestedSelector(playerStateSelector, pss => pss?.Level, ResetToken);

            Subscribe(playerStateSelector, p => _character = p);
            Subscribe(levelIdSelector, state.Selectors.LocalPlayerLevelIdSelector, (levelId, localLevelId) => {
                this.SmartActive(levelId == localLevelId);
            });
        }

        /// <summary>
        /// In update, read the controls.
        /// Currently the client is in charge of calculating the speed, so there is no limitation to speeding or teleporting cheats.
        /// </summary>
        private void Update() {
            if (_character == null) return;
            var levelId = _character.Level.Value;
            if (levelId == null || !ClientEngine.State.Levels.ContainsKey(levelId)) return;
            if (!ClientEngine.State.Levels.TryGetValue(levelId, out var level)) return;

            // update or calculate display position
            var pos = (Vector3) _character.Position;
            var vel = (Vector3) _character.Velocity * Time.deltaTime;
            if (_lastPosition != pos) {
                _calculatedPosition = pos;
            } else {
                _calculatedPosition += vel;
            }

            // interpolate rendering
            transform.position = Vector3.Lerp(transform.position, _calculatedPosition, VisualSnappingStrength * 50 * Time.deltaTime);
            Vector3 currentRotation = transform.eulerAngles;
            currentRotation.y = Mathf.LerpAngle(currentRotation.y, Character.UncompressAngle(_character.Angle), VisualSnappingStrength * 50 * Time.deltaTime);
            transform.eulerAngles = currentRotation;
            UpdateAnimation(vel, _character.IsInAir);
        }
    }
}