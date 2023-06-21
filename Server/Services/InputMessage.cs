using Shared.Net;

namespace Server {
    public struct InputMessage {
        public ushort Id;
        public INetworkMessage Message;

        public InputMessage(ushort id, INetworkMessage message) {
            Id = id;
            Message = message;
        }
    }
}