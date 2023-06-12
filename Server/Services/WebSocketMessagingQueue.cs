using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Shared.Net;

namespace Server {
    /// <summary>
    /// Thread Safe way to manage sockets and message sending
    /// </summary>
    public class WebSocketMessagingQueue : BackgroundService {
        private readonly List<WebSocket> _openSockets = new();
        private Channel<IMessage>? _outputQueue;

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

        public bool Broadcast(INetworkMessage msg) {
            if (_outputQueue == null) return false;
            return _outputQueue.Writer.TryWrite(new Broadcast(msg));
        }

        public bool Send(WebSocket recipient, INetworkMessage msg) {
            if (_outputQueue == null) return false;
            return _outputQueue.Writer.TryWrite(new Send(recipient, msg));
        }

        public override Task StartAsync(CancellationToken cancellationToken) {
            _outputQueue = Channel.CreateSingleConsumerUnbounded<IMessage>();
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
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

                            break;
                        case Send s:
                            await SendAsync(stoppingToken, s.Recipient, s.Msg);
                            break;
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }

        public override void Dispose() {
            base.Dispose();
            _outputQueue?.Writer.TryComplete();
        }

        private async Task SendAsync(CancellationToken stoppingToken, WebSocket ws, INetworkMessage msg) {
            if (ws.State != WebSocketState.Open) {
                await Console.Error.WriteLineAsync("[WebSocketSender] Message not sent: socket must be open. Closing the socket.");
                lock (_openSockets) {
                    _openSockets.Remove(ws);
                }

                await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "A message was queued but the socket is not longer open", stoppingToken);
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