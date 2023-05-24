using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine.Rendering {
    [RequireComponent(typeof(Renderer))]
    public class BlockInstance : MonoBehaviour {
        [Range(0, 16), OnValueChanged("UpdateShader")]
        public float TextureIndex;

        private MaterialPropertyBlock? _propertyBlock;
        private Renderer? _renderer;
        private static readonly int Index = Shader.PropertyToID("_TextureIndex");

        public void UpdateShader() {
            if (_propertyBlock == null) _propertyBlock = new();
            if (_renderer == null) _renderer = GetComponent<Renderer>();

            _propertyBlock.SetFloat(Index, TextureIndex);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}