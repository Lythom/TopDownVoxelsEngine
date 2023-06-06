using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;

namespace Shared {
    public static class GameLoop {
        public static async UniTask Tick(GameState state, PriorityLevel priorityLevel, LevelGenerator levelGenerator, CancellationToken cancellationToken, SideEffectManager? sideEffectManager) {
            try {
               
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }
    }
}