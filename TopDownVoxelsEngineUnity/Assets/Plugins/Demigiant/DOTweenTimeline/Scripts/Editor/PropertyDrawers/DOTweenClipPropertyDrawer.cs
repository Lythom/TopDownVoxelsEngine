// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/26

using System;
using System.Collections.Generic;
using DG.DemiEditor;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using DG.Tweening.TimelineEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace DG.Tweening.TimelineEditor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(DOTweenClip))]
    public class DOTweenClipPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent _GcDrag = new GUIContent("≡");
        static readonly GUIContent _GcEdit = new GUIContent("EDIT", "Open this DOTweenClip in DOTween's Timeline");
        static readonly GUIContent _GcDelete = new GUIContent("×", "Delete this DOTweenClip");
        static DOTimelineEditorSettings _settings;
        static DOTweenClip[] _stub;
        static readonly Dictionary<string, bool> _ClipGuidToFoldout = new Dictionary<string, bool>();
        static readonly Dictionary<string, SerializedProperty> _ClipGuidToSoClip = new Dictionary<string, SerializedProperty>();
        static readonly List<SerializedProperty> _TmpSOClips = new List<SerializedProperty>();
        static readonly HashSet<string> _TmpClipsGUIDs = new HashSet<string>();

        #region GUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetHeight(property.serializedObject.targetObject as Component, property.CastTo<DOTweenClip>(), property, false, false);
        }

        static float GetHeight(Component src, DOTweenClip clip, SerializedProperty soClip, bool isClipComponentMode, bool isClipCollectionMode)
        {
            if (_settings == null) _settings = DOTimelineEditorSettings.Load();
            bool isFoldout = isClipComponentMode || _settings.forceFoldoutsOpen || GetFoldout(clip.guid);
            int lines = !isFoldout
                ? 1
                : isClipCollectionMode
                    ? 7
                    : 6;
            if (TimelineSession.showClipGuid) lines++;
            float h = !isFoldout
                ? PropDClip.LineHeight + PropDClip.LinesOffsetY
                : (PropDClip.LineHeight + PropDClip.LinesOffsetY) * (lines - 2)
                  + (PropDClip.SmlLineHeight + PropDClip.LinesOffsetY) * 2
                  + PropDClip.ExtraBottomOpenMargin;
            if (clip != null) {
                if (isFoldout) {
                    if (clip.hasOnRewind) h += PropDClip.GetSoOnRewind(soClip, clip.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                    if (clip.hasOnComplete) h += PropDClip.GetSoOnComplete(soClip, clip.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                    if (clip.hasOnStepComplete) h += PropDClip.GetSoOnStepComplete(soClip, clip.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                    if (clip.hasOnUpdate) h += PropDClip.GetSoOnUpdate(soClip, clip.guid).GetUnityEventHeight() + PropDClip.LinesOffsetY;
                }
            }
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DOTweenClip clip = property.CastTo<DOTweenClip>();
            if (clip == null) return;
            DoField(position, label, property.serializedObject.targetObject as Component, clip, property, false, false, null, ref _stub);
            if (GUI.changed) {
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Meant to be used only by <see cref="DOTweenClipComponent"/> Inspector
        /// </summary>
        internal static void Internal_ClipField(Component src, GUIContent label, DOTweenClip clip, Editor editor)
        {
            SerializedProperty soClip = GetSoClip(src, clip.guid);
            float h = GetHeight(src, clip, soClip, true, false);
            Rect r = GUILayoutUtility.GetRect(10, 10000, h, h);
            DoField(r, label, src, clip, soClip, true, false, editor, ref _stub);
        }

        /// <summary>
        /// Meant to be used only by <see cref="DOTweenClipCollection"/> Inspector
        /// </summary>
        internal static void Internal_ClipCollectionField(
            Component src, DOTweenClip clip,
            bool isClipCollectionMode, Editor editor, ref DOTweenClip[] clipsList, int index = -1
        ){
            SerializedProperty soClip = GetSoClip(src, clip.guid);
            float h = GetHeight(src, clip, soClip, false, true);
            Rect r = GUILayoutUtility.GetRect(10, 10000, h, h);
            DoField(r, null, src, clip, soClip, false, isClipCollectionMode, editor, ref clipsList, index);
        }

        static void DoField(
            Rect r, GUIContent label, Component src, DOTweenClip clip, SerializedProperty soClip,
            bool isClipComponentMode, bool isClipCollectionMode, Editor editor, ref DOTweenClip[] clipCollectionModeClips, int index = -1
        ){
            if (Event.current.type == EventType.Layout) return;

            const int miniButtonW = 18;
            const int editW = 40;
            if (soClip == null) soClip = GetSoClip(src, clip.guid);
            bool forceClearAtEnd = false; // Set to TRUE when all data needs to be re-acquired (like after dragging/rearranging a clip in a collection)
            DOEGUI.BeginGUI();
            Undo.RecordObject(src, "DOTweenClip");
            bool hasModifiedProperties = soClip.serializedObject.hasModifiedProperties;
            soClip.serializedObject.ApplyModifiedProperties();
            if (hasModifiedProperties) ValidateAllClips(src, soClip.serializedObject); // Visual Inspector List fix
            soClip.serializedObject.Update();

            // --------------------------------
            // [content]
            // └[drag][foldout][label][subcontent]
            //                        └[isActive][name][edit][delete]
            Rect indentedR = EditorGUI.IndentedRect(r);
            float indentedOffset = r.width - indentedR.width;
            using (new DeGUI.LabelFieldWidthScope(EditorGUIUtility.labelWidth - indentedOffset)) {
                Rect contentR = indentedR.SetHeight(EditorGUIUtility.singleLineHeight); // Full line
                Rect subcontentR = isClipComponentMode ? contentR.ShiftXAndResize(0)
                    : isClipCollectionMode || label == null // Line without eventual label
                        ? contentR.ShiftXAndResize(miniButtonW * 2)
                        : contentR.ShiftXAndResize(EditorGUIUtility.labelWidth + 2);
                Rect dragR = contentR.SetWidth(isClipCollectionMode ? miniButtonW : 0); // ClipCollection only
                Rect foldoutR = contentR.SetX(dragR.xMax)
                    .SetWidth(isClipCollectionMode || label == null ? miniButtonW : EditorGUIUtility.labelWidth);
                Rect labelR = contentR.SetX(foldoutR.xMax) // Non-ClipCollection only
                    .SetWidth(isClipCollectionMode || label == null ? 0 : EditorGUIUtility.labelWidth - foldoutR.width);
                Rect deleteR = subcontentR.ShiftXAndResize(subcontentR.width - miniButtonW); // ClipCollection only
                Rect editR = isClipCollectionMode
                    ? subcontentR.SetX(deleteR.x - 2 - editW).SetWidth(editW)
                    : subcontentR.SetX(subcontentR.xMax - editW).SetWidth(editW);
                Rect isActiveR = subcontentR.SetWidth(16);
                Rect nameR = subcontentR.SetX(isActiveR.xMax)
                    .SetWidth(editR.x - isActiveR.xMax - 2);

                int currIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; // Without this indented GUI draws wrongly for some weird reason (Unity bug)
                
                // Drag (ClipCollection only)
                if (isClipCollectionMode) {
                    if (DeGUI.PressButton(dragR, _GcDrag, GUI.skin.button)) {
                        DeGUIDrag.StartDrag(editor, clipCollectionModeClips, index);
                    }
                }
                // Foldout
                bool isFoldout = isClipComponentMode || _settings.forceFoldoutsOpen || GetFoldout(clip.guid);
                if (isClipComponentMode) {
                    GUI.Label(labelR, label);
                } else {
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        isFoldout = label != null
                            ? DeGUI.FoldoutLabel(foldoutR.Shift(-3, 0, 3, 0), isFoldout, label) || _settings.forceFoldoutsOpen
                            : DeGUI.FoldoutLabel(foldoutR, isFoldout, GUIContent.none) || _settings.forceFoldoutsOpen;
                        if (check.changed) SetFoldout(clip.guid, isFoldout);
                    }
                }
                // Active
                using (new DeGUI.ColorScope(null, null, clip.isActive ? Color.green : Color.red)) {
                    clip.isActive = EditorGUI.Toggle(isActiveR, clip.isActive);
                    if (!clip.isActive) {
                        DeGUI.DrawColoredSquare(
                            new Rect(isActiveR.x + 4, isActiveR.center.y, isActiveR.width - 10, 1), DeGUI.IsProSkin ? Color.red : Color.white
                        );
                    }
                }
                // Name + Edit
                using (new EditorGUI.DisabledScope(!clip.isActive)) {
                    clip.name = EditorGUI.TextField(nameR, clip.name);
                    using (new DeGUI.ColorScope(DOEGUI.Colors.global.purple)) {
                        if (GUI.Button(editR, _GcEdit, DOEGUI.Styles.timeline.seqBtEdit)) {
                            DOTweenClipTimeline.ShowWindow(src, clip, soClip);
                        }
                    }
                }
                // Delete (ClipCollection only)
                if (isClipCollectionMode) {
                    using (new DeGUI.ColorScope(Color.red)) {
                        if (GUI.Button(deleteR, _GcDelete)) {
                            DeEditorUtils.Array.RemoveAtIndexAndContract(ref clipCollectionModeClips, index);
                            GUI.changed = true;
                        }
                    }
                }

                if (TimelineSession.showClipGuid) {
                    PropDClip.ShiftRects(PropDClip.LineHeight, ref contentR, ref subcontentR, out labelR);
                    GUI.Label(contentR, clip.guid);
                }

                if (isFoldout) {
                    PropDClip.DrawSettings(contentR, clip, soClip, isClipComponentMode, isClipCollectionMode);
                }
                EditorGUI.indentLevel = currIndentLevel;
            }

            if (isClipCollectionMode) {
                var dragResult = DeGUIDrag.Drag(clipCollectionModeClips, index, r);
                if (dragResult.outcome == DeDragResultType.Accepted) {
                    GUI.changed = forceClearAtEnd = true;
                    // Close clip if it was opened (so all data is refreshed when reopening it)
                    DOTweenClip draggedClip = clipCollectionModeClips[dragResult.movedToIndex];
                    if (DOTweenClipTimeline.clip != null && DOTweenClipTimeline.clip.guid == draggedClip.guid) DOTweenClipTimeline.CloseCurrentClip();
                }
            }
            if (GUI.changed) {
                soClip.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(src);
            }
            if (forceClearAtEnd) ForceClear();
            return;
        }

        static bool GetFoldout(string clipGuid)
        {
            if (!_ClipGuidToFoldout.ContainsKey(clipGuid)) {
                _ClipGuidToFoldout.Add(clipGuid, false);
            }
            return _ClipGuidToFoldout[clipGuid];
        }
        static void SetFoldout(string clipGuid, bool foldout)
        {
            if (!_ClipGuidToFoldout.ContainsKey(clipGuid)) {
                _ClipGuidToFoldout.Add(clipGuid, foldout);
            } else _ClipGuidToFoldout[clipGuid] = foldout;
        }

        #endregion

        #region Public Methods

        // Called by DOTweenClipTimeline when prefab editing mode changes
        public static void ForceClear()
        {
            _ClipGuidToSoClip.Clear();
            PropDClip.ForceClear();
        }

        #endregion

        #region Methods

        static SerializedProperty GetSoClip(Component src, string clipGuid)
        {
            if (!_ClipGuidToSoClip.ContainsKey(clipGuid)) {
                _ClipGuidToSoClip.Add(clipGuid, TimelineEditorUtils.GetSerializedClip(src, clipGuid));
            }
            try {
                if (_ClipGuidToSoClip[clipGuid].serializedObject.targetObject == null) throw new Exception("SO Destroyed");
                bool val = _ClipGuidToSoClip[clipGuid].serializedObject.hasModifiedProperties;
            } catch {
                // SerializedObject has been disposed, regenerate
                _ClipGuidToSoClip.Remove(clipGuid);
                _ClipGuidToSoClip.Add(clipGuid, TimelineEditorUtils.GetSerializedClip(src, clipGuid));
            }
            return _ClipGuidToSoClip[clipGuid];
        }

        /// <summary>
        /// Visual Inspector List fix: Validates all clips on the component and if it finds one with the same GUID as another resets it
        /// (it happens when a new array element is created in the Inspector as a copy of the previous one)
        /// </summary>
        static void ValidateAllClips(Component src, SerializedObject so)
        {
//            Debug.Log(string.Format("<color=#00ff00>VALIDATE CLIPS (fired from #{0} change)</color>", clip.guid));
            _TmpSOClips.Clear();
            _TmpClipsGUIDs.Clear();
            int totChanged = 0;
            TimelineEditorUtils.GetAllSerializedClipsInComponent(src, so, _TmpSOClips);
            for (int i = 0; i < _TmpSOClips.Count; ++i) {
                SerializedProperty p = _TmpSOClips[i];
                DOTweenClip s = p.CastTo<DOTweenClip>();
                if (s == null) continue;
                if (_TmpClipsGUIDs.Contains(s.guid)) {
                    // Change guid of element and partially reset clip
                    totChanged++;
                    s.Editor_Reset(true, true);
                }
                _TmpClipsGUIDs.Add(s.guid);
            }
            _TmpSOClips.Clear();
            _TmpClipsGUIDs.Clear();
            if (totChanged > 0) {
//                Debug.Log(string.Format("   Recreated GUIDs and partially reset {0} DOTweenClips", totChanged));
                so.ApplyModifiedProperties();
            }
        }

        #endregion
    }
}