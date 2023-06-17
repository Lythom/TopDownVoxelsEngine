using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shared.Net;

namespace Server {
    internal static class WebSocketHandler {
        const int Ko = 1024;

        private static ushort _nextShortId = 0;
        private static Stack<ushort> _shortIdPool;

        static WebSocketHandler() {
            _shortIdPool = new Stack<ushort>();
            for (int i = ushort.MaxValue; i >= 0; i--) _shortIdPool.Push((ushort) i);
        }

        public static async Task InitWebSocketAsync(WebSocket webSocket, HttpContext context) {
            // Possible concurrent access to OpenSockets, lock the operation
            Console.WriteLine("New client !");
            try {
                var messageSender = context.RequestServices.GetRequiredService<WebSocketMessagingQueue>();
                messageSender.AddSocket(webSocket);
                await ListenWebsocketBytesAsync(context, webSocket, messageSender);
            } catch (Exception e) {
                Console.WriteLine(e);
            } finally {
                var messageSender = context.RequestServices.GetRequiredService<WebSocketMessagingQueue>();
                messageSender.RemoveSocket(webSocket);
                Console.WriteLine("Client left !");
            }
        }

        private static async UniTask ListenWebsocketBytesAsync(HttpContext context, WebSocket webSocket, WebSocketMessagingQueue messageSender) {
            var server = context.RequestServices.GetRequiredService<VoxelsEngineServer>();

            var shortId = _shortIdPool.Pop();
            server.NotifyConnection(shortId, webSocket);

            var buffer = new byte[1024 * Ko];
            bool socketOpen = true;
            while (socketOpen) {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var bufferOffset = result.Count;

                if (!result.CloseStatus.HasValue && result.MessageType != WebSocketMessageType.Close) {
                    // While the message is not complete, keep listening to fill the buffer
                    while (!result.EndOfMessage) {
                        var freeSpace = buffer.Length - bufferOffset - result.Count;
                        if (freeSpace <= 0) {
                            await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, result.CloseStatusDescription, CancellationToken.None);
                            return;
                        }

                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, bufferOffset, freeSpace), CancellationToken.None);
                        bufferOffset += result.Count;
                    }

                    // buffer completed
                    switch (result.MessageType) {
                        case WebSocketMessageType.Text:
                            await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, result.CloseStatusDescription, CancellationToken.None);
                            return;
                        case WebSocketMessageType.Binary:
                            var bytes = new ArraySegment<byte>(buffer, 0, bufferOffset);
                            var msg = MessagePackSerializer.Deserialize<INetworkMessage>(bytes);
                            await server.HandleMessageAsync(
                                msg,
                                answer => messageSender.Send(webSocket, answer),
                                messageSender.Broadcast,
                                shortId
                            );
                            break;
                    }
                } else {
                    // Socket closed by client or timed out
                    Console.WriteLine("Client disconnected " + webSocket);
                    server.NotifyDisconnection(shortId);
                    socketOpen = false;
                    _shortIdPool.Push(shortId);
                    if (result.CloseStatus != null) await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }
        }
    }
}