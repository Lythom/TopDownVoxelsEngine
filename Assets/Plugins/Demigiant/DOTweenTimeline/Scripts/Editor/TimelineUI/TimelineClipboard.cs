// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/02/08

using System.Collections.Generic;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;

namespace DG.Tweening.TimelineEditor
{
    internal static class TimelineClipboard
    {
        public static readonly List<ClipElementCopy> ClipElementCopies = new List<ClipElementCopy>();

        #region Public Methods

        public static void CopyClipElements(DOTweenClip fromClip, List<DOTweenClipElement> clipElementsToCopy)
        {
            ClipElementCopies.Clear();
            if (clipElementsToCopy == null) return;
            float earlierStartTime = float.MaxValue;
            int topLayerIndex = int.MaxValue;
            foreach (DOTweenClipElement clipElement in clipElementsToCopy) {
                ClipElementCopy copy = new ClipElementCopy(fromClip, clipElement);
                ClipElementCopies.Add(copy);
                if (clipElement.startTime < earlierStartTime) earlierStartTime = clipElement.startTime;
                if (copy.layerIndex < topLayerIndex) topLayerIndex = copy.layerIndex;
            }
            // Set offsetWhenPasting
            foreach (ClipElementCopy sCopy in ClipElementCopies) {
                sCopy.startTimeOffsetFromFirst = sCopy.clipElement.startTime - earlierStartTime;
                sCopy.layerIndexOffsetFromUpper = sCopy.layerIndex - topLayerIndex;
            }
        }

        public static bool HasMemorizedClipElements()
        {
            return ClipElementCopies.Count > 0;
        }

        public static int TotMemorizedClipElements()
        {
            return ClipElementCopies.Count;
        }

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        public class ClipElementCopy
        {
            public string originalGuid;
            public int layerIndex;
            public DOTweenClipElement clipElement;
            public float startTimeOffsetFromFirst; // Set directly
            public int layerIndexOffsetFromUpper; // Set directly

            public ClipElementCopy(DOTweenClip clip, DOTweenClipElement clipElement)
            {
                this.originalGuid = clipElement.guid;
                this.layerIndex = clip.FindClipElementLayerIndexByGuid(clipElement.guid);
                this.clipElement = clipElement.Clone(false);
            }

            public DOTweenClipElement GenerateCopy(bool regenerateGuid)
            {
                return clipElement.Clone(regenerateGuid);
            }
        }
    }
}