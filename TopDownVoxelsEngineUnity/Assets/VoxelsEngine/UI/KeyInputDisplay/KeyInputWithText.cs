using System.Collections.Generic;
using Shared;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using VoxelsEngine;
using VoxelsEngine.UI;

// ConnectedMonoBehaviour triggers "OnSetup" and provides the game state
public class KeyInputWithText : ConnectedBehaviour {
    [Title("Bindings")]

    // The Input action we want to visualize
    [Required]
    public InputActionReference InputActionRef = null!;

    [Required]
    public Image Image = null!;

    [Required]
    public TextMeshProUGUI Text = null!;

    // The currently displayed scheme (Keyboard or Gamepad)
    public ControlSchemeId SchemeId = ControlSchemeId.Keyboard;

    // An input can be composite (multiple keys are bound to the same action)
    // This index allows to select in the inspector which keys to display
    public int CompositeIdx = -1;

    private static readonly InputBinding GamepadMask = InputBinding.MaskByGroup(nameof(ControlSchemeId.Gamepad));
    private static readonly InputBinding KeyboardMask = InputBinding.MaskByGroup(nameof(ControlSchemeId.Keyboard));

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
        Text.text = InputActionRef.action.name;

        // Gamepad sprites are prefixed with XboxSeriesX_
        var prefix = SchemeId == ControlSchemeId.Gamepad ? "XboxSeriesX_" : "";
        // Keyboard sprites are suffixed with _Key_Dark
        var suffix = SchemeId == ControlSchemeId.Gamepad ? "" : "_Key_Dark";
        // Get the mask to filter current scheme bindings
        var bindingMask = SchemeId == ControlSchemeId.Gamepad ? GamepadMask : KeyboardMask;
        // Get sprite name
        var key = GetBindingDisplayString(InputActionRef.action, bindingMask, CompositeIdx);
        var spriteName = prefix + key.layoutKeyName + suffix;
        // Load and display sprite
        var sprite = Resources.Load<Sprite>("Keys/" + spriteName);
        if (sprite is null && key.rawKeyName != key.layoutKeyName) {
            spriteName = prefix + key.rawKeyName + suffix;
            sprite = Resources.Load<Sprite>("Keys/" + spriteName);
        }

        if (sprite is not null) Image.sprite = sprite;
    }

    public static (string layoutKeyName, string rawKeyName) GetBindingDisplayString(InputAction action, InputBinding bindingMask, int compositeIdx) {
        // get all the bindings for the action
        var bindings = action.bindings;
        List<InputBinding> compositeList = new();
        for (var i = 0; i < bindings.Count; ++i) {
            // binding must match the mask
            var b = bindings[i];
            if (!bindingMask.Matches(b))
                continue;

            // if binding is part of a composite, add it to the list and continue
            if (b.isPartOfComposite) {
                compositeList.Add(b);
                continue;
            }

            // binding is the first to match the mask and is not part of a composite, get the name (without interaction text) and sanitize to get sprite equivalent
            return GetLayoutAwareDisplayString(b);
        }

        if (compositeIdx > -1 && compositeIdx < compositeList.Count) {
            // We got all the bindings of the composite, get the name of the configured one (compositeIdx)
            return GetLayoutAwareDisplayString(compositeList[compositeIdx]);
        }

        // Failed to get name, display nothing
        return (string.Empty, string.Empty);
    }

    private static (string layoutKeyName, string rawKeyName) GetLayoutAwareDisplayString(InputBinding binding) {
        // Get the display string without interactions
        string rawKeyName = binding.ToDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions).Replace("/", "_").Replace(" ", "_");

        // If this is a keyboard binding, try to get the actual key based on current layout
        if (binding.path.StartsWith("<Keyboard>")) {
            // Get current keyboard
            var keyboard = Keyboard.current;
            if (keyboard != null) {
                // Try to find the key control from the binding path
                string keyPath = binding.path;
                var control = InputSystem.FindControl(keyPath);

                if (control is KeyControl keyControl) {
                    // Get the display name from the current keyboard layout
                    string layoutKeyName = keyControl.displayName;
                    if (!string.IsNullOrEmpty(layoutKeyName)) {
                        return (layoutKeyName.Replace("/", "_").Replace(" ", "_"), rawKeyName);
                    }
                }
            }
        }

        // Sanitize to get sprite equivalent
        return (rawKeyName, rawKeyName);
    }

#if UNITY_EDITOR
    [Button]
    public void RenameGameObject() {
        var newName = InputActionRef.action.name.Replace(" ", "_");
        gameObject.name = newName;
    }
#endif
}