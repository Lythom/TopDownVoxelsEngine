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

            Logr.Log("Connecting Tcp Client on server and port " + port, Tags.Client);
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

            Logr.Log("Connected !", Tags.Client);

            StartListening().Forget();
        }

        private async UniTask StartListening() {
            var stream = _client.GetStream();
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            int messageLength = 0;

            // Read the message in chunks
            try {
                while (_cts != null
                       && !_cts.Token.IsCancellationRequested
                       && _client.Connected
                       && (bytesRead = await stream.ReadAsync(buffer, messageLength, BufferSize - messageLength, _cts.Token)) > 0) {
                    messageLength += bytesRead; // update message length
                    if (_cts.IsCancellationRequested) break;
                    if (stream.DataAvailable) continue; // while there is more data, keep reading in the buffer
                    var msg = MessagePackSerializer.Deserialize<INetworkMessage>(new ReadOnlySequence<byte>(buffer, 0, messageLength));
                    OnNetworkMessage?.Invoke(msg);
                    messageLength = 0; // reset message length for next message
                }
            } catch (Exception e) {
                Logr.LogException(e);
                throw;
            } finally {
                _client.GetStream().Close();
                _client.Close();
            }
        }

        public async UniTask Send(INetworkMessage msg) {
            if (_cts == null || _cts.IsCancellationRequested) return;
            var stream = _client.GetStream();
            var buffer = MessagePackSerializer.Serialize(msg);
            var token = _cts?.Token ?? CancellationToken.None;
            await stream.WriteAsync(buffer, 0, buffer.Length, token);
            if (_cts == null || _cts.IsCancellationRequested) return;
            await stream.FlushAsync(token);
            Logr.Log("Sent to server: " + msg, Tags.Client);
        }

        public void Close() {
            _cts?.Cancel(false);
        }
    }
}