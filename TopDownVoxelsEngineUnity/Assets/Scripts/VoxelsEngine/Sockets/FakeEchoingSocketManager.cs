using System;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using NativeWebSocket;
using UnityEngine;

namespace Shared.Net {
    public class FakeEchoingSocketManager : IWebSocketManager {
        public UniTask Init(string socketServerURL) {
            return UniTask.CompletedTask;
        }

        public UniTask Send(INetworkMessage msg) {
            Debug.Log("Echoing " + msg);
            OnNetworkMessage?.Invoke(msg);
            return UniTask.CompletedTask;
        }

        public UniTask Close() {
            return UniTask.CompletedTask;
        }

        public void DispatchMessages() {
        }

        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }
        public WebSocketState SocketState => WebSocketState.Open;
    }
}