using LoneStoneStudio.Tools;
using Shared;
using UnityEngine;
using Vector3 = Shared.Vector3;

namespace VoxelsEngine {
    public class CharacterAgent : ConnectedBehaviour {
        // TODO: sync CharacterAgents and characters (use sync prefab list ?)

        public ushort CharacterId = 0;
        private Character? _character;
        private Vector3 _nextPosition;

        private void Awake() {
            var position = transform.position;
            transform.position = new Vector3(position.x, 10, position.z);
        }

        protected override void OnSetup(GameState state) {
            var playerStateSelector = ReactiveHelpers.CreateSelector(
                state.Characters,
                characters => characters.Dictionary.TryGetValue(CharacterId, out var value) ? value : null,
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

            transform.position = _character.Position += _character.Velocity * Time.deltaTime;
        }

        /// <summary>
        /// In fixed update, update the display
        /// </summary>
        private void FixedUpdate() {
            // optimistic update
            if (_character == null) return;
            // Récupérer la rotation actuelle du GameObject
            UnityEngine.Vector3 currentRotation = transform.eulerAngles;
            currentRotation.y = Character.UncompressAngle(_character.Angle);
            transform.eulerAngles = currentRotation;
            transform.position = _character.Position;
        }
    }
}