using System.Collections.Generic;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VoxelsEngine;
using VoxelsEngine.UI;

// ConnectedMonoBehaviour triggers "OnSetup" and provide the game state
[RequireComponent(typeof(Image))]
public class KeyInputDisplay : ConnectedBehaviour {
    [Title("Bindings")]

    // The Input action we want to visualize
    [Required]
    public InputActionReference InputActionRef = null!;

    // Target Image component
    [Required]
    public Image Image = null!;

    // The currently displayed scheme (Keyboard or Gamepad)
    public ControlSchemeId SchemeId = ControlSchemeId.Keyboard;

    // An input can be composite (multiple keys are bound to the same action)
    // This index allows to select in the inspector which keys to display
    public int CompositeIdx = -1;

    private static readonly InputBinding GamepadMask = InputBinding.MaskByGroup(ControlSchemeId.Gamepad.ToString());
    private static readonly InputBinding KeyboardMask = InputBinding.MaskByGroup(ControlSchemeId.Keyboard.ToString());

    protected override void OnSetup(GameState state, Selectors selectors) {
        SubscribeSideEffect<ControlSchemeId>(scheme => {
            SchemeId = scheme;
            UpdateVisual();
        });
    }

    protected override void OnEnable() {
        base.OnEnable();
        UpdateVisual();
    }

    [Button]
    private void UpdateVisual() {
        // Gamepad sprites are prefixed with XboxSeriesX_
        var prefix = SchemeId == ControlSchemeId.Gamepad ? "XboxSeriesX_" : "";
        // Keyboard sprites are suffixed with _Key_Dark
        var suffix = SchemeId == ControlSchemeId.Gamepad ? "" : "_Key_Dark";
        // Get the mask to filter current scheme bindings
        var bindingMask = SchemeId == ControlSchemeId.Gamepad ? GamepadMask : KeyboardMask;
        // Get sprite name
        var spriteName = prefix + GetBindingDisplayString(InputActionRef.action, bindingMask, CompositeIdx) + suffix;
        // Load and display sprite
        var sprite = Resources.Load<Sprite>("Keys/" + spriteName);
        if (sprite != null) {
            Image.sprite = sprite;
        }
    }

    public static string GetBindingDisplayString(InputAction action, InputBinding bindingMask, int compositeIdx) {
        // get all the bindings for the action
        var bindings = action.bindings;
        List<InputBinding> compositeList = new();
        for (var i = 0; i < bindings.Count; ++i) {
            // binding must match the mask
            if (!bindingMask.Matches(bindings[i]))
                continue;

            // if binding is part of a composite, add it to the list and continue
            if (bindings[i].isPartOfComposite) {
                compositeList.Add(bindings[i]);
                continue;
            }

            // binding is the first to match the mask and is not part of a composite, get the name (without interaction text) and sanitize to get sprite equivalent
            return bindings[i].ToDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions).Replace("/", "_").Replace(" ", "_");
        }

        if (compositeIdx > -1 && compositeIdx < compositeList.Count) {
            // We got all the bindings of the composite, get the name of the configured one (compositeIdx)
            return compositeList[compositeIdx].ToDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions).Replace("/", "_").Replace(" ", "_");
        }

        // Failed to get name, display nothing
        return string.Empty;
    }
}