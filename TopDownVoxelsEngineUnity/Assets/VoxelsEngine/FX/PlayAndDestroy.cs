using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class PlayAndDestroy : MonoBehaviour {
        [Required]
        public AudioSource Audio = null!;

        void Start() {
            DoPlay().Forget();
        }

        async UniTask DoPlay() {
            var c = Configurator.Instance;
            Audio.pitch = Random.Range(1 - c.SFXFootstepPitchRange, 1 + c.SFXFootstepPitchRange);
            Audio.PlayOneShot(Audio.clip);
            await UniTask.Delay(1000);
            if (gameObject != null) Destroy(gameObject);
        }
    }
}