using LoneStoneStudio.Tools;

namespace Shared.SideEffects {
    public class ChunkDirtySEffect {
        public string Level;
        public int ChX;
        public int ChZ;

        public ChunkDirtySEffect(string level, int chX, int chZ) {
            ChX = chX;
            ChZ = chZ;
            Level = level;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sideEffectManager"></param>
        /// <param name="chX">Chunk X</param>
        /// <param name="chZ">Chunk Z</param>
        /// <param name="cx">Cell X position in chunk [0;Chunk.Size[</param>
        /// <param name="cz">Cell Z position in chunk [0;Chunk.Size[</param>
        public static void Trigger(SideEffectManager? sideEffectManager, string levelId, int chX, int chZ, uint cx, uint cz) {
            sideEffectManager?.For<ChunkDirtySEffect>().Trigger(new(levelId, chX, chZ));
            if (cx == 0) {
                sideEffectManager?.For<ChunkDirtySEffect>().Trigger(new(levelId, chX - 1, chZ));
            } else if (cx == Chunk.Size - 1) {
                sideEffectManager?.For<ChunkDirtySEffect>().Trigger(new(levelId, chX + 1, chZ));
            }

            if (cz == 0) {
                sideEffectManager?.For<ChunkDirtySEffect>().Trigger(new(levelId, chX, chZ - 1));
            } else if (cz == Chunk.Size - 1) {
                sideEffectManager?.For<ChunkDirtySEffect>().Trigger(new(levelId, chX, chZ + 1));
            }
        }
    }
}