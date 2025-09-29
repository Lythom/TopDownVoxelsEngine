using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;
using Server.DbModel;
using Server.Services;
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
        private Mock<IBlueprintService> _blueprintServiceMock;
        private VoxelsEngineServer _server;

        private string TestUsername = "TestUsername";
        private string TestUsername2 = "TestUsername2";
        private ushort _testShortId = 1337;
        private ushort _testShortId2 = 1338;

        [SetUp]
        public void SetUp() {
            // Créez des mocks pour les dépendances de VoxelsEngineServer
            // Créez un mock pour IUserStore<IdentityUser>
            _userStoreMock = new Mock<IUserStore<IdentityUser>>();
            // Créez une instance de UserManager<IdentityUser> en utilisant le mock
            _userManagerMock = new UserManager<IdentityUser>(_userStoreMock.Object, null, null, null, null, null, null, null, null);
            _contextMock = new Mock<GameSavesContext>();
            _socketServerMock = new Mock<SocketServer>();
            _blueprintServiceMock = new Mock<IBlueprintService>();
            var blockRegistryMock = new Mock<IRegistry<BlockConfigJson>>(MockBehavior.Loose);
            blockRegistryMock.Setup(r => r.Get(It.IsAny<string>())).Returns(new BlockConfigJson());
            blockRegistryMock.Setup(r => r.Get()).Returns(new Dictionary<string, BlockConfigJson>());

            var dbChunk0 = new DbChunk() {Cells = MessagePackSerializer.Serialize(new CellV0[16, 16, 16]), IsGenerated = true, ChX = 0, ChZ = 0};
            var dbChunk1 = new DbChunk() {Cells = MessagePackSerializer.Serialize(new CellV0[16, 16, 16]), IsGenerated = true, ChX = 1, ChZ = 0};
            var dbChunk2 = new DbChunk() {Cells = MessagePackSerializer.Serialize(new CellV0[16, 16, 16]), IsGenerated = false, ChX = 1, ChZ = 1};
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
                    SerializedData = MessagePackSerializer.Serialize(new CharacterV0(TestUsername, Vector3.zero, "Lobby")),
                    Level = levels[0],
                }
            };
            var characters2 = new List<DbCharacter> {
                new DbCharacter {
                    Name = TestUsername2,
                    SerializedData = MessagePackSerializer.Serialize(new CharacterV0(TestUsername2, Vector3.zero, "Lobby")),
                    Level = levels[0],
                }
            };
            var identityUser = new IdentityUser(TestUsername);
            var identityUser2 = new IdentityUser(TestUsername2);
            var players = new List<DbPlayer> {
                new DbPlayer {
                    Characters = characters,
                    IdentityUser = identityUser
                },
                new DbPlayer {
                    Characters = characters2,
                    IdentityUser = identityUser2
                }
            };
            _userStoreMock.Setup(s => s.FindByNameAsync(TestUsername, It.IsAny<CancellationToken>())).ReturnsAsync(identityUser);
            _userStoreMock.Setup(s => s.FindByNameAsync(TestUsername2, It.IsAny<CancellationToken>())).ReturnsAsync(identityUser2);

            _contextMock.Setup(g => g.Games).ReturnsDbSet(games);
            _contextMock.Setup(g => g.Levels).ReturnsDbSet(levels);
            _contextMock.Setup(c => c.Characters).ReturnsDbSet(characters);
            _contextMock.Setup(c => c.Players).ReturnsDbSet(players);


            // Créez un mock pour IServiceScope
            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(GameSavesContext))).Returns(_contextMock.Object);
            serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(UserManager<IdentityUser>))).Returns(_userManagerMock);
            serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IBlueprintService))).Returns(_blueprintServiceMock.Object);
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            // Créez une instance de VoxelsEngineServer à tester
            _server = new VoxelsEngineServer(serviceScopeFactoryMock.Object, _socketServerMock.Object, blockRegistryMock.Object);
        }

        [Test]
        public async Task StartAsync_WhenCalled_SetsIsReadyToTrue() {
            // Arrange

            // Act
            _server.StartAsync(9004).Forget();
            while (!_server.IsReady) await Task.Delay(10);

            // Assert
            Assert.IsTrue(_server.IsReady);
        }

        [Test]
        public async Task StopAsync_WhenCalled_SetsIsReadyToFalse() {
            // Arrange

            // Act
            await _server.StopAsync();

            // Assert
            Assert.IsFalse(_server.IsReady);
        }

        [Test]
        public async Task HandleMessageAsync_WhenPassedRegisterPlayerCommand_AddsCharacterToConnectedCharacters() {
            // Arrange
            _server.StartAsync(9005).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});
            await WaitOutboxProcessed();

            // Assert
            // Check the player character is registered as connected
            Assert.IsTrue(_server.State.Characters.ContainsKey(_testShortId));
            Assert.AreEqual(TestUsername, _server.State.Characters[_testShortId].Name);
            // Check we correctly broadcasted CharacterJoinGameEvent after joining
            _socketServerMock.Verify(q => q.Send(It.IsAny<ushort>(), It.Is<CharacterJoinGameEvent>(e => e.Character.Name == TestUsername)), Times.Once);
        }


        [Test]
        public async Task NotifyConnection_CheckJoinBroadcast() {
            // Arrange
            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            var helloMessage2 = new RegisterPlayerCommand {Username = TestUsername2};
            _server.StartAsync(9000).Forget();
            while (!_server.IsReady) await Task.Delay(10);

            _server.NotifyConnection(_testShortId);
            _server.NotifyConnection(_testShortId2);

            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId2, Message = helloMessage2});
            await WaitOutboxProcessed();

            // Assert
            // Check the player character is registered on server
            Assert.IsTrue(_server.State.Characters.Any(c => c.Value.Name == TestUsername2));

            // Check we correctly broadcasted CharacterJoinGameEvent
            _socketServerMock.Verify(q => q.Send(_testShortId, It.Is<CharacterJoinGameEvent>(e => e.CharacterShortId == _testShortId2)), Times.Once);
            _socketServerMock.Verify(q => q.Send(_testShortId2, It.Is<CharacterJoinGameEvent>(e => e.CharacterShortId == _testShortId2)), Times.Once);

            // check we send to the new user the current user list
            _socketServerMock.Verify(q => q.Send(_testShortId2, It.Is<CharacterJoinGameEvent>(e => e.CharacterShortId == _testShortId)), Times.Once);
        }

        [Test]
        public async Task NotifyDisconnection_WhenCalled_RemovesCharacterFromStateAndBroadcastsCharacterLeaveGameEvent() {
            // Arrange
            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            var helloMessage2 = new RegisterPlayerCommand {Username = TestUsername2};
            _server.StartAsync(9000).Forget();
            while (!_server.IsReady) await Task.Delay(10);

            _server.NotifyConnection(_testShortId);
            _server.NotifyConnection(_testShortId2);

            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId2, Message = helloMessage2});

            // Act
            _server.NotifyDisconnection(_testShortId);
            await WaitOutboxProcessed();

            // Assert
            // Check the player character is unregistered
            Assert.IsFalse(_server.State.Characters.Any(c => c.Value.Name == helloMessage.Username));
            // Check we correctly broadcasted CharacterLeaveGameEvent after leaving to remaining user
            _socketServerMock.Verify(q => q.Send(It.IsAny<ushort>(), It.Is<CharacterLeaveGameEvent>(e => e.CharacterShortId == _testShortId)), Times.Once);
        }

        [Test]
        public async Task ScheduleChunkUpload_WhenCalled_AddsCorrectChunksToUserUploadQueue() {
            // Arrange
            var userSessionDataField = _server.GetType().GetField("_userSessionData", BindingFlags.NonPublic | BindingFlags.Instance);
            var userSessionData = userSessionDataField?.GetValue(_server) as ConcurrentDictionary<ushort, UserSessionData>;

            _server.StartAsync(9001).Forget();
            while (!_server.IsReady) await Task.Delay(10);

            _server.NotifyConnection(_testShortId);
            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});

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
                    var key = new ChunkKey(levelId, i, j);
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
            var userSessionData = userSessionDataField?.GetValue(_server) as ConcurrentDictionary<ushort, UserSessionData>;

            _server.StartAsync(9002).Forget();
            while (!_server.IsReady) await Task.Delay(10);

            _server.NotifyConnection(_testShortId);
            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});

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
                    var key = new ChunkKey(levelId, i, j);
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
            var userSessionData = userSessionDataField?.GetValue(_server) as ConcurrentDictionary<ushort, UserSessionData>;

            _server.StartAsync(9003).Forget();
            while (!_server.IsReady) await Task.Delay(10);

            _server.NotifyConnection(_testShortId);
            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});

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
                    var key = new ChunkKey(levelId, i, j);
                    expectedList.Add(key);
                }
            }

            Assert.AreEqual(16, expectedList.Count);

            while (userSession.UploadQueue.TryDequeue(out var k, out var p)) {
                Assert.IsTrue(expectedList.Remove(k));
            }

            Assert.IsEmpty(expectedList);
        }

        [Test]
        public async Task HandleMessageAsync_SaveBlueprintCommand_CallsBlueprintService() {
            // Arrange
            _server.StartAsync(9100).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});

            var SaveBlueprintCommand = new SaveBlueprintCommand(
                1,
                _testShortId,
                "Test Blueprint",
                10, 5, 10,
                3, 3, 3
            );

            // Configure mock to return success using Returns instead of ReturnsAsync
            _blueprintServiceMock.Setup(s => s.SaveBlueprintAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Vector3Int>(),
                It.IsAny<Vector3Int>(),
                It.IsAny<LevelMap>(),
                It.IsAny<BlockPathMapping>()
            )).Returns(new UniTask<(bool success, string? error)>((true, null)));

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = SaveBlueprintCommand});

            // Assert
            _blueprintServiceMock.Verify(s => s.SaveBlueprintAsync(
                TestUsername,
                "Test Blueprint",
                new Vector3Int(10, 5, 10),
                new Vector3Int(3, 3, 3),
                It.IsAny<LevelMap>(),
                It.IsAny<BlockPathMapping>()
            ), Times.Once);
        }

        [Test]
        public async Task HandleMessageAsync_LoadBlueprintListQuery_SendsResponseWithBlueprintList() {
            // Arrange
            _server.StartAsync(9101).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});

            var LoadBlueprintListQuery = new LoadBlueprintListQuery(1, _testShortId, 0, 10);

            var blueprintList = new BlueprintMetadataV0[] {
                new BlueprintMetadataV0 {
                    Id = Guid.NewGuid(),
                    Name = "Test Blueprint 1",
                    CreatorId = TestUsername,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Size = new Vector3Int(3, 3, 3)
                },
                new BlueprintMetadataV0 {
                    Id = Guid.NewGuid(),
                    Name = "Test Blueprint 2",
                    CreatorId = TestUsername,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Size = new Vector3Int(5, 3, 5)
                }
            };

            // Configure mock to return blueprints using Returns
            _blueprintServiceMock.Setup(s => s.GetBlueprintListAsync(0, 10))
                .Returns(new UniTask<(BlueprintMetadataV0[] blueprints, int totalCount)>((blueprintList, blueprintList.Length)));

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = LoadBlueprintListQuery});
            await WaitOutboxProcessed();

            // Assert
            _blueprintServiceMock.Verify(s => s.GetBlueprintListAsync(0, 10), Times.Once);
            _socketServerMock.Verify(s => s.Send(_testShortId, It.Is<LoadBlueprintListResponse>(e =>
                e.Id == 1 &&
                e.CharacterShortId == _testShortId &&
                e.Blueprints.Length == 2 &&
                e.TotalCount == 2)), Times.Once);
        }

        [Test]
        public async Task HandleMessageAsync_LoadBlueprintQuery_SendsResponseWithBlueprint() {
            // Arrange
            _server.StartAsync(9102).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});
            await WaitOutboxProcessed();

            var blueprintId = Guid.NewGuid();
            var LoadBlueprintQuery = new LoadBlueprintQuery(1, _testShortId, blueprintId);

            var blueprint = new BlueprintV0 {
                Id = blueprintId,
                Name = "Test Blueprint",
                CreatorId = TestUsername,
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                Size = new Vector3Int(3, 3, 3),
                Cells = new CellArrayV0(new CellV0[3, 3, 3]),
                BlockMapping = new BlockPathMapping(),
                FloorHeight = 0,
                PossibleSymmetries = Symmetries.None
            };

            // Configure mock to return blueprint using Returns
            _blueprintServiceMock.Setup(s => s.GetBlueprintAsync(blueprintId))
                .Returns(new UniTask<BlueprintV0?>(blueprint));

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = LoadBlueprintQuery});
            await WaitOutboxProcessed();

            // Assert
            _blueprintServiceMock.Verify(s => s.GetBlueprintAsync(blueprintId), Times.Once);
            _socketServerMock.Verify(s => s.Send(_testShortId, It.Is<LoadBlueprintResponse>(e =>
                e.Id == 1 &&
                e.CharacterShortId == _testShortId &&
                e.Blueprint.Id == blueprintId)), Times.Once);
        }

        [Test]
        public async Task HandleMessageAsync_PlaceBlueprintCommand_UpdatesLevelAndSendsUpdate() {
            // Arrange
            _server.StartAsync(9103).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);
            _server.NotifyConnection(_testShortId2);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            var helloMessage2 = new RegisterPlayerCommand {Username = TestUsername2};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId2, Message = helloMessage2});
            await WaitOutboxProcessed();

            var blueprintId = Guid.NewGuid();
            var PlaceBlueprintCommand = new PlaceBlueprintCommand(
                1,
                _testShortId,
                blueprintId,
                10, 5, 10,
                0,
                Symmetries.None
            );

            var blueprint = new BlueprintV0 {
                Id = blueprintId,
                Name = "Test Blueprint",
                CreatorId = TestUsername,
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                Size = new Vector3Int(3, 3, 3),
                Cells = new CellArrayV0(new CellV0[3, 3, 3]),
                BlockMapping = new BlockPathMapping(),
                FloorHeight = 0,
                PossibleSymmetries = Symmetries.None
            };

            var modifiedChunks = new HashSet<ChunkKey> {
                new ChunkKey("Lobby", 0, 0),
                new ChunkKey("Lobby", 0, 1)
            };

            // Configure mock to return blueprint using Returns
            _blueprintServiceMock.Setup(s => s.GetBlueprintAsync(blueprintId))
                .Returns(new UniTask<BlueprintV0?>(blueprint));

            // Configure mock to return modified chunks using Returns
            _blueprintServiceMock.Setup(s => s.PlaceBlueprintAsync(
                blueprintId,
                10, 5, 10,
                0,
                Symmetries.None,
                It.IsAny<LevelMap>()
            )).Returns(new UniTask<IReadOnlySet<ChunkKey>>(modifiedChunks));

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = PlaceBlueprintCommand});
            await WaitOutboxProcessed();

            // Assert
            _blueprintServiceMock.Verify(s => s.PlaceBlueprintAsync(
                blueprintId,
                10, 5, 10,
                0,
                Symmetries.None,
                It.IsAny<LevelMap>()
            ), Times.Once);

            // Verify that the server sent ChunkUpdateGameEvent to all clients for each modified chunk
            _socketServerMock.Verify(s => s.Send(
                    It.IsAny<ushort>(),
                    It.IsAny<ChunkUpdateGameEvent>()),
                Times.AtLeastOnce());
        }

        [Test]
        public async Task HandleMessageAsync_SaveBlueprintCommand_HandlesFailure() {
            // Arrange
            _server.StartAsync(9104).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});
            await WaitOutboxProcessed();

            var SaveBlueprintCommand = new SaveBlueprintCommand(
                1,
                _testShortId,
                "Test Blueprint",
                10, 5, 10,
                2, 3, 2 // Even sizes - should fail
            );

            // Configure mock to return failure using Returns
            _blueprintServiceMock.Setup(s => s.SaveBlueprintAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Vector3Int>(),
                It.IsAny<Vector3Int>(),
                It.IsAny<LevelMap>(),
                It.IsAny<BlockPathMapping>()
            )).Returns(new UniTask<(bool success, string? error)>((false, "Blueprint size must be odd in X and Z dimensions")));

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = SaveBlueprintCommand});
            await WaitOutboxProcessed();

            // Assert
            _blueprintServiceMock.Verify(s => s.SaveBlueprintAsync(
                TestUsername,
                "Test Blueprint",
                new Vector3Int(10, 5, 10),
                new Vector3Int(2, 3, 2),
                It.IsAny<LevelMap>(),
                It.IsAny<BlockPathMapping>()
            ), Times.Once);

            // We would expect some error notification to be sent to the client here
            // The exact implementation depends on how the server handles errors
        }

        [Test]
        public async Task HandleMessageAsync_PlaceBlueprintCommand_HandlesNonExistentBlueprint() {
            // Arrange
            _server.StartAsync(9105).Forget();
            while (!_server.IsReady) await Task.Delay(10);
            _server.NotifyConnection(_testShortId);

            var helloMessage = new RegisterPlayerCommand {Username = TestUsername};
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = helloMessage});
            await WaitOutboxProcessed();

            var nonExistentBlueprintId = Guid.NewGuid();
            var placeBlueprintCommand = new PlaceBlueprintCommand(
                1,
                _testShortId,
                nonExistentBlueprintId,
                10, 5, 10,
                0,
                Symmetries.None
            );

            // Configure mock to return null (blueprint not found) using Returns
            _blueprintServiceMock.Setup(s => s.GetBlueprintAsync(nonExistentBlueprintId))
                .Returns(new UniTask<BlueprintV0?>(null));

            // Act
            await _server.HandleMessageAsync(new InputMessage {Id = _testShortId, Message = placeBlueprintCommand});
            await WaitOutboxProcessed();

            // Assert
            // PlaceBlueprintAsync should not be called since GetBlueprintAsync returns null
            _blueprintServiceMock.Verify(s => s.PlaceBlueprintAsync(
                It.IsAny<Guid>(),
                It.IsAny<short>(),
                It.IsAny<short>(),
                It.IsAny<short>(),
                It.IsAny<byte>(),
                It.IsAny<Symmetries>(),
                It.IsAny<LevelMap>()
            ), Times.Never);

            // An error should be sent since the blueprint doesn't exist
            _socketServerMock.Verify(s => s.Send(
                    _testShortId,
                    It.IsAny<ServerErrorGameEvent>()),
                Times.Once);
        }

        private async Task WaitOutboxProcessed() {
            while (_server.HasOutboxMessages()) await Task.Delay(2);
        }
    }
}