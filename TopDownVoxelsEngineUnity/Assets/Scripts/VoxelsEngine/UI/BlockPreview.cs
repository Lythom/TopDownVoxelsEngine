using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class BlocPreview : ConnectedBehaviour {
        [Required]
        public Image Preview = null!;

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.PlayerBlockSelector, state.Selectors.PlayerToolSelector, (block, tool) => {
                this.SmartActive(tool == ToolId.PlaceBlock || tool == ToolId.ExchangeBlock);
                var lib = Configurator.Instance.BlocksRenderingLibrary;
                int id = (int) block;
                Preview.sprite = (id >= 0 && id < lib.Count ? lib[id].ItemPreview : null)!;
            });
        }
    }
}