using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class AckResponse : INetworkMessage {
        [Key(0)]
        public int Id;

        [Key(1)]
        public string? Error;
        
        public bool IsSuccess() => Error == null;

        public AckResponse(int id, string? error) {
            Id = id;
            Error = error;
        }
    }
}