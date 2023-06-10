using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public class NPC : IUpdatable<NPC> {
        public Vector3 Position;
        public Vector3 Velocity;
        public byte Angle;
        
        public void UpdateValue(NPC nextState) {
            Position = nextState.Position;
            Velocity = nextState.Velocity;
            Angle = nextState.Angle;
        }
    }
}