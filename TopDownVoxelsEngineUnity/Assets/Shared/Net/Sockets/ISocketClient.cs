using System;
using Cysharp.Threading.Tasks;

namespace Shared.Net {
    public interface ISocketClient {
        public UniTask Init(string host, int port);
        public UniTask Send(INetworkMessage msg);
        public void Close();
        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }
    }
}