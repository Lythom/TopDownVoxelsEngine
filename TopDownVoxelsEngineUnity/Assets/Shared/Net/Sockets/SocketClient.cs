using System;
using System.Buffers;
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
        public Action<Exception>? OnReconnectionFailed { get; set; }

        public async UniTask Init(string host, int port) {
            _cts = new CancellationTokenSource();

            Logr.Log("Connecting Tcp Client on server and port " + port);
            _client = new TcpClient();
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

            Logr.Log("Connected !");

            StartListening().Forget();
        }

        private async UniTask StartListening() {
            var stream = _client.GetStream();
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            int messageLength = 0;

            // Read the message in chunks
            while (_cts != null
                   && !_cts.Token.IsCancellationRequested
                   && _client.Connected
                   && (bytesRead = await stream.ReadAsync(buffer, messageLength, BufferSize - messageLength, _cts.Token)) > 0) {
                messageLength += bytesRead; // update message length

                if (stream.DataAvailable) continue; // while there is more data, keep reading in the buffer
                var msg = MessagePackSerializer.Deserialize<INetworkMessage>(new ReadOnlySequence<byte>(buffer, 0, messageLength));
                OnNetworkMessage?.Invoke(msg);
                messageLength = 0; // reset message length for next message
            }
        }

        public async UniTask Send(INetworkMessage msg) {
            var stream = _client.GetStream();
            var buffer = MessagePackSerializer.Serialize(msg);
            var token = _cts?.Token ?? CancellationToken.None;
            await stream.WriteAsync(buffer, 0, buffer.Length, token);
            await stream.FlushAsync(token);
            Logr.Log("Sent to server: " + msg);
        }

        public void Close() {
            _cts?.Cancel();
            _client.GetStream().Close();
            _client.Close();
        }
    }
}