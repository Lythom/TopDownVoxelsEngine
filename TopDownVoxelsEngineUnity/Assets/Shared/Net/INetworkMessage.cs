using MessagePack;

namespace Shared.Net {
    [Union(0, typeof(RegisterPlayerCommand))]
    [Union(1, typeof(ChangeBlockGameEvent))]
    [Union(2, typeof(ChangeToolGameEvent))]
    [Union(3, typeof(CharacterJoinGameEvent))]
    [Union(4, typeof(CharacterLeaveGameEvent))]
    [Union(5, typeof(CharacterMoveGameEvent))]
    [Union(6, typeof(ChunkUpdateGameEvent))]
    [Union(7, typeof(PlaceBlocksGameEvent))]
    [Union(8, typeof(TickGameEvent))]
    [Union(9, typeof(AckResponse))]
    [Union(10, typeof(ErrorNetworkMessage))]
    public interface INetworkMessage {
    }
    
    public interface INetworkQuery<T> where T : INetworkMessage {
    }
}