using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public class NPC {
        public Vector3 Position;
        public Vector3 Velocity;
        public byte Angle;
    }
}