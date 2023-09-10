using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine;

public class AnimationEventHandle : MonoBehaviour {
    public AudioSource ToesLeft;
    public AudioSource ToesRight;

    [Required, ValidateInput("IsICharacterSpeed", "MonoBehaviour must implements ICharacterSpeed")]
    public MonoBehaviour Character; // ICharacterSpeed

    public ICharacterSpeed? ICharacterSpeed = null!;
    public bool IsICharacterSpeed(MonoBehaviour b) => b is ICharacterSpeed;

    public void HandleEvent(int foot) {
        if (ICharacterSpeed == null) ICharacterSpeed = (ICharacterSpeed) Character;
        var c = Configurator.Instance;
        if (foot == 0) {
            ToesLeft.volume = Mathf.Clamp01(ICharacterSpeed?.CurrentSpeed ?? 1);
            ToesLeft.pitch = Random.Range(1 - c.SFXFootstepPitchRange, 1 + c.SFXFootstepPitchRange);
            ToesLeft.PlayOneShot(c.SFXFootstep);
        } else {
            ToesRight.volume = Mathf.Clamp01(ICharacterSpeed?.CurrentSpeed ?? 1);
            ToesRight.pitch = Random.Range(1 - c.SFXFootstepPitchRange, 1 + c.SFXFootstepPitchRange);
            ToesRight.PlayOneShot(c.SFXFootstep);
        }
    }
}