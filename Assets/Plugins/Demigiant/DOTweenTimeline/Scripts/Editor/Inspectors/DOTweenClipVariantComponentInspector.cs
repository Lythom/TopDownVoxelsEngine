// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/17

using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.TimelineEditor.PropertyDrawers;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor.Inspectors
{
    [CustomEditor(typeof(DOTweenClipVariantComponent))]
    class DOTweenClipVariantComponentInspector : Editor
    {
        static readonly GUIContent _GcLabel = new GUIContent("Original Clip");
        DOTweenClipVariantComponent _src;

        #region Unity and GUI Methods

        void OnEnable()
        {
            _src = target as DOTweenClipVariantComponent;
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(_src, "DOTweenClipVariantComponent");
            DOEGUI.BeginGUI();

            using (new GUILayout.VerticalScope(DOEGUI.Styles.comps.clipHeaderBox)) {
                _src.killTweensOnDestroy = DeGUILayout.ToggleButton(_src.killTweensOnDestroy, TimelineGUIContent.KillOnDestroy, ToggleColors.Critical);
                using (new GUILayout.HorizontalScope()) {
                    const int labelW = 61;
                    GUILayout.Label("On Enable", GUILayout.Width(labelW));
                    _src.onEnableBehaviour = (OnEnableBehaviour)EditorGUILayout.EnumPopup(_src.onEnableBehaviour);
                    GUILayout.Space(4);
                    GUILayout.Label("On Disable", GUILayout.Width(labelW + 4));
                    _src.onDisableBehaviour = (OnDisableBehaviour)EditorGUILayout.EnumPopup(_src.onDisableBehaviour);
                }
            }

            DOTweenClipVariantPropertyDrawer.Internal_ClipVariantField(_src, _GcLabel, _src.clipVariant);

            if (GUI.changed) EditorUtility.SetDirty(_src);
        }

        #endregion
    }
}