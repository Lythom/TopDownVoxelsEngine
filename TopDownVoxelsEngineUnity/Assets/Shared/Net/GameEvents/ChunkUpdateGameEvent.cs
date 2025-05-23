using System;
using LoneStoneStudio.Tools;
using MessagePack;
using Shared.SideEffects;

namespace Shared.Net {
    [MessagePackObject]
    public class ChunkUpdateGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public string LevelId;

        [Key(2)]
        public Chunk Chunk;

        [Key(3)]
        public ushort ChunkPosition;

        public override int GetId() => Id;

        public ChunkUpdateGameEvent(int id, string levelId, Chunk chunk, ushort chunkPosition) {
            Id = id;
            LevelId = levelId;
            Chunk = chunk;
            ChunkPosition = chunkPosition;
        }

        public override string ToString() {
            return $"ChunkUpdateGameEvent({LevelId},{ChunkPosition})";
        }

        public ChunkUpdateGameEvent(int id, string levelId, Chunk chunk, int chX, int chZ)
            : this(id, levelId, chunk, Chunk.GetFlatIndex(chX, chZ)) {
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            var (chX, chZ) = Chunk.GetCoordsFromIndex(ChunkPosition);
            gameState.Levels[LevelId].Chunks[chX, chZ] = Chunk;
            sideEffectManager?.For<ChunkUpdateGameEvent>().Trigger(this);
            sideEffectManager?.For<ChunkDirtySEffect>().Trigger(new(LevelId, chX, chZ));
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (!gameState.Levels.ContainsKey(LevelId)) throw new ApplicationException("Level must exists before being able to update");
        }
    }
}