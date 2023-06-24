using Shared;
using Sirenix.OdinInspector;
using TMPro;

namespace VoxelsEngine.UI {
    public class ToolPreview : ConnectedBehaviour {
        [Required]
        public TextMeshProUGUI Text = null!;

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.PlayerToolSelector, t => Text.text = t.ToString());
        }
    }
}