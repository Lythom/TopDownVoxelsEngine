using System;
using System.Collections.Generic;
using System.Linq;
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
        [IgnoreMember]
        public bool IsApplyingEvent => _isApplyingEvent;

        public readonly ReactiveDictionary<ushort, Character> Characters = new();
        public readonly ReactiveDictionary<string, LevelMap> Levels = new();
        public readonly string?[] BlockPathById = new string?[ushort.MaxValue];
        public readonly Dictionary<string, ushort> BlockIdByPath = new();

        public readonly float Gravity = 1.4f;

        [IgnoreMember]
        public object LockObject = new();

        [IgnoreMember]
        public readonly Selectors Selectors;

        // internal or non serialized properties
        [IgnoreMember]
        public readonly LevelGenerator LevelGenerator;

        [IgnoreMember]
        private readonly HashSet<uint> _dirtyChunks = new();

        [IgnoreMember]
        private bool _isApplyingEvent;

        public GameState(ReactiveDictionary<ushort, Character>? characters, ReactiveDictionary<string, LevelMap>? levels) {
            if (characters != null) Characters.SynchronizeToTarget(characters);
            if (levels != null) Levels.SynchronizeToTarget(levels);
            Selectors = new Selectors(this);
            LevelGenerator = new LevelGenerator(BlockIdByPath);
        }

        public void UpdateBlockMapping(Registry<BlockConfigJson> registry) {
            BlockPathById[0] = "Air";
            foreach (var block in BlockPathById) {
                if (block != null && block != "Air" && registry.Get(block) == null) {
                    throw new ApplicationException($"Le block {block} utilisé dans cette save n'est pas présent dans le BlockRegistry. Comportement de suppression de bloc non implémenté.");
                }
            }

            foreach (var blockPath in registry.Get().Keys) {
                if (BlockPathById.Contains(blockPath)) continue;
                var nextIdx = Array.IndexOf(BlockPathById, null);
                if (nextIdx == -1) {
                    throw new InvalidOperationException("Le tableau BlockPathById est plein. Impossible d'ajouter de nouveaux éléments.");
                }

                BlockPathById[nextIdx] = blockPath;
            }

            // update inverted lookup
            BlockIdByPath.Clear();
            for (ushort blockId = 0; blockId < BlockPathById.Length; blockId++) {
                var blockPath = BlockPathById[blockId];
                if (blockPath != null) BlockIdByPath[blockPath] = blockId;
            }
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
            for (var i = 0; i < BlockPathById.Length; i++) BlockPathById[i] = nextState.BlockPathById[i];
            BlockIdByPath.Clear();
            foreach (var (key, value) in nextState.BlockIdByPath) BlockIdByPath[key] = value;
        }
    }


    // ReSharper disable once InconsistentNaming
}