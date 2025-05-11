using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Shared.Net {
    public interface ISocketManager {
        public ValueTask Send(ushort target, INetworkMessage msg);
        public ValueTask Broadcast(INetworkMessage msg);
        public void Close();
        public Action<ushort>? OnOpen { get; set; }
        public Action<ushort>? OnClose { get; set; }
        public Action<ushort, INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }
        ValueTask HandleClientAsync(WebSocket webSocket);
    }
}