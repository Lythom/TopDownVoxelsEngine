using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class ChunkUpdateGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public byte CharacterId;

        [Key(2)]
        public short BlockInChunk;

        [Key(3)]
        public short ChunkPosition;

        [Key(4)]
        public byte Angle;

        public override int GetId() => Id;

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterId].Angle = Angle;
        }

        public override void AssertApplicationConditions(in GameState gameState) {
        }
    }
}