using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;
using Nerdbank.Streams;
using Server.DbModel;
using Shared;
using Shared.Net;

namespace Server {
    public class VoxelsEngineServer {
        private GameState? _state;
        private readonly GameSavesContext _ctx;
        private bool _stateDirty;
        private readonly Sequence<byte> _stateBackup = new();

        private readonly SemaphoreSlim _handleMessageSemaphore = new(1, 1);
        private readonly Save _ctxSave;

        public VoxelsEngineServer() {
            try {
                _ctx = new GameSavesContext();
                var save = _ctx.Saves.FirstOrDefault();
                if (save != null) {
                    try {
                        if (save.SerializedGame?.Length > 0)
                            _state = MessagePackSerializer.Deserialize<GameState>(save.SerializedGame);
                    } catch (Exception e) {
                        Console.WriteLine("Could not deserialize gameState from server, clearing the save.");
                        Console.WriteLine(e);
                        save.SerializedGame = Array.Empty<byte>();
                    }
                } else {
                    save = new Save {
                        SerializedGame = Array.Empty<byte>(),
                    };
                    _ctx.Saves.Add(save);
                }

                _ctxSave = new Save {
                    SerializedGame = Array.Empty<byte>(),
                };
            } catch (Exception e) {
                throw new ApplicationException("Could not start Server", e);
            }
        }
        
        // TODO: démarrer un serveur qui tick tout le temps, nouvelle partie si pas de partie
        // TODO: gérer la connexion d'un nouveau joueur (créer un nouveau personnage)
        // TODO: gérer la connexion joueur existant
        // - identité
        // - récupérer personnage
        

        public async UniTask HandleMessageAsync(
            INetworkMessage netMessage,
            Func<INetworkMessage, bool> answer,
            Func<INetworkMessage, bool> broadcast
        ) {
            try {
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

                        _stateDirty = true;
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
                        _stateDirty = true;
                        break;
                    case HelloNetworkMessage hello:
                        Console.WriteLine("A client said hello : " + hello.Hey);
                        if (_state == null) break;
                        lock (_state) answer(new NewGameNetworkMessage(_state));
                        break;
                }

                _stateBackup.Dispose(); // TODO: have a test for the backup reload
                if (_state != null) {
                    lock (_state) MessagePackSerializer.Serialize(_stateBackup, _state);
                }

                if (_stateDirty) {
                    await SaveStateAsync(_stateBackup.AsReadOnlySequence);
                }
            } catch (Exception e) {
                Console.WriteLine($"An error occured with message {netMessage.GetType().Name}. Rolling state back.\n" +
                                  e);
                lock (_state!) {
                    _state = MessagePackSerializer.Deserialize<GameState>(_stateBackup.AsReadOnlySequence);
                    broadcast(new NewGameNetworkMessage(_state));
                }
            }
        }

        private async UniTask SaveStateAsync(ReadOnlySequence<byte> stateBackup) {
            try {
                _ctxSave.SerializedGame = stateBackup.ToArray();
                await _ctx.SaveChangesAsync();
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}