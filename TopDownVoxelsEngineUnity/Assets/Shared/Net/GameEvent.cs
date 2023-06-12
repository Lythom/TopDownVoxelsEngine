using LoneStoneStudio.Tools;

namespace Shared.Net
{
    public abstract class GameEvent : IGameEvent, INetworkMessage
    {
        public abstract int GetId();

        public void Apply(GameState gameState, SideEffectManager? sideEffectManager)
        {
            gameState.ApplyEvent(DoApply, sideEffectManager);
        }

        protected internal abstract void DoApply(GameState gameState, SideEffectManager? sideEffectManager);
        public abstract void AssertApplicationConditions(in GameState gameState);
    }
}