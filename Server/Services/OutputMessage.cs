using Shared.Net;

namespace Server {
    public struct OutputMessage {
        public ushort RecipientId;
        public INetworkMessage Message;
        public bool IsBroadcast;

        /// Send to single target
        public OutputMessage(ushort recipientId, INetworkMessage message) {
            RecipientId = recipientId;
            Message = message;
            IsBroadcast = false;
        }
        
        /// Broadcast
        public OutputMessage(INetworkMessage message) {
            RecipientId = 0;
            Message = message;
            IsBroadcast = true;
        }
    }
}