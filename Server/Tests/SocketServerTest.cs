using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Shared.Net;

namespace SocketTest {
    // replace this with actual implementation of INetworkMessage used in your application
    internal class NetworkMessageMock : INetworkMessage {
        public string Message { get; set; }
    }

    [TestFixture]
    public class SocketTest {
        private SocketServer _server;
        private SocketClient _client;
        private string _message = "Hello, World!";
        private int _port = 5000;

        [OneTimeSetUp]
        public async Task Setup() {
            _server = new SocketServer();
            _client = new SocketClient();
            var tcs = new TaskCompletionSource<bool>();

            _server.OnNetworkMessage = (id, msg) => {
                Console.WriteLine("Server received " + msg.ToString());
                if (msg is NetworkMessageMock msgMock) {
                    Assert.AreEqual(_message, msgMock.Message);
                    tcs.SetResult(true);
                }
            };

            _server.OnOpen = _ => Console.WriteLine("Server opened");
            _server.OnClose = _ => Console.WriteLine("Server closed");

            _client.OnNetworkMessage = (msg) => {
                Console.WriteLine("Client received " + msg.ToString());
                if (msg is NetworkMessageMock msgMock) {
                    Assert.AreEqual(_message, msgMock.Message);
                    tcs.SetResult(true);
                }
            };

            _server.Init(_port);

            await _client.Init("localhost", _port);
        }

        [Test]
        public async Task ServerCanSendMessageToClient() {
            var messageMock = new NetworkMessageMock {Message = _message};
            await _server.Send(0, messageMock);
            // we wait for the network operation to complete
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task ClientCanSendMessageToServer() {
            var messageMock = new NetworkMessageMock {Message = _message};
            _client.Send(messageMock);
            // we wait for the network operation to complete
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        [OneTimeTearDown]
        public void TearDown() {
            _client.Close();
            _server.Close();
        }
    }
}