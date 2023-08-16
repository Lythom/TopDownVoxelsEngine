using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class CharacterMoveGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public ushort CharacterShortId;

        [Key(2)]
        public Vector3 Position;

        [Key(3)]
        public Vector3 Velocity;

        [Key(4)]
        public byte Angle;

        [Key(5)]
        public bool IsInAir;

        public override int GetId() => Id;

        public CharacterMoveGameEvent(int id, ushort characterShortId, Vector3 position, Vector3 velocity, byte angle, bool isInAir) {
            Id = id;
            CharacterShortId = characterShortId;
            Position = position;
            Velocity = velocity;
            Angle = angle;
            IsInAir = isInAir;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterShortId].Position = Position;
            gameState.Characters[CharacterShortId].Velocity = Velocity;
            gameState.Characters[CharacterShortId].Angle = Angle;
            gameState.Characters[CharacterShortId].IsInAir = IsInAir;
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterShortId)) throw new ApplicationException("Character must exists");
        }
    }
}