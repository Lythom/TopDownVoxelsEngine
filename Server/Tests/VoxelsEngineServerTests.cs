using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using LoneStoneStudio.Tools;
using MessagePack;
using Microsoft.AspNetCore.Identity;
using Moq;
using Moq.EntityFrameworkCore;
using Server.DbModel;
using Shared;
using Shared.Net;

namespace Server.Tests {
    [TestFixture]
    public class VoxelsEngineServerTests {
        private UserManager<IdentityUser> _userManagerMock;
        private Mock<IUserStore<IdentityUser>> _userStoreMock;

        private Mock<GameSavesContext> _contextMock;
        private Mock<WebSocketMessagingQueue> _queueMock;
        private VoxelsEngineServer _server;

        private string TestUsername = "TestUsername";

        [SetUp]
        public void SetUp() {
            // Créez des mocks pour les dépendances de VoxelsEngineServer
            // Créez un mock pour IUserStore<IdentityUser>
            _userStoreMock = new Mock<IUserStore<IdentityUser>>();
            // Créez une instance de UserManager<IdentityUser> en utilisant le mock
            _userManagerMock = new UserManager<IdentityUser>(_userStoreMock.Object, null, null, null, null, null, null, null, null);
            _contextMock = new Mock<GameSavesContext>();
            _queueMock = new Mock<WebSocketMessagingQueue>();

            var dbChunk0 = new DbChunk() {Cells = MessagePackSerializer.Serialize(new Cell[16, 16, 16]), IsGenerated = true, ChX = 0, ChZ = 0};
            var dbChunk1 = new DbChunk() {Cells = MessagePackSerializer.Serialize(new Cell[16, 16, 16]), IsGenerated = true, ChX = 1, ChZ = 0};
            var dbChunk2 = new DbChunk() {Cells = MessagePackSerializer.Serialize(new Cell[16, 16, 16]), IsGenerated = false, ChX = 1, ChZ = 1};
            var levels = new List<DbLevel>() {
                new DbLevel() {
                    Name = "Lobby",
                    Chunks = new List<DbChunk>() {
                        dbChunk0,
                        dbChunk1,
                        dbChunk2,
                    },
                    Seed = 0,
                    SpawnPointX = 4,
                    SpawnPointY = 4,
                    SpawnPointZ = 4,
                }
            };

            var games = new List<DbGame> {
                new DbGame() {
                    Levels = levels, // defined previously
                    DataVersion = 1,
                    Seed = 0
                }
            };

            var characters = new List<DbCharacter> {
                new DbCharacter {
                    Name = TestUsername,
                    SerializedData = MessagePackSerializer.Serialize(new Character(TestUsername, Vector3.zero, "Lobby")),
                }
            };
            var identityUser = new IdentityUser(TestUsername);
            var players = new List<DbPlayer> {
                new DbPlayer {
                    Characters = characters,
                    IdentityUser = identityUser
                }
            };
            _userStoreMock.Setup(s => s.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(identityUser);

            _contextMock.Setup(g => g.Games).ReturnsDbSet(games);
            _contextMock.Setup(g => g.Levels).ReturnsDbSet(levels);
            _contextMock.Setup(c => c.Characters).ReturnsDbSet(characters);
            _contextMock.Setup(c => c.Players).ReturnsDbSet(players);


            // Créez une instance de VoxelsEngineServer à tester
            _server = new VoxelsEngineServer(_contextMock.Object, _userManagerMock, _queueMock.Object);
        }

        [Test]
        public async Task StartAsync_WhenCalled_SetsIsReadyToTrue() {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _server.StartAsync(cancellationToken);

            // Assert
            Assert.IsTrue(_server.IsReady);
        }

        [Test]
        public async Task StopAsync_WhenCalled_SetsIsReadyToFalse() {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _server.StopAsync(cancellationToken);

            // Assert
            Assert.IsFalse(_server.IsReady);
        }

        [Test]
        public async Task HandleMessageAsync_WhenPassedHelloNetworkMessage_AddsCharacterToConnectedCharacters() {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _server.StartAsync(cancellationToken);

            var helloMessage = new HelloNetworkMessage {Username = TestUsername};

            // Act
            await _server.HandleMessageAsync(helloMessage, _ => false, _ => false);

            // Assert

            // Check the player character is registered as connected
            var connectedCharactersField = _server.GetType().GetField("_connectedCharacters", BindingFlags.NonPublic | BindingFlags.Instance);
            var connectedCharacters = connectedCharactersField?.GetValue(_server) as ReactiveDictionary<ushort, string>;
            Assert.IsTrue(connectedCharacters?.Any(c => c.Value == helloMessage.Username));
            // Check we correctly broadcasted CharacterJoinGameEvent after joining
            _queueMock.Verify(q => q.Broadcast(It.Is<CharacterJoinGameEvent>(e => e.Character.Name == TestUsername)), Times.Once);
        }

        [Test]
        public async Task NotifyDisconnection_WhenCalled_RemovesCharacterFromConnectedCharactersAndBroadcastsCharacterLeaveGameEvent() {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _server.StartAsync(cancellationToken);

            var webSocket = new Mock<WebSocket>().Object;
            _server.NotifyConnection(webSocket);

            var helloMessage = new HelloNetworkMessage {Username = TestUsername};
            await _server.HandleMessageAsync(helloMessage, _ => false, _ => false);


            // Act
            _server.NotifyDisconnection(webSocket);

            // Assert

            // Check the player character is no longer registered as connected
            var connectedCharactersField = _server.GetType().GetField("_connectedCharacters", BindingFlags.NonPublic | BindingFlags.Instance);
            var connectedCharacters = connectedCharactersField?.GetValue(_server) as ReactiveDictionary<ushort, string>;
            Assert.IsFalse(connectedCharacters?.Any(c => c.Value == helloMessage.Username));

            // Check we correctly broadcasted CharacterLeaveGameEvent after leaving
            _queueMock.Verify(q => q.Broadcast(It.Is<CharacterLeaveGameEvent>(e => e.CharacterId == helloMessage.Username)), Times.Once);
        }
    }
}