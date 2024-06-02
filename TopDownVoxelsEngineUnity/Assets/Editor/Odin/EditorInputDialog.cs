using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable NotAccessedField.Local

// adapted from https://forum.unity.com/threads/is-there-a-way-to-input-text-using-a-unity-editor-utility.473743/
public class EditorInputDialog : OdinEditorWindow {
    private string? _description;

    [InfoBox("@_description")]
    [HideLabel]
    [ValidateInput("ValidateInput", "@_validationErrorMessage")]
    public string? Input;

    public bool ValidateInput(string input) {
        return _validateString == null || _validateString(input);
    }

    private string? _okButton;
    private Action? _onOkButton;
    private string? _cancelButton;
    private Func<string, bool>? _validateString;
    private string? _validationErrorMessage;

    public void OkButton() {
        _onOkButton?.Invoke();
        Close();
    }

    protected override void OnGUI() {
        GUI.SetNextControlName("MyTextField");
        Input = EditorGUILayout.TextField(Input);
        if (!ValidateInput(Input)) {
            GUIHelper.PushColor(Color.red);
            EditorGUILayout.TextArea(_validationErrorMessage);
            GUIHelper.PopColor();
        }

        if (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl())) {
            GUI.FocusControl("MyTextField");
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Ok")) {
            OkButton();
        }

        if (GUILayout.Button("Cancel")) {
            Input = null;
            Close();
        }

        GUILayout.EndHorizontal();

        // Check for Enter key press
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) {
            OkButton();
        }
    }


    #region Show()

    /// <summary>
    /// Returns text player entered, or null if player cancelled the dialog.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="inputText"></param>
    /// <param name="okButton"></param>
    /// <param name="cancelButton"></param>
    /// <param name="validateString"></param>
    /// <param name="validationErrorMessage"></param>
    /// <returns></returns>
    public static string? Show(string title, string? description, string inputText, string okButton = "OK", string cancelButton = "Cancel",
                               Func<string, bool>? validateString = null, string validationErrorMessage = "") {
        string? ret = null;
        //var window = EditorWindow.GetWindow<InputDialog>();
        var window = CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(title);
        window.minSize = new Vector2(700, 200);
        window._description = description;
        window.Input = inputText;
        window._okButton = okButton;
        window._cancelButton = cancelButton;
        window._onOkButton += () => ret = window.Input;
        window._validateString = validateString;
        window._validationErrorMessage = validationErrorMessage;

        window.ShowModal();

        return ret;
    }

    #endregion Show()
}