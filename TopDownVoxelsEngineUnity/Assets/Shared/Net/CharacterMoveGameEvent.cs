using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class CharacterMoveGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public byte CharacterId;

        [Key(2)]
        public Vector3 Position;

        [Key(3)]
        public Vector3 Velocity;

        [Key(4)]
        public byte Angle;

        public override int GetId() => Id;

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterId].Position = Position;
            gameState.Characters[CharacterId].Velocity = Velocity;
            gameState.Characters[CharacterId].Angle = Angle;
        }

        public override void AssertApplicationConditions(GameState gameState) {
        }
    }
}