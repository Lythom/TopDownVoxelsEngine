using System.Collections.Generic;
using System.Net.WebSockets;
using Shared;

namespace Server {
    public class UserSessionData {
        public bool IsLogged;
        public ushort ShortId;
        public WebSocket Ws;
        public string Name;
        public HashSet<ChunkKey> UploadedChunks = new();
        public PriorityQueue<ChunkKey, uint> UploadQueue = new();

        public UserSessionData(bool isLogged, ushort shortId, WebSocket ws) {
            IsLogged = isLogged;
            ShortId = shortId;
            Ws = ws;
        }
    }
}