using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class TickGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public PriorityLevel MinPriority;

        public override int GetId() => Id;

        protected internal override void DoApply(GameState state, SideEffectManager? sideEffectManager) {
            if (!state.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            // Generate missing chunks
            foreach (var (key, c) in state.Characters) {
                MoveCharacter(c, state);
                var (chx, chz) = LevelTools.GetChunkPosition(c.Position);
                if (c.Level.Value != null && state.Levels.ContainsKey(c.Level.Value)) state.LevelGenerator.EnqueueChunksAround(c.Level.Value, chx, chz, 3, state.Levels);
            }

            state.LevelGenerator.GenerateFromQueue(MinPriority, state.Levels);
        }

        private void MoveCharacter(Character character, GameState gameState) {
            //var levelId = character.Level;
            //var level = gameState.Levels[levelId];
            character.Position += character.Velocity;
        }

        public override void AssertApplicationConditions(GameState state) {
        }
    }
}