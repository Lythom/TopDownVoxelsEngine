using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;

namespace Shared.Net {
    public class SocketClient : ISocketClient {
        private CancellationTokenSource? _cts;
        private TcpClient _client = null!;
        private const int BufferSize = 1_000_000; // ~1Mo
        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action? OnConnexionLost { get; set; }
        private readonly ConcurrentQueue<INetworkMessage> _outbox = new();
        private readonly ConcurrentQueue<INetworkMessage> _inbox = new();

        public async UniTask Init(string host, int port) {
            _cts = new CancellationTokenSource();

            Logr.Log("Connecting Tcp Client on server and port " + port, Tags.Client);
            _client = new TcpClient();
            _client.NoDelay = true;
            var attempts = 5;
            while (!_client.Connected && attempts >= 0) {
                try {
                    await _client.ConnectAsync(host, port);
                } catch (Exception e) {
                    Logr.LogException(e, "Error while connection to the server");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken: _cts.Token);
                    attempts--;
                }
            }

            if (attempts < 0) throw new ApplicationException("Not connected");

            Logr.Log("Connected !", Tags.Client);

            Thread inThread = new Thread(StartListening);
            Thread outThread = new Thread(StartOutboxSendingAsync);
            inThread.Start();
            outThread.Start();

            StartInboxAsync().Forget();
        }

        private async void StartOutboxSendingAsync() {
            while (_cts != null
                   && !_cts.Token.IsCancellationRequested
                   && _client.Connected) {
                bool hasMessage = false;
                hasMessage = _outbox.TryDequeue(out var m);
                try {
                    if (hasMessage) {
                        await DoSend(m);
                        await _client.GetStream().FlushAsync(_cts.Token);
                    } else {
                        await UniTask.Yield();
                    }
                } catch (Exception) {
                    if (hasMessage) Logr.LogError($"A message {m.GetType().Name} was not sent to server.");
                }
            }
        }

        private async UniTask StartInboxAsync() {
            while (_cts != null
                   && !_cts.Token.IsCancellationRequested
                   && _client.Connected) {
                bool hasMessage = false;
                hasMessage = _inbox.TryDequeue(out var m);
                try {
                    if (hasMessage) {
                        OnNetworkMessage?.Invoke(m);
                    } else {
                        await UniTask.Yield();
                    }
                } catch (Exception) {
                    if (hasMessage) Logr.LogError($"A message {m.GetType().Name} was not sent to server.");
                }
            }
        }

        private async void StartListening() {
            var sw = new Stopwatch();
            var stream = _client.GetStream();
            byte[] lengthBuffer = new byte[4];
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            int readBytesFromMessage = 0;

            // Read the message in chunks
            try {
                while (_cts != null
                       && !_cts.Token.IsCancellationRequested
                       && _client.Connected) {
                    sw.Restart();
                    bytesRead = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length, _cts.Token);
                    // Logr.Log("Elapsed = " + sw.ElapsedMilliseconds);
                    if (bytesRead != lengthBuffer.Length) throw new Exception("Failed to read message length");

                    var messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    readBytesFromMessage = 0;
                    while (readBytesFromMessage < messageLength) {
                        if (_cts.IsCancellationRequested) break;
                        bytesRead = await stream.ReadAsync(buffer, readBytesFromMessage, messageLength - readBytesFromMessage, _cts.Token);
                        readBytesFromMessage += bytesRead;
                    }

                    if (_cts.IsCancellationRequested) break;

                    var msg = MessagePackSerializer.Deserialize<INetworkMessage>(new ReadOnlySequence<byte>(buffer, 0, messageLength));
                    _inbox.Enqueue(msg);
                }
            } catch (Exception e) {
                Logr.LogException(e);
                throw;
            } finally {
                OnConnexionLost?.Invoke();
                if (_client.Connected) _client.GetStream().Close();
                if (_client.Connected) _client.Close();
            }
        }

        Queue<INetworkMessage> _workQueue = new();

        public void Send(INetworkMessage msg) {
            // filter previous CharacterMoveGameEvent (now obsolete)
            if (msg is CharacterMoveGameEvent) {
                while (_outbox.TryDequeue(out var m)) {
                    if (m is not CharacterMoveGameEvent) _workQueue.Enqueue(m);
                }

                while (_workQueue.TryDequeue(out var m)) {
                    _outbox.Enqueue(m);
                }
            }

            _outbox.Enqueue(msg);
        }

        private async UniTask DoSend(INetworkMessage msg) {
            if (_cts == null || _cts.IsCancellationRequested) return;
            var stream = _client.GetStream();
            var token = _cts?.Token ?? CancellationToken.None;
            var sw = new Stopwatch();

            int bufferLength;
            try {
                if (!stream.CanWrite) return;
                var buffer = MessagePackSerializer.Serialize(msg);
                bufferLength = buffer.Length;
                var lengthBuffer = BitConverter.GetBytes(bufferLength);
                await stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length, token);
                await stream.WriteAsync(buffer, 0, bufferLength, token);
                if (msg is not CharacterMoveGameEvent) Logr.Log($"Sent to server ({bufferLength}): {msg}", Tags.Client);
                if (_cts == null || _cts.IsCancellationRequested) return;
            } catch (Exception e) {
                Logr.LogException(e);
                throw;
            }
        }

        public void Close() {
            _cts?.Cancel(false);
            if (_client.Connected) _client.Close();
        }
    }
}