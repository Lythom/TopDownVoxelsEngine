using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.DbModel;
using Shared;
using Shared.Net;

namespace Server {
    public class VoxelsEngineServer {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        // Data
        private GameState _state = new();
        private GameState _stateBackup = new();

        private readonly Dictionary<ushort, UserSessionData> _userSessionData = new();

        // Running
        private bool _isReady = false;
        public bool IsReady => _isReady;
        public GameState State => _state;

        private SocketServer _socketServer;
        private ServerClock _serverClock;
        private readonly CancellationTokenSource _cts;

        public VoxelsEngineServer(IServiceScopeFactory serviceScopeFactory, SocketServer socketServer) {
            _serviceScopeFactory = serviceScopeFactory;
            try {
                _cts = new CancellationTokenSource();
                _socketServer = socketServer;
                _serverClock = new ServerClock(this);
            } catch (Exception e) {
                throw new ApplicationException("Could not start Server", e);
            }
        }

        public async UniTask StartAsync() {
            using (var scope = _serviceScopeFactory.CreateScope()) {
                var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();

                var dbSave = await context.Games
                    .Include(g => g.Levels!)
                    .ThenInclude(l => l.Chunks)
                    .FirstOrDefaultAsync(_cts.Token);
                if (dbSave == null) {
                    dbSave = InitNewGame();
                    context.Games.Add(dbSave);
                    await context.SaveChangesAsync(_cts.Token);
                }

                InitState(dbSave);

                _serverClock = new ServerClock(this);
                _serverClock.StartFixedUpdateAsync().Forget();

                _socketServer.OnNetworkMessage += HandleMessage;
                _socketServer.OnOpen += NotifyConnection;
                _socketServer.OnClose += NotifyDisconnection;
                _socketServer.Init(9999);

                _isReady = true;
                Console.WriteLine("Server ready!");
            }
        }


        public UniTask StopAsync() {
            _isReady = false;
            _serverClock.Stop();
            _cts.Cancel(false);
            _socketServer.Close();
            return UniTask.CompletedTask;
        }

        public void NotifyDisconnection(ushort shortId) {
            if (!_userSessionData.ContainsKey(shortId)) return;
            var userData = _userSessionData[shortId];
            if (userData.IsLogged) {
                var characterLeaveGameEvent = new CharacterLeaveGameEvent(0, shortId);
                characterLeaveGameEvent.Apply(_state, null);
                _socketServer.Broadcast(characterLeaveGameEvent).GetAwaiter().GetResult();
            }

            _userSessionData.Remove(shortId);
        }

        public void NotifyConnection(ushort shortId) {
            _userSessionData.Add(shortId, new UserSessionData(false, shortId));
        }

        private void InitState(DbGame currentDbSave) {
            if (currentDbSave.Levels == null) throw new ApplicationException("load levels with chunks please");
            // load levels
            foreach (var dbLevel in currentDbSave.Levels) {
                var spawnPosition = new Vector3(dbLevel.SpawnPointX, dbLevel.SpawnPointY, dbLevel.SpawnPointZ);
                var levelMap = new LevelMap(dbLevel.Name, spawnPosition);
                foreach (var dbChunk in dbLevel.Chunks!.Where(c => c.IsGenerated)) {
                    levelMap.Chunks[dbChunk.ChX, dbChunk.ChZ] = new() {
                        Cells = MessagePackSerializer.Deserialize<Cell[,,]>(dbChunk.Cells),
                        IsGenerated = true
                    };
                }

                _state.Levels.Add(dbLevel.Name, levelMap);
                var (chX, chZ) = LevelTools.GetChunkPosition(spawnPosition);
                _state.LevelGenerator.EnqueueUninitializedChunksAround(levelMap.LevelId, chX, chZ, 6, _state.Levels);
            }

            _state.LevelGenerator.GenerateFromQueue(PriorityLevel.All, _state.Levels);
        }

        private static DbGame InitNewGame() {
            Console.WriteLine("Generating a new GameState");
            DbGame save = new DbGame {
                Seed = Random.Shared.Next(),
                DataVersion = 1,
                Levels = new List<DbLevel> {
                    new() {
                        Name = "Lobby",
                        Seed = Random.Shared.Next(),
                        Chunks = new List<DbChunk>(),
                        // spawn point initialized on the middle of the middle chunk
                        SpawnPointX = (LevelMap.LevelChunkSize / 2) * Chunk.Size + Chunk.Size / 2,
                        SpawnPointY = Chunk.Size / 2,
                        SpawnPointZ = (LevelMap.LevelChunkSize / 2) * Chunk.Size + Chunk.Size / 2,
                    }
                },
            };

            return save;
        }

        public void HandleMessage(ushort clientShortId, INetworkMessage netMessage) {
            Logr.Log("Received message sync: " + netMessage);
            HandleMessageAsync(clientShortId, netMessage).Forget();
        }

        public async UniTask HandleMessageAsync(ushort clientShortId, INetworkMessage netMessage) {
            try {
                Logr.Log("Received message: " + netMessage);

                if (!IsReady) {
                    await _socketServer.Send(clientShortId, new ErrorNetworkMessage($"Server not ready. Please wait and retry."));
                }

                // lock the state in case of concurrent access
                switch (netMessage) {
                    case IGameEvent evt:
                        if (_state == null)
                            throw new ApplicationException(
                                "The state of the game was not found. Please init a game before applying events.");
                        // ReSharper disable once InconsistentlySynchronizedField
                        evt.AssertApplicationConditions(in _state);
                        lock (_state) {
                            evt.Apply(_state, null);
                        }

                        await _socketServer.Broadcast((INetworkMessage) evt);
                        break;
                    case HelloNetworkMessage hello: {
                        Console.WriteLine("A client said hello : " + hello.Username);
                        using var scope = _serviceScopeFactory.CreateScope();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                        var user = await userManager.FindByNameAsync(hello.Username);
                        try {
                            var character = await GetOrCreateCharacterAsync(user, hello);
                            var characterJoinGameEvent = new CharacterJoinGameEvent(0, clientShortId, character);
                            characterJoinGameEvent.Apply(_state, null);
                            _userSessionData[clientShortId].IsLogged = true;
                            await _socketServer.Broadcast(characterJoinGameEvent);
                        } catch (Exception e) {
                            await _socketServer.Send(clientShortId, new ErrorNetworkMessage(e.Message));
                            Console.WriteLine(e.ToString());
                            return;
                        }

                        break;
                    }
                }

                // backup the state before applying
                lock (_state) {
                    _stateBackup.UpdateValue(_state);
                }
            } catch (Exception e) {
                Console.WriteLine($"An error occured with message {netMessage.GetType().Name}. Rolling state back.\n" + e);

                // treat the event as a transaction, cancel any partially applied event
                if (_state != null) {
                    lock (_state) {
                        _state.UpdateValue(_stateBackup);
                    }
                }
            }
        }

        private async Task<Character> GetOrCreateCharacterAsync(IdentityUser? user, HelloNetworkMessage hello) {
            using var scope = _serviceScopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Character character;
            if (user == null) {
                // create a new user + player + character
                user = new IdentityUser(hello.Username);
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded) {
                    try {
                        var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();
                        var dbSave = await context.Games.Include(g => g.Levels).FirstAsync();
                        var dbLevel = dbSave.Levels.First();
                        var pos = new Vector3(dbLevel.SpawnPointX, dbLevel.SpawnPointY, dbLevel.SpawnPointZ);
                        character = new Character(hello.Username, pos, dbLevel.Name);
                        var player = new DbPlayer {
                            IdentityUser = user,
                            Characters = new List<DbCharacter> {
                                new() {
                                    Name = hello.Username,
                                    Level = dbLevel,
                                    X = pos.X,
                                    Y = pos.Y,
                                    Z = pos.Z,
                                    SerializedData = MessagePackSerializer.Serialize(character)
                                }
                            }
                        };

                        context.Players.Add(player);
                        await context.SaveChangesAsync();
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        var deleted = await userManager.DeleteAsync(user);
                        throw;
                    }
                } else {
                    throw new ApplicationException($"Server error: Failed to find or create user {hello.Username}.");
                }
            } else {
                var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();
                var player = await context.Players
                    .Include(p => p.Characters)
                    .FirstOrDefaultAsync(p => p.IdentityUser.Id == user.Id);
                if (player == null) {
                    throw new ApplicationException("All users should have a player on creation. Ask an admin !");
                    // TODO: remove user if no player found so that it can re-creates ?
                }

                var dbCharacter = player.Characters.First();
                character = MessagePackSerializer.Deserialize<Character>(dbCharacter.SerializedData);
            }

            return character;
        }

        public void ScheduleChunkUpload(ushort playerKey, string levelId, int chX, int chZ) {
            var range = 3;
            var userSessionData = _userSessionData.Select(u => u.Value).FirstOrDefault(u => u.ShortId == playerKey);
            if (userSessionData == null) return;
            for (int x = -range; x <= range; x++) {
                for (int z = -range; z <= range; z++) {
                    var i = chX + x;
                    var j = chZ + z;
                    if (i < 0 || i >= LevelMap.LevelChunkSize || j < 0 || j >= LevelMap.LevelChunkSize) continue;
                    var distance = (uint) Math.Abs(x) + (uint) Math.Abs(z);
                    var key = new ChunkKey(levelId, i, j);
                    if (userSessionData.UploadedChunks.Contains(key)) continue;
                    // never sent this chunk, schedule sending
                    userSessionData.UploadQueue.Enqueue(key, distance);
                    userSessionData.UploadedChunks.Add(key);
                }
            }
        }

        public async UniTask SendScheduledChunksAsync() {
            foreach (var (userKey, userSessionData) in _userSessionData) {
                // try dequeue one chunk per user per tick
                if (userSessionData.UploadQueue.TryDequeue(out var cKey, out _)) {
                    var chunk = _state.Levels[cKey.LevelId].Chunks[cKey.ChX, cKey.ChZ];
                    await _socketServer.Send(userKey, new ChunkUpdateGameEvent(0, cKey.LevelId, chunk, cKey.ChX, cKey.ChZ));
                }
            }
        }
    }
}