// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/10/06

using System.Collections.Generic;
using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace DG.Tweening.TimelineEditor.PropertyDrawers
{
    /// <summary>
    /// PropertyDrawer utils
    /// </summary>
    public class PropDClip
    {
        public const int LinesOffsetY = 2;
        public const int ExtraBottomOpenMargin = 3;
        public const int SubRowsOffsetX = 12;
        public const int ClipCollectionSubRowsOffsetX = 25;
        public static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        public static readonly float SmlLineHeight = EditorGUIUtility.singleLineHeight - 4;

        static readonly GUIContent _GcStartupBehaviour = new GUIContent("Startup Behavior", "Behaviour to apply when to DOTweenClip is generated");
        static readonly GUIContent _GcAutoplay = new GUIContent("AutoPlay", "If toggled starts playing the DOTweenClip as soon as it's generated");
        static readonly GUIContent _GcAutokill = new GUIContent("Kill On Complete", "If toggled kills the DOTweenClip once it completes," +
                                                                                    " freeing up memory and resources but preventing it from being reused");
        static readonly GUIContent _GcIgnoreTimeScale = new GUIContent("Ignore UTimeScale", "If toggled the DOTweenClip will ignore Unity's timeScale");
        static readonly GUIContent _GcDelay = new GUIContent("Startup Delay");
        static readonly GUIContent _GcTimeScale = new GUIContent("Internal TimeScale", "Clip's internal timeScale");
        static readonly GUIContent _GcOverloadDuration = new GUIContent("Overload Duration", "Clip's overall duration (in this mode the clip elements duration will be considered as a percentage to achieve this overall duration)");
        static readonly GUIContent _GcLoops = new GUIContent("Loops");
        static readonly GUIContent _GcInfiniteLoops = new GUIContent("∞");
        static readonly GUIContent _GcOnRewind = new GUIContent("On Rewind", "Events to call when the DOTweenClip is completely rewound (and also when it completes a PlayBackwards)");
        static readonly GUIContent _GcOnComplete = new GUIContent("On Complete", "Events to call when the DOTweenClip completes all its loops");
        static readonly GUIContent _GcOnStepComplete = new GUIContent("On Step", "Events to call when the DOTweenClip completes each loop cycle");
        static readonly GUIContent _GcOnUpdate = new GUIContent("On Update", "Events to call at each frame the DOTweenClip is updated");

        static readonly Dictionary<string, SerializedProperty> _ClipGuidToSoOnRewind = new Dictionary<string, SerializedProperty>();
        static readonly Dictionary<string, SerializedProperty> _ClipGuidToSoOnComplete = new Dictionary<string, SerializedProperty>();
        static readonly Dictionary<string, SerializedProperty> _ClipGuidToSoOnStepComplete = new Dictionary<string, SerializedProperty>();
        static readonly Dictionary<string, SerializedProperty> _ClipGuidToSoOnUpdate = new Dictionary<string, SerializedProperty>();

        #region Public Methods

        public static void ForceClear()
        {
            _ClipGuidToSoOnRewind.Clear();
            _ClipGuidToSoOnComplete.Clear();
            _ClipGuidToSoOnStepComplete.Clear();
            _ClipGuidToSoOnUpdate.Clear();
        }

        // Returns the total rect occupied by the settings
        public static Rect DrawSettings(Rect contentR, DOTweenClipBase clip, SerializedProperty soClip, bool isClipComponentMode, bool isClipCollectionMode = false)
        {
            Rect resultR;
            float subRowsOffset = isClipComponentMode ? 0 : isClipCollectionMode ? ClipCollectionSubRowsOffsetX : SubRowsOffsetX;
            using (new DeGUI.LabelFieldWidthScope(EditorGUIUtility.labelWidth - subRowsOffset)) {
                resultR = contentR = contentR.ShiftXAndResize(subRowsOffset);
                Rect labelR;
                Rect subcontentR = contentR.ShiftXAndResize(EditorGUIUtility.labelWidth + 2);
                // --------------------------------
                if (isClipCollectionMode) {
                    // Startup Behaviour (ClipCollection only)
                    ShiftRects(LineHeight, ref contentR, ref subcontentR, out labelR);
                    EditorGUI.PrefixLabel(labelR, _GcStartupBehaviour);
                    clip.startupBehaviour = (StartupBehaviour)EditorGUI.EnumPopup(subcontentR, clip.startupBehaviour);
                }
                // --------------------------------
                // Autoplay + Autokill + IgnoreTimeScale
                ShiftRects(SmlLineHeight, ref contentR, ref subcontentR, out labelR);
                Rect autoplayR = contentR.SetWidth((int)(contentR.width / 3) - 1);
                Rect autokillR = autoplayR.SetX(autoplayR.xMax + 2);
                Rect ignoreTimeScaleR = contentR.SetX(autokillR.xMax + 2).SetWidth(contentR.xMax - autokillR.xMax - 2);
                clip.autoplay = DeGUI.ToggleButton(autoplayR, clip.autoplay, _GcAutoplay, DOEGUI.Styles.global.toggleSmlLabel);
                clip.autokill = DeGUI.ToggleButton(autokillR, clip.autokill, _GcAutokill, ToggleColors.Critical, DOEGUI.Styles.global.toggleSmlLabel);
                clip.ignoreTimeScale = DeGUI.ToggleButton(
                    ignoreTimeScaleR, clip.ignoreTimeScale, _GcIgnoreTimeScale, DOEGUI.Styles.global.toggleSmlLabel
                );
                // --------------------------------
                // Startup Delay
                ShiftRects(LineHeight, ref contentR, ref subcontentR, out labelR);
                clip.startupDelay = EditorGUI.Slider(contentR, _GcDelay, clip.startupDelay, 0, 1000);
                // --------------------------------
                // TimeScale/Duration-overload
                ShiftRects(LineHeight, ref contentR, ref subcontentR, out labelR);
                float timeModeW = clip.timeMode == TimeMode.TimeScale ? 86 : 124;
                Rect contentModR = contentR.Shift(0, 0, -timeModeW - 2, 0);
                Rect timeModeR = new Rect(contentModR.xMax + 2, contentR.y, timeModeW, contentR.height);
                switch (clip.timeMode) {
                case TimeMode.TimeScale:
                    clip.timeScale = EditorGUI.Slider(contentModR, _GcTimeScale, clip.timeScale, 0.01f, 100);
                    break;
                case TimeMode.DurationOverload:
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        clip.durationOverload = EditorGUI.FloatField(contentModR, _GcOverloadDuration, clip.durationOverload);
                        if (check.changed) {
                            if (clip.durationOverload < 0) clip.durationOverload = 0;
                        }
                    }
                    break;
                }
                clip.timeMode = (TimeMode)EditorGUI.EnumPopup(timeModeR, clip.timeMode);
                // --------------------------------
                // Loops
                ShiftRects(LineHeight, ref contentR, ref subcontentR, out labelR);
                Rect loopsR = contentR.SetWidth(labelR.width + 58);
                Rect infiniteLoopsR = loopsR.SetX(loopsR.xMax + 2).SetWidth(20);
                Rect loopTypeR = contentR.ShiftXAndResize(infiniteLoopsR.xMax - contentR.x + 2);
                bool infiniteLoops = clip.loops == -1;
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    infiniteLoops = DeGUI.ToggleButton(
                        infiniteLoopsR.Contract(0, 1), infiniteLoops, _GcInfiniteLoops, DOEGUI.Styles.timeline.seqInfiniteLoopToggle
                    );
                    if (check.changed) clip.loops = infiniteLoops ? -1 : 1;
                }
                if (infiniteLoops) {
                    // Draw loops with label and field separate, so I can disable only the field and not the label
                    Rect loopsLabelR = loopsR.Shift(0, 0, -58, 0);
                    Rect loopsFieldR = loopsR.ShiftXAndResize(labelR.width);
                    GUI.Label(loopsLabelR, _GcLoops, DOEGUI.Styles.global.prefixLabel);
                    using (new EditorGUI.DisabledScope(true)) EditorGUI.IntField(loopsFieldR, GUIContent.none, clip.loops);
                } else {
                    using (new EditorGUI.DisabledScope(clip.loops == -1)) {
                        using (var check = new EditorGUI.ChangeCheckScope()) {
                            clip.loops = EditorGUI.IntField(loopsR, _GcLoops, clip.loops);
                            if (check.changed && clip.loops < 1) clip.loops = 1;
                        }
                    }
                }
                using (new EditorGUI.DisabledScope(clip.loops < 2 && clip.loops > -1)) {
                    clip.loopType = (LoopType)EditorGUI.EnumPopup(loopTypeR, clip.loopType);
                }
                // --------------------------------
                // Events
                ShiftRects(SmlLineHeight, ref contentR, ref subcontentR, out labelR);
                Rect onRewindR = contentR.SetWidth(contentR.width / 4 - 1);
                Rect onCompleteR = onRewindR.SetX(onRewindR.xMax + 2);
                Rect onStepCompleteR = onCompleteR.SetX(onCompleteR.xMax + 2);
                Rect onUpdateR = onStepCompleteR.SetX(onStepCompleteR.xMax + 2);
                DrawEventToggle(onRewindR, _GcOnRewind, soClip, ref clip.hasOnRewind, ref clip.onRewind);
                DrawEventToggle(onCompleteR, _GcOnComplete, soClip, ref clip.hasOnComplete, ref clip.onComplete);
                DrawEventToggle(onStepCompleteR, _GcOnStepComplete, soClip, ref clip.hasOnStepComplete, ref clip.onStepComplete);
                DrawEventToggle(onUpdateR, _GcOnUpdate, soClip, ref clip.hasOnUpdate, ref clip.onUpdate);
                Rect eventR = contentR;
                if (clip.hasOnRewind) DrawEvent(ref eventR, _GcOnRewind, GetSoOnRewind(soClip, clip.guid));
                if (clip.hasOnComplete) DrawEvent(ref eventR, _GcOnComplete, GetSoOnComplete(soClip, clip.guid));
                if (clip.hasOnStepComplete) DrawEvent(ref eventR, _GcOnStepComplete, GetSoOnStepComplete(soClip, clip.guid));
                if (clip.hasOnUpdate) DrawEvent(ref eventR, _GcOnUpdate, GetSoOnUpdate(soClip, clip.guid));
                resultR.SetHeight(eventR.yMax - resultR.y);
            }
            return resultR;
        }

        public static void ShiftRects(float newLineHeight, ref Rect contentR, ref Rect subcontentR, out Rect labelR)
        {
            contentR = contentR.Shift(0, contentR.height + LinesOffsetY, 0, 0).SetHeight(newLineHeight);
            subcontentR = subcontentR.SetY(contentR.y).SetHeight(newLineHeight);
            labelR = contentR.SetWidth(EditorGUIUtility.labelWidth + 2);
        }

        public static SerializedProperty GetSoOnRewind(SerializedProperty soClip, string clipGuid)
        {
            return GetSoRelative(_ClipGuidToSoOnRewind, soClip, "onRewind", clipGuid);
        }
        public static SerializedProperty GetSoOnComplete(SerializedProperty soClip, string clipGuid)
        {
            return GetSoRelative(_ClipGuidToSoOnComplete, soClip, "onComplete", clipGuid);
        }
        public static SerializedProperty GetSoOnStepComplete(SerializedProperty soClip, string clipGuid)
        {
            return GetSoRelative(_ClipGuidToSoOnStepComplete, soClip, "onStepComplete", clipGuid);
        }
        public static SerializedProperty GetSoOnUpdate(SerializedProperty soClip, string clipGuid)
        {
            return GetSoRelative(_ClipGuidToSoOnUpdate, soClip, "onUpdate", clipGuid);
        }

        #endregion

        #region Methods

        static void DrawEventToggle(Rect r, GUIContent label, SerializedProperty property, ref bool toggled, ref UnityEvent unityEvent)
        {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                toggled = DeGUI.ToggleButton(r, toggled, label, ToggleColors.Cyan, DOEGUI.Styles.global.toggleSmlLabel);
                if (check.changed) {
                    if (!toggled) unityEvent = new UnityEvent();
                }
            }
        }

        static void DrawEvent(ref Rect r, GUIContent label, SerializedProperty soEvent)
        {
            r = r.Shift(0, r.height + LinesOffsetY, 0, 0).SetHeight(soEvent.GetUnityEventHeight());
            EditorGUI.PropertyField(r, soEvent, label);
        }

        static SerializedProperty GetSoRelative(
            Dictionary<string, SerializedProperty> dict, SerializedProperty soClip, string propertyName, string clipGuid
        ){
            if (!dict.ContainsKey(clipGuid)) {
                dict.Add(clipGuid, soClip.FindPropertyRelative(propertyName));
            }
            try {
                bool val = dict[clipGuid].editable;
            } catch {
                // SerializedObject has been disposed, regenerate
                dict.Remove(clipGuid);
                dict.Add(clipGuid, soClip.FindPropertyRelative(propertyName));
            }
            return dict[clipGuid];
        }

        #endregion
    }
}