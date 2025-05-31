using System;
using System.Collections.Generic;
using LoneStoneStudio.Tools;
using MessagePack;
using Shared.Signals;

namespace Shared {
    public class LocalState {
        public static readonly LocalState Instance = new();
        public readonly Signal<ushort> CurrentPlayerId = new(ushort.MaxValue);
        public string CurrentPlayerName = "Lythom2";
        public readonly Signal<SessionStatus> Session = new(SessionStatus.Disconnected);
    }

    [MessagePackObject(true)]
    public class GameState : IUpdatable<GameState> {
        // Public state
        [IgnoreMember]
        public bool IsApplyingEvent => _isApplyingEvent;

        public readonly SignalDictionary<ushort, Character> Characters = new();
        public readonly SignalDictionary<string, LevelMap> Levels = new();

        // Keep the same field name for backward compatibility with existing saves
        [Key("BlockPathById")]
        public string?[] BlockPathById => _blockMapping.BlockPathById;

        [IgnoreMember]
        private readonly BlockPathMapping _blockMapping;

        // Redirect to BlockMapping's dictionary
        [IgnoreMember]
        public Dictionary<string, ushort> BlockIdByPath => _blockMapping.BlockIdByPath;

        public readonly float Gravity = 1.4f;

        [IgnoreMember]
        public object LockObject = new();

        // internal or non serialized properties
        [IgnoreMember]
        public readonly LevelGenerator LevelGenerator;

        [IgnoreMember]
        private readonly HashSet<uint> _dirtyChunks = new();

        [IgnoreMember]
        private bool _isApplyingEvent;

        [SerializationConstructor]
        public GameState(SignalDictionary<ushort, Character>? characters, SignalDictionary<string, LevelMap>? levels, string?[]? blockPathById) {
            if (characters != null) Characters.SynchronizeToTarget(characters);
            if (levels != null) Levels.SynchronizeToTarget(levels);
            _blockMapping = new BlockPathMapping(blockPathById ?? new string?[ushort.MaxValue]);
            LevelGenerator = new LevelGenerator(BlockIdByPath);
        }

        // Access the BlockMapping directly if needed
        public BlockPathMapping BlockMapping => _blockMapping;

        public void UpdateBlockMapping(IRegistry<BlockConfigJson> registry) {
            _blockMapping.UpdateBlockMapping(registry);
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
            _blockMapping.UpdateValue(nextState.BlockMapping);
        }
    }


    // ReSharper disable once InconsistentNaming
}