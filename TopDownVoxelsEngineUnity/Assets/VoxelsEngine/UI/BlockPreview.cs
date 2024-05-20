using System;
using System.Collections.Generic;
using System.Linq;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class BlocPreview : ConnectedBehaviour {
        [Required]
        public RawImage Preview = null!;

        private Dictionary<BlockId, Texture?> _previewsById;

        private void Awake() {
            var blockConfigs = Resources.LoadAll<BlockConfiguration>("Configurations");
            _previewsById = blockConfigs.ToDictionary(c => c.Id, c => c.ItemPreview);
        }

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.PlayerBlockSelector, state.Selectors.PlayerToolSelector, (block, tool) => {
                this.SmartActive(tool == ToolId.PlaceBlock || tool == ToolId.ExchangeBlock);
                if (_previewsById.TryGetValue(block, out var value))
                    Preview.texture = value!;
            });
        }
    }
}