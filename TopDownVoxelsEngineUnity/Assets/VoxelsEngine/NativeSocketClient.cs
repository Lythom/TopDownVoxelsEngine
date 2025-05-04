using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;
using NativeWebSocket;

namespace Shared.Net {
    public class NativeSocketClient : ISocketClient {
        private CancellationTokenSource? _cts;
        private WebSocket? _socket;
        private readonly ConcurrentQueue<INetworkMessage> _outbox = new();

        private readonly ConcurrentQueue<INetworkMessage> _inbox = new();

        public async UniTask Init(string host, int port) {
            _cts = new CancellationTokenSource();

            var url = "ws://" + host + ":" + port + "/ws";
            Logr.Log("URL=" + url);
            _socket = new WebSocket(url);
            _socket.OnOpen += HandleOpen;
            _socket.OnError += HandleError;
            _socket.OnClose += HandleClose;
            _socket.OnMessage += HandleMessage;

            await _socket.Connect();
            await UniTask.WaitUntil(() => _socket.State == WebSocketState.Open);

            Thread outThread = new Thread(StartOutboxSendingAsync);
            outThread.Start();

            StartInboxAsync().Forget();
        }

        private void HandleMessage(byte[] data) {
            var msg = MessagePackSerializer.Deserialize<INetworkMessage>(data);
            _inbox.Enqueue(msg);
        }

        private async UniTask StartInboxAsync() {
            while (_cts != null
                   && !_cts.Token.IsCancellationRequested
                   && _socket!.State is WebSocketState.Connecting or WebSocketState.Open) {
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

        private void HandleClose(WebSocketCloseCode closeCode) {
            Logr.Log("Websocket Connection closed");
        }

        private void HandleError(string errorMsg) {
            Logr.LogError(errorMsg);
        }

        private void HandleOpen() {
            Logr.Log("Websocket Connection opened");
        }

        private async void StartOutboxSendingAsync() {
            while (_cts != null
                   && !_cts.Token.IsCancellationRequested
                   && _socket!.State is WebSocketState.Open) {
                bool hasMessage = false;
                hasMessage = _outbox.TryDequeue(out var m);
                try {
                    if (hasMessage) {
                        await _socket.Send(MessagePackSerializer.Serialize(m));
                    } else {
                        await UniTask.Yield();
                    }
                } catch (Exception) {
                    if (hasMessage) Logr.LogError($"A message {m.GetType().Name} was not sent to server.");
                }
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

        public void Close() {
            if (_socket is not null && _socket.State is WebSocketState.Open or WebSocketState.Connecting) {
                _socket.Close();
            }
        }

        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action? OnConnexionLost { get; set; }
    }
}