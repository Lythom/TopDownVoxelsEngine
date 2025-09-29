using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class ServerErrorGameEvent : INetworkMessage {
        [Key(0)]
        public readonly uint ErrorId;

        [Key(1)]
        public readonly string Text;

        public ServerErrorGameEvent(uint errorId, string text) {
            ErrorId = errorId;
            Text = text;
        }

    }
}