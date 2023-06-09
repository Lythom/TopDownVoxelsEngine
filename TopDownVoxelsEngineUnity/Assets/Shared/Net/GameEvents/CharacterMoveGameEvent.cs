using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class CharacterMoveGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public short CharacterId;

        [Key(2)]
        public Vector3 Position;

        [Key(3)]
        public Vector3 Velocity;

        [Key(4)]
        public byte Angle;

        public override int GetId() => Id;

        public CharacterMoveGameEvent(int id, short characterId, Vector3 position, Vector3 velocity, byte angle) {
            Id = id;
            CharacterId = characterId;
            Position = position;
            Velocity = velocity;
            Angle = angle;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterId].Position = Position;
            gameState.Characters[CharacterId].Velocity = Velocity;
            gameState.Characters[CharacterId].Angle = Angle;
        }

        public override void AssertApplicationConditions(GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterId)) throw new ApplicationException("Character must exists");
        }
    }
}