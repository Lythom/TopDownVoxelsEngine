using System;
using System.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    /// <summary>
    ///  Le rôle de client engine est de
    /// - servir de serveur au client local sans qu'il ait besoin de passer par un serveur distant.
    /// - Simuler les actions d'un serveur localement (synchronisation d'états)
    /// </summary>
    public class ClientEngine : MonoBehaviour {
        public ISocketClient SocketClient = new FakeEchoingSocketClient();
        public bool IsLocalEngine() => SocketClient is FakeEchoingSocketClient;

        [ShowInInspector]
        public GameState State = new(null, null, null);

        public readonly SideEffectManager SideEffectManager = new();

        private readonly TickGameEvent _tick = new();
        private PriorityLevel _minLevel = PriorityLevel.All;
        private bool _started;

        private void HandleNetMessage(INetworkMessage obj) {
            Logr.Log("Received " + obj);
            switch (obj) {
                case CharacterJoinGameEvent joinEvent:
                    if (joinEvent.Character.Name == LocalState.Instance.CurrentPlayerName) {
                        _started = true;
                        SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
                        SideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
                    }

                    HandleEvent(joinEvent);
                    SideEffectManager.For<CharacterJoinGameEvent>().Trigger(joinEvent);
                    break;
                case IGameEvent gameEvent:
                    HandleEvent(gameEvent);
                    break;
                case ErrorNetworkMessage err:
                    Debug.LogError("[Server Error] " + err.Message);
                    break;
            }
        }

        public void StartLocal() {
            _started = true;
            SocketClient.OnNetworkMessage -= HandleNetMessage;
            SocketClient.OnNetworkMessage += HandleNetMessage;
            SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
            SideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
        }

        public void Stop() {
            _started = false;
            SocketClient.OnNetworkMessage -= HandleNetMessage;
            SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
        }

        private void UpdatePriorityLevel(PriorityLevel l) {
            _minLevel = l;
        }

        private void FixedUpdate() {
            if (!_started) return;
            _tick.Id++;
            _tick.MinPriority = _minLevel;
            _tick.Apply(State, SideEffectManager);

            // When this is run by the client for himself, ne need to ask server
            if (IsLocalEngine()) {
                foreach (var (key, c) in State.Characters) {
                    var (chx, chz) = LevelTools.GetChunkPosition(c.Position);
                    if (c.Level.Value != null && State.Levels.ContainsKey(c.Level.Value)) {
                        State.LevelGenerator.EnqueueUninitializedChunksAround(c.Level.Value, chx, chz, 3, State.Levels);
                    }
                }

                State.LevelGenerator.GenerateFromQueue(_minLevel, State.Levels);
            }

            // var c = State.Characters[LocalState.Instance.CurrentPlayerId];
            // SocketManager.Send(
            //     new CharacterMoveGameEvent(
            //         0,
            //         LocalState.Instance.CurrentPlayerId,
            //         c.Position,
            //         c.Velocity,
            //         c.Angle
            //     )
            // ).Forget();
        }

        public void HandleEvent(IGameEvent evt) {
            evt.Apply(State, SideEffectManager);
        }

        // TODO: auto reconnexion

        public async Task InitRemote(int port) {
            SocketClient = new SocketClient();
            SocketClient.OnNetworkMessage += HandleNetMessage;
            await SocketClient.Init("localhost", port);
            await Task.Delay(500);
            await SocketClient.Send(new HelloNetworkMessage(LocalState.Instance.CurrentPlayerName));
        }

        private void OnDestroy() {
            if (SocketClient != null) {
                SocketClient.Close();
            }
        }
    }
}