using MessagePack;

namespace Shared.Net {
    /// <summary>
    /// ClientToServer: (User) Tell the server the client wants to play
    /// </summary>
    [MessagePackObject]
    public class RegisterPlayerCommand : INetworkMessage {
        [Key(0)]
        public string Username;

        public RegisterPlayerCommand() {
            Username = string.Empty;
        }

        public RegisterPlayerCommand(string username) {
            Username = username;
        }
    }
}