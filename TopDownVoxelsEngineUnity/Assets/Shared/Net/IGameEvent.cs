using LoneStoneStudio.Tools;

namespace Shared.Net {
    public interface IGameEvent {
        public int GetId();
        public void Apply(GameState gameState, SideEffectManager? sideEffectManager);
        public void AssertApplicationConditions(GameState gameState);
    }
}