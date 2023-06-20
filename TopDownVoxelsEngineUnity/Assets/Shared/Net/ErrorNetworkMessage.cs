using MessagePack;

namespace Shared.Net {
    /// <summary>
    /// ClientToServer: (User) Tell the server the client is ready to play.
    /// </summary>
    [MessagePackObject]
    public class ErrorNetworkMessage : INetworkMessage {
        [Key(0)]
        public string Message;

        public ErrorNetworkMessage() {
            Message = string.Empty;
        }

        public ErrorNetworkMessage(string message) {
            Message = message;
        }
    }
}