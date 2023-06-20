using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class CharacterJoinGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public ushort CharacterId;

        [Key(2)]
        public Character Character;

        [Key(3)]
        public Vector3 LevelSpawn;

        public override int GetId() => Id;

        public CharacterJoinGameEvent(int id, ushort characterId, Character character) {
            Id = id;
            CharacterId = characterId;
            Character = character;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            var levelId = Character.Level.Value;
            if (levelId != null && !gameState.Levels.ContainsKey(levelId)) {
                gameState.Levels[levelId] = new LevelMap(levelId, LevelSpawn);
            }
            gameState.Characters.Add(CharacterId, new Character(Character.Name, Character.Position, levelId));
            gameState.Characters[CharacterId].UpdateValue(Character);
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (gameState.Characters.ContainsKey(CharacterId)) throw new ApplicationException("Character already known.");
        }
    }
}