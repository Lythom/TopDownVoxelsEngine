using Shared;
using Shared.Signals;
using TinkState;

namespace VoxelsEngine {
    public class Selectors {
        public readonly Observable<PlayerTool?> SelectedTool;
        public readonly ObservableList<PlayerTool> ToolQueue = new SignalList<PlayerTool>(); // 0 is current, 1 is next, 2 is following, etc.
        public readonly Observable<BlockId?> SelectedBlock;
        public readonly Observable<Character?> CurrentCharacter;
        public readonly Observable<string?> CurrentLevel;

        public Selectors(GameState state) {
            CurrentCharacter = Observable.Auto(() => state.Characters.TryGetValue(LocalState.Instance.CurrentPlayerId.Value, out var c) ? c : null, CharacterEqualityComparer.Instance);
            SelectedBlock = Observable.Auto(() => CurrentCharacter.Value?.SelectedBlock.Value);
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
            CurrentLevel = Observable.Auto(() => CurrentCharacter.Value?.Level.Value);
        }
    }
}