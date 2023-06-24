using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class CharacterJoinGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public ushort CharacterShortId;

        [Key(2)]
        public Character Character;

        [Key(3)]
        public Vector3 LevelSpawn;

        public override int GetId() => Id;

        public CharacterJoinGameEvent(int id, ushort characterShortId, Character character, Vector3 levelSpawn) {
            Id = id;
            CharacterShortId = characterShortId;
            Character = character;
            LevelSpawn = levelSpawn;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            var levelId = Character.Level.Value;
            if (levelId != null && !gameState.Levels.ContainsKey(levelId)) {
                gameState.Levels[levelId] = new LevelMap(levelId, LevelSpawn);
            }

            gameState.Characters.Add(CharacterShortId, new Character(Character.Name, Character.Position, levelId));
            gameState.Characters[CharacterShortId].UpdateValue(Character);
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (gameState.Characters.ContainsKey(CharacterShortId)) throw new ApplicationException("Character already known.");
        }

        public override string ToString() {
            return $"CharacterJoin({CharacterShortId}, {Character.Name}, {LevelSpawn})";
        }
    }
}