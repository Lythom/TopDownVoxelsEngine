namespace Shared.SideEffects {
    public class ChunkDirtySEffect {
        public int PlayerId;
        public int ChX;
        public int ChZ;

        public ChunkDirtySEffect(int playerId, int chX, int chZ) {
            PlayerId = playerId;
            ChX = chX;
            ChZ = chZ;
        }
    }
}