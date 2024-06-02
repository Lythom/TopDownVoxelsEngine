using System.Diagnostics.CodeAnalysis;
using System.Threading;
using LoneStoneStudio.Tools;

namespace Shared {
    public class Selectors {
        private readonly GameState _gameState;
        private readonly CancellationTokenSource _selectorsSubscription;
        public readonly Reactive<Character?> LocalPlayerStateSelector;
        public readonly Reactive<string?> LocalPlayerLevelIdSelector;
        public Reactive<BlockId> PlayerBlockSelector;
        public Reactive<ToolId> PlayerToolSelector;


        public Selectors(GameState gameState) {
            _gameState = gameState;

            _selectorsSubscription?.Cancel();
            _selectorsSubscription = new CancellationTokenSource();
            var cancellationToken = _selectorsSubscription.Token;

            LocalPlayerStateSelector = ReactiveHelpers.CreateSelector(
                _gameState.Characters,
                LocalState.Instance.CurrentPlayerId,
                (characters, playerId) => characters.Dictionary.TryGetValue(playerId, out var value) ? value : null,
                null,
                cancellationToken
            );
            LocalPlayerLevelIdSelector = new(null);
            LocalPlayerLevelIdSelector.BindNestedSelector(LocalPlayerStateSelector, lpss => lpss?.Level, cancellationToken);

            PlayerBlockSelector = new Reactive<BlockId>(1);
            PlayerBlockSelector.BindNestedSelector(LocalPlayerStateSelector, c => c?.SelectedBlock, cancellationToken);
            PlayerToolSelector = new Reactive<ToolId>(ToolId.None);
            PlayerToolSelector.BindNestedSelector(LocalPlayerStateSelector, c => c?.SelectedTool, cancellationToken);
        }
    }
}