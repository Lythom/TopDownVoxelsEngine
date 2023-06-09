﻿using LoneStoneStudio.Tools;
using Shared;
using UnityEngine;
using Vector3 = Shared.Vector3;

namespace VoxelsEngine {
    public class CharacterAgent : ConnectedBehaviour {
        // TODO: sync CharacterAgents and characters (use sync prefab list ?)

        public Reactive<ushort> CharacterId = new(0);
        private Character? _character;
        private Vector3 _nextPosition;
        public float VisualSnappingStrength = 0.28f;

        private void Awake() {
            var position = transform.position;
            transform.position = new Vector3(position.x, 10, position.z);
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

            transform.position = UnityEngine.Vector3.Lerp(transform.position, _character.Position, VisualSnappingStrength * 50 * Time.deltaTime);
            UnityEngine.Vector3 currentRotation = transform.eulerAngles;
            currentRotation.y = Mathf.LerpAngle(currentRotation.y, Character.UncompressAngle(_character.Angle), VisualSnappingStrength * 50 * Time.deltaTime);
            transform.eulerAngles = currentRotation;
        }
    }
}