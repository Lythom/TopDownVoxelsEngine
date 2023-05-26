// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/16

using System;
using UnityEngine;
using UnityEngine.Events;

namespace DG.Tweening.Timeline.Core
{
    /// <summary>
    /// Abstract base class for <see cref="DOTweenClip"/> and DOTweenClipVariant
    /// </summary>
    [Serializable]
    public abstract class DOTweenClipBase
    {
        #region Serialized
#pragma warning disable 0649

        [SerializeField] protected string _guid = Guid.NewGuid().ToString();
        /// <summary>If TRUE immediately completes the tween on creation then inverts its direction,
        /// so that PlayForward will play it from the end to the beginning and PlayBackwards from the beginning to the end</summary>
        public bool invert = false;
        public StartupBehaviour startupBehaviour = StartupBehaviour.Create;
        public float startupDelay = 0;
        public bool autoplay = true;
        public bool autokill = true;
        public bool ignoreTimeScale = false;
        public TimeMode timeMode = TimeMode.TimeScale;
        public float timeScale = 1; // Tween's timeScale: used in case of TimeMode.TimeScale
        public float durationOverload = 1; // Tween's overall duration: used in case of TimeMode.DurationOverload
        public int loops = 1;
        public LoopType loopType = LoopType.Yoyo;
        public bool hasOnRewind, hasOnComplete, hasOnStepComplete, hasOnUpdate;
        public UnityEvent onRewind, onComplete, onStepComplete, onUpdate;

#pragma warning restore 0649
        #endregion

        /// <summary>Unique GUID for this clip</summary>
        public string guid { get { return _guid; } }
        /// <summary>Contains the generated tween (to be precise, a <see cref="Sequence"/>).
        /// Returns NULL if the tween hasn't yet been generated or if creation was skipped</summary>
        public Sequence tween { get; protected set; }

        // Used and reused to pass settings snapshots
        internal static readonly SettingsSnapshot TmpSettingsSnapshot = new SettingsSnapshot();

        #region Public Methods

        public abstract Sequence Play(bool restartIfExists = true);

        public abstract Sequence GenerateTween(StartupBehaviour? behaviour = null, bool? andPlay = null, bool rewindIfExists = true);

        public abstract Sequence ForceGenerateTween(bool rewindIfExists = true, StartupBehaviour? behaviour = null, bool? andPlay = null);

        #region Control Methods

        /// <summary>
        /// Kills the eventual tween generated and NULLs the <see cref="tween"/> reference
        /// </summary>
        /// <param name="complete">If TRUE completes the tween before killing it</param>
        public void KillTween(bool complete = false)
        {
            tween.Kill(complete);
            tween = null;
        }

        /// <summary>
        /// Stops and rewinds the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        public void RewindTween(bool includeDelay = true)
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.RewindTween ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.Rewind(includeDelay);
        }

        /// <summary>
        /// Completes the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="withCallbacks">If TRUE will fire all internal callbacks up from the current position to the end, otherwise will ignore them</param>
        public void CompleteTween(bool withCallbacks = false)
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.CompleteTween ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.Complete(withCallbacks);
        }

        /// <summary>
        /// Restarts the tween from the beginning. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        /// <param name="changeDelayTo">If <see cref="includeDelay"/> is true and this value is bigger than 0 assigns it as the new delay</param>
        public void RestartTween(bool includeDelay = true, float changeDelayTo = -1f)
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.RestartTween ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.Restart(includeDelay, changeDelayTo);
        }

        /// <summary>
        /// Regenerates the tween and restarts it from the current targets state (meaning all dynamic elements values will be re-evaluated).
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        /// <param name="changeDelayTo">If <see cref="includeDelay"/> is true and this value is bigger than 0 assigns it as the new delay</param>
        public virtual void RestartTweenFromHere(bool includeDelay = true, float changeDelayTo = -1f)
        {
            if (HasTween()) {
                tween.Kill();
                tween = null;
            }
            ForceGenerateTween(false, StartupBehaviour.Create, false);
            if (tween != null) tween.Restart(includeDelay, changeDelayTo);
        }

        /// <summary>
        /// Pauses the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PauseTween()
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.PauseTween ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.Pause();
        }

        /// <summary>
        /// Resumes the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PlayTween()
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.PlayTween ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.Play();
        }

        /// <summary>
        /// Resumes the tween and plays it backwards. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PlayTweenBackwards()
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.PlayTweenBackwards ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.PlayBackwards();
        }

        /// <summary>
        /// Resumes the tween and plays it forward. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PlayTweenForward()
        {
            if (!HasTween()) {
                Debug.LogWarning("DOTweenClipBase.PlayTweenForward ► The tween hasn't been generated or was killed. You must call GenerateTween to create it first.");
                return;
            }
            tween.PlayForward();
        }

        #endregion

        #region Info Methods

        /// <summary>Returns TRUE if the tween was generated and hasn't been killed</summary>
        public bool HasTween()
        {
            return tween != null && tween.active;
        }

        /// <summary>Returns TRUE if the tween is complete
        /// (silently fails and returns FALSE if the tween has been killed)</summary>
        public bool IsTweenComplete()
        {
            return tween != null && tween.active && tween.IsComplete();
        }

        /// <summary>Returns TRUE if the tween has been generated and is playing</summary>
        public bool IsTweenPlaying()
        {
            return tween != null && tween.active && tween.IsPlaying();
        }

        #endregion

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        internal class SettingsSnapshot
        {
            public bool invert; // Only set by ClipVariant

            public StartupBehaviour startupBehaviour;
            public float startupDelay;
            public bool autoplay;
            public bool autokill;
            public bool ignoreTimeScale;
            public TimeMode timeMode;
            public float timeScale;
            public float durationOverload;
            public int loops;
            public LoopType loopType;

            public bool hasOnRewind, hasOnComplete, hasOnStepComplete, hasOnUpdate;
            public UnityEvent onRewind, onComplete, onStepComplete, onUpdate;

            public SettingsSnapshot Refresh(
                DOTweenClipBase clipBase, StartupBehaviour? startupBehaviourOverride = null, bool? autoplayOverride = null, bool? invertOverride = null
            ){
                invert = invertOverride == null ? clipBase.invert : (bool)invertOverride;

                startupBehaviour = startupBehaviourOverride == null ? clipBase.startupBehaviour : (StartupBehaviour)startupBehaviourOverride;
                startupDelay = clipBase.startupDelay;
                autoplay = clipBase.autoplay;
                autoplay = autoplayOverride == null ? clipBase.autoplay : (bool)autoplayOverride;
                autokill = clipBase.autokill;
                ignoreTimeScale = clipBase.ignoreTimeScale;
                timeMode = clipBase.timeMode;
                timeScale = clipBase.timeScale;
                durationOverload = clipBase.durationOverload;
                loops = clipBase.loops;
                loopType = clipBase.loopType;

                hasOnRewind = clipBase.hasOnRewind;
                hasOnComplete = clipBase.hasOnComplete;
                hasOnStepComplete = clipBase.hasOnStepComplete;
                hasOnUpdate = clipBase.hasOnUpdate;
                onRewind = clipBase.onRewind;
                onComplete = clipBase.onComplete;
                onStepComplete = clipBase.onStepComplete;
                onUpdate = clipBase.onUpdate;

                return this;
            }
        }
    }
}