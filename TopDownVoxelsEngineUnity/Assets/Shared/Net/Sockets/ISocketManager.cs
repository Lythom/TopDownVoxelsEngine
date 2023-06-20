using System;
using Cysharp.Threading.Tasks;

namespace Shared.Net {
    public interface ISocketManager {
        public void Init(int serverPort);
        public UniTask Send(ushort target, INetworkMessage msg);
        public UniTask Broadcast(INetworkMessage msg);
        public void Close();
        public Action<ushort>? OnOpen { get; set; }
        public Action<ushort>? OnClose { get; set; }
        public Action<ushort, INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }
    }
}