using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;
using Shared.Net;

namespace Server {
    /// <summary>
    /// Thread Safe way to manage sockets and message sending
    /// </summary>
    public class WebSocketMessagingQueue : IDisposable {
        private readonly List<WebSocket> _openSockets = new();
        private Channel<IMessage>? _outputQueue;
        private CancellationTokenSource? _cts;

        private Queue<Broadcast?> broadcastPool = new();
        private Queue<Send> sendPool = new();

        public int OpenSocketsCount {
            get {
                lock (_openSockets) return _openSockets.Count;
            }
        }

        public WebSocketMessagingQueue() {
            Console.WriteLine("WebSocketMessagingQueue() " + Random.Shared.Next(0, 1000));
        }

        public void AddSocket(WebSocket s) {
            lock (_openSockets) {
                _openSockets.Add(s);
            }
        }

        public void RemoveSocket(WebSocket s) {
            lock (_openSockets) {
                _openSockets.Remove(s);
            }
        }

        public virtual bool Broadcast(INetworkMessage msg) {
            if (_outputQueue == null) return false;
            if (broadcastPool.TryDequeue(out var b)) {
                b!.Msg = msg;
            } else {
                b = new Broadcast(msg);
            }

            return _outputQueue.Writer.TryWrite(b);
        }

        public virtual bool Send(WebSocket recipient, INetworkMessage msg) {
            if (_outputQueue == null) return false;
            if (sendPool.TryDequeue(out var s)) {
                s.Recipient = recipient;
                s.Msg = msg;
            } else {
                s = new Send(recipient, msg);
            }

            return _outputQueue.Writer.TryWrite(new Send(recipient, msg));
        }

        public void Start() {
            _cts = new CancellationTokenSource();
            _outputQueue = Channel.CreateSingleConsumerUnbounded<IMessage>();
            ExecuteAsync(_cts.Token).Forget();
        }

        private async UniTask ExecuteAsync(CancellationToken stoppingToken) {
            if (_outputQueue == null) return;
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    IMessage queuedMessage = await _outputQueue.Reader.ReadAsync(stoppingToken);
                    switch (queuedMessage) {
                        case Broadcast b:

                            List<WebSocket>? sockets = null;
                            lock (_openSockets) {
                                sockets = _openSockets.ToList();
                            }

                            foreach (var socket in sockets) {
                                await SendAsync(stoppingToken, socket, b.Msg);
                            }

                            broadcastPool.Enqueue(b);
                            break;
                        case Send s:
                            await SendAsync(stoppingToken, s.Recipient, s.Msg);
                            sendPool.Enqueue(s);
                            break;
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }

        public void Dispose() {
            _outputQueue?.Writer.TryComplete();
            _cts?.Cancel(false);
        }

        private async Task SendAsync(CancellationToken stoppingToken, WebSocket ws, INetworkMessage msg) {
            if (ws.State != WebSocketState.Open) {
                lock (_openSockets) {
                    _openSockets.Remove(ws);
                }
            }

            await ws.SendAsync(MessagePackSerializer.Serialize(msg), WebSocketMessageType.Binary, true, stoppingToken);
        }
    }

    internal interface IMessage {
    }

    internal class Broadcast : IMessage {
        public INetworkMessage Msg;

        public Broadcast(INetworkMessage msg) {
            Msg = msg;
        }
    }

    internal class Send : IMessage {
        public WebSocket Recipient;
        public INetworkMessage Msg;

        public Send(WebSocket recipient, INetworkMessage msg) {
            Recipient = recipient;
            Msg = msg;
        }
    }
}