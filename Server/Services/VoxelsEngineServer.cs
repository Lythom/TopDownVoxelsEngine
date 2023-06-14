using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using LoneStoneStudio.Tools;
using MessagePack;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Server.DbModel;
using Shared;
using Shared.Net;

namespace Server {
    public class VoxelsEngineServer : IHostedService {
        // Data
        private readonly UserManager<IdentityUser> _userManager;
        private GameState _state = new();
        private GameState _stateBackup = new();
        private readonly GameSavesContext _context;
        private DbGame? _currentDbSave;
        private readonly WebSocketMessagingQueue _webSocketMessagingQueue;

        private ReactiveDictionary<ushort, string> _connectedCharacters = new();
        private readonly Dictionary<WebSocket, UserData> _websocketData = new();

        private ushort _nextShortId = 0;

        private ushort GetNextCharacterShortId() {
            while (_connectedCharacters.ContainsKey(_nextShortId)) _nextShortId++;
            return _nextShortId;
        }

        // Running
        private bool _isReady = false;
        public bool IsReady => _isReady;
        public GameState State => _state;

        private ServerClock _serverClock;

        public VoxelsEngineServer(GameSavesContext gameSavesContext, UserManager<IdentityUser> userManager, WebSocketMessagingQueue webSocketMessagingQueue) {
            try {
                _webSocketMessagingQueue = webSocketMessagingQueue;
                _userManager = userManager;
                _context = gameSavesContext;
                _serverClock = new ServerClock(this);
            } catch (Exception e) {
                throw new ApplicationException("Could not start Server", e);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            var dbSave = await _context.Games
                .Include(g => g.Levels)
                .ThenInclude(l => l.Chunks)
                .FirstOrDefaultAsync(cancellationToken);
            if (dbSave == null) {
                dbSave = InitNewGame();
                _context.Games.Add(dbSave);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _currentDbSave = dbSave;

            await InitStateAsync(_currentDbSave);

            _connectedCharacters.ForEachAwaitAsync(async action => {
                switch (action.Event) {
                    case {Action: NotifyCollectionChangedAction.Add, IsSingleItem: true, NewItem: var item}:
                        var dbCharacter = await _context.Characters.SingleOrDefaultAsync(c => c.Name == item.Value, cancellationToken: cancellationToken);
                        if (dbCharacter == null) return;
                        var character = MessagePackSerializer.Deserialize<Character>(dbCharacter.SerializedData);
                        _webSocketMessagingQueue.Broadcast(new CharacterJoinGameEvent(0, item.Key, character));
                        break;
                    case {Action: NotifyCollectionChangedAction.Remove, IsSingleItem: true, NewItem: var item}:
                        _webSocketMessagingQueue.Broadcast(new CharacterLeaveGameEvent(0, item.Key));
                        break;
                }
            }, cancellationToken: cancellationToken);

            _serverClock = new ServerClock(this);
            _serverClock.StartFixedUpdateAsync().Forget();
            _isReady = true;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _isReady = false;
            _serverClock.Stop();
            return Task.CompletedTask;
        }

        public void NotifyDisconnection(WebSocket webSocket) {
            if (!_websocketData.ContainsKey(webSocket)) return;
            var userData = _websocketData[webSocket];
            if (userData.IsLogged) {
                _connectedCharacters.Remove(userData.ShortId);
            }
        }

        public void NotifyConnection(WebSocket webSocket) {
            _websocketData.Add(webSocket, new UserData(false, 0));
        }

        private async UniTask InitStateAsync(DbGame currentDbSave) {
            // load levels
            foreach (var dbLevel in currentDbSave.Levels) {
                var levelMap = new LevelMap(dbLevel.Name);
                foreach (var dbChunk in dbLevel.Chunks.Where(c => c.IsGenerated)) {
                    levelMap.Chunks[dbChunk.ChX, dbChunk.ChZ] = new() {
                        Cells = MessagePackSerializer.Deserialize<Cell[,,]>(dbChunk.Cells),
                        IsGenerated = true
                    };
                }

                _state.Levels.Add(dbLevel.Name, levelMap);
            }
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
                        SpawnPointX = LevelMap.LevelChunkSize * Chunk.Size + Chunk.Size / 2,
                        SpawnPointY = Chunk.Size / 2,
                        SpawnPointZ = LevelMap.LevelChunkSize * Chunk.Size + Chunk.Size / 2,
                    }
                },
            };

            return save;
        }

        public async UniTask HandleMessageAsync(
            INetworkMessage netMessage,
            Func<INetworkMessage, bool> answer,
            Func<INetworkMessage, bool> broadcast
        ) {
            try {
                if (!IsReady) {
                    answer(new ErrorNetworkMessage($"Server not ready. Please wait and retry."));
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

                        broadcast((INetworkMessage) evt);
                        break;
                    case NewGameNetworkMessage newGame:
                        if (newGame.GameState != null) {
                            lock (_state) {
                                _state.UpdateValue(newGame.GameState);
                                broadcast(newGame);
                            }
                        }

                        Console.WriteLine("Game State reset !");
                        break;
                    case HelloNetworkMessage hello:
                        Console.WriteLine("A client said hello : " + hello.Username);
                        var user = await _userManager.FindByNameAsync(hello.Username);
                        try {
                            var character = await GetOrCreateCharacterAsync(user, hello);
                            _connectedCharacters.Add(GetNextCharacterShortId(), character.Name);
                        } catch (Exception e) {
                            answer(new ErrorNetworkMessage(e.Message));
                            return;
                        }

                        // TODO: broadcast add player with PlayerID short value so that we can yse a dictionary again
                        answer(hello);


                        break;
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
            Character character;
            if (user == null) {
                // create a new user + player + character
                user = new IdentityUser(hello.Username);
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded) {
                    var dbLevel = _currentDbSave!.Levels.First();
                    var pos = new Vector3(dbLevel.SpawnPointX, dbLevel.SpawnPointY, dbLevel.SpawnPointZ);
                    character = new Character(hello.Username, pos, dbLevel.Name);
                    var player = new DbPlayer {
                        IdentityUser = user,
                        Characters = new List<DbCharacter> {
                            new() {
                                Name = hello.Username,
                                DbLevel = dbLevel,
                                X = pos.X,
                                Y = pos.Y,
                                Z = pos.Z,
                                SerializedData = MessagePackSerializer.Serialize(character)
                            }
                        }
                    };
                    _context.Players.Add(player);
                    await _context.SaveChangesAsync();
                } else {
                    throw new ApplicationException($"Server error: Failed to find or create user {hello.Username}.");
                }
            } else {
                var player = await _context.Players
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
    }
}