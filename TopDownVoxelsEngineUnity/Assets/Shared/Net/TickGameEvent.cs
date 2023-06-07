using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class TickGameEvent : GameEvent {
        public float CharactersSpeed = 5.0f;
        public float CharactersJumpForce = 0.2f;
        public float CharactersJumpChargeIntensity = 1f;
        public int CharactersPlacementRadius = 4;
        public float Gravity = 0.2f;

        [Key(0)]
        public int Id;

        [Key(1)]
        public PriorityLevel MinPriority;

        public override int GetId() => Id;

        protected internal override void DoApply(GameState state, SideEffectManager? sideEffectManager) {
            if (!state.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            // Generate missing chunks
            foreach (var c in state.Characters) {
                var (chx, chz) = LevelTools.GetChunkPosition(c.Position);
                state.LevelGenerator.EnqueueChunksAround(c.Level, chx, chz, 3);
            }

            state.LevelGenerator.GenerateFromQueue(MinPriority, state.Levels);
        }

        public override void AssertApplicationConditions(GameState state) {
        }
    }
}