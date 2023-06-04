// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/17

using DG.Tweening.Timeline.Core;
using UnityEngine;

namespace DG.Tweening.Timeline
{
    /// <summary>
    /// This components holds a single <see cref="DOTweenClipVariant"/>.
    /// Note that you can also simply add a serialized <see cref="DOTweenClipVariant"/> to your own custom components
    /// in order to control them directly
    /// </summary>
    [AddComponentMenu("DOTween/DOTween ClipVariant")]
    public class DOTweenClipVariantComponent : DOTweenClipComponentBase
    {
        #region Serialized
#pragma warning disable 0649

        public DOTweenClipVariant clipVariant;

#pragma warning restore 0649
        #endregion

        internal override DOTweenClipBase clipBase { get { return clipVariant; } }

        #region Unity

        void Start()
        {
            if (DOTweenTimelineSettings.I.debugLogs) {
                DOLog.Normal(string.Format("DOTweenClipVariantComponent <color=#d568e3>{0}</color> : Startup", this.name), this);
            }
            if (!clipVariant.HasTween()) clipVariant.GenerateTween();
        }

        #endregion
    }
}