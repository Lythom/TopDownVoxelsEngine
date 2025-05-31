using Shared;
using TinkState;

namespace VoxelsEngine {
    public class Selectors {
        public readonly Observable<ToolId?> SelectedTool;
        public readonly Observable<BlockId?> SelectedBlock;
        public readonly Observable<Character?> CurrentCharacter;
        public readonly Observable<string?> CurrentLevel;

        public Selectors(GameState state) {
            CurrentCharacter = LocalState.Instance.CurrentPlayerId.Map(id => state.Characters.TryGetValue(id, out var c) ? c : null);
            SelectedBlock = CurrentCharacter.Map(c => c?.SelectedBlock.Value);
            SelectedTool = CurrentCharacter.Map(c => c?.SelectedTool.Value);
            CurrentLevel = CurrentCharacter.Map(c => c?.Level.Value);
        }
    }
}