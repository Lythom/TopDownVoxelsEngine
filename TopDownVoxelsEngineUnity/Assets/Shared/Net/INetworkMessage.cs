using System;
using MessagePack;

namespace Shared.Net {
    [Union(0, typeof(NewGameNetworkMessage))]
    [Union(1, typeof(HelloNetworkMessage))]
    [Union(2, typeof(ChangeToolGameEvent))]
    [Union(3, typeof(CharacterMoveGameEvent))]
    [Union(4, typeof(ChunkUpdateGameEvent))]
    [Union(5, typeof(PlaceBlocksGameEvent))]
    [Union(6, typeof(TickGameEvent))]
    public interface INetworkMessage {
    }

    /// <summary>
    /// ClientToServer: (Admin) Tell the server to reset the state of the game with the provided state.
    /// ServerToClient: Tell the client to reset the state of the game with the provided state.
    /// </summary>
    [MessagePackObject]
    public class NewGameNetworkMessage : INetworkMessage {
        [Key(0)]
        public GameState? GameState;

        public NewGameNetworkMessage(GameState? gameState) {
            GameState = gameState;
        }
    }

    /// <summary>
    /// ClientToServer: (User) Tell the server the client is ready to play.
    /// </summary>
    [MessagePackObject]
    public class HelloNetworkMessage : INetworkMessage {
        [Key(0)]
        public string Hey;

        public HelloNetworkMessage() {
            Hey = string.Empty;
        }

        public HelloNetworkMessage(string hey) {
            Hey = hey;
        }
    }

    public class GameEventApplicationException : ApplicationException {
        public GameEventApplicationException(string message) : base(message) {
        }
    }
}