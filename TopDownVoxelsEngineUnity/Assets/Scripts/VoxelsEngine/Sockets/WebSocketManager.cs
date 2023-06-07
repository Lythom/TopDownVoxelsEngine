using System;
using Cysharp.Threading.Tasks;
using MessagePack;
using NativeWebSocket;
using UnityEngine;
using VoxelsEngine;

namespace Shared.Net {
    public class WebSocketManager : IWebSocketManager {
        public Action<INetworkMessage>? OnNetworkMessage { get; set; }
        public Action<Exception>? OnReconnectionFailed { get; set; }

        public WebSocketState SocketState => _websocket?.State ?? WebSocketState.Closed;
        private WebSocket? _websocket;

        public async UniTask Init(string socketServerURL) {
            if (_websocket != null) await Close();
            await StartWebsocket(socketServerURL);
        }

        public async UniTask Send(INetworkMessage msg) {
            if (_websocket == null) {
                Debug.Log("_websocket is null");
                return;
            }

            var bytes = MessagePackSerializer.Serialize(msg);
            await _websocket.Send(bytes);
        }

        private async UniTask StartWebsocket(string socketServerURL = "localhost:5001") {
            if (_websocket != null) return;
            var utcs = new UniTaskCompletionSource();
            var openBeforeClose = false;

            var url = $"{socketServerURL}/ws";
            Debug.Log("Connecting to " + url);
            _websocket = new WebSocket(url);

            _websocket.OnOpen += () => {
                Debug.Log("Connection open!");
                openBeforeClose = true;
                utcs.TrySetResult();
            };

            _websocket.OnError += e => {
                Debug.Log("Error! " + e);
                utcs.TrySetException(new ApplicationException(e));
            };

            _websocket.OnClose += async e => {
                Debug.Log("Connection closed! " + e);
                _websocket = null;
                utcs.TrySetException(new ApplicationException("Connection closed: " + e));

                // Try to reconnect
                if (openBeforeClose && e != WebSocketCloseCode.Normal) {
                    try {
                        await StartWebsocket(socketServerURL);
                    } catch (Exception ex) {
                        // could not reconnect
                        _websocket = null;
                        OnReconnectionFailed?.Invoke(ex);
                    }
                }
            };

            _websocket.OnMessage += bytes => {
                try {
                    INetworkMessage message = MessagePackSerializer.Deserialize<INetworkMessage>(bytes, Configurator.MessagePackOptions);
                    Debug.Log($"Received {(message == null ? "null" : message.GetType())} of length {bytes.Length}");
                    if (message != null) OnNetworkMessage?.Invoke(message);
                } catch (MessagePackSerializationException e) {
                    Debug.Log("[OnMessage] server message was not deserialized properly.");
                    Debug.LogException(e);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            };

            // waiting for messages
            _websocket.Connect().AsUniTask().Forget();

            await utcs.Task;
        }

        public async UniTask Close() {
            if (_websocket != null) {
                await _websocket.Close();
                _websocket = null;
            }
        }

        public void DispatchMessages() {
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket?.DispatchMessageQueue();
#endif
        }
    }
}