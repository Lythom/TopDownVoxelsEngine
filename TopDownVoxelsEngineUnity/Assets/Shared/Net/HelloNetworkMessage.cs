using MessagePack;

namespace Shared.Net {
    /// <summary>
    /// ClientToServer: (User) Tell the server the client wants to play
    /// </summary>
    [MessagePackObject]
    public class HelloNetworkMessage : INetworkMessage {
        [Key(0)]
        public string Username;

        public HelloNetworkMessage() {
            Username = string.Empty;
        }

        public HelloNetworkMessage(string username) {
            Username = username;
        }
    }
}