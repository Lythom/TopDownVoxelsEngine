using System;
using System.Collections.Generic;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared {
    public class LocalState {
        public int CurrentPlayerId;
    }

    [MessagePackObject(true)]
    public class GameState {
        public bool IsApplyingEvent => _isApplyingEvent;
        public List<Character> Characters = new();
        public List<NPC> NPCs = new();
        public Dictionary<string, LevelMap> Levels = new();

        private bool _isApplyingEvent;

        public void ApplyEvent(Action<GameState, SideEffectManager?> apply, SideEffectManager? sideEffectManager) {
            if (_isApplyingEvent)
                throw new ApplicationException(
                    $"An event is already being applied and event applications cannot be nested. Refactor the event being currently applied so that it can directly modify the state.");
            _isApplyingEvent = true;
            try {
                // Logr.Log($"[{_id}] Applying evt {apply.Method.DeclaringType}");
                apply(this, sideEffectManager);
                OnEventApplied(sideEffectManager);
            } finally {
                _isApplyingEvent = false;
            }
        }

        private void OnEventApplied(SideEffectManager? sideEffectManager) {
            // no post events atm
        }
    }


    // ReSharper disable once InconsistentNaming
}