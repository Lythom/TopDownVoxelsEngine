// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/24

using DG.DemiEditor;
using DG.DemiLib;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal static class DOScope
    {
        public class UndoableSerialization : DeScope
        {
            readonly bool _markSettings, _markComponent;
            public UndoableSerialization(bool markSettings = true, bool markComponent = true)
            {
                _markSettings = markSettings;
                _markComponent = markComponent;
                DOTweenClipTimeline.editor.MarkForUndo(_markSettings, _markComponent);
            }
            protected override void CloseScope()
            {
                DOTweenClipTimeline.editor.MarkDirty(_markSettings, _markComponent);
            }
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        /// <summary>
        /// Only for settings, doesn't mark for undo/dirty the component
        /// </summary>
        public class NonUndoableSettingsSerialization : DeScope
        {
            public NonUndoableSettingsSerialization()
            {
                Undo.IncrementCurrentGroup();
                // This allows the undo to be nullified but also prevents it to work
                Undo.RegisterCompleteObjectUndo(DOTweenClipTimeline.settings, "DOTween Timeline");
            }
            protected override void CloseScope()
            {
                DOTweenClipTimeline.editor.MarkDirty(true, false);
                Undo.ClearUndo(DOTweenClipTimeline.settings);
            }
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        public static class SettingsPanel
        {
            public enum Type
            {
                Main,
                Section,
                Subsection
            }

            public class SectionScope : DeScope
            {
                Type _type;

                readonly Color _defMainToolbarBgColor = new Color(0.64f, 0.89f, 0.44f);
                readonly Color _defMainToolbarTextColor = Color.black;
                readonly Color _defSectionToolbarBgColor = new Color(0.33f, 0.56f, 0.89f);
                readonly Color _defSectionToolbarTextColor = Color.white;
                readonly Color _defSubsectionToolbarBgColor = new Color(0.05f, 0.05f, 0.05f);
                readonly Color _defSubsectionToolbarTextColor = new Color(0.59f, 0.75f, 1f);

                public SectionScope(Type type, string title, Color? bgColor = null, Color? textColor = null)
                {
                    _type = type;
                    Color bgCol, textCol;
                    GUIStyle style = DOEGUI.Styles.global.toolbarLabelWhite;
                    switch (_type) {
                    case Type.Section:
                        bgCol = bgColor == null ? _defSectionToolbarBgColor : (Color)bgColor;
                        textCol = textColor == null ? _defSectionToolbarTextColor : (Color)textColor;
                        break;
                    case Type.Subsection:
                        bgCol = bgColor == null ? _defSubsectionToolbarBgColor : (Color)bgColor;
                        textCol = textColor == null ? _defSubsectionToolbarTextColor : (Color)textColor;
                        break;
                    default:
                        style = DOEGUI.Styles.timeline.settingsMainToolbarLabel;
                        bgCol = bgColor == null ? _defMainToolbarBgColor : (Color)bgColor;
                        textCol = textColor == null ? _defMainToolbarTextColor : (Color)textColor;
                        break;
                    }
                    using (new DeGUILayout.ToolbarScope(bgColor == null ? bgCol : (Color)bgColor, DOEGUI.Styles.toolbar.flat)) {
                        using (new DeGUI.ColorScope(null, textColor == null ? textCol : (Color)textColor)) {
                            GUILayout.Label(title, style, GUILayout.ExpandWidth(true));
                        }
                    }
                    if (_type == Type.Main) GUILayout.BeginVertical(DOEGUI.Styles.timeline.settingsMainBox);
                }
                protected override void CloseScope()
                {
                    switch (_type) {
                    case Type.Main:
                        GUILayout.EndVertical();
                        break;
                    default:
                        GUILayout.Space(4);
                        break;
                    }
                }
            }
        }
    }
}