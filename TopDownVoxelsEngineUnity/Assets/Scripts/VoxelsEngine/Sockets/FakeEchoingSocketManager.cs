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

        public async UniTask Send(INetworkMessage msg) {
            await UniTask.DelayFrame(1);
            Debug.Log("Echoing " + msg);
            OnNetworkMessage?.Invoke(msg);
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