// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/03/02

using DG.DemiEditor;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(DOTweenClipElement))]
    public class DOTweenClipElementPropertyDrawer : PropertyDrawer
    {
        readonly GUIContent _gcError = new GUIContent(
            "<b>RED ALERT!</b>\nYou serialized a <b>DOTweenClipElement</b> instead of a <b>DOTweenClip</b>");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DeGUI.BeginGUI();

            position = EditorGUI.PrefixLabel(position, label);
            using (new DeGUI.ColorScope(new Color(0.74f, 0f, 0.16f))) {
                GUI.Label(position, _gcError, DOEGUI.Styles.global.errorBoxLabel);
            }
        }
    }
}