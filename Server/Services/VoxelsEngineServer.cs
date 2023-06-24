using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly GameState _state = new();
        private readonly GameState _stateBackup = new();

        private readonly Dictionary<ushort, UserSessionData> _userSessionData = new();

        // Running
        private bool _isReady = false;
        public bool IsReady => _isReady;
        public GameState State => _state;

        private readonly SocketServer _socketServer;
        private readonly Queue<InputMessage> _inbox = new();
        private readonly Queue<OutputMessage> _outbox = new();
        private ServerClock _serverClock;
        private readonly CancellationTokenSource _cts;

        // TODO: refuser une connexion si utilisateur du même nom déjà connecté
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

        public async UniTask StartAsync(int port) {
            using var scope = _serviceScopeFactory.CreateScope();
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

            _socketServer.OnNetworkMessage += HandleMessage;
            _socketServer.OnOpen += NotifyConnection;
            _socketServer.OnClose += NotifyDisconnection;
            _socketServer.Init(port);

            _serverClock = new ServerClock(this);
            _serverClock.StartFixedUpdateAsync(_inbox).Forget();
            StartNetworkSendingAsync().Forget();

            _isReady = true;
            Console.WriteLine("Server ready!");
        }

        private async UniTask StartNetworkSendingAsync() {
            while (!_cts.Token.IsCancellationRequested) {
                try {
                    if (_outbox.TryDequeue(out var m)) {
                        if (m.IsBroadcast) {
                            await _socketServer.Broadcast(m.Message);
                        } else {
                            await _socketServer.Send(m.Id, m.Message);
                        }
                    } else {
                        await UniTask.Yield();
                    }
                } catch (Exception e) {
                    Logr.LogException(e);
                    throw;
                }
            }
        }


        public UniTask StopAsync() {
            _isReady = false;
            _serverClock.Stop();
            _cts.Cancel(false);
            _socketServer.Close();
            return UniTask.CompletedTask;
        }

        public void SelfApply(InputMessage m) {
            _inbox.Enqueue(m);
        }

        public void Send(ushort id, INetworkMessage m) {
            if (m == null) throw new InvalidOperationException("message must not be null");
            _outbox.Enqueue(new OutputMessage(id, m));
        }

        public void Broadcast(INetworkMessage m) {
            if (m == null) throw new InvalidOperationException("message must not be null");
            _outbox.Enqueue(new OutputMessage(m));
        }

        public void NotifyDisconnection(ushort shortId) {
            lock (_userSessionData) {
                if (!_userSessionData.ContainsKey(shortId)) return;
                var userData = _userSessionData[shortId];
                if (userData.IsLogged) {
                    var characterLeaveGameEvent = new CharacterLeaveGameEvent(0, shortId);
                    SelfApply(new InputMessage(shortId, characterLeaveGameEvent));
                    Broadcast(characterLeaveGameEvent);
                }

                _userSessionData.Remove(shortId);
            }

            // If any unsent element for this user, cancel.
            lock (_outbox) {
                var tmpQueue = new Queue<OutputMessage>();
                while (_outbox.TryDequeue(out var e)) {
                    if (e.Id != shortId) tmpQueue.Enqueue(e);
                }

                while (tmpQueue.TryDequeue(out var e)) {
                    _outbox.Enqueue(e);
                }
            }
        }

        public void NotifyConnection(ushort shortId) {
            lock (_userSessionData) {
                _userSessionData.Add(shortId, new UserSessionData(false, shortId));
            }
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

        [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "Manipulate only powers of 2.")]
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
            lock (_inbox) {
                _inbox.Enqueue(new InputMessage {Id = clientShortId, Message = netMessage});
            }
        }

        public async UniTask HandleMessageAsync(InputMessage m) {
            var clientShortId = m.Id;
            var netMessage = m.Message;
            try {
                Logr.Log("Received message: " + netMessage);

                if (!IsReady) {
                    Send(clientShortId, new ErrorNetworkMessage($"Server not ready. Please wait and retry."));
                }

                // lock the state in case of concurrent access
                switch (netMessage) {
                    case IGameEvent evt:
                        if (_state == null)
                            throw new ApplicationException(
                                "The state of the game was not found. Please init a game before applying events.");
                        // ReSharper disable once InconsistentlySynchronizedField
                        evt.AssertApplicationConditions(in _state);
                        evt.Apply(State, null);
                        Broadcast(evt);
                        break;
                    case HelloNetworkMessage hello: {
                        Console.WriteLine("A client said hello : " + hello.Username);
                        using var scope = _serviceScopeFactory.CreateScope();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                        var user = await userManager.FindByNameAsync(hello.Username);
                        try {
                            var (character, levelSpawn) = await GetOrCreateCharacterAsync(user, hello);
                            var characterJoinGameEvent = new CharacterJoinGameEvent(0, clientShortId, character, levelSpawn);
                            characterJoinGameEvent.Apply(State, null);
                            lock (_userSessionData) {
                                _userSessionData[clientShortId].IsLogged = true;
                            }

                            Broadcast(characterJoinGameEvent);
                        } catch (Exception e) {
                            Send(clientShortId, new ErrorNetworkMessage(e.Message));
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

        private async UniTask<(Character character, Vector3 spawnPosition)> GetOrCreateCharacterAsync(IdentityUser? user, HelloNetworkMessage hello) {
            using var scope = _serviceScopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Character character;
            Vector3 spawnPosition;
            if (user == null) {
                // create a new user + player + character
                user = new IdentityUser(hello.Username);
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded) {
                    try {
                        var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();
                        var dbSave = await context.Games
                            .Include(g => g.Levels)
                            .FirstAsync();
                        var dbLevel = dbSave.Levels!.First();
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
                        spawnPosition = new Vector3(dbLevel.SpawnPointX, dbLevel.SpawnPointY, dbLevel.SpawnPointZ);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        await userManager.DeleteAsync(user);
                        throw;
                    }
                } else {
                    throw new ApplicationException($"Server error: Failed to find or create user {hello.Username}.");
                }
            } else {
                var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();
                var player = await context.Players
                    .Include(p => p.Characters)
                    .ThenInclude(c => c.Level)
                    .FirstOrDefaultAsync(p => p.IdentityUser!.Id == user.Id);
                if (player == null) {
                    throw new ApplicationException("All users should have a player on creation. Ask an admin !");
                    // TODO: remove user if no player found so that it can re-creates ?
                }

                var dbCharacter = player.Characters.First();
                character = MessagePackSerializer.Deserialize<Character>(dbCharacter.SerializedData);
                spawnPosition = new Vector3(dbCharacter.Level!.SpawnPointX, dbCharacter.Level!.SpawnPointY, dbCharacter.Level!.SpawnPointZ);
            }

            return (character, spawnPosition);
        }

        public void ScheduleChunkUpload(ushort playerKey, string levelId, int chX, int chZ) {
            var range = 3;
            UserSessionData? userSessionData;
            lock (_userSessionData) {
                userSessionData = _userSessionData
                    .Select(u => u.Value)
                    .FirstOrDefault(u => u.ShortId == playerKey);
            }

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

        public void SendScheduledChunks() {
            lock (_userSessionData) {
                foreach (var (userKey, userSessionData) in _userSessionData) {
                    // try dequeue one chunk per user per tick
                    if (userSessionData.UploadQueue.TryDequeue(out var cKey, out _)) {
                        var chunk = _state.Levels[cKey.LevelId].Chunks[cKey.ChX, cKey.ChZ];
                        Send(userKey, new ChunkUpdateGameEvent(0, cKey.LevelId, chunk, cKey.ChX, cKey.ChZ));
                    }
                }
            }
        }
    }
}