using System.Collections.Generic;
using Shared;

namespace Server {
    public class UserSessionData {
        public bool IsLogged;
        public ushort ShortId;
        public HashSet<ChunkKey> UploadedChunks = new();
        public PriorityQueue<ChunkKey, uint> UploadQueue = new();

        public UserSessionData(bool isLogged, ushort shortId) {
            IsLogged = isLogged;
            ShortId = shortId;
        }
    }
}