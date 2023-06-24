using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;

namespace Shared.Net {
    public class SocketServer : ISocketManager {
        private CancellationTokenSource? _cts;
        private TcpListener _listener = null!;
        private const int BufferSize = 1_000_000; // ~1Mo

        public Action<ushort, INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<ushort>? OnOpen { get; set; }
        public Action<ushort>? OnClose { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }

        private readonly Stack<ushort> _shortIdPool = new();
        private readonly Dictionary<ushort, TcpClient> _clients = new();
        public TcpClient? GetClient(ushort shortId) => _clients.ContainsKey(shortId) ? _clients[shortId] : null;

        public void Init(int port) {
            _cts = new CancellationTokenSource();
            if (_shortIdPool.Count == 0) {
                for (int i = ushort.MaxValue; i >= 0; i--) _shortIdPool.Push((ushort) i);
            }

            Logr.Log("Starting Tcp Listener on port " + port);
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            AcceptClient().Forget();
        }

        private async UniTask AcceptClient() {
            Logr.Log("Ready to accept connections");
            while (_cts != null && !_cts.Token.IsCancellationRequested) {
                try {
                    var client = await _listener.AcceptTcpClientAsync();
                    HandleClient(client).Forget();
                } catch (Exception e) {
                    Logr.LogException(e);
                }
            }
        }

        private async UniTask HandleClient(TcpClient client) {
            if (_cts == null) throw new ApplicationException("Socket server not initialized");
            var stream = client.GetStream();
            var buffer = new byte[BufferSize];
            int bytesRead = 0;
            int messageLength = 0;

            ushort shortId = _shortIdPool.Pop();
            _clients[shortId] = client;
            OnOpen?.Invoke(shortId);

            Logr.Log("New client ! " + shortId);

            try {
                while ((bytesRead = await stream.ReadAsync(buffer, messageLength, BufferSize - messageLength, _cts.Token)) > 0) {
                    messageLength += bytesRead; // update message length

                    if (_cts == null || _cts.Token.IsCancellationRequested || !client.Connected) break;
                    if (!stream.DataAvailable) {
                        // if no more data, process the message
                        var request = MessagePackSerializer.Deserialize<INetworkMessage>(new ReadOnlySequence<byte>(buffer, 0, messageLength));
                        OnNetworkMessage?.Invoke(shortId, request);
                        messageLength = 0; // reset message length for next message
                    }
                }
            } catch (Exception e) {
                Logr.LogException(e);
            }

            _clients.Remove(shortId);
            OnClose?.Invoke(shortId);
            _shortIdPool.Push(shortId);
        }

        public async UniTask Send(ushort target, INetworkMessage msg) {
            Logr.Log("Sent " + msg);
            var client = GetClient(target);
            if (client != null && client.Connected) {
                var token = _cts?.Token ?? CancellationToken.None;
                var buffer = MessagePackSerializer.Serialize(msg);

                var stream = client.GetStream();
                await stream.WriteAsync(buffer, 0, buffer.Length, token);
                await stream.FlushAsync(token);
            }
        }

        public void Close() {
            _cts?.Cancel();
        }

        public virtual async UniTask Broadcast(INetworkMessage msg) {
            Logr.Log("Broadcasted " + msg);
            var buffer = MessagePackSerializer.Serialize(msg);
            var token = _cts?.Token ?? CancellationToken.None;

            foreach (var (_, client) in _clients) {
                if (client.Connected) {
                    var stream = client.GetStream();
                    await stream.WriteAsync(buffer, 0, buffer.Length, token);
                    await stream.FlushAsync(token);
                }
            }
        }
    }
}