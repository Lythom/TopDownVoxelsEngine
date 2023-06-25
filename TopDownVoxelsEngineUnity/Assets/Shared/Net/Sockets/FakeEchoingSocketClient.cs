using System;
using Cysharp.Threading.Tasks;

namespace Shared.Net {
    public class FakeEchoingSocketClient : ISocketClient {
        public UniTask Init(string host, int port) => UniTask.CompletedTask;

        public UniTask Send(INetworkMessage msg) {
            Logr.Log("Echoing " + msg, Tags.Standalone);
            OnNetworkMessage?.Invoke(msg);
            return UniTask.CompletedTask;
        }

        public void Close() {
        }

        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }
    }
}