using System;

namespace Shared.Net {
    public static class AssertionHelpers {
        public static void AssertChunkReady(GameState gameState, float worldX, float worldZ, string? level) {
            LevelTools.GetChunkPosition(worldX, worldZ, out var chX, out var chZ);
            if (level == null || !gameState.Levels.TryGetValue(level, out var stateLevel)) throw new ApplicationException("Unknown level");
            var chunk = stateLevel.Chunks[chX, chZ];
            if (!chunk.IsGenerated) throw new ApplicationException("Can't set blocks in non ready chunks");
        }
    }
}