using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
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
        private readonly GameState _state = new(null, null);
        private readonly GameState _stateBackup = new(null, null);
        private readonly Registry<BlockConfigJson> _blockRegistry;

        private readonly ConcurrentDictionary<ushort, UserSessionData> _userSessionData = new();

        // Running
        private bool _isReady = false;
        public bool IsReady => _isReady;
        public GameState State => _state;
        public int Port { get; private set; }

        private readonly SocketServer _socketServer;
        private readonly ConcurrentQueue<InputMessage> _inbox = new();
        private readonly ConcurrentQueue<OutputMessage> _outbox = new();
        private readonly ConcurrentDictionary<ChunkKey, Chunk> _dirtyChunks = new();
        private ServerClock _serverClock;
        private readonly CancellationTokenSource _cts;

        public VoxelsEngineServer(
            IServiceScopeFactory serviceScopeFactory,
            SocketServer socketServer,
            Registry<BlockConfigJson> blockRegistry
        ) {
            _blockRegistry = blockRegistry;

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
            Port = port;
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

            _state.UpdateBlockMapping(_blockRegistry);
            InitState(dbSave);
            SubscribeRemoveSessionOnCharacterLeave(_state);

            _socketServer.OnNetworkMessage += HandleMessage;
            _socketServer.OnOpen += NotifyConnection;
            _socketServer.OnClose += NotifyDisconnection;
            _socketServer.Init(port);

            _serverClock = new ServerClock(this);
            _serverClock.StartFixedUpdateAsync(_inbox).Forget();
            StartNetworkSendingAsync().Forget();

            _isReady = true;
            Logr.Log("Server ready!");
        }

        private void SubscribeRemoveSessionOnCharacterLeave(GameState gameState) {
            gameState.Characters.ForEachAsync(operation => {
                try {
                    if (operation.Event.Action == NotifyCollectionChangedAction.Remove) {
                        if (operation.Event.IsSingleItem) {
                            var key = operation.Event.OldItem.Key;
                            _userSessionData.TryRemove(key, out var data);
                            Logr.Log($"User left {data?.Name} ({key})", Tags.Server);
                        } else {
                            foreach (var (key, value) in operation.Event.OldItems) {
                                _userSessionData.TryRemove(key, out _);
                            }
                        }
                    }
                } catch (Exception e) {
                    Logr.LogException(e);
                }
            }, _cts.Token).Forget();
        }

        public static ushort id = 0;

        private async UniTask StartNetworkSendingAsync() {
            while (!_cts.Token.IsCancellationRequested) {
                bool hasMessage = false;
                hasMessage = _outbox.TryDequeue(out var m);
                try {
                    if (hasMessage) {
                        if (m.IsBroadcast) {
                            await _socketServer.Broadcast(m.Message);
                        } else {
                            await _socketServer.Send(m.RecipientId, m.Message);
                        }
                    } else {
                        await UniTask.Yield();
                    }
                } catch (Exception) {
                    if (hasMessage) Logr.LogError($"A message {m.Message.GetType().Name} was not sent to {m.RecipientId}");
                }
            }
        }

        private async UniTask StartPersistingAsync(DbGame currentDbSave) {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();

            if (currentDbSave == null || currentDbSave.Levels == null) {
                throw new Exception("The game save should include levels");
            }

            Dictionary<string, Guid> levelsByName = new();

            void UpdateLevelsRefs() {
                foreach (var dbLevel in context.Levels.Where(l => l.GameId == currentDbSave.GameId)) {
                    levelsByName[dbLevel.Name] = dbLevel.LevelId;
                }
            }

            UpdateLevelsRefs();

            while (!_cts.Token.IsCancellationRequested) {
                bool hasDirtyChunks = _dirtyChunks.Count > 0;
                try {
                    if (hasDirtyChunks) {
                        var dirtyChunksCopy = new List<KeyValuePair<ChunkKey, Chunk>>(_dirtyChunks);
                        _dirtyChunks.Clear();
                        foreach (var (chunkKey, chunk) in dirtyChunksCopy) {
                            if (!levelsByName.ContainsKey(chunkKey.LevelId)) UpdateLevelsRefs();
                            var levelId = levelsByName[chunkKey.LevelId];
                            var dbChunk = context.Chunks.SingleOrDefault(c => c.ChX == chunkKey.ChX && c.ChZ == chunkKey.ChZ && c.LevelId == levelId);
                            if (dbChunk == null) {
                                dbChunk = new DbChunk {
                                    Cells = MessagePackSerializer.Serialize(chunk.Cells),
                                    IsGenerated = chunk.IsGenerated,
                                    LevelId = levelId,
                                    ChX = (short) chunkKey.ChX,
                                    ChZ = (short) chunkKey.ChZ
                                };
                                context.Chunks.Add(dbChunk);
                                Logr.Log(context.Entry(dbChunk).State.ToString());
                            } else {
                                dbChunk.Cells = MessagePackSerializer.Serialize(chunk.Cells);
                                dbChunk.IsGenerated = chunk.IsGenerated;
                            }
                        }

                        await using var transaction = await context.Database.BeginTransactionAsync();
                        try {
                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            Logr.Log("chunks updated");
                        } catch (Exception ex) {
                            await transaction.RollbackAsync();
                            Logr.LogError($"transaction failed. Exception: {ex.Message}");
                        }
                    } else {
                        await UniTask.Yield();
                    }
                } catch (Exception ex) {
                    Logr.LogError($"Could not persist {_dirtyChunks.Count} chunks. Exception: {ex.Message}");
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

        private void Send(ushort id, INetworkMessage m) {
            if (m == null) throw new InvalidOperationException("message must not be null");
            _outbox.Enqueue(new OutputMessage(id, m));
        }

        private void SmartBroadcast(INetworkMessage m) {
            if (m == null) throw new InvalidOperationException("message must not be null");
            switch (m) {
                case ChangeBlockGameEvent changeBlockGameEvent:
                    BroadcastBut(changeBlockGameEvent.CharacterShortId, m);
                    break;
                case CharacterJoinGameEvent characterJoinGameEvent:
                    BroadcastBut(characterJoinGameEvent.CharacterShortId, m);
                    Send(characterJoinGameEvent.CharacterShortId, m);
                    break;
                case CharacterLeaveGameEvent characterLeaveGameEvent:
                    BroadcastBut(characterLeaveGameEvent.CharacterShortId, m);
                    break;
                case CharacterMoveGameEvent characterMoveGameEvent:
                    BroadcastBut(characterMoveGameEvent.CharacterShortId, m);
                    break;
                case PlaceBlocksGameEvent placeBlocksGameEvent:
                    BroadcastBut(e => {
                        if (!State.Characters.TryGetValue(e.CharacterShortId, out var c)) return false;
                        var levelId = c.Level.Value;
                        if (levelId == null) return false;
                        if (!_userSessionData.TryGetValue(e.CharacterShortId, out var userSessionData)) return false;
                        var (chX, chZ) = LevelTools.GetChunkPosition(e.X, e.Z);
                        var chunkKey = ChunkKeyPool.Get(levelId, chX, chZ);
                        var shouldSend = userSessionData.UploadedChunks.Contains(chunkKey);
                        ChunkKeyPool.Return(chunkKey);
                        return shouldSend;
                    }, placeBlocksGameEvent);
                    break;
                case TickGameEvent:
                case ChunkUpdateGameEvent:
                    // never send tick event
                    break;
                default:
                    _outbox.Enqueue(new OutputMessage(m));
                    break;
            }
        }

        private void BroadcastBut<T>(Func<T, bool> shouldEnqueue, T m) where T : INetworkMessage {
            foreach (var (key, value) in _userSessionData) {
                if (shouldEnqueue(m)) {
                    _outbox.Enqueue(new OutputMessage(key, m));
                }
            }
        }

        private void BroadcastBut(ushort ignoredShortId, INetworkMessage m) {
            // if (m is not CharacterMoveGameEvent) Logr.Log($"BroadcastBut({ignoredShortId}, {m.GetType().Name})", Tags.Debug);

            foreach (var (key, value) in _userSessionData) {
                if (key != ignoredShortId && _userSessionData.TryGetValue(key, out var recipient) && recipient.IsLogged) {
                    // if (m is not CharacterMoveGameEvent) Logr.Log($"↓ Enqueued for {key}", Tags.Debug);
                    _outbox.Enqueue(new OutputMessage(key, m));
                }
            }
        }

        public void NotifyDisconnection(ushort shortId) {
            if (!_userSessionData.ContainsKey(shortId)) return;
            if (_userSessionData.TryGetValue(shortId, out var userData) && userData.IsLogged) {
                var characterLeaveGameEvent = new CharacterLeaveGameEvent(0, shortId);
                characterLeaveGameEvent.Apply(State, null);
                SmartBroadcast(characterLeaveGameEvent);

                // If any unsent element for this user, cancel.
                RemoveUserMessagesFromOutbox(shortId);
            }
        }

        private void RemoveUserMessagesFromOutbox(ushort shortId) {
            var tmpQueue = new Queue<OutputMessage>();
            while (_outbox.TryDequeue(out var e)) {
                if (e.RecipientId != shortId) tmpQueue.Enqueue(e);
            }

            while (tmpQueue.TryDequeue(out var e)) {
                _outbox.Enqueue(e);
            }
        }

        public void NotifyConnection(ushort shortId) {
            _userSessionData.TryAdd(shortId, new UserSessionData(shortId));
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
            StartPersistingAsync(currentDbSave).Forget();
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

        private void HandleMessage(ushort clientShortId, INetworkMessage netMessage) {
            // if (netMessage is PlaceBlocksGameEvent) Logr.Log("Handled and enqueued " + netMessage);
            _inbox.Enqueue(new InputMessage {Id = clientShortId, Message = netMessage});
        }

        public async UniTask HandleMessageAsync(InputMessage m) {
            var clientShortId = m.Id;
            var netMessage = m.Message;
            if (!_userSessionData.ContainsKey(clientShortId)) {
                Logr.Log($"Client {clientShortId} is no longer ready to share messages.");
                return;
            }

            try {
                // if (netMessage is not CharacterMoveGameEvent) Logr.Log("Received message: " + netMessage);

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
                        evt.Apply(_state, null);

                        SmartBroadcast(evt);

                        if (evt is PlaceBlocksGameEvent pbgm) {
                            var (chX, chZ) = LevelTools.GetChunkPosition(pbgm.X, pbgm.Z);
                            var level = _state.Characters[pbgm.CharacterShortId].Level.Value;
                            if (level != null) _dirtyChunks[new ChunkKey(level, chX, chZ)] = _state.Levels[level].Chunks[chX, chZ];
                        }

                        break;
                    case HelloNetworkMessage hello: {
                        Console.WriteLine("A client said hello : " + hello.Username);
                        if (_userSessionData.Any(d => d.Value.Name == hello.Username)) {
                            Send(clientShortId, new ErrorNetworkMessage($"A player named {hello.Username} is already logged in."));
                            return;
                        } else {
                            _userSessionData[clientShortId].Name = hello.Username;
                            _userSessionData[clientShortId].Status = SessionStatus.GettingReady;
                        }

                        using var scope = _serviceScopeFactory.CreateScope();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                        var user = await userManager.FindByNameAsync(hello.Username);
                        try {
                            var (character, levelSpawn) = await GetOrCreateCharacterAsync(user, hello);
                            var characterJoinGameEvent = new CharacterJoinGameEvent(0, clientShortId, character, levelSpawn);
                            characterJoinGameEvent.AssertApplicationConditions(in _state);
                            characterJoinGameEvent.Apply(State, null);

                            // send the new player infos about the already existing players
                            foreach (var characterShortId in State.Characters.Keys) {
                                if (characterShortId != clientShortId) {
                                    // except for himself
                                    Send(clientShortId, new CharacterJoinGameEvent(0, characterShortId, State.Characters[characterShortId], Vector3.zero));
                                }
                            }

                            // Send all players info about the new players.
                            // It also confirms last to the new players it's entry
                            SmartBroadcast(characterJoinGameEvent);
                            _userSessionData[clientShortId].Status = SessionStatus.Ready;
                        } catch (Exception e) {
                            Send(clientShortId, new ErrorNetworkMessage(e.Message));
                            Console.WriteLine(e.ToString());
                            return;
                        }

                        break;
                    }
                }

                // backup the state before applying
                lock (_state.LockObject) {
                    _stateBackup.UpdateValue(_state);
                }
            } catch (Exception e) {
                Console.WriteLine($"An error occured with message {netMessage.GetType().Name}. Rolling state back.\n" + e);

                // treat the event as a transaction, cancel any partially applied event
                if (_state != null) {
                    lock (_state.LockObject) {
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
                }

                var dbCharacter = player.Characters.First();
                character = MessagePackSerializer.Deserialize<Character>(dbCharacter.SerializedData);
                spawnPosition = new Vector3(dbCharacter.Level!.SpawnPointX, dbCharacter.Level!.SpawnPointY, dbCharacter.Level!.SpawnPointZ);
            }

            return (character, spawnPosition);
        }

        public void ScheduleChunkUpload(ushort playerKey, string levelId, int chX, int chZ) {
            var range = 3;
            var userSessionData = _userSessionData
                .Select(u => u.Value)
                .SingleOrDefault(u => u.ShortId == playerKey);

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

        public void TryGenerateChunks(PriorityLevel priority) {
            foreach (var (key, c) in State.Characters) {
                var (chx, chz) = LevelTools.GetChunkPosition(c.Position);
                if (c.Level.Value != null && State.Levels.ContainsKey(c.Level.Value)) {
                    State.LevelGenerator.EnqueueUninitializedChunksAround(c.Level.Value, chx, chz, 4, State.Levels);
                    ScheduleChunkUpload(key, c.Level.Value, chx, chz);
                }
            }

            State.LevelGenerator.GenerateFromQueue(priority, State.Levels);
        }

        public void SendScheduledChunks() {
            foreach (var (userKey, userSessionData) in _userSessionData) {
                // try dequeue one chunk per user per tick
                if (userSessionData.IsLogged && userSessionData.UploadQueue.TryDequeue(out var cKey, out _)) {
                    var chunk = _state.Levels[cKey.LevelId].Chunks[cKey.ChX, cKey.ChZ];
                    Send(userKey, new ChunkUpdateGameEvent(0, cKey.LevelId, chunk, cKey.ChX, cKey.ChZ));
                }
            }
        }
    }
}