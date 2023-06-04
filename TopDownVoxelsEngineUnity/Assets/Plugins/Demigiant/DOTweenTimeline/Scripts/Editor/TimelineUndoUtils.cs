// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/10/10

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal static class TimelineUndoUtils
    {
        const int _DefTMProMaxVisibleChars = 99999;
        const int _DefTMProMaxVisibleWords = 99999;
        static readonly List<MonoBehaviour> _TMProComponents = new List<MonoBehaviour>();
        static readonly List<GameObject> _TMP_Gos = new List<GameObject>();
        static PropertyInfo _piTMPro_maxVisibleChars;
        static PropertyInfo _piTMPro_maxVisibleWords;

        #region Public Methods

        public static void ClearCache()
        {
            _TMProComponents.Clear();
        }

        public static void RegisterSceneUndo()
        {
            ClearCache();
            _TMP_Gos.Clear();
            TimelineEditorUtils.FindAllGameObjectsInScene(_TMP_Gos, true);
            foreach (GameObject go in _TMP_Gos) {
                MonoBehaviour[] comps = go.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour comp in comps) {
                    if (comp == null) continue;
                    // TextMeshPro special behaviour (not using prefix-only anymore in case someone created a sub-TMPro-namespace custom class
                    string typeFullName = comp.GetType().FullName;
                    if (typeFullName != null && (typeFullName == "TMPro.TextMeshPro" || typeFullName == "TMPro.TextMeshProUGUI")) {
                        _TMProComponents.Add(comp);
                    }
                }
            }
            _TMP_Gos.Clear();
        }

        public static void RestoreAndClearCache()
        {
            // Reset maxVisibleCharacters and words on TMP_Text components
            int tmproLen = _TMProComponents.Count;
            if (tmproLen > 0) {
                if (_piTMPro_maxVisibleChars == null) {
                    _piTMPro_maxVisibleChars = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro").GetProperty("maxVisibleCharacters", BindingFlags.Public | BindingFlags.Instance);
                    _piTMPro_maxVisibleWords = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro").GetProperty("maxVisibleWords", BindingFlags.Public | BindingFlags.Instance);
                }
                foreach (Component tmproComp in _TMProComponents) {
                    _piTMPro_maxVisibleChars.SetValue(tmproComp, _DefTMProMaxVisibleChars);
                    _piTMPro_maxVisibleWords.SetValue(tmproComp, _DefTMProMaxVisibleWords);
                }
            }
            ClearCache();
        }

        #endregion
    }
}