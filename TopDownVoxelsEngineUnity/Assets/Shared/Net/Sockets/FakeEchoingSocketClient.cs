using System;
using Cysharp.Threading.Tasks;

namespace Shared.Net {
    public class FakeEchoingSocketClient : ISocketClient {
        public UniTask Init(string host, int port) => UniTask.CompletedTask;

        public void Send(INetworkMessage msg) {
            Logr.Log("Echoing " + msg, Tags.Standalone);
            OnNetworkMessage?.Invoke(msg);
        }

        public void Close() {
        }

        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action? OnConnexionLost { get; set; }
    }
}