// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/16

using System;
using System.Collections.Generic;
using DG.Tweening.Timeline.Core;
using DG.Tweening.Timeline.Core.Plugins;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace DG.Tweening.Timeline
{
    /// <summary>
    /// Add a serialized instance of this class to any MonoBehaviour/Behaviour/Component to have the Inspector show all its options.<para/>
    /// Note that a <see cref="DOTweenClip"/> doesn't automatically generate its tween,
    /// you'll need to call <see cref="GenerateTween"/> or <see cref="GenerateIndependentTween"/> to do that.
    /// </summary>
    [Serializable]
#if UNITY_EDITOR
    public class DOTweenClip : DOTweenClipBase, ISerializationCallbackReceiver
#else
    public class DOTweenClip : DOTweenClipBase
#endif
    {
        #region Serialized
#pragma warning disable 0649

        /// <summary>If FALSE <see cref="GenerateTween"/> will have no effect and will not generate this clip's tween</summary>
        public bool isActive = true;
        /// <summary>Will be set as the string ID for the generated tween, so it can be used with DOTween's static id-based methods</summary>
        public string name = "Unnamed Clip";
        [FormerlySerializedAs("sequenceds")]
        public DOTweenClipElement[] elements = new DOTweenClipElement[0];
        public ClipLayer[] layers = new []{ new ClipLayer("Layer 1") };

        // Editor-only
        /// <summary>INTERNAL USE ONLY</summary>
        public EditorData editor = new EditorData();

#pragma warning restore 0649
        #endregion

        static readonly List<Object> _TmpOldComponentsToReplace = new List<Object>(); // Used by generate-and-replace system
        static readonly List<Object> _TmpNewComponentsToReplace = new List<Object>();
        static readonly List<DOTweenClipElement> _TmpClipElements = new List<DOTweenClipElement>(); // Used by FindClipElementsByPinNoAlloc

        public DOTweenClip(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// INTERNAL USE ONLY: Parameterless constructor that uses the GUID already assigned (for clips not created via <see cref="DOTweenClipCollection"/>)
        /// </summary>
        public DOTweenClip() {}

        /// <summary>INTERNAL USE ONLY: Use this constructor when creating a new clip object, so that you can pass a unique GUID.
        /// Note that the "name" parameter is unnecessary, and is here only to distinguish it from name-based constructor</summary>
        public DOTweenClip(string guid, string name)
        {
            _guid = guid;
            if (!string.IsNullOrEmpty(name)) this.name = name;
        }

        #region Unity

#if UNITY_EDITOR

        /// <summary>INTERNAL USAGE</summary>
        public void OnBeforeSerialize() {}

        /// <summary>INTERNAL USAGE</summary>
        public void OnAfterDeserialize()
        {
            // Visual Inspector List fix: detect if new DOTweenClip was just created as first array element
            // (in which case it's set with all default type values ignoring the ones set by myself, and I need to reset it)
            if (!string.IsNullOrEmpty(_guid)) return;
            Editor_Reset(true);
//            Debug.Log("<color=#ff0000>AFTER DESERIALIZE ► Clip was reset because GUID was empty (meaning it's a newly created element in a visual list):</color> " + _guid);
        }

#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and starts (or restarts) the clip's tween (to be precise, a <see cref="Sequence"/>) and returns it
        /// (unless its <see cref="isActive"/> toggle is disabled).
        /// This method is a more straightforward version of <see cref="GenerateTween"/> which will ignore the clip's
        /// startup behaviour set in the Inspector and will either:<para/>
        /// - If this clip's tween had not been created already or had been killed: creates it and plays it immediately.<para/>
        /// - If this clip's tween already exists: plays it or restarts it based on the given parameters.
        /// </summary>
        /// <param name="restartIfExists">If TRUE (default) and the tween was already generated and exists,
        /// rewinds it and replays it from the beginning, otherwise simply plays it from where it is</param>
        public override Sequence Play(bool restartIfExists = true)
        {
            return GenerateTween(StartupBehaviour.Create, true, restartIfExists);
        }

        /// <summary>
        /// Generates the clip's tween (to be precise, a <see cref="Sequence"/>) and returns it
        /// (unless <see cref="isActive"/> is set to FALSE/unchecked in the Inspector
        /// or the tween is immediately killed by a Complete with autoKill active).<para/>
        /// If the tween was already generated simply returns it (eventually rewinding it first)
        /// and plays it according to the <see cref="andPlay"/> parameter
        /// (use <see cref="ForceGenerateTween"/>) if instead you want to force the tween to be killed and regenerated.<para/>
        /// </summary>
        /// <param name="behaviour">Leave as NULL to use Startup Behaviour set in the Inspector</param>
        /// <param name="andPlay">Leave as NULL to use Autoplay option set in the Inspector</param>
        /// <param name="rewindIfExists">If TRUE and the tween already exists rewinds it before returning it</param>
        public override Sequence GenerateTween(StartupBehaviour? behaviour = null, bool? andPlay = null, bool rewindIfExists = true)
        {
            bool isApplicationPlaying = DOTweenTimelineSettings.isApplicationPlaying;
            if (isApplicationPlaying && !isActive) return null;
            if (HasTween()) { // Tween already exists: return it
                if (DOTween.logBehaviour == LogBehaviour.Verbose) {
                    DOLog.Normal(string.Format(
                        "GenerateTween ► DOTweenClip <color=#d568e3>\"{0}\"</color> tween has already been generated. Will rewind and return it.",
                        this.name));
                }
                if (rewindIfExists) tween.Rewind();
                bool assignedAndPlay = andPlay == null ? autoplay : (bool)andPlay;
                if (assignedAndPlay) tween.PlayForward();
                return tween;
            }
            Sequence s = INTERNAL_DoGenerateTween(isApplicationPlaying, TmpSettingsSnapshot.Refresh(this, behaviour, andPlay));
            if (s == null) {
                DOLog.Warning(string.Format("GenerateTween ► DOTweenClip <color=#d568e3>\"{0}\"</color> tween was not generated.", this.name));
                return null;
            }
            tween = s;
            return tween;
        }

        /// <summary>
        /// Regenerates the clip's tween (to be precise, a <see cref="Sequence"/>) regardless of this clip's <see cref="isActive"/> value and returns it
        /// (unless the tween is immediately killed by a Complete with autoKill active).<para/>
        /// If the tween had already been generated it rewinds+kills it or simply kills it (depending on the <see cref="rewindIfExists"/> parameter)
        /// before recreating it.<para/>
        /// </summary>
        /// <param name="rewindIfExists">If TRUE and a tween had already been generated rewinds it before killing it, otherwise just kills it</param>
        /// <param name="behaviour">Leave as NULL to use Startup Behaviour set in the Inspector</param>
        /// <param name="andPlay">Leave as NULL to use Autoplay option set in the Inspector</param>
        public override Sequence ForceGenerateTween(bool rewindIfExists = true, StartupBehaviour? behaviour = null, bool? andPlay = null)
        {
            bool isApplicationPlaying = DOTweenTimelineSettings.isApplicationPlaying;
            if (HasTween()) {
                if (rewindIfExists) tween.Rewind();
                tween.Kill();
            }
            Sequence s = INTERNAL_DoGenerateTween(isApplicationPlaying, TmpSettingsSnapshot.Refresh(this, behaviour, andPlay));
            if (s == null) {
                DOLog.Warning(string.Format("ForceGenerateTween ► DOTweenClip <color=#d568e3>{0}</color> tween was not generated.", this.name));
                return null;
            }
            tween = s;
            return tween;
        }

        /// <summary>
        /// Generates a tween from this clip (to be precise, a <see cref="Sequence"/>) and returns it,
        /// but keeps it completely independent from this <see cref="DOTweenClip"/> object,
        /// meaning that after creation you will only be able to control it as a normal tween and not via this <see cref="DOTweenClip"/> methods.<para/>
        /// In short, it's like <see cref="GenerateTween"/> but for advanced usage
        /// (like having a single <see cref="DOTweenClip"/> for multiple separate tweens,
        /// where you change the clipElement targets before creating the tween):
        /// it doesn't care if this <see cref="DOTweenClip"/> was set as active
        /// nor if the <see cref="tween"/> already existed, and it doesn't store the tween internally.
        /// </summary>
        /// <param name="andPlay">If TRUE also plays the tween immediately, otherwise creates it in a paused state</param>
        /// <param name="startupDelay">Eventual startup delay. Leave NULL to use the one set in the Inspector for this DOTweenClip</param>
        /// <param name="targetsToReplace">Eventual targets to replace, must be written in old/new pair where each pair is of the same type:<para/>
        /// <code>oldTransformToReplace, newTransformToReplace, oldImageToReplace, newImageToReplace</code></param>
        public Sequence GenerateIndependentTween(bool andPlay = true, float? startupDelay = null, params Object[] targetsToReplace)
        {
            bool isApplicationPlaying = DOTweenTimelineSettings.isApplicationPlaying;
            Sequence s = INTERNAL_DoGenerateTween(isApplicationPlaying, TmpSettingsSnapshot.Refresh(this, StartupBehaviour.Create, andPlay), targetsToReplace);
            if (s == null) {
                DOLog.Warning(string.Format("GenerateIndependentTween ► DOTweenClip <color=#d568e3>\"{0}\"</color> was not generated.", this.name));
                return null;
            }
            if (startupDelay != null) s.SetDelay((float)startupDelay);
            return s;
        }

        /// <summary>
        /// FOR INTERNAL DOTWEEN TIMELINE USAGE ONLY, do not use.<para/>
        /// Assumes that the tween doesn't exist.
        /// </summary>
        internal Sequence INTERNAL_DoGenerateTween(
            bool isApplicationPlaying, SettingsSnapshot settings, Object[] targetsToReplace = null
        ){
            if (isApplicationPlaying && settings.startupBehaviour == StartupBehaviour.DoNothing) return null;
            // Validate eventual replaceTargets
            bool replaceTargets = false;
            if (targetsToReplace != null) {
                int replaceLen = targetsToReplace.Length;
                if (replaceLen > 0) {
                    if (replaceLen % 2 != 0) { // Uneven number of targets
                        DOLog.Error("GenerateTween ► targetsToReplace must be written in pair (oldTargetToReplace, newTargetToReplace)");
                    } else {
                        replaceTargets = true;
                        _TmpOldComponentsToReplace.Clear();
                        _TmpNewComponentsToReplace.Clear();
                        for (int i = 0; i < replaceLen; i+=2) {
                            _TmpOldComponentsToReplace.Add(targetsToReplace[i]);
                            _TmpNewComponentsToReplace.Add(targetsToReplace[i + 1]);
                        }
                    }
                }
            }
            //
            Sequence s = null;
            float timeMultiplier = 1; // Ignored in editor preview, where duration overload is simulated via timeScale (easier to manage)
            float finalTimeScale = 1;
            switch (settings.timeMode) {
            case TimeMode.TimeScale:
                finalTimeScale = settings.timeScale;
                break;
            case TimeMode.DurationOverload:
                if (isApplicationPlaying) timeMultiplier = settings.durationOverload / TimelineUtils.GetClipDuration(this);
                else finalTimeScale = TimelineUtils.GetClipDuration(this) / settings.durationOverload;
                break;
            }
            int len = elements.Length;
            for (int i = 0; i < len; ++i) {
                DOTweenClipElement clipElement = elements[i];
                if (!clipElement.isActive) continue;
                switch (clipElement.type) {
                case DOTweenClipElement.Type.Event:
                    if (!isApplicationPlaying) break; // Events are not previewed in editor
                    InsertEvent(ref s, this, clipElement, timeMultiplier);
                    break;
                case DOTweenClipElement.Type.Action:
                    if (!isApplicationPlaying && !clipElement.executeInEditMode) break;
                    InsertAction(ref s, this, clipElement, timeMultiplier);
                    break;
                case DOTweenClipElement.Type.Interval:
                    InsertInterval(ref s, this, clipElement, timeMultiplier);
                    break;
                case DOTweenClipElement.Type.GlobalTween:
                    if (!isApplicationPlaying) break; // Global tweens are not previewed in editor
                    InsertSequentiableTween(ref s, this, clipElement, true, timeMultiplier);
                    break;
                default:
                    InsertSequentiableTween(ref s, this, clipElement, false, timeMultiplier, replaceTargets);
                    break;
                }
            }
            if (s == null) return null;
            if (isApplicationPlaying) {
                if (settings.startupDelay > 0) s.SetDelay(settings.startupDelay, false);
                bool wasKilled = false;
                s.SetAutoKill(settings.autokill);
                switch (settings.startupBehaviour) {
                case StartupBehaviour.ForceInitialization:
                    s.ForceInit();
                    if (!settings.autoplay) s.Pause();
                    break;
                case StartupBehaviour.Complete:
                    s.Complete(false);
                    wasKilled = settings.autokill;
                    break;
                case StartupBehaviour.CompleteWithInternalCallbacks:
                    s.Complete(true);
                    wasKilled = settings.autokill;
                    break;
                default:
                    if (!settings.autoplay) s.Pause();
                    break;
                }
                if (wasKilled) return null;
                if (settings.onRewind.GetPersistentEventCount() > 0) s.OnRewind(settings.onRewind.Invoke);
                if (settings.onComplete.GetPersistentEventCount() > 0) s.OnComplete(settings.onComplete.Invoke);
                if (settings.onStepComplete.GetPersistentEventCount() > 0) s.OnStepComplete(settings.onStepComplete.Invoke);
                if (settings.onUpdate.GetPersistentEventCount() > 0) s.OnUpdate(settings.onUpdate.Invoke);
            }
            if (!string.IsNullOrEmpty(name)) s.SetId(name);
            if (settings.invert) s.SetInverted(true);
            s.SetUpdate(settings.ignoreTimeScale);
            s.SetLoops(settings.loops, settings.loopType);
            s.timeScale = finalTimeScale;
            return s;
        }

        /// <summary>
        /// Returns the <see cref="DOTweenClipElement"/> with the given GUID in this clip, or NULL if it can't be found
        /// </summary>
        public DOTweenClipElement FindClipElementByGuid(string clipElementGuid)
        {
            int len = elements.Length;
            for (int i = 0; i < len; ++i) {
                if (elements[i].guid == clipElementGuid) return elements[i];
            }
            return null;
        }

        /// <summary>
        /// Returns all of this clip's <see cref="DOTweenClipElement"/> elements that have the given pin, or NULL if none was found
        /// </summary>
        /// <param name="pin">Pin Id (set by right-clicking on a clipElement in the Timeline)</param>
        public List<DOTweenClipElement> FindClipElementsByPin(int pin)
        {
            List<DOTweenClipElement> li = FindClipElementsByPinNoAlloc(pin); // internal readonly list
            return li.Count == 0 ? null : new List<DOTweenClipElement>(li);
        }
        /// <summary>
        /// Returns all of this clip's <see cref="DOTweenClipElement"/> that have the given pin, or an empty list if none was found.<para/>
        /// <code>IMPORTANT:</code> to avoid allocations this method always uses an internal list for the result.
        /// This means you shouldn't modify the resulting list, only its items.
        /// </summary>
        /// <param name="pin">Pin Id (set by right-clicking on a clipElement in the Timeline)</param>
        public List<DOTweenClipElement> FindClipElementsByPinNoAlloc(int pin)
        {
            _TmpClipElements.Clear();
            int len = elements.Length;
            for (int i = 0; i < len; ++i) {
                if (elements[i].pin == pin) _TmpClipElements.Add(elements[i]);
            }
            return _TmpClipElements;
        }

        /// <summary>
        /// Returns all of this clip's <see cref="DOTweenClipElement"/> elements that have the given target, or NULL if none was found
        /// </summary>
        /// <param name="target">The target to look for</param>
        public List<DOTweenClipElement> FindClipElementsByTarget(Object target)
        {
            List<DOTweenClipElement> li = FindClipElementsByTargetNoAlloc(target); // internal readonly list
            return li.Count == 0 ? null : new List<DOTweenClipElement>(li);
        }
        /// <summary>
        /// Returns all of this clip's <see cref="DOTweenClipElement"/> elements that have the given target, or an empty list if none was found.<para/>
        /// <code>IMPORTANT:</code> to avoid allocations this method always uses an internal list for the result.
        /// This means you shouldn't modify the resulting list, only its items.
        /// </summary>
        /// <param name="target">The target to look for</param>
        public List<DOTweenClipElement> FindClipElementsByTargetNoAlloc(Object target)
        {
            _TmpClipElements.Clear();
            int len = elements.Length;
            for (int i = 0; i < len; ++i) {
                if (elements[i].target == target) _TmpClipElements.Add(elements[i]);
            }
            return _TmpClipElements;
        }

        /// <summary>
        /// Returns the index of the <see cref="ClipLayer"/> containing the given clipElement, or -1 if it can't be found
        /// </summary>
        public int FindClipElementLayerIndexByGuid(string clipElementGuid)
        {
            int len = layers.Length;
            for (int i = 0; i < len; ++i) {
                int sublen = layers[i].clipElementGuids.Length;
                for (int j = 0; j < sublen; ++j) {
                    if (layers[i].clipElementGuids[j] == clipElementGuid) return i;
                }
            }
            return -1;
        }

        #region Agnostic Clip Methods

        /// <summary>
        /// Replaces the given target with the new one in all of this clip's elements
        /// and returns itself (so you can directly chain multiple Replace calls).<para/>
        /// NOTE: old and new target must be of the same type.<para/>
        /// NOTE: if you're using this clip as a blueprint and you will want to replace the same target multiple times
        /// you should first <see cref="Clone"/> it (so that you can keep this original clip untouched).<para/>
        /// EXAMPLE:<para/>
        /// <code>DOTweenClip newClip = Clone();
        /// newClip.ReplaceTarget(oldTransform, newTransform);
        /// newClip.ReplaceTarget(oldSpriteRenderer, newSpriteRenderer);</code>
        /// </summary>
        /// <param name="oldTarget">Target to replace</param>
        /// <param name="newTarget">New target to assign</param>
        public DOTweenClip ReplaceTarget(Object oldTarget, Object newTarget)
        {
            if (oldTarget.GetType() != newTarget.GetType()) {
                DOLog.Error(string.Format("ReplaceTarget ► old and new target ({0} / {1}) cannot be of different types", oldTarget, newTarget));
                return this;
            }
            int totClipElements = elements.Length;
            for (int i = 0; i < totClipElements; ++i) {
                if (elements[i].target == oldTarget) elements[i].target = newTarget;
            }
            return this;
        }

        #endregion

        #region Cloning

        /// <summary>
        /// Clones this clip and returns the new copy.<para/>
        /// <code>IMPORTANT:</code> note that at runtime Unity events will always be copied as references, not as actual independent copies.
        /// </summary>
        /// <param name="regenerateGuid">If TRUE doesn't copy the GUID but generates a new one (you should normally leave this to the default TRUE)</param>
        public DOTweenClip Clone(bool regenerateGuid = true)
        {
            DOTweenClip result = new DOTweenClip(regenerateGuid ? Guid.NewGuid().ToString() : this.guid, null);
            result.AssignPropertiesFrom(this, true);
            return result;
        }

        /// <summary>
        /// For INTERNAL USAGE: assigns this clip's properties from another clip
        /// </summary>
        /// <param name="clip"><see cref="DOTweenClip"/> whose properties to copy in this one</param>
        /// <param name="cloneProperties">If TRUE creates clones of all properties,
        /// otherwise assigns properties as references to the given clip ones</param>
        public void AssignPropertiesFrom(DOTweenClip clip, bool cloneProperties)
        {
            isActive = clip.isActive;
            name = clip.name;
            startupBehaviour = clip.startupBehaviour;
            startupDelay = clip.startupDelay;
            autoplay = clip.autoplay;
            autokill = clip.autokill;
            ignoreTimeScale = clip.ignoreTimeScale;
            timeMode = clip.timeMode;
            timeScale = clip.timeScale;
            durationOverload = clip.durationOverload;
            loops = clip.loops;
            loopType = clip.loopType;
            hasOnRewind = clip.hasOnRewind;
            hasOnComplete = clip.hasOnComplete;
            hasOnStepComplete = clip.hasOnStepComplete;
            hasOnUpdate = clip.hasOnUpdate;
#if UNITY_EDITOR
            if (cloneProperties && !Application.isPlaying) Editor_AssignEventsClonesFrom(clip);
            else AssignEventsReferencesFrom(clip);
#else
            AssignEventsReferencesFrom(clip);
#endif
            elements = cloneProperties ? clip.CloneClipElements() : clip.elements;
            layers = cloneProperties ? clip.CloneVisualLayers() : clip.layers;
            editor = clip.editor;
        }

        DOTweenClipElement[] CloneClipElements()
        {
            DOTweenClipElement[] result = new DOTweenClipElement[elements.Length];
            for (int i = 0; i < elements.Length; ++i) result[i] = elements[i].Clone(false);
            return result;
        }
        ClipLayer[] CloneVisualLayers()
        {
            ClipLayer[] result = new ClipLayer[layers.Length];
            for (int i = 0; i < layers.Length; ++i) result[i] = layers[i].Clone();
            return result;
        }

        void AssignEventsReferencesFrom(DOTweenClip clip)
        {
            onRewind = clip.onRewind;
            onComplete = clip.onComplete;
            onStepComplete = clip.onStepComplete;
            onUpdate = clip.onUpdate;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor-Only

        void Editor_AssignEventsClonesFrom(DOTweenClip clip)
        {
            onRewind = EditorConnector.Request.Dispatch_OnCloneUnityEvent(clip.onRewind);
            onComplete = EditorConnector.Request.Dispatch_OnCloneUnityEvent(clip.onComplete);
            onStepComplete = EditorConnector.Request.Dispatch_OnCloneUnityEvent(clip.onStepComplete);
            onUpdate = EditorConnector.Request.Dispatch_OnCloneUnityEvent(clip.onUpdate);
        }

        /// <summary>
        /// Editor-only. Resets the clip and eventually generates a new GUID
        /// </summary>
        /// <param name="regenerateGuid">If TRUE regenerates the GUID</param>
        /// <param name="partial">If TRUE resets important properties but leaves other unchanged
        /// (useful to keep some data when it's copied from the previous array element in the Inspector)</param>
        public void Editor_Reset(bool regenerateGuid, bool partial = false)
        {
            if (regenerateGuid) _guid = Guid.NewGuid().ToString();
            isActive = true;
            name = "Unnamed Clip";
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
            elements = new DOTweenClipElement[0];
            layers = new []{ new ClipLayer("Layer 1") };
            editor = new EditorData();
        }

        #endregion
#endif

        #endregion

        #region Methods

        static void InsertSequentiableTween(
            ref Sequence s, DOTweenClip clip, DOTweenClipElement clipElement, bool isGlobal, float timeMultiplier, bool replaceTargets = false
        ){
            DOVisualTweenPlugin plug;
            if (isGlobal) {
                plug = DOVisualPluginsManager.GetGlobalTweenPlugin(clipElement.plugId);
            } else {
                if (clipElement.target == null) return;
                plug = DOVisualPluginsManager.GetTweenPlugin(clipElement.target);
            }
            if (plug == null) return; // Missing plugin
            // Find correct target (in case replaceTargets is active)
            Object target = clipElement.target;
            if (replaceTargets) {
                int replaceIndex = _TmpOldComponentsToReplace.IndexOf(target);
                if (replaceIndex != -1) target = _TmpNewComponentsToReplace[replaceIndex];
            }
            if (!isGlobal && target == null) return;
            // Create
            ITweenPluginData plugData = plug.GetPlugData(clipElement);
            if (plugData == null) return;
            if (plugData.onCreation != null) {
                plugData.onCreation(
                    clipElement.target, clipElement.stringOption0, clipElement.intOption1
                );
            }
            Tweener t = plug.CreateTween(clipElement, target, timeMultiplier, plugData);
            if (t == null) return; // Missing plugData or tween creation error
            if (DOTweenTimelineSettings.addTargetToNestedTweens) t.SetTarget(target);
            if (DOTweenTimelineSettings.I.debugLogs) {
                string plugStr = plug.GetPlugData(clipElement).label;
                if (isGlobal) {
                    DOLog.Normal(
                        string.Format("DOTweenClip <color=#d568e3>{0}</color> : Insert global <color=#68b3e2>{1}</color> → <color=#ffa047>{2}</color> tween at {3}\"→{4}\"",
                            string.IsNullOrEmpty(clip.name) ? "[unnamed]" : clip.name, clipElement.plugId, plugStr, clipElement.startTime * timeMultiplier, clipElement.duration * timeMultiplier
                    ));
                } else {
                    DOLog.Normal(
                        string.Format("DOTweenClip <color=#d568e3>{0}</color> : Insert <color=#68b3e2>{1}</color> → <color=#ffa047>{2}</color> tween at {3}\"→{4}\"",
                            string.IsNullOrEmpty(clip.name) ? "[unnamed]" : clip.name, clipElement.target.name, plugStr, clipElement.startTime * timeMultiplier, clipElement.duration * timeMultiplier
                    ));
                }
            }
            if (s == null) s = DOTween.Sequence();
            s.Insert(clipElement.startTime * timeMultiplier, t);
        }

        static void InsertEvent(ref Sequence s, DOTweenClip clip, DOTweenClipElement clipElement, float timeMultiplier)
        {
            int totEvents = clipElement.onComplete.GetPersistentEventCount();
            if (totEvents <= 0) return;
            if (DOTweenTimelineSettings.I.debugLogs) {
                DOLog.Normal(
                    string.Format("DOTweenClip <color=#d568e3>{0}</color> : Insert <color=#68b3e2>{1} UnityEvent{2}</color> at {3}\"",
                        string.IsNullOrEmpty(clip.name) ? "[unnamed]" : clip.name, totEvents, totEvents < 2 ? "" : "s", clipElement.startTime * timeMultiplier
                ));
            }
            if (s == null) s = DOTween.Sequence();
            s.InsertCallback(clipElement.startTime * timeMultiplier, clipElement.onComplete.Invoke);
        }

        static void InsertAction(ref Sequence s, DOTweenClip clip, DOTweenClipElement clipElement, float timeMultiplier)
        {
            DOVisualActionPlugin plug = DOVisualPluginsManager.GetActionPlugin(clipElement.plugId);
            if (plug == null) return; // Missing plugin
            PlugDataAction plugDataAction = plug.GetPlugData(clipElement);
            if (plugDataAction == null) return; // Missing plugData
            if (plugDataAction.wantsTarget && clipElement.target == null) return;
            if (DOTweenTimelineSettings.I.debugLogs) {
                DOLog.Normal(
                    string.Format("DOTweenClip <color=#d568e3>{0}</color> : Insert action <color=#68b3e2>{1}</color> → <color=#ffa047>{2}{3}</color> at {4}\"",
                        string.IsNullOrEmpty(clip.name) ? "[unnamed]" : clip.name, clipElement.plugId, plugDataAction.label,
                        plugDataAction.wantsTarget ? ("(" + clipElement.target.name + ")") : "", clipElement.startTime * timeMultiplier
                ));
            }
            if (s == null) s = DOTween.Sequence();
            s.InsertCallback(clipElement.startTime * timeMultiplier,
                ()=> plugDataAction.action(
                    clipElement.target, clipElement.boolOption0, clipElement.toStringVal, clipElement.objOption, clipElement.toFloatVal, clipElement.fromFloatVal,
                    clipElement.floatOption0, clipElement.floatOption1, clipElement.toIntVal
                )
            );
            if (plugDataAction.onCreation != null) {
                plugDataAction.onCreation(
                    clipElement.target, clipElement.boolOption0, clipElement.toStringVal, clipElement.objOption, clipElement.toFloatVal, clipElement.fromFloatVal,
                    clipElement.floatOption0, clipElement.floatOption1, clipElement.toIntVal
                );
            }
        }

        static void InsertInterval(ref Sequence s, DOTweenClip clip, DOTweenClipElement clipElement, float timeMultiplier)
        {
            if (DOTweenTimelineSettings.I.debugLogs) {
                DOLog.Normal(
                    string.Format("DOTweenClip <color=#d568e3>{0}</color> : Insert interval at {1}\"→{2}\"",
                        string.IsNullOrEmpty(clip.name) ? "[unnamed]" : clip.name, clipElement.startTime * timeMultiplier, clipElement.duration * timeMultiplier
                    ));
            }
            if (s == null) s = DOTween.Sequence();
            float sCurrDuration = s.Duration(false);
            float requiredDuration = (clipElement.startTime + clipElement.duration) * timeMultiplier;
            if (sCurrDuration < requiredDuration) s.AppendInterval(requiredDuration - sCurrDuration);
        }

        #endregion
        

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public struct EditorData
        {
#pragma warning disable 0649
            /// <summary>Use directly only if you are moving the timeline, otherwise use roundedAreaShift</summary>
            public Vector2 areaShift;
#pragma warning restore 0649
            public Vector2Int roundedAreaShift {
                get { return new Vector2Int((int)areaShift.x, (int)areaShift.y); }
                set { areaShift = value; }
            }
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public class ClipLayer
        {
#pragma warning disable 0649
            public bool isActive = true;
            [FormerlySerializedAs("sequencedGuids")]
            public string[] clipElementGuids = new string[0];
            // Editor-only
            public string name; // Doesn't need to be univocal
            public bool locked;
            public Color color = DefColor;
#pragma warning restore 0649

            public static readonly Color DefColor = new Color(0.2f, 0.2f, 0.2f);

            public ClipLayer(string name)
            {
                this.name = name;
            }

            public ClipLayer Clone()
            {
                return new ClipLayer(this.name) {
                    isActive = this.isActive,
                    clipElementGuids = this.CloneClipElementGuids(),
                    name = this.name,
                    locked = this.locked,
                    color = this.color
                };
            }

            string[] CloneClipElementGuids()
            {
                string[] result = new string[clipElementGuids.Length];
                for (int i = 0; i < clipElementGuids.Length; ++i) result[i] = clipElementGuids[i];
                return result;
            }
        }
    }
}