// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/22

using UnityEngine;

namespace DG.Tweening.Timeline.Core
{
    /// <summary>
    /// Runtime settings for DOTween Timeline
    /// </summary>
    public class DOTweenTimelineSettings : ScriptableObject
    {
        public const string Version = "0.9.756";
        public const string ResourcePath = "DOTweenTimelineSettings";

        #region Serialized
#pragma warning disable 0649

        public bool foo_debugLogs;

#pragma warning restore 0649
        #endregion

        public static DOTweenTimelineSettings I { get {
            if (_instance == null) _instance = Resources.Load<DOTweenTimelineSettings>(ResourcePath);
            return _instance;
        }}
        static DOTweenTimelineSettings _instance;
        public bool debugLogs { get { return foo_debugLogs && isApplicationPlaying; } }
        public static bool isApplicationPlaying {
            get {
                if (!_foo_isApplicationPlayingSet) {
                    _foo_isApplicationPlaying = Application.isPlaying;
                    _foo_isApplicationPlayingSet = true;
                }
                return _foo_isApplicationPlaying;
            }
        }
        static bool _foo_isApplicationPlaying;
        static bool _foo_isApplicationPlayingSet;
        public static bool addTargetToNestedTweens {
            get {
                if (!_foo_addTargetToNestedTweensSet) {
                    _foo_addTargetToNestedTweensSet = true;
                    _foo_addTargetToNestedTweens = DOTween.useSafeMode && DOTween.debugMode && DOTween.debugStoreTargetId;
                }
                return _foo_addTargetToNestedTweens;
            }
        }
        static bool _foo_addTargetToNestedTweens = true; // If TRUE chains a SetTarget when creating a nested tween
        static bool _foo_addTargetToNestedTweensSet;

#if UNITY_EDITOR
        static DOTweenTimelineSettings()
        {
            UnityEditor.EditorApplication.playModeStateChanged += Editor_OnPlayModeStateChanged;
        }
        static void Editor_OnPlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            _foo_isApplicationPlayingSet = false;
        }
#endif
    }
}