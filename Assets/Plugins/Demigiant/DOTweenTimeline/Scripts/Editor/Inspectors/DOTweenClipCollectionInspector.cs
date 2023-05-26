// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/16

using System;
using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.TimelineEditor.PropertyDrawers;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor.Inspectors
{
    [CustomEditor(typeof(DOTweenClipCollection))]
    class DOTweenClipCollectionInspector : Editor
    {
        readonly GUIContent _gcKillAll = new GUIContent("Kill All On Destroy", TimelineGUIContent.KillOnDestroy.tooltip);
        DOTweenClipCollection _src;

        #region Unity and GUI Methods

        void OnEnable()
        {
            _src = target as DOTweenClipCollection;
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(_src, "DOTweenClipCollection");
            DOEGUI.BeginGUI();

            using (new GUILayout.HorizontalScope()) {
                if (GUILayout.Button("+ Add DOTweenClip", DOEGUI.Styles.global.btLayout)) {
                    if (_src.clips == null) _src.clips = new[] {new DOTweenClip(Guid.NewGuid().ToString(), null)};
                    DeEditorUtils.Array.ExpandAndAdd(ref _src.clips, new DOTweenClip(Guid.NewGuid().ToString(), null));
                    GUI.changed = true;
                }
                _src.killTweensOnDestroy = DeGUILayout.ToggleButton(_src.killTweensOnDestroy, _gcKillAll, ToggleColors.Critical);
            }
            if (_src.clips == null) return;

            for (int i = 0; i < _src.clips.Length; ++i) {
                DOTweenClipPropertyDrawer.Internal_ClipCollectionField(_src, _src.clips[i], true, this, ref _src.clips, i);
            }

            if (GUI.changed) EditorUtility.SetDirty(_src);
        }

        #endregion
    }
}