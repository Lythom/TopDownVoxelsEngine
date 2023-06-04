// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/15

using System;
using System.Collections.Generic;
using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DG.Tweening.TimelineEditor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(DOTweenClipVariant))]
    public class DOTweenClipVariantPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent _GcClip = new GUIContent("- Select DOTweenClip", "Reference to DOTweenClip you want this variant to be linked to");
        static readonly GUIContent _GcOpenClipArrow = new GUIContent("►", "Open this DOTweenClip in the Timeline");
        static readonly GUIContent _GcOverrideClipSettings = new GUIContent("Override Clip Settings");
        static readonly GUIContent _GcInvert = new GUIContent("← Invert", "If toggled inverts the animation, loops included, meaning that" +
                                                                                         " PlayForward will play it from the end to the beginning" +
                                                                                         " and PlayBackwards from the beginning to the end");
        static readonly GUIContent _GcArrow = new GUIContent("►");
        static readonly GUIContent _GcDot = new GUIContent("●");
        const int _Offset = 3;
        const int _SettingsDistance = 4;
        const int _SettingsOpenBottomPadding = 5;
        static DOTweenClipVariant _currClipVariant;
        static DOTweenClip _currClip;
        static DOTweenClipVariant _cmClipVariant;
        static SerializedProperty _cmClipVariantSP;
        static readonly Dictionary<string,SerializedProperty> _ClipVariantGuidToSoClipVariant = new Dictionary<string, SerializedProperty>();
        static readonly List<Object> _TmpTargets = new List<Object>();
        static readonly List<SerializedProperty> _TmpSOClipVariants = new List<SerializedProperty>();
        static readonly HashSet<string> _TmpClipVariantsGUIDs = new HashSet<string>();

        #region GUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetHeight(property.CastTo<DOTweenClipVariant>(), property);
        }

        static float GetHeight(DOTweenClipVariant clipVariant, SerializedProperty soClip)
        {
            if (clipVariant == null) return 0;

            Refresh(clipVariant);
            bool isFoldout = _currClipVariant.editor_foldout;
            int lines = TimelineSession.showClipGuid ? 3 : 1;
            if (isFoldout) {
                lines += _currClipVariant.targetSwaps.Length + (_currClipVariant.overrideClipSettings ? 6 : 1);
            }
            float h = lines * EditorGUIUtility.singleLineHeight + _Offset;
            if (isFoldout) {
                h += _SettingsDistance + (_currClipVariant.overrideClipSettings ? _SettingsOpenBottomPadding : 1);
                if (_currClipVariant.overrideClipSettings) {
                    if (clipVariant.hasOnRewind) h += PropDClip.GetSoOnRewind(soClip, clipVariant.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                    if (clipVariant.hasOnComplete) h += PropDClip.GetSoOnComplete(soClip, clipVariant.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                    if (clipVariant.hasOnStepComplete) h += PropDClip.GetSoOnStepComplete(soClip, clipVariant.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                    if (clipVariant.hasOnUpdate) h += PropDClip.GetSoOnUpdate(soClip, clipVariant.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                }
            }
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Event.current.type == EventType.Layout) return;
            DoField(position, label, property.serializedObject.targetObject as Component, property.CastTo<DOTweenClipVariant>(), property);
        }

        /// <summary>
        /// Meant to be used only by <see cref="DOTweenClipVariantComponent"/> Inspector
        /// </summary>
        internal static void Internal_ClipVariantField(Component src, GUIContent label, DOTweenClipVariant clipVariant)
        {
            SerializedProperty soClipVariant = null;
            if (_ClipVariantGuidToSoClipVariant.ContainsKey(clipVariant.guid)) {
                soClipVariant = _ClipVariantGuidToSoClipVariant[clipVariant.guid];
                if (soClipVariant.serializedObject.targetObject == null) {
                    // Same GUID of another clip in another scene previously opened, so we're caching the wrong object
                    soClipVariant = null;
                    _ClipVariantGuidToSoClipVariant.Remove(clipVariant.guid);
                }
            }
            if (soClipVariant == null) {
                soClipVariant = new SerializedObject(src).FindProperty("clipVariant");
                _ClipVariantGuidToSoClipVariant.Add(clipVariant.guid, soClipVariant);
            }
            float h = GetHeight(clipVariant, soClipVariant);
            Rect r = GUILayoutUtility.GetRect(10, 10000, h, h);
            DoField(r, label, src, clipVariant, soClipVariant, true);
        }

        static void DoField(
            Rect r, GUIContent label, Component src, DOTweenClipVariant clipVariant, SerializedProperty soClipVariant, bool isComponentMode = false
        ){
            if (Event.current.type == EventType.Layout) return;
            if (clipVariant == null) return;

            Refresh(clipVariant, true);
            bool clipSet = _currClip != null;
            DOEGUI.BeginGUI();
            Undo.RecordObject(soClipVariant.serializedObject.targetObject, "DOTweenClipVariant");
            bool hasModifiedProperties = soClipVariant.serializedObject.hasModifiedProperties;
            soClipVariant.serializedObject.ApplyModifiedProperties();
            if (hasModifiedProperties) ValidateAllClips(src, soClipVariant.serializedObject); // Visual Inspector List fix
            soClipVariant.serializedObject.Update();

            if (TimelineSession.showClipGuid) {
                Rect guidR = r.SetHeight(EditorGUIUtility.singleLineHeight * 2);
                r = r.Shift(0, guidR.height, 0, -guidR.height);
                GUI.Label(guidR, string.Format("{0}\n└ {1}", clipVariant.guid, _currClip == null ? "[ - ]" : _currClip.guid));
            }

            Rect lineR = r.SetHeight(EditorGUIUtility.singleLineHeight);
            Rect foldoutR = lineR.SetWidth(EditorGUIUtility.labelWidth);
            Rect clipR = lineR.ShiftXAndResize(foldoutR.width + 2);
            if (clipSet) clipR = clipR.Shift(0, 0, -20, 0);
            Rect btOpenClipR = lineR.HangToRightAndResize(clipR.xMax);
            if (isComponentMode) {
                GUI.Label(foldoutR.Shift(-3, 0, 3, 0), label);
            } else {
                _currClipVariant.editor_foldout = DeGUI.FoldoutLabel(foldoutR.Shift(-3, 0, 3, 0), _currClipVariant.editor_foldout, label);
            }
            using (new DeGUI.ColorScope(null, null, clipSet ? DOEGUI.Colors.global.green : Color.white)) {
                _GcClip.text = !clipSet ? "- Select DOTweenClip" : _currClip.name;
                if (GUI.Button(clipR, _GcClip, EditorStyles.popup)) {
                    _cmClipVariant = _currClipVariant;
                    _cmClipVariantSP = soClipVariant;
                    TimelineEditorUtils.CM_SelectClipInScene(clipR, _currClip, true, OnClipSelected);
                }
            }
            if (clipSet) {
                using (new DeGUI.ColorScope(DOEGUI.Colors.global.purple)) {
                    if (GUI.Button(btOpenClipR, _GcOpenClipArrow, DOEGUI.Styles.timeline.seqBtEdit)) {
                        DOTweenClipTimeline.ShowWindow(_currClipVariant.clipComponent, _currClip, null);
                    }
                }
            }

            if (clipSet && (isComponentMode || _currClipVariant.editor_foldout)) {
                Rect contentR;
                lineR = lineR.ShiftY(_Offset);
                // Target swaps
                foreach (DOTweenClipVariant.TargetSwap tSwap in _currClipVariant.targetSwaps) {
                    bool hasReplacement = tSwap.newTarget != null;
                    lineR = lineR.ShiftY(EditorGUIUtility.singleLineHeight);
                    contentR = lineR.ShiftXAndResize(isComponentMode ? 6 : 20);
                    Rect originalR = contentR.SetWidth((int)(contentR.width * 0.5f) - 6);
                    Rect arrowR = contentR.HangToRightAndResize(originalR.xMax).SetWidth(12);
                    Rect replaceR = contentR.HangToRightAndResize(arrowR.xMax);
                    Type targetT = tSwap.originalTarget.GetType();
                    using (new DeGUI.ColorScope(null, null, hasReplacement ? Color.green : Color.red)) {
                        GUI.Label(contentR.SetWidth(contentR.height).ShiftX(-9), _GcDot, DOEGUI.Styles.global.whiteLabel);
                    }
                    GUI.Label(originalR, string.Format("{0} ({1})", tSwap.originalTarget.name, TimelineEditorUtils.GetCleanType(targetT)));
                    GUI.Label(arrowR, _GcArrow);
                    using (new DeGUI.ColorScope(hasReplacement ? Color.green : Color.red)) {
                        tSwap.newTarget = EditorGUI.ObjectField(replaceR, tSwap.newTarget, targetT, true);
                    }
                }
                // Settings
                lineR = lineR.ShiftY(EditorGUIUtility.singleLineHeight + _SettingsDistance);
                contentR = lineR.ShiftXAndResize(isComponentMode ? 0 : 12);
                Rect invertR = contentR.SetWidth(80);
                Rect overrideR = contentR.HangToRightAndResize(invertR.xMax);
                clipVariant.overrideClipSettings = DeGUI.ToggleButton(overrideR, clipVariant.overrideClipSettings, _GcOverrideClipSettings);
                clipVariant.invert = DeGUI.ToggleButton(invertR, clipVariant.invert, _GcInvert, ToggleColors.Yellow);
                if (clipVariant.overrideClipSettings) {
                    // Rect settingsR = lineR.Shift(10, 0, -10, 0);
                    Rect settingsR = lineR;
                    PropDClip.DrawSettings(settingsR, clipVariant, soClipVariant, isComponentMode);
                }
            }

            if (GUI.changed) soClipVariant.serializedObject.ApplyModifiedProperties();
        }

        static void OnClipSelected(DOTweenClip clip, Component clipComponent)
        {
            Undo.RecordObject(_cmClipVariantSP.serializedObject.targetObject, "DOTweenClipVariant");
            _currClip = clip;
            _cmClipVariant.clipGuid = clip == null ? null : clip.guid;
            _cmClipVariant.clipComponent = clipComponent;
            Refresh(_cmClipVariant, true);
            EditorUtility.SetDirty(_cmClipVariantSP.serializedObject.targetObject);
        }

        #endregion

        #region Methods

        static void Refresh(DOTweenClipVariant clipVariant, bool forceRefresh = false)
        {
            if (!forceRefresh && _currClipVariant != null && _currClipVariant.guid == clipVariant.guid) return;

            _currClipVariant = clipVariant;
            _currClip = null;

            if (!string.IsNullOrEmpty(clipVariant.clipGuid) && clipVariant.clipComponent != null) {
                bool wasInsideNestedObj = false;
                TimelineEditorUtils.SelectionClip selClip = TimelineEditorUtils.FindSerializedClipInComponent(
                    clipVariant.clipGuid, clipVariant.clipComponent, ref wasInsideNestedObj
                );
                if (selClip != null) {
                    _currClip = selClip.clip;
                    clipVariant.lookForClipInNestedObjs = wasInsideNestedObj;
                }
            }

            if (_currClip == null) {
                // Reset
                clipVariant.Editor_Reset(false);
            } else {
                // Store unique clipElements targets
                _TmpTargets.Clear();
                foreach (DOTweenClipElement clipElement in _currClip.elements) {
                    switch (clipElement.type) {
                    case DOTweenClipElement.Type.Tween:
                    case DOTweenClipElement.Type.Action:
                        break;
                    default:
                        continue;
                    }
                    if (clipElement.target == null || _TmpTargets.Contains(clipElement.target)) continue;
                    _TmpTargets.Add(clipElement.target);
                }
                int totUniqueTargets = _TmpTargets.Count;
                if (totUniqueTargets > 0) {
                    // Sort targets by name
                    _TmpTargets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.InvariantCultureIgnoreCase));
                }
                // Remove clipVariant targetSwaps if they don't exist among unique targets
                for (int i = 0; i < clipVariant.targetSwaps.Length; i++) {
                    DOTweenClipVariant.TargetSwap tSwap = clipVariant.targetSwaps[i];
                    if (_TmpTargets.Contains(tSwap.originalTarget)) continue;
                    DeEditorUtils.Array.RemoveAtIndexAndContract(ref clipVariant.targetSwaps, i);
                    --i;
                }
                // Remove unique targets if they already exist in clipVariant
                foreach (DOTweenClipVariant.TargetSwap tSwap in clipVariant.targetSwaps) {
                    if (!_TmpTargets.Contains(tSwap.originalTarget)) continue;
                    _TmpTargets.Remove(tSwap.originalTarget);
                }
                // Assign missing ones to the clipVariant
                int totToAdd = _TmpTargets.Count;
                if (totToAdd > 0) {
                    int startIndex = clipVariant.targetSwaps.Length;
                    int newSize = clipVariant.targetSwaps.Length + totToAdd;
                    Array.Resize(ref clipVariant.targetSwaps, newSize);
                    for (int i = startIndex; i < newSize; ++i) {
                        clipVariant.targetSwaps[i] = new DOTweenClipVariant.TargetSwap(_TmpTargets[i - startIndex]);
                    }
                }
                _TmpTargets.Clear();
            }
        }

        /// <summary>
        /// Visual Inspector List fix: Validates all clips on the component and if it finds one with the same GUID as another resets it
        /// (it happens when a new array element is created in the Inspector as a copy of the previous one)
        /// </summary>
        static void ValidateAllClips(Component src, SerializedObject so)
        {
//            Debug.Log(string.Format("<color=#00ff00>VALIDATE CLIPVARIANTS (fired from #{0} change)</color>", clip.guid));
            _TmpSOClipVariants.Clear();
            _TmpClipVariantsGUIDs.Clear();
            int totChanged = 0;
            TimelineEditorUtils.GetAllSerializedClipVariantsInComponent(src, so, _TmpSOClipVariants);
            for (int i = 0; i < _TmpSOClipVariants.Count; ++i) {
                SerializedProperty p = _TmpSOClipVariants[i];
                DOTweenClipVariant s = p.CastTo<DOTweenClipVariant>();
                if (s == null) continue;
                if (_TmpClipVariantsGUIDs.Contains(s.guid)) {
                    // Change guid of element and partially reset clip
                    totChanged++;
                    s.Editor_Reset(true, true);
                }
                _TmpClipVariantsGUIDs.Add(s.guid);
            }
            _TmpSOClipVariants.Clear();
            _TmpClipVariantsGUIDs.Clear();
            if (totChanged > 0) {
//                Debug.Log(string.Format("   Recreated GUIDs and partially reset {0} DOTweenClipVariants", totChanged));
                so.ApplyModifiedProperties();
            }
        }

        #endregion
    }
}