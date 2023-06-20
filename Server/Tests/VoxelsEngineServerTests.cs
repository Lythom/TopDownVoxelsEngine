using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;
using Server.DbModel;
using Shared;
using Shared.Net;

#pragma warning disable CS8625

namespace Server.Tests {
    [TestFixture]
    public class VoxelsEngineServerTests {
        private UserManager<IdentityUser> _userManagerMock;
        private Mock<IUserStore<IdentityUser>> _userStoreMock;

        private Mock<GameSavesContext> _contextMock;
        private Mock<SocketServer> _socketServerMock;
        private VoxelsEngineServer _server;

        private string TestUsername = "TestUsername";
        private ushort _testShortId = 1337;

        [SetUp]
        public void SetUp() {
            // Créez des mocks pour les dépendances de VoxelsEngineServer
            // Créez un mock pour IUserStore<IdentityUser>
            _userStoreMock = new Mock<IUserStore<IdentityUser>>();
            // Créez une instance de UserManager<IdentityUser> en utilisant le mock
            _userManagerMock = new UserManager<IdentityUser>(_userStoreMock.Object, null, null, null, null, null, null, null, null);
            _contextMock = new Mock<GameSavesContext>();
            _socketServerMock = new Mock<SocketServer>();

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


            // Créez un mock pour IServiceScope
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(GameSavesContext))).Returns(_contextMock.Object);
            serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(UserManager<IdentityUser>))).Returns(_userManagerMock);
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            // Créez une instance de VoxelsEngineServer à tester
            _server = new VoxelsEngineServer(serviceScopeFactoryMock.Object, _socketServerMock.Object);
        }

        [Test]
        public async Task StartAsync_WhenCalled_SetsIsReadyToTrue() {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _server.StartAsync();

            // Assert
            Assert.IsTrue(_server.IsReady);
        }

        [Test]
        public async Task StopAsync_WhenCalled_SetsIsReadyToFalse() {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _server.StopAsync();

            // Assert
            Assert.IsFalse(_server.IsReady);
        }

        [Test]
        public async Task HandleMessageAsync_WhenPassedHelloNetworkMessage_AddsCharacterToConnectedCharacters() {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _server.StartAsync();

            var webSocket = new Mock<WebSocket>();
            _server.NotifyConnection(_testShortId);

            var helloMessage = new HelloNetworkMessage {Username = TestUsername};

            // Act
            await _server.HandleMessageAsync(_testShortId, helloMessage);

            // Assert

            // Check the player character is registered as connected
            Assert.IsTrue(_server.State.Characters.ContainsKey(_testShortId));
            Assert.AreEqual(TestUsername, _server.State.Characters[_testShortId].Name);
            // Check we correctly broadcasted CharacterJoinGameEvent after joining
            _socketServerMock.Verify(q => q.Broadcast(It.Is<CharacterJoinGameEvent>(e => e.Character.Name == TestUsername)), Times.Once);
        }


        [Test]
        public async Task NotifyDisconnection_WhenCalled_RemovesCharacterFromStateAndBroadcastsCharacterLeaveGameEvent() {
            // Arrange
            var webSocket = new Mock<WebSocket>();

            var cancellationToken = CancellationToken.None;
            var helloMessage = new HelloNetworkMessage {Username = TestUsername};
            await _server.StartAsync();

            _server.NotifyConnection(_testShortId);
            await _server.HandleMessageAsync(_testShortId, helloMessage);

            // Act
            _server.NotifyDisconnection(_testShortId);

            // Assert
            // Check the player character is unregistered
            Assert.IsFalse(_server.State.Characters.Any(c => c.Value.Name == helloMessage.Username));
            // Check we correctly broadcasted CharacterLeaveGameEvent after leaving
            _socketServerMock.Verify(q => q.Broadcast(It.Is<CharacterLeaveGameEvent>(e => e.CharacterId == _testShortId)), Times.Once);
        }

        [Test]
        public async Task ScheduleChunkUpload_WhenCalled_AddsCorrectChunksToUserUploadQueue() {
            // Arrange
            var userSessionDataField = _server.GetType().GetField("_userSessionData", BindingFlags.NonPublic | BindingFlags.Instance);
            var userSessionData = userSessionDataField?.GetValue(_server) as Dictionary<ushort, UserSessionData>;

            var cancellationToken = CancellationToken.None;
            var webSocket = new Mock<WebSocket>();

            await _server.StartAsync();

            _server.NotifyConnection(_testShortId);
            var helloMessage = new HelloNetworkMessage {Username = TestUsername};
            await _server.HandleMessageAsync(_testShortId, helloMessage);

            var levelId = "Lobby";
            var chX = 1;
            var chZ = 1;

            // Act
            _server.ScheduleChunkUpload(_testShortId, levelId, chX, chZ);

            // Assert
            var userSession = userSessionData[_testShortId];
            // 7x7 chunks around the specified chunk should be scheduled for upload EXCLUDED out of bounds (negative) values
            Assert.AreEqual(25, userSession.UploadQueue.Count);

            // Check that the correct chunks are in the queue
            var expectedList = new List<ChunkKey>();
            for (int x = -3; x <= 3; x++) {
                for (int z = -3; z <= 3; z++) {
                    var i = chX + x;
                    var j = chZ + z;
                    if (i < 0 || i >= LevelMap.LevelChunkSize || j < 0 || j >= LevelMap.LevelChunkSize) continue;
                    var key = ChunkKeyPool.Get(levelId, i, j);
                    expectedList.Add(key);
                }
            }

            Assert.AreEqual(25, expectedList.Count);

            while (userSession.UploadQueue.TryDequeue(out var k, out var p)) {
                Assert.IsTrue(expectedList.Remove(k));
            }

            Assert.IsEmpty(expectedList);
        }

        [Test]
        public async Task ScheduleChunkUpload_WhenCalled_AddsCorrectChunksToUserUploadQueueSpawn() {
            // Arrange
            var userSessionDataField = _server.GetType().GetField("_userSessionData", BindingFlags.NonPublic | BindingFlags.Instance);
            var userSessionData = userSessionDataField?.GetValue(_server) as Dictionary<ushort, UserSessionData>;

            var cancellationToken = CancellationToken.None;
            var webSocket = new Mock<WebSocket>();

            await _server.StartAsync();

            _server.NotifyConnection(_testShortId);
            var helloMessage = new HelloNetworkMessage {Username = TestUsername};
            await _server.HandleMessageAsync(_testShortId, helloMessage);

            var levelId = "Lobby";
            var chX = 32;
            var chZ = 32;

            // Act
            _server.ScheduleChunkUpload(_testShortId, levelId, chX, chZ);

            // Assert
            var userSession = userSessionData[_testShortId];
            // 7x7 chunks around the specified chunk should be scheduled for upload EXCLUDED out of bounds (negative) values
            Assert.AreEqual(49, userSession.UploadQueue.Count);

            // Check that the correct chunks are in the queue
            var expectedList = new List<ChunkKey>();
            for (int x = -3; x <= 3; x++) {
                for (int z = -3; z <= 3; z++) {
                    var i = chX + x;
                    var j = chZ + z;
                    if (i < 0 || i >= LevelMap.LevelChunkSize || j < 0 || j >= LevelMap.LevelChunkSize) continue;
                    var key = ChunkKeyPool.Get(levelId, i, j);
                    expectedList.Add(key);
                }
            }

            Assert.AreEqual(49, expectedList.Count);

            while (userSession.UploadQueue.TryDequeue(out var k, out var p)) {
                Assert.IsTrue(expectedList.Remove(k));
            }

            Assert.IsEmpty(expectedList);
        }

        [Test]
        public async Task ScheduleChunkUpload_WhenCalled_AddsCorrectChunksToUserUploadQueueMaxBounds() {
            // Arrange
            var userSessionDataField = _server.GetType().GetField("_userSessionData", BindingFlags.NonPublic | BindingFlags.Instance);
            var userSessionData = userSessionDataField?.GetValue(_server) as Dictionary<ushort, UserSessionData>;

            var cancellationToken = CancellationToken.None;
            var webSocket = new Mock<WebSocket>();

            await _server.StartAsync();

            _server.NotifyConnection(_testShortId);
            var helloMessage = new HelloNetworkMessage {Username = TestUsername};
            await _server.HandleMessageAsync(_testShortId, helloMessage);

            var levelId = "Lobby";
            var chX = LevelMap.LevelChunkSize - 1;
            var chZ = LevelMap.LevelChunkSize - 1;

            // Act
            _server.ScheduleChunkUpload(_testShortId, levelId, chX, chZ);

            // Assert
            var userSession = userSessionData[_testShortId];
            // 7x7 chunks around the specified chunk should be scheduled for upload EXCLUDED out of bounds (negative) values
            Assert.AreEqual(16, userSession.UploadQueue.Count);

            // Check that the correct chunks are in the queue
            var expectedList = new List<ChunkKey>();
            for (int x = -3; x <= 3; x++) {
                for (int z = -3; z <= 3; z++) {
                    var i = chX + x;
                    var j = chZ + z;
                    if (i < 0 || i >= LevelMap.LevelChunkSize || j < 0 || j >= LevelMap.LevelChunkSize) continue;
                    var key = ChunkKeyPool.Get(levelId, i, j);
                    expectedList.Add(key);
                }
            }

            Assert.AreEqual(16, expectedList.Count);

            while (userSession.UploadQueue.TryDequeue(out var k, out var p)) {
                Assert.IsTrue(expectedList.Remove(k));
            }

            Assert.IsEmpty(expectedList);
        }
    }
}