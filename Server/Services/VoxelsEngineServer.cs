using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using MessagePack;
using Microsoft.AspNetCore.Identity;
using Nerdbank.Streams;
using Server.DbModel;
using Shared;
using Shared.Net;
using Chunk = Server.DbModel.Chunk;

namespace Server {
    public class VoxelsEngineServer {
        // Data
        private readonly UserManager<IdentityUser> _userManager;
        private GameState? _state;
        private GameState _stateBackup = new();
        private readonly GameSavesContext _context;
        private readonly Game _currentSave;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        // Running
        public int Frames { get; private set; } = 0;
        public int SimulatedFrameTime { get; set; } = 0; // For testing

        private const int TargetFrameTime = 20; // 20ms per frame = 50 FPS
        private const int MaxCatchUpTime = 15; // up to 300ms delay is acceptable. Beyond that we throttle.

        private int _catchUpTime = 0;
        private PriorityLevel _minimumPriority = PriorityLevel.All;
        private readonly Stopwatch _frameStopwatch = new();

        private readonly TickGameEvent _tick = new() {
            Id = -1,
            MinPriority = PriorityLevel.All
        };

        public VoxelsEngineServer(GameSavesContext gameSavesContext, UserManager<IdentityUser> userManager) {
            try {
                _userManager = userManager;
                _context = gameSavesContext;
                var save = _context.Games.FirstOrDefault();
                if (save == null) {
                    save = InitNewGame();
                    _context.Games.Add(save);
                    _context.SaveChanges();
                }
                _currentSave = save;
                _state.Levels
                    TODO: init save from initial state ? Create initial save ? Should it be empty and load on demand ?
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;
                StartFixedUpdateAsync().Forget();
            } catch (Exception e) {
                throw new ApplicationException("Could not start Server", e);
            }
        }

        private static Game InitNewGame() {
            Console.WriteLine("Generating a new GameState");
            Game save = new Game {
                Seed = Random.Shared.Next(),
                DataVersion = 1,
                Levels = new List<Level> {
                    new() {
                        Name = "Lobby",
                        Seed = Random.Shared.Next(),
                        Chunks = new List<Chunk>(),
                        // spawn point initialized on the middle of the middle chunk
                        SpawnPointX = LevelMap.LevelChunkSize * Shared.Chunk.Size + Shared.Chunk.Size / 2,
                        SpawnPointY = Shared.Chunk.Size / 2,
                        SpawnPointZ = LevelMap.LevelChunkSize * Shared.Chunk.Size + Shared.Chunk.Size / 2,
                    }
                },
            };
            
            _engine.State.LevelGenerator.EnqueueChunksAround("World", spawnPositionChX, spawnPositionChZ, 5, _engine.State.Levels);
            _engine.State.LevelGenerator.GenerateFromQueue(PriorityLevel.LoadingTime, _engine.State.Levels);

            return save;
        }

        private async UniTask StartFixedUpdateAsync() {
            SideEffectManager sideEffectManager = new SideEffectManager();
            sideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
            while (!_cancellationToken.IsCancellationRequested) {
                await Tick(_cancellationToken, sideEffectManager);
            }

            sideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
        }

        private void UpdatePriorityLevel(PriorityLevel level) {
            _minimumPriority = level;
        }

        public async UniTask Tick(CancellationToken cancellationToken, SideEffectManager sideEffectManager) {
            if (_state == null) return;
            _frameStopwatch.Restart();

            _tick.MinPriority = _minimumPriority;
            _tick.Apply(_state, sideEffectManager);

            // Simulate work being done for a frame for test purpose
            if (SimulatedFrameTime > 0) await Task.Delay(SimulatedFrameTime, cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            Frames++;

            var elapsed = _frameStopwatch.ElapsedMilliseconds;
            var remaining = TargetFrameTime - (int) elapsed;
            if (remaining > 0) {
                // We finished early, wait the remaining time or try to catch up
                if (_catchUpTime > 0) {
                    // We have time to catch up !
                    if (remaining > _catchUpTime) {
                        // Cover the catchUp immediately and wait the remaining of that
                        await Task.Delay(remaining - _catchUpTime, cancellationToken);
                        _catchUpTime = 0;
                        // we can cover all processing needs
                        _minimumPriority = PriorityLevel.All;
                    } else {
                        // can't full cover the catch up, deduce the time no waited and keep going immediatly
                        _catchUpTime -= remaining;

                        if (_catchUpTime > MaxCatchUpTime) {
                            // we are very late, try to catch up doing only essential processing
                            _minimumPriority = PriorityLevel.Must;
                        } else {
                            // we are somewhat late, try to avoid less important processing
                            _minimumPriority = PriorityLevel.Should;
                        }
                    }
                } else {
                    // we are in time, wait for the next tick
                    await Task.Delay(remaining, cancellationToken);
                }
            } else {
                // We're behind schedule (this tick took too long), so skip waiting to catch up
                // and register how late we are
                _catchUpTime -= remaining;
            }
        }
        
        // TODO: gérer la connexion d'un nouveau joueur (créer un nouveau personnage)
        // TODO: gérer la connexion joueur existant
        // - identité
        // - récupérer personnage


        public async UniTask HandleMessage(
            INetworkMessage netMessage,
            Func<INetworkMessage, bool> answer,
            Func<INetworkMessage, bool> broadcast
        ) {
            try {
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
                        if (_state == null) {
                            // ReSharper disable once InconsistentlySynchronizedField
                            _state = newGame.GameState;
                            broadcast(newGame);
                        } else {
                            if (newGame.GameState != null) {
                                lock (_state) {
                                    _state.UpdateValue(newGame.GameState);
                                    broadcast(newGame);
                                }
                            }
                        }

                        Console.WriteLine("Game State reset !");
                        break;
                    case HelloNetworkMessage hello:
                        Console.WriteLine("A client said hello : " + hello.Username);
                        var user = await _userManager.FindByNameAsync(hello.Username);
                        if (user == null) {
                            user = new IdentityUser(hello.Username);
                            var result = await _userManager.CreateAsync(user);
                            if (result.Succeeded) {
                                answer(hello);
                            } else {
                                answer(new ErrorNetworkMessage($"Server error: Failed to find or create user {hello.Username}."));
                            }
                        }

                        break;
                }

                // backup the state before applying
                if (_state != null) {
                    lock (_state) {
                        _stateBackup.UpdateValue(_state);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine($"An error occured with message {netMessage.GetType().Name}. Rolling state back.\n" +
                                  e);

                // treat the event as a transaction, cancel any partially applied event
                if (_state != null) {
                    lock (_state) {
                        _state.UpdateValue(_stateBackup);
                    }
                }
            }
        }
    }
}