using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class BlocPreview : ConnectedBehaviour {
        [Required]
        public Image Preview = null!;

        protected override void OnSetup(GameState state) {
            var playerId = LocalState.Instance.CurrentPlayerId;
            var playerStateSelector = ReactiveHelpers.CreateSelector(
                state.Characters,
                characters => characters.Dictionary.TryGetValue(playerId, out var value) ? value : null,
                null,
                ResetToken
            );
            var playerBlockSelector = new Reactive<BlockId>(BlockId.Dirt);
            playerBlockSelector.BindCompoundValue(playerStateSelector, c => c?.SelectedBlock, ResetToken);
            var playerToolSelector = new Reactive<ToolId>(ToolId.None);
            playerToolSelector.BindCompoundValue(playerStateSelector, c => c?.SelectedTool, ResetToken);

            Subscribe(playerBlockSelector, playerToolSelector, (block, tool) => {
                this.SmartActive(tool == ToolId.PlaceBlock || tool == ToolId.ExchangeBlock);
                var lib = Configurator.Instance.BlocksRenderingLibrary;
                int id = (int) block;
                Preview.sprite = (id >= 0 && id < lib.Count ? lib[id].ItemPreview : null)!;
            });
        }
    }
}