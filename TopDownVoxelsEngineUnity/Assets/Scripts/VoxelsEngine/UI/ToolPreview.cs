using Cysharp.Threading.Tasks.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class ToolPreview : MonoBehaviour {
        [FormerlySerializedAs("Character")]
        [Required]
        public CharacterAgent CharacterAgent = null!;

        [Required]
        public Image Preview = null!;

        private void Start() {
            CharacterAgent.SelectedItem.ForEachAsync(v => {
                int id = (int) v;

                var lib = Configurator.Instance.BlocksRenderingLibrary;
                Preview.sprite = (id >= 0 && id < lib.Count ? lib[id].ItemPreview : null)!;
            });
        }
    }
}