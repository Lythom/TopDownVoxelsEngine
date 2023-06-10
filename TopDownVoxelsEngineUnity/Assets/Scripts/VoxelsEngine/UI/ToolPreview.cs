using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class ToolPreview : ConnectedBehaviour {
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
            var playerToolSelector = new Reactive<BlockId>(BlockId.Air);
            playerToolSelector.BindCompoundValue(playerStateSelector, c => c?.SelectedBlock, ResetToken);
            
            Subscribe(playerToolSelector, block => {
                var lib = Configurator.Instance.BlocksRenderingLibrary;
                int id = (int) block;
                Preview.sprite = (id >= 0 && id < lib.Count ? lib[id].ItemPreview : null)!;
            });
        }
    }
}