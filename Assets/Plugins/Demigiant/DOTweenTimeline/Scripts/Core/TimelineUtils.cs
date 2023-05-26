// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/04/08

namespace DG.Tweening.Timeline.Core
{
    public static class TimelineUtils
    {
        #region Public Methods

        public static float GetClipDuration(DOTweenClip clip, bool includeClipElementsLoops = true)
        {
            float endTime = 0;
            foreach (DOTweenClipElement clipElement in clip.elements) {
                DOTweenClip.ClipLayer layer = clip.layers[clip.FindClipElementLayerIndexByGuid(clipElement.guid)];
                if (!layer.isActive) continue;
                float sEndTime = clipElement.startTime + clipElement.duration * (includeClipElementsLoops ? GetClipElementPositiveLoopValue(clipElement) : 1);
                if (sEndTime > endTime) endTime = sEndTime;
            }
            return endTime;
        }

        public static int GetClipElementPositiveLoopValue(DOTweenClipElement clipElement)
        {
            switch (clipElement.loops) {
            case 0: return 1;
            case -1: return int.MaxValue;
            default: return clipElement.loops;
            }
        }

        #endregion
    }
}