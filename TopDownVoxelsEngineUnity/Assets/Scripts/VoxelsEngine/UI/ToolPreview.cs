using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TMPro;

namespace VoxelsEngine.UI {
    public class ToolPreview : ConnectedBehaviour {
        [Required]
        public TextMeshProUGUI Text = null!;

        protected override void OnSetup(GameState state) {
            var playerStateSelector = new Reactive<Character?>(null);
            Subscribe(LocalState.Instance.CurrentPlayerId, state.Characters, (id, characters) => {
                playerStateSelector.Value = characters.Dictionary.TryGetValue(id, out var value) ? value : null;
            });

            var playerToolSelector = new Reactive<ToolId>(ToolId.None);
            playerToolSelector.BindCompoundValue(playerStateSelector, c => c?.SelectedTool, ResetToken);
            Subscribe(playerToolSelector, t => Text.text = t.ToString());
        }
    }
}