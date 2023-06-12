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
        public IWebSocketManager SocketManager = new FakeEchoingSocketManager();
        public bool IsLocalEngine() => SocketManager is FakeEchoingSocketManager;

        [ShowInInspector]
        public GameState State = new(null, null, null);

        public readonly SideEffectManager SideEffectManager = new();

        private readonly TickGameEvent _tick = new();
        private PriorityLevel _minLevel = PriorityLevel.All;
        private bool _started;

        private void HandleNetMessage(INetworkMessage obj) {
            switch (obj) {
                case IGameEvent gameEvent:
                    HandleEvent(gameEvent);
                    break;
            }
        }

        public void Start() {
            _started = true;
            SocketManager.OnNetworkMessage -= HandleNetMessage;
            SocketManager.OnNetworkMessage += HandleNetMessage;
            SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
            SideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
        }

        public void Stop() {
            _started = false;
            SocketManager.OnNetworkMessage -= HandleNetMessage;
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
                        State.LevelGenerator.EnqueueChunksAround(c.Level.Value, chx, chz, 3, State.Levels);
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

        public async Task InitRemote(string serverURL) {
            SocketManager = new WebSocketManager();
            SocketManager.OnNetworkMessage += HandleNetMessage;
            await SocketManager.Init(serverURL);
            await SocketManager.Send(new HelloNetworkMessage("Coucou toi"));
        }
    }
}