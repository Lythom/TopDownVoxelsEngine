// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/17

using DG.Tweening.Timeline.Core;
using UnityEngine;

namespace DG.Tweening.Timeline
{
    /// <summary>
    /// This components holds a single <see cref="DOTweenClip"/>.
    /// Note that you can also simply add a serialized <see cref="DOTweenClip"/> to your own custom components
    /// in order to control them directly
    /// (or use the <see cref="DOTweenClipCollection"/> component if you want multiple <see cref="DOTweenClip"/> in a single gameObject)
    /// </summary>
    [AddComponentMenu("DOTween/DOTween Clip")]
    public class DOTweenClipComponent : DOTweenClipComponentBase
    {
        #region Serialized
#pragma warning disable 0649

        public DOTweenClip clip;

#pragma warning restore 0649
        #endregion

        internal override DOTweenClipBase clipBase { get { return clip; } }

        #region Unity

        void Start()
        {
            if (DOTweenTimelineSettings.I.debugLogs) {
                DOLog.Normal(string.Format("DOTweenClipComponent <color=#d568e3>{0}</color> : Startup", this.name), this);
            }
            if (!clip.HasTween()) clip.GenerateTween();
        }

        #endregion
    }
}