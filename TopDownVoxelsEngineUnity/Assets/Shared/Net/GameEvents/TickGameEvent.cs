using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class TickGameEvent : GameEvent {
        public const float DeltaTime = (1f / 50);

        [Key(0)]
        public int Id;

        [Key(1)]
        public PriorityLevel MinPriority;

        public override int GetId() => Id;

        protected internal override void DoApply(GameState state, SideEffectManager? sideEffectManager) {
            if (!state.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            // Generate missing chunks
            // foreach (var (key, c) in state.Characters) {
            //     MoveCharacter(c, state);
            // }

            //state.LevelGenerator.EnqueueChunksAround(c.Level.Value, chx, chz, 3, state.Levels);
            //state.LevelGenerator.GenerateFromQueue(MinPriority, state.Levels);
        }

        private void MoveCharacter(Character character, GameState gameState) {
            var levelId = character.Level.Value;
            if (levelId == null || !gameState.Levels.TryGetValue(levelId, out var level)) return;
            character.Position += character.Velocity;
            var groundPosition = (character.Position + Vector3.down).WorldToCell();
            var groundCell = level.TryGetExistingCell(groundPosition);
            if (!groundCell.HasValue || groundCell.IsAir()) {
                // fall if no ground under
                character.Velocity = new Vector3(character.Velocity.X, character.Velocity.Y - gameState.Gravity * DeltaTime, character.Velocity.Z);
                if (character.Velocity.Y < -0.9f) character.Velocity.Y = -0.9f;
            } else if (character.Velocity.Y < 0) {
                character.Velocity = new Vector3(character.Velocity.X, 0, character.Velocity.Z);
                character.Position = new Vector3(character.Position.X, groundPosition.Y + 1.45f, character.Position.Z);
            }
        }

        public override void AssertApplicationConditions(in GameState state) {
        }
    }
}