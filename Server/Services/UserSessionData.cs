using System.Collections.Generic;
using System.Net.WebSockets;
using Shared;

namespace Server {
    public class UserSessionData {
        public bool IsLogged => Name != null;
        public SessionStatus Status = SessionStatus.NeedAuthentication;
        public ushort ShortId;
        public HashSet<ChunkKey> UploadedChunks = new();
        public PriorityQueue<ChunkKey, uint> UploadQueue = new();
        public string? Name;

        public UserSessionData(ushort shortId) {
            ShortId = shortId;
        }
    }
}