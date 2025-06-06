using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TinkState;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class BlocPreview : ConnectedBehaviour {
        [Required]
        public RawImage Preview = null!;

        protected override void OnSetup(GameState state, Selectors clientEngineSelectors) {
            Observable.AutoRun(() => {
                var tool = Selectors.SelectedTool.Value;
                var block = Selectors.SelectedBlock.Value;
                this.SmartActive(tool?.Purpose is PlayerToolPurpose.PlaceBlock);
                var blockPath = block.HasValue ? state.BlockPathById[block.Value.Id] : null;
                if (blockPath != null && Configurator.Instance.BlocksRenderingLibrary.TryGetValue(blockPath, out var b)) {
                    Preview.texture = b.ItemPreview!;
                } else {
                    Preview.texture = null;
                }
            }).AddTo(ResetToken);
        }
    }
}