using Cysharp.Threading.Tasks;
using Shared;
using Sirenix.OdinInspector;
using TinkState;
using TMPro;

namespace VoxelsEngine.UI {
    public class ToolPreview : ConnectedBehaviour {
        [Required]
        public TextMeshProUGUI Text = null!;

        protected override void OnSetup(GameState state) {
            Observable.AutoRun(() => Text.text = "<+spread>" + Selectors.SelectedTool).AddTo(ResetToken);
        }
    }
}