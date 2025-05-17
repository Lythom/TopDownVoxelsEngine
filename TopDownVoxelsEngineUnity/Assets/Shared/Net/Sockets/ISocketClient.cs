using System;
using Cysharp.Threading.Tasks;

namespace Shared.Net {
    public interface ISocketClient {
        public UniTask Init(string host);
        public void Send(INetworkMessage msg);
        public void Close();
        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action? OnConnexionLost { get; set; }
    }
}