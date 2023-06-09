using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared;
using Shared.Net;
using UnityEngine;

namespace VoxelsEngine {
    public class ClientEngine : MonoBehaviour {
        public IWebSocketManager SocketManager = new FakeEchoingSocketManager();
        public readonly GameState State = new();
        public readonly SideEffectManager SideEffectManager = new();

        private readonly TickGameEvent _tick = new();
        private PriorityLevel _minLevel = PriorityLevel.All;

        private void Awake() {
            SideEffectManager.For<PriorityLevel>().AddTo(gameObject.GetCancellationTokenOnDestroy());
            SideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
        }

        private void UpdatePriorityLevel(PriorityLevel l) {
            _minLevel = l;
        }

        private void FixedUpdate() {
            _tick.Id++;
            _tick.MinPriority = _minLevel;
            _tick.Apply(State, SideEffectManager);

            var c = State.Characters[LocalState.Instance.CurrentPlayerId];
            SocketManager.Send(
                new CharacterMoveGameEvent(
                    0,
                    LocalState.Instance.CurrentPlayerId,
                    c.Position,
                    c.Velocity,
                    c.Angle
                )
            ).Forget();
        }

        public void SendEvent(IGameEvent evt) {
            evt.Apply(State, SideEffectManager);
        }
    }
}