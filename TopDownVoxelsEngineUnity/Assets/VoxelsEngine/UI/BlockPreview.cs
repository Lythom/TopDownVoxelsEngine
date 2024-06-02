using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class BlocPreview : ConnectedBehaviour {
        [Required]
        public RawImage Preview = null!;

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.PlayerBlockSelector, state.Selectors.PlayerToolSelector, (block, tool) => {
                this.SmartActive(tool == ToolId.PlaceBlock || tool == ToolId.ExchangeBlock);
                var blockPath = state.BlockPathById[block];
                if (blockPath != null && Configurator.Instance.BlocksRenderingLibrary.TryGetValue(blockPath, out var b)) {
                    Preview.texture = b.ItemPreview!;
                } else {
                    Preview.texture = null!;
                }
            });
        }
    }
}