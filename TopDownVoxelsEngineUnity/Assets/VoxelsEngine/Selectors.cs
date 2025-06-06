using Shared;
using TinkState;

namespace VoxelsEngine {
    public class Selectors {
        public readonly Observable<PlayerTool?> SelectedTool;
        public readonly Observable<BlockId?> SelectedBlock;
        public readonly Observable<Character?> CurrentCharacter;
        public readonly Observable<string?> CurrentLevel;

        public Selectors(GameState state) {
            CurrentCharacter = Observable.Auto(() => state.Characters.TryGetValue(LocalState.Instance.CurrentPlayerId.Value, out var c) ? c : null, CharacterEqualityComparer.Instance);
            SelectedBlock = Observable.Auto(() => CurrentCharacter.Value?.SelectedBlock.Value);
            SelectedTool = Observable.Auto(() => {
                var index = CurrentCharacter.Value?.SelectedTool.Value ?? 0;
                if (index >= Configurator.Instance.PlayerTools.Count) return null;
                return Configurator.Instance.PlayerTools[index];
            });
            CurrentLevel = Observable.Auto(() => CurrentCharacter.Value?.Level.Value);
        }
    }
}