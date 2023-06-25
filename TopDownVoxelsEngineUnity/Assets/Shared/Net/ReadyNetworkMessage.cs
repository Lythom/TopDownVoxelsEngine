using MessagePack;

namespace Shared.Net {
    /// <summary>
    /// ClientToServer: (User) Tell the server the client is receive realtime updates
    /// </summary>
    [MessagePackObject]
    public class ReadyNetworkMessage : INetworkMessage {
        [Key(0)]
        public ushort ShortId;

        public ReadyNetworkMessage(ushort shortId) {
            ShortId = shortId;
        }
    }
}