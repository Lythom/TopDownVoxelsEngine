﻿using System;
using System.Collections.Generic;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared {
    public class LocalState {
        public static LocalState Instance = new();
        public readonly Reactive<ushort> CurrentPlayerId = new(ushort.MaxValue);
        public string CurrentPlayerName = "Lythom2";
        public Reactive<SessionStatus> Session = new(SessionStatus.Disconnected);
    }

    [MessagePackObject(true)]
    public class GameState : IUpdatable<GameState> {
        // Public state
        public bool IsApplyingEvent => _isApplyingEvent;
        public readonly ReactiveDictionary<ushort, Character> Characters = new();
        public readonly ReactiveDictionary<string, LevelMap> Levels = new();
        public readonly float Gravity = 1.4f;

        [IgnoreMember]
        public object LockObject = new();

        [IgnoreMember]
        public readonly Selectors Selectors;

        // internal or non serialized properties
        [IgnoreMember]
        public readonly LevelGenerator LevelGenerator = new();

        private readonly HashSet<uint> _dirtyChunks = new();

        private bool _isApplyingEvent;

        public GameState() {
            Selectors = new Selectors(this);
        }

        public GameState(ReactiveDictionary<ushort, Character>? characters, ReactiveList<NPC>? npcs, ReactiveDictionary<string, LevelMap>? levels) {
            if (characters != null) Characters.SynchronizeToTarget(characters);
            if (levels != null) Levels.SynchronizeToTarget(levels);
            Selectors = new Selectors(this);
        }

        public void ApplyEvent(Action<GameState, SideEffectManager?> apply, SideEffectManager? sideEffectManager) {
            lock (LockObject) {
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
        }

        private void OnEventApplied(SideEffectManager? sideEffectManager) {
            // no post events atm
        }

        public void SetChunkDirty(uint chMorton) {
            _dirtyChunks.Add(chMorton);
        }

        public void UpdateValue(GameState nextState) {
            Characters.SynchronizeToTarget(nextState.Characters);
            Levels.SynchronizeToTarget(nextState.Levels);
        }
    }


    // ReSharper disable once InconsistentNaming
}