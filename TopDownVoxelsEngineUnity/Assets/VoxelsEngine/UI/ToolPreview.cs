using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TinkState;
using TMPro;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class ToolPreview : ConnectedBehaviour {
        [Required]
        public TextMeshProUGUI Text = null!;

        [Required]
        public RawImage Sprite = null!;

        protected override void OnSetup(GameState state) {
            Observable.AutoRun(() => {
                var tool = Selectors.SelectedTool.Value;
                Text.SmartActive(tool != null);
                Sprite.SmartActive(tool != null);
                if (tool == null) return;
                Text.text = $"<+spread>{tool.Name}";
                Sprite.texture = tool.Sprite;
            }).AddTo(ResetToken);
        }
    }
}