using Cysharp.Threading.Tasks.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class ToolPreview : MonoBehaviour {
        [Required]
        public Character Character = null!;

        [Required]
        public Image Preview = null!;

        private void Start() {
            Character.CurrentBlock.ForEachAsync(v => {
                int id = (int) v;

                var lib = Configurator.Instance.BlocksLibrary;
                Preview.sprite = (id > 0 && id < lib.Count ? lib[id].ItemPreview : null)!;
            });
        }
    }
}