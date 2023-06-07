using System;
using Cysharp.Threading.Tasks;
using NativeWebSocket;

namespace Shared.Net {
    public interface IWebSocketManager {
        public UniTask Init(string socketServerURL);
        public UniTask Send(INetworkMessage msg);
        public UniTask Close();
        public void DispatchMessages();
        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }
        public WebSocketState SocketState { get; }
    }
}