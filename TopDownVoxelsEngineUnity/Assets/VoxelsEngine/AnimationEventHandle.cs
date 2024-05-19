using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine;

public class AnimationEventHandle : MonoBehaviour {
    [Required]
    public AudioSource ToesLeft = null!;

    [Required]
    public AudioSource ToesRight = null!;

    [Required, ValidateInput("IsICharacterSpeed", "MonoBehaviour must implements ICharacterSpeed")]
    public MonoBehaviour Character = null!; // ICharacterSpeed

    private ICharacterSpeed? _characterSpeed;
    public bool IsICharacterSpeed(MonoBehaviour b) => b is ICharacterSpeed;

    public void HandleEvent(int foot) {
        if (_characterSpeed == null) _characterSpeed = (ICharacterSpeed) Character;
        var c = Configurator.Instance;
        if (foot == 0) {
            ToesLeft.volume = Mathf.Clamp01(_characterSpeed?.CurrentSpeed ?? 1);
            ToesLeft.pitch = Random.Range(1 - c.SFXFootstepPitchRange, 1 + c.SFXFootstepPitchRange);
            ToesLeft.PlayOneShot(c.SFXFootstep);
        } else {
            ToesRight.volume = Mathf.Clamp01(_characterSpeed?.CurrentSpeed ?? 1);
            ToesRight.pitch = Random.Range(1 - c.SFXFootstepPitchRange, 1 + c.SFXFootstepPitchRange);
            ToesRight.PlayOneShot(c.SFXFootstep);
        }
    }
}