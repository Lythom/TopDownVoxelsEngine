using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private readonly ConcurrentDictionary<ushort, TcpClient> _clients = new();
        public TcpClient? GetClient(ushort shortId) => _clients.TryGetValue(shortId, out var client) ? client : null;

        public void Init(int port) {
            _cts = new CancellationTokenSource();
            if (_shortIdPool.Count == 0) {
                for (int i = ushort.MaxValue; i >= 0; i--) _shortIdPool.Push((ushort) i);
            }

            Logr.Log("Starting Tcp Listener on port " + port, Tags.Server);
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            AcceptClientAsync().Forget();
        }

        private async UniTask AcceptClientAsync() {
            Logr.Log("Ready to accept connections", Tags.Server);
            while (_cts != null && !_cts.Token.IsCancellationRequested) {
                try {
                    var client = await _listener.AcceptTcpClientAsync();
                    HandleClientAsync(client).Forget();
                } catch (Exception e) {
                    Logr.LogException(e);
                }
            }

            Logr.Log("Not listening anymore", Tags.Server);
        }

        private async UniTask HandleClientAsync(TcpClient client) {
            if (_cts == null) throw new ApplicationException("Socket server not initialized");
            var stream = client.GetStream();
            var buffer = new byte[BufferSize];
            int bytesRead = 0;
            int messageLength = 0;

            ushort shortId = _shortIdPool.Pop();
            _clients[shortId] = client;

            OnOpen?.Invoke(shortId);

            Logr.Log("New client ! " + shortId, Tags.Server);

            try {
                while (!_cts.Token.IsCancellationRequested && client.Connected) {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    if ((bytesRead = await stream.ReadAsync(buffer, messageLength, BufferSize - messageLength, cts.Token)) > 0) {
                        messageLength += bytesRead; // update message length

                        if (_cts == null || _cts.Token.IsCancellationRequested || !client.Connected) break;
                        if (!stream.DataAvailable) {
                            // if no more data, process the message
                            var request = MessagePackSerializer.Deserialize<INetworkMessage>(new ReadOnlySequence<byte>(buffer, 0, messageLength));
                            OnNetworkMessage?.Invoke(shortId, request);
                            messageLength = 0; // reset message length for next message
                        }
                    } else {
                        break;
                    }
                }
            } catch (Exception e) {
                if (e is not OperationCanceledException && e is not IOException) Logr.LogException(e);
            }

            Logr.Log("Client disconnected " + shortId, Tags.Server);
            OnClose?.Invoke(shortId);
            _clients.TryRemove(shortId, out _);
            _shortIdPool.Push(shortId);
        }

        public async UniTask Send(ushort target, INetworkMessage msg) {
            Logr.Log("Sent " + msg, Tags.Server);
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
            if (msg is not CharacterMoveGameEvent) Logr.Log("Broadcasted " + msg, Tags.Server);
            var buffer = MessagePackSerializer.Serialize(msg);
            var token = _cts?.Token ?? CancellationToken.None;

            foreach (var (shortId, client) in _clients) {
                if (client.Connected) {
                    var stream = client.GetStream();
                    await stream.WriteAsync(buffer, 0, buffer.Length, token);
                    await stream.FlushAsync(token);
                }
            }
        }
    }
}