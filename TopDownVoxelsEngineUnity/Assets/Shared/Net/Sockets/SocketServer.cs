using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;

namespace Shared.Net {
    public class SocketServer : ISocketManager {
        private CancellationTokenSource? _cts;
        private const int BufferSize = 1_000_000; // ~1Mo

        public Action<ushort, INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<ushort>? OnOpen { get; set; }
        public Action<ushort>? OnClose { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }

        private readonly Stack<ushort> _shortIdPool = new();
        private readonly ConcurrentDictionary<ushort, WebSocket> _clients = new();
        public WebSocket? GetClient(ushort shortId) => _clients.TryGetValue(shortId, out var client) ? client : null;

        public SocketServer() {
            _cts = new CancellationTokenSource();
            if (_shortIdPool.Count == 0) {
                for (int i = ushort.MaxValue; i >= 0; i--) _shortIdPool.Push((ushort) i);
            }
        }

        public async ValueTask HandleClientAsync(WebSocket webSocket) {
            if (_cts == null) throw new ApplicationException("Socket server not initialized");

            ushort shortId = _shortIdPool.Pop();
            _clients[shortId] = webSocket;

            OnOpen?.Invoke(shortId);
            Logr.Log("New WebSocket client! " + shortId, Tags.Server);

            try {
                var buffer = new byte[BufferSize];
                var receiveBuffer = new ArraySegment<byte>(buffer);

                while (!_cts.Token.IsCancellationRequested &&
                       webSocket.State == WebSocketState.Open) {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var result = await webSocket.ReceiveAsync(receiveBuffer, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close) {
                        break;
                    }

                    if (result.Count > 0) {
                        try {
                            var message = MessagePackSerializer.Deserialize<INetworkMessage>(
                                new ReadOnlySequence<byte>(buffer, 0, result.Count));
                            OnNetworkMessage?.Invoke(shortId, message);
                        } catch (Exception e) {
                            Logr.LogException(e, "Cannot deserialize message from client. Ensure client only sends INetworkMessage binary messages.");
                        }
                    }
                }
            } catch (Exception e) {
                if (e is not OperationCanceledException) Logr.LogException(e);
            } finally {
                if (webSocket.State == WebSocketState.Open) {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }

                Logr.Log("Client disconnected " + shortId, Tags.Server);
                OnClose?.Invoke(shortId);
                _clients.TryRemove(shortId, out _);
                _shortIdPool.Push(shortId);
            }
        }

        public virtual ValueTask Send(ushort target, INetworkMessage msg) {
            var client = GetClient(target);
            if (client != null && client.State == WebSocketState.Open) {
                var buffer = MessagePackSerializer.Serialize(msg);
                return client.SendAsync(
                    new ReadOnlyMemory<byte>(buffer),
                    WebSocketMessageType.Binary,
                    true,
                    _cts?.Token ?? CancellationToken.None);
            } else {
                Logr.Log($"!! Failed to send {msg} to {target} because disconnected", Tags.Debug);
                return new ValueTask(Task.CompletedTask);
            }
        }

        public void Close() {
            _cts?.Cancel();
        }

        public virtual async ValueTask Broadcast(INetworkMessage msg) {
            var buffer = MessagePackSerializer.Serialize(msg);
            var segment = new ReadOnlyMemory<byte>(buffer);
            var token = _cts?.Token ?? CancellationToken.None;

            foreach (var (shortId, client) in _clients) {
                if (client.State == WebSocketState.Open) {
                    await client.SendAsync(
                        segment,
                        WebSocketMessageType.Binary,
                        true,
                        token);
                }
            }
        }

        private byte[] GetBuffer() => ArrayPool<byte>.Shared.Rent(BufferSize);
        private void ReturnBuffer(byte[] buffer) => ArrayPool<byte>.Shared.Return(buffer);
    }
}