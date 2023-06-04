// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/18

using System;
using UnityEngine;

namespace DG.Tweening.Timeline.Core
{
    /// <summary>
    /// Abstract base class for all clip-related components (except for <see cref="DOTweenClipCollection"/>)
    /// </summary>
    public abstract class DOTweenClipComponentBase : MonoBehaviour
    {
        #region Serialized
#pragma warning disable 0649

        [HideInInspector] public bool killTweensOnDestroy = true;
        [HideInInspector] public OnEnableBehaviour onEnableBehaviour = OnEnableBehaviour.PlayIfExists;
        [HideInInspector] public OnDisableBehaviour onDisableBehaviour = OnDisableBehaviour.PauseIfExists;

#pragma warning restore 0649
        #endregion

        internal abstract DOTweenClipBase clipBase { get; }

        #region Unity

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

            switch (onEnableBehaviour) {
            case OnEnableBehaviour.PlayIfExists:
                if (clipBase.HasTween()) clipBase.PlayTween();
                break;
            case OnEnableBehaviour.RestartIfExists:
                if (clipBase.HasTween()) clipBase.RestartTween();
                break;
            case OnEnableBehaviour.CreateOrRestart:
                clipBase.Play(true);
                break;
            }
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

            switch (onDisableBehaviour) {
            case OnDisableBehaviour.Kill:
                if (clipBase.HasTween()) clipBase.KillTween();
                break;
            case OnDisableBehaviour.PauseIfExists:
                if (clipBase.HasTween()) clipBase.PauseTween();
                break;
            case OnDisableBehaviour.RewindIfExists:
                if (clipBase.HasTween()) clipBase.RewindTween();
                break;
            }
        }

        protected virtual void OnDestroy()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

            if (!killTweensOnDestroy) return;

            clipBase.KillTween();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and starts (or restarts) this Component's clip's tween (to be precise, a <see cref="Sequence"/>) and returns it
        /// (unless the clip's <see cref="DOTweenClip.isActive"/> toggle is disabled in the Inspector).
        /// This will ignore the clip's startup behaviour set in the Inspector and will either:<para/>
        /// - If this clip's tween had not been created already or had been killed: creates it and plays it immediately.<para/>
        /// - If this clip's tween already exists: plays it or restarts it based on the given parameters.
        /// </summary>
        /// <param name="restartIfExists">If TRUE (default) and the tween was already generated and exists,
        /// rewinds it and replays it from the beginning, otherwise simply plays it from where it is</param>
        public Sequence Play(bool restartIfExists = true)
        {
            return clipBase.GenerateTween(StartupBehaviour.Create, true, restartIfExists);
        }

        #region Control Methods

        /// <summary>
        /// Kills the eventual tween generated
        /// </summary>
        /// <param name="complete">If TRUE completes the tween before killing it</param>
        public void KillTween(bool complete = false)
        {
            clipBase.KillTween(complete);
        }

        /// <summary>
        /// Stops and rewinds the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        public void RewindTween(bool includeDelay = true)
        {
            clipBase.RewindTween(includeDelay);
        }

        /// <summary>
        /// Completes the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="withCallbacks">If TRUE will fire all internal callbacks up from the current position to the end, otherwise will ignore them</param>
        public void CompleteTween(bool withCallbacks = false)
        {
            clipBase.CompleteTween(withCallbacks);
        }

        /// <summary>
        /// Restarts the tween from the beginning. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        public void RestartTween(bool includeDelay = true)
        { RestartTween(includeDelay, -1f); }
        /// <summary>
        /// Restarts the tween from the beginning. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        /// <param name="changeDelayTo">If <see cref="includeDelay"/> is true and this value is bigger than 0 assigns it as the new delay</param>
        public void RestartTween(bool includeDelay, float changeDelayTo)
        {
            clipBase.RestartTween(includeDelay, changeDelayTo);
        }

        /// <summary>
        /// Regenerates the tween and restarts it from the current targets state (meaning all dynamic elements values will be re-evaluated).
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        public void RestartTweenFromHere(bool includeDelay = true)
        { RestartTweenFromHere(includeDelay, -1f); }
        /// <summary>
        /// Regenerates the tween and restarts it from the current targets state (meaning all dynamic elements values will be re-evaluated).
        /// </summary>
        /// <param name="includeDelay">If TRUE includes the eventual delay set on the clip</param>
        /// <param name="changeDelayTo">If <see cref="includeDelay"/> is true and this value is bigger than 0 assigns it as the new delay</param>
        public void RestartTweenFromHere(bool includeDelay, float changeDelayTo)
        {
            clipBase.RestartTweenFromHere(includeDelay, changeDelayTo);
        }

        /// <summary>
        /// Pauses the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PauseTween()
        {
            clipBase.PauseTween();
        }

        /// <summary>
        /// Resumes the tween. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PlayTween()
        {
            clipBase.PlayTween();
        }

        /// <summary>
        /// Resumes the tween and plays it backwards. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PlayTweenBackwards()
        {
            clipBase.PlayTweenBackwards();
        }

        /// <summary>
        /// Resumes the tween and plays it forward. Does nothing if the tween hasn't been generated yet or has been killed.
        /// </summary>
        public void PlayTweenForward()
        {
            clipBase.PlayTweenForward();
        }

        #endregion

        #endregion
    }
}