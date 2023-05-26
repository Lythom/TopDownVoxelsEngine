// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/15

using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening.Timeline.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DG.Tweening.Timeline
{
    /// <summary>
    /// Holds a reference to a <see cref="DOTweenClip"/> and allows to generate a tween from the original but with different targets/properties.<para/>
    /// Note that this method uses (fast) Reflection in order to find the original clip at runtime.
    /// </summary>
    [Serializable]
#if UNITY_EDITOR
    public class DOTweenClipVariant : DOTweenClipBase, ISerializationCallbackReceiver
#else
    public class DOTweenClipVariant : DOTweenClipBase
#endif
    {
        #region Serialized
#pragma warning disable 0649

        // [SerializeField] string _guid = Guid.NewGuid().ToString();
        public string clipGuid;
        public bool overrideClipSettings = false;
        public Component clipComponent; // Component containing the clip (to speed up clip retrieval and also keep a reference that is not just a GUID)
        public TargetSwap[] targetSwaps = new TargetSwap[0];
        public bool lookForClipInNestedObjs = false; // If TRUE Reflection will look for the clip inside nestedObjs (set when setting the clip)

        // Editor-only
        public bool editor_foldout = true;

#pragma warning restore 0649
        #endregion

        bool _initialized;
        DOTweenClip _clip;
        static readonly Type _TClip = typeof(DOTweenClip);
        static readonly Type _TClipArray = typeof(DOTweenClip[]);
        static readonly Type _TClipList = typeof(List<DOTweenClip>);
        static readonly Type _TSerializeFieldAttribute = typeof(SerializeField);
        static readonly Type _TNonSerializedAttribute = typeof(NonSerializedAttribute);
        static readonly Type _TUnityObject = typeof(UnityEngine.Object);
        static readonly List<FieldInfo> _TmpFInfos = new List<FieldInfo>();

        public DOTweenClipVariant() {}
        public DOTweenClipVariant(string guid)
        {
            _guid = guid;
        }

#region Unity + INIT

        void Init()
        {
            if (_initialized) return;
            _initialized = true;
            _clip = string.IsNullOrEmpty(clipGuid) || clipComponent == null ? null : FindClip(clipComponent, clipGuid, lookForClipInNestedObjs);
        }

#if UNITY_EDITOR

        /// <summary>INTERNAL USAGE</summary>
        public void OnBeforeSerialize() {}

        /// <summary>INTERNAL USAGE</summary>
        public void OnAfterDeserialize()
        {
            // Visual Inspector List fix: detect if new DOTweenClipVariant was just created as first array element
            // (in which case it's set with all default type values ignoring the ones set by myself, and I need to reset it)
            if (!string.IsNullOrEmpty(_guid)) return;
            Editor_Reset(true);
//            Debug.Log("<color=#ff0000>AFTER DESERIALIZE ► ClipVariant was reset because GUID was empty (meaning it's a newly created element in a visual list):</color> " + _guid);
        }

#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and starts (or restarts) the clipVariant's tween (to be precise, a <see cref="Sequence"/>) and returns it
        /// (unless the original clip's <see cref="DOTweenClip.isActive"/> toggle is disabled).
        /// This method is a more straightforward version of <see cref="GenerateTween"/> which will ignore the clip's
        /// startup behaviour set in the Inspector and will either:<para/>
        /// - If this clipVariant's tween had not been created already or had been killed: creates it and plays it immediately.<para/>
        /// - If this clipVariant's tween already exists: plays it or restarts it based on the given parameters.
        /// </summary>
        /// <param name="restartIfExists">If TRUE (default) and the tween was already generated and exists,
        /// rewinds it and replays it from the beginning, otherwise simply plays it from where it is</param>
        public override Sequence Play(bool restartIfExists = true)
        {
            return GenerateTween(StartupBehaviour.Create, true, restartIfExists);
        }

        /// <summary>
        /// Generates the clipVariant's tween (to be precise, a <see cref="Sequence"/>) and returns it
        /// (unless isActive is set to FALSE/unchecked in the original <see cref="DOTweenClip"/> Inspector
        /// or the tween is immediately killed by a Complete with autoKill active).<para/>
        /// If the tween was already generated simply returns it (eventually rewinding it first)
        /// and plays it according to the <see cref="andPlay"/> parameter
        /// (use <see cref="ForceGenerateTween"/>) if instead you want to force the tween to be killed and regenerated.<para/>
        /// </summary>
        /// <param name="behaviour">Leave as NULL to use the original <see cref="DOTweenClip"/>'s Startup Behaviour</param>
        /// <param name="andPlay">Leave as NULL to use the original <see cref="DOTweenClip"/>'s Autoplay option</param>
        /// <param name="rewindIfExists">If TRUE and the tween already exists rewinds it before returning it</param>
        public override Sequence GenerateTween(StartupBehaviour? behaviour = null, bool? andPlay = null, bool rewindIfExists = true)
        {
            Init();
            if (_clip == null) {
                DOLog.Warning("GenerateTween ► This DOTweenClipVariant tween was not generated because its clip is missing");
                return null;
            }
            bool isApplicationPlaying = DOTweenTimelineSettings.isApplicationPlaying;
            if (isApplicationPlaying && !_clip.isActive) return null;
            if (HasTween()) { // Tween already exists: return it
                if (DOTween.logBehaviour == LogBehaviour.Verbose) {
                    DOLog.Normal("GenerateTween ► This DOTweenClipVariant tween has already been generated. Will rewind and return it.");
                }
                if (rewindIfExists) tween.Rewind();
                bool assignedAndPlay = andPlay == null ? _clip.autoplay : (bool)andPlay;
                if (assignedAndPlay) tween.PlayForward();
                return tween;
            }
            Sequence s = _clip.INTERNAL_DoGenerateTween(
                isApplicationPlaying,
                overrideClipSettings
                    ? TmpSettingsSnapshot.Refresh(this, behaviour, andPlay, invert)
                    : TmpSettingsSnapshot.Refresh(_clip, behaviour, andPlay, invert),
                ConvertTargetSwapsToTargetPairs()
            );
            if (s == null) {
                DOLog.Warning("GenerateTween ► DOTweenClipVariant tween was not generated " +
                              "(either because the original clip had no playable content or because all variant's target replacements were left to NULL)");
                return null;
            }
            tween = s;
            return tween;
        }

        /// <summary>
        /// Regenerates the clipVariant's tween (to be precise, a <see cref="Sequence"/>)
        /// regardless of its original clip's isActive property and returns it
        /// (unless the tween is immediately killed by a Complete with autoKill active).<para/>
        /// If the tween had already been generated it rewinds+kills it or simply kills it (depending on the <see cref="rewindIfExists"/> parameter)
        /// before recreating it.<para/>
        /// </summary>
        /// <param name="rewindIfExists">If TRUE and a tween had already been generated rewinds it before killing it, otherwise just kills it</param>
        /// <param name="behaviour">Leave as NULL to use the original <see cref="DOTweenClip"/>'s Startup Behaviour</param>
        /// <param name="andPlay">Leave as NULL to use the original <see cref="DOTweenClip"/>'s Autoplay option</param>
        public override Sequence ForceGenerateTween(bool rewindIfExists = true, StartupBehaviour? behaviour = null, bool? andPlay = null)
        {
            Init();
            if (_clip == null) {
                DOLog.Warning("GenerateTween ► This DOTweenClipVariant tween was not generated because its clip is missing");
                return null;
            }
            bool isApplicationPlaying = DOTweenTimelineSettings.isApplicationPlaying;
            if (HasTween()) {
                if (rewindIfExists) tween.Rewind();
                tween.Kill();
            }
            Sequence s = _clip.INTERNAL_DoGenerateTween(
                isApplicationPlaying,
                overrideClipSettings
                    ? TmpSettingsSnapshot.Refresh(this, behaviour, andPlay, invert)
                    : TmpSettingsSnapshot.Refresh(_clip, behaviour, andPlay, invert),
                ConvertTargetSwapsToTargetPairs()
            );
            if (s == null) {
                DOLog.Warning("ForceGenerateTween ► DOTweenClipVariant tween was not generated.");
                return null;
            }
            tween = s;
            return tween;
        }

        #endregion

        #region Methods

        Object[] ConvertTargetSwapsToTargetPairs()
        {
            int totSwaps = targetSwaps.Length;
            Object[] result = new Object[totSwaps * 2];
            for (int i = 0; i < totSwaps; ++i) {
                result[i * 2] = targetSwaps[i].originalTarget;
                result[i * 2 + 1] = targetSwaps[i].newTarget;
            }
            return result;
        }

        // Assumes clipGuid is neither empty nor NULL
        static DOTweenClip FindClip(object withinObj, string clipGuid, bool lookInNestedObjs = true)
        {
            DOTweenClip result;
            List<object> nestedObjs = null;
            // Find all fields also inside base classes
            _TmpFInfos.Clear();
            Type t = withinObj.GetType();
            FieldInfo[] fInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Try to find clip in direct fields
            for (int i = 0; i < fInfos.Length; ++i) {
                result = FindClipFromFieldInfo(fInfos[i], withinObj, clipGuid, lookInNestedObjs, ref nestedObjs);
                if (result != null) return result;
            }
            // Try to look in base types fields
            for (int i = 0; i < fInfos.Length; ++i) _TmpFInfos.Add(fInfos[i]);
            t = t.BaseType;
            while (t != null) {
                fInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fInfos.Length; ++i) {
                    // Check if field was already retrieved from child class
                    bool isNewField = true;
                    for (int j = 0; j < _TmpFInfos.Count; ++j) {
                        if (_TmpFInfos[j].Name != fInfos[i].Name) continue;
                        isNewField = false;
                        break;
                    }
                    if (isNewField) {
                        // Try to find clip in new field
                        result = FindClipFromFieldInfo(fInfos[i], withinObj, clipGuid, lookInNestedObjs, ref nestedObjs);
                        if (result != null) {
                            _TmpFInfos.Clear();
                            return result;
                        }
                        _TmpFInfos.Add(fInfos[i]);
                    }
                }
                t = t.BaseType;
            }
            // Look inside nested serialized classes
            _TmpFInfos.Clear();
            if (lookInNestedObjs && nestedObjs != null) {
                for (int i = 0; i < nestedObjs.Count; ++i) {
                    result = FindClip(nestedObjs[i], clipGuid, true);
                    if (result != null) {
                        return result;
                    }
                }
            }
            return null;
        }

        static DOTweenClip FindClipFromFieldInfo(FieldInfo fInfo, object withinObj, string clipGuid, bool lookInNestedObjs, ref List<object> nestedObjs)
        {
            bool isSerialized = fInfo.IsPublic && !Attribute.IsDefined(fInfo, _TNonSerializedAttribute)
                                || !fInfo.IsPublic && Attribute.IsDefined(fInfo, _TSerializeFieldAttribute);
            if (!isSerialized) return null;
            if (fInfo.FieldType == _TClip) {
                DOTweenClip s = fInfo.GetValue(withinObj) as DOTweenClip;
                if (s != null && s.guid == clipGuid) return s;
            } else if (fInfo.FieldType == _TClipArray || fInfo.FieldType == _TClipList) {
                IList<DOTweenClip> listS = fInfo.GetValue(withinObj) as IList<DOTweenClip>;
                if (listS != null) {
                    for (int i = 0; i < listS.Count; i++) {
                        if (listS[i] != null && listS[i].guid == clipGuid) return listS[i];
                    }
                }
            } else if (lookInNestedObjs) {
                // Non-clip object: check if it's serialized in which case we'll look inside that too
                object nestedObj = fInfo.GetValue(withinObj);
                if (nestedObj != null && nestedObj != withinObj && !fInfo.FieldType.IsSubclassOf(_TUnityObject)) {
                    if (nestedObjs == null) nestedObjs = new List<object>();
                    nestedObjs.Add(nestedObj);
                }
            }
            return null;
        }

        #region Editor-Only
#if UNITY_EDITOR

        /// <summary>
        /// Editor-only. Resets the clipVariant and eventually generates a new GUID
        /// </summary>
        /// <param name="regenerateGuid">If TRUE regenerates the GUID</param>
        /// <param name="partial">If TRUE resets important properties but leaves other unchanged
        /// (useful to keep some data when it's copied from the previous array element in the Inspector)</param>
        public void Editor_Reset(bool regenerateGuid, bool partial = false)
        {
            if (regenerateGuid) _guid = Guid.NewGuid().ToString();
            clipGuid = null;
            lookForClipInNestedObjs = false;
            clipComponent = null;
            targetSwaps = new TargetSwap[0];
            if (!partial) {
                startupBehaviour = StartupBehaviour.Create;
                startupDelay = 0;
                autoplay = true;
                autokill = true;
                ignoreTimeScale = false;
                timeMode = TimeMode.TimeScale;
                timeScale = 1;
                durationOverload = 1;
                loops = 1;
                loopType = LoopType.Yoyo;
            }
            hasOnRewind = false;
            hasOnComplete = false;
            hasOnStepComplete = false;
            hasOnUpdate = false;
            onRewind = null;
            onComplete = null;
            onStepComplete = null;
            onUpdate = null;
        }

#endif
        #endregion

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public class TargetSwap
        {
            public Object originalTarget, newTarget;

            public TargetSwap(Object originalTarget)
            {
                this.originalTarget = originalTarget;
            }
        }
    }
}