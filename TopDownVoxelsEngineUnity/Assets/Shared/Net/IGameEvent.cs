using LoneStoneStudio.Tools;

namespace Shared.Net {
    public interface IGameEvent : INetworkMessage {
        public int GetId();
        public void Apply(GameState gameState, SideEffectManager? sideEffectManager);
        public void AssertApplicationConditions(in GameState gameState);
    }
}