using System.Linq;
using Shared;
using Shared.Signals;
using TinkState;
using ZLinq;

namespace VoxelsEngine {
    public class Selectors {
        public readonly Observable<PlayerTool?> SelectedTool;
        public readonly ObservableList<PlayerTool> ToolQueue = new SignalList<PlayerTool>(); // 0 is current, 1 is next, 2 is following, etc.
        public readonly ObservableList<BlockId> BlockQueue = new SignalList<BlockId>(); // 0 is current, 1 is next, 2 is following, etc.
        public readonly Observable<BlockId?> SelectedBlock;
        public readonly Observable<CharacterV0?> CurrentCharacter;
        public readonly Observable<string?> CurrentLevelId;

        public Selectors(GameState state) {
            CurrentCharacter = Observable.Auto(() => state.Characters.TryGetValue(LocalState.Instance.CurrentPlayerId.Value, out var c) ? c : null, CharacterEqualityComparer.Instance);
            SelectedBlock = Observable.Auto(() => CurrentCharacter.Value?.SelectedBlock.Value);
            CurrentLevelId = Observable.Auto(() => CurrentCharacter.Value?.Level.Value);
            SelectedTool = Observable.Auto(() => {
                if (Configurator.Instance.PlayerTools.Count == 0) return null;
                var index = CurrentCharacter.Value?.SelectedTool.Value ?? 0;
                if (index >= Configurator.Instance.PlayerTools.Count) return null;
                return Configurator.Instance.PlayerTools[index];
            });
            SelectedTool.Bind(_ => {
                ToolQueue.Clear();
                var offset = CurrentCharacter.Value?.SelectedTool.Value ?? 0;
                var playerToolsCount = Configurator.Instance.PlayerTools.Count;
                if (playerToolsCount == 0) return;
                for (int i = 0; i < playerToolsCount; i++) {
                    ToolQueue.Add(Configurator.Instance.PlayerTools[(i + offset) % playerToolsCount]);
                }
            });
            SelectedBlock.Bind(_ => {
                BlockQueue.Clear();
                var selected = CurrentCharacter.Value?.SelectedBlock.Value ?? 0;
                // ignore air
                var stateBlockPathById = state.BlockPathById;
                var playerBlocksCount = state.BlockIdByPath.Count;
                for (int i = 0; i < playerBlocksCount; i++) {
                    var index = (i + selected) % playerBlocksCount;
                    if (index == 0) {
                        index = 1;
                        i++;
                    }
                    var p = stateBlockPathById[index];
                    if (p != null) BlockQueue.Add(index);
                }
            });
        }
    }
}