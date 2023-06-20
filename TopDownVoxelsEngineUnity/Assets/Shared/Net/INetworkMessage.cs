using System;
using MessagePack;

namespace Shared.Net {
    [Union(0, typeof(HelloNetworkMessage))]
    [Union(1, typeof(ChangeBlockGameEvent))]
    [Union(2, typeof(ChangeToolGameEvent))]
    [Union(3, typeof(CharacterJoinGameEvent))]
    [Union(4, typeof(CharacterLeaveGameEvent))]
    [Union(5, typeof(CharacterMoveGameEvent))]
    [Union(6, typeof(ChunkUpdateGameEvent))]
    [Union(7, typeof(PlaceBlocksGameEvent))]
    [Union(8, typeof(TickGameEvent))]
    [Union(9, typeof(AckNetworkMessage))]
    [Union(10, typeof(ErrorNetworkMessage))]
    public interface INetworkMessage {
    }

    [MessagePackObject]
    public class AckNetworkMessage : INetworkMessage {
        [Key(0)]
        public int Id;

        public AckNetworkMessage(int id) {
            Id = id;
        }
    }

    public class GameEventApplicationException : ApplicationException {
        public GameEventApplicationException(string message) : base(message) {
        }
    }
}