// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/02/02

using System;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal static class TimelineExtensions
    {
        #region Public Methods

        #region Clip

        public static int Editor_GetClipElementIndex(this DOTweenClip clip, string clipElementGuid)
        {
            for (int i = 0; i < clip.elements.Length; ++i) {
                if (clip.elements[i].guid == clipElementGuid) return i;
            }
            return -1;
        }

        public static DOTweenClip.ClipLayer Editor_GetClipElementLayer(this DOTweenClip clip, string clipElementGuid)
        {
            foreach (DOTweenClip.ClipLayer layer in clip.layers) {
                for (int i = 0; i < layer.clipElementGuids.Length; ++i) {
                    if (layer.clipElementGuids[i] == clipElementGuid) return layer;
                }
            }
            return null;
        }

        #endregion

        #region ClipElement

        public static float Editor_DrawDuration(this DOTweenClipElement clipElement)
        {
            switch (clipElement.type) {
            case DOTweenClipElement.Type.Event:
            case DOTweenClipElement.Type.Action: return DOTweenClipTimeline.settings.actionsLayoutDuration;
            default: return Mathf.Max(clipElement.duration, 0.1f);
            }
        }

        public static string Editor_GetShortName(this Type t)
        {
            string result = t.ToString();
            int index = result.LastIndexOf('.');
            return index == -1 ? result : result.Substring(index + 1);
        }

        public static bool Editor_HasDuration(this DOTweenClipElement clipElement)
        {
            switch (clipElement.type) {
            case DOTweenClipElement.Type.GlobalTween:
            case DOTweenClipElement.Type.Tween:
            case DOTweenClipElement.Type.Interval:
                return true;
            default:
                return false;
            }
        }

        public static bool Editor_HasMultipleLoops(this DOTweenClipElement clipElement)
        {
            switch (clipElement.loops) {
            case -1: return true;
            case 0: return false;
            case 1: return false;
            default: return true;
            }
        }

        public static int Editor_PositiveLoopValue(this DOTweenClipElement clipElement)
        {
            return TimelineUtils.GetClipElementPositiveLoopValue(clipElement);
        }

        #endregion

        #endregion
    }
}