// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/22

using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline.Core;
using DG.Tweening.TimelineEditor.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal class TimelineSettingsUI : ABSTimelineElement
    {
        readonly Color _defSectionToolbarBgColor = new Color(0.33f, 0.56f, 0.89f);
        readonly Color _defSectionToolbarTextColor = Color.white;
        readonly GUIContent _gcMinPixelDragDistance = new GUIContent("Min Drag Activation Distance",
            "Minimum pixels the mouse must move during dragging before the drag operation really starts");
        readonly GUIContent _gcMaxSnapPixelDistance = new GUIContent("Max Snap Distance",
            "Max distance at which elements will snap to when dragging them with the ALT key pressed");
        Vector2 _scrollP;

        #region Public Methods

        public void Refresh() {}

        public override void Draw(Rect drawArea)
        {
            base.Draw(drawArea);

            editor.MarkForUndo(true, false, true);
            DOEGUI.BeginGUI();

            // Toolbar
            using (new DeGUILayout.ToolbarScope()) {
                GUILayout.Label("Settings", DOEGUI.Styles.label.toolbar, GUILayout.ExpandWidth(true));
                GUILayout.Label("v" + DOTweenTimelineSettings.Version, DOEGUI.Styles.label.toolbar, GUILayout.ExpandWidth(false));
                using (new DeGUI.ColorScope(new DeSkinColor(0.7f, 0.3f))) {
                    if (GUILayout.Button("×", DOEGUI.Styles.button.tool, GUILayout.Width(18))) {
                        DOTweenClipTimeline.mode = DOTweenClipTimeline.Mode.Default;
                    }
                }
            }

            _scrollP = GUILayout.BeginScrollView(_scrollP);

            using (new DeGUI.LabelFieldWidthScope(224)) {
                // Runtime settings
                using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Main, "RUNTIME Settings")) {
                    EditorGUILayout.HelpBox("Log behaviour (errors-only, errors+warnings or verbose) is inherited from DOTween preferences", MessageType.Info);
                    runtimeSettings.foo_debugLogs = EditorGUILayout.Toggle("Debug Logs", runtimeSettings.foo_debugLogs, GUILayout.ExpandWidth(false));
                }


                // Editor settings
                using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Main, "EDITOR Settings")) {
                    using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Section, "Timeline")) {
                        settings.enforceTargetTypeInClipElement = EditorGUILayout.Toggle(
                            new GUIContent("Enforce Existing Target Type", "If toggled you won't be able to drag a different target type on an existing clip element"),
                            settings.enforceTargetTypeInClipElement, GUILayout.ExpandWidth(false)
                        );
                        settings.actionsLayoutDuration = EditorGUILayout.Slider("Actions/Events Span", settings.actionsLayoutDuration, 0.25f, 2, Width());
                        settings.minPixelDragDistance = EditorGUILayout.IntSlider(_gcMinPixelDragDistance, settings.minPixelDragDistance, 5, 50, Width());
                        settings.maxSnapPixelDistance = EditorGUILayout.IntSlider(_gcMaxSnapPixelDistance, settings.maxSnapPixelDistance, 10, 100, Width());
                        using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Subsection, "Default settings for new tweens")) {
                            settings.defaults.duration = EditorGUILayout.Slider("Duration", settings.defaults.duration, 0, 100, Width());
                            settings.defaults.ease = (Ease)EditorGUILayout.EnumPopup("Ease", settings.defaults.ease, Width());
                            settings.defaults.loopType = (LoopType)EditorGUILayout.EnumPopup("Loop Type", settings.defaults.loopType, Width());
                        }
                        using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Section, "Inspectors")) {
                            settings.forceFoldoutsOpen = EditorGUILayout.Toggle("Force Inspector Foldouts Open", settings.forceFoldoutsOpen, GUILayout.ExpandWidth(false));
                        }
                        using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Section, "Hierarchy")) {
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                settings.evidenceClipComponentInHierarchy = EditorGUILayout.Toggle("Show DOTweenClip icons", settings.evidenceClipComponentInHierarchy, GUILayout.ExpandWidth(false));
                                using (new EditorGUI.DisabledScope(!settings.evidenceClipComponentInHierarchy)) {
                                    settings.evidenceClipInHierarchy = EditorGUILayout.Toggle("└ Include clips in custom Components", settings.evidenceClipInHierarchy, GUILayout.ExpandWidth(false));
                                }
                                if (check.changed) {
                                    DOTimelineHierarchy.Refresh();
                                    EditorApplication.RepaintHierarchyWindow();
                                }
                            }
                        }
                    }
                    using (new DOScope.SettingsPanel.SectionScope(DOScope.SettingsPanel.Type.Section, "Experimental (use at your own risk)", DOEGUI.Colors.global.orange)) {
                        using (new DeGUI.ColorScope(null, null, Color.yellow)) {
                            settings.experimental.enableRecordMode = EditorGUILayout.Toggle("Enable Record Mode", settings.experimental.enableRecordMode, GUILayout.ExpandWidth(false));
                        }
                    }
                }
            }

            GUILayout.EndScrollView();
            if (GUI.changed) editor.MarkDirty(true, false, true);
        }

        GUILayoutOption Width()
        {
            return GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 300);
        }

        #endregion
    }
}