using Sirenix.OdinInspector;
using UnityEngine;

    [ExecuteInEditMode]
    public class FaceController : MonoBehaviour {
        public enum FACES {
            Smile,
            Smile_Blink,
            Angry,
            Empty
        }

        public FACES CurrentFace;
        private FACES _displayedFace;

        [Required]
        public Renderer renderer;

        private static readonly int Index = Shader.PropertyToID("_Index");
        private MaterialPropertyBlock _mpb;


        void Update() {
            if (renderer == null || _displayedFace == CurrentFace) {
                return;
            }

            if (_mpb == null) {
                _mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(_mpb);
            }

            
            _mpb.SetFloat(Index, (int) CurrentFace);
            renderer.SetPropertyBlock(_mpb);

            _displayedFace = CurrentFace;
        }
    }
