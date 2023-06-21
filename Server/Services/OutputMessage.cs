using Shared.Net;

namespace Server {
    public struct OutputMessage {
        public ushort Id;
        public INetworkMessage Message;
        public bool IsBroadcast;

        /// Send to single target
        public OutputMessage(ushort id, INetworkMessage message) {
            Id = id;
            Message = message;
            IsBroadcast = false;
        }
        
        /// Broadcast
        public OutputMessage(INetworkMessage message) {
            Id = 0;
            Message = message;
            IsBroadcast = true;
        }
    }
}