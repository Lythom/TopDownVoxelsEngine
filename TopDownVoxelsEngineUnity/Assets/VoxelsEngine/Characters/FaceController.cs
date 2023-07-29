using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class FaceController : MonoBehaviour {
    public enum Faces {
        Smile,
        SmileBlink,
        Angry,
        Empty
    }

    public Faces CurrentFace;
    private Faces _displayedFace;

    [FormerlySerializedAs("renderer")]
    [Required]
    public Renderer Renderer = null!;

    private static readonly int Index = Shader.PropertyToID("_Index");
    private MaterialPropertyBlock? _mpb;

    private bool _isReady = false;

    private void OnEnable() {
        _isReady = Renderer != null;
    }

    private void OnDisable() {
        _isReady = false;
    }


    void Update() {
        if (!_isReady || _displayedFace == CurrentFace) {
            return;
        }

        if (_mpb == null) {
            _mpb = new MaterialPropertyBlock();
            Renderer.GetPropertyBlock(_mpb);
        }


        _mpb.SetFloat(Index, (int) CurrentFace);
        Renderer.SetPropertyBlock(_mpb);

        _displayedFace = CurrentFace;
    }
}