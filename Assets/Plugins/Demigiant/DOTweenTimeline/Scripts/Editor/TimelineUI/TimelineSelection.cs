// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/19

using System.Collections.Generic;
using DG.DemiEditor;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal static class TimelineSelection
    {
        static DOTimelineEditorSettings _settings { get { return DOTweenClipTimeline.settings; } }
        static Component _src { get { return DOTweenClipTimeline.src; } }
        static DOTweenClip _clip { get { return DOTweenClipTimeline.clip; } }

        public static readonly List<SelectedClipElement> ClipElements = new List<SelectedClipElement>(); // Last selected is always first element
        public static int totClipElements { get; private set; }
        public static bool containsClipElements { get { return totClipElements > 0; } }
        public static bool isDraggingSelection; // Set by TimelineMain
        public static bool isDraggingClipElements; // Set by TimelineMain
        public static bool isDraggingDuration; // Set by TimelineMain

        #region Public Methods

        /// <summary>
        /// Clears the selection without recording an undo
        /// </summary>
        public static void Clear()
        {
            ClipElements.Clear();
            totClipElements = 0;
        }

        public static void Select(
            DOTweenClipElement clipElement, bool add = false, bool refreshSettings = true,
            float? forcedOriginalStartTime = null, float? forcedOriginalDuration = null, int? forcedOriginalLayerIndex = null
        ){
            if (!add) Clear();
            int existingIndex = IndexOf(clipElement);
            if (existingIndex != -1) {
                DeEditorUtils.List.Shift(ClipElements, existingIndex, 0);
                return;
            }
            SelectedClipElement sel = new SelectedClipElement(clipElement, forcedOriginalStartTime, forcedOriginalDuration, forcedOriginalLayerIndex);
            ClipElements.Insert(0, sel);
            totClipElements++;
            if (refreshSettings) _settings.RefreshSelected(_src, _clip);
        }

        public static void SelectAllIn(DOTweenClip clip, bool includeLockedLayers)
        {
            Clear();
            for (int i = 0; i < clip.elements.Length; ++i) {
                if (!includeLockedLayers && clip.layers[clip.FindClipElementLayerIndexByGuid(clip.elements[i].guid)].locked) continue;
                SelectedClipElement sel = new SelectedClipElement(clip.elements[i]);
                ClipElements.Add(sel);
                totClipElements++;
            }
            _settings.RefreshSelected(_src, _clip);
        }

        public static void Deselect(DOTweenClipElement clipElement)
        {
            if (!containsClipElements) return;
            int index = IndexOf(clipElement);
            if (index == -1) return;
            ClipElements.RemoveAt(index);
            totClipElements--;
            _settings.RefreshSelected(_src, _clip);
        }

        public static void DeselectAll()
        {
            Clear();
            _settings.RefreshSelected(_src, _clip);
        }

        public static bool Contains(DOTweenClipElement clipElement)
        {
            if (!containsClipElements) return false;
            for (int i = 0; i < totClipElements; ++i) {
                if (ClipElements[i].clipElement == clipElement) return true;
            }
            return false;
        }

        public static bool HasSelections(bool ofSameType = false)
        {
            if (totClipElements == 0) return false;
            if (!ofSameType || totClipElements == 1) return true;
            DOTweenClipElement.Type sType = ClipElements[0].clipElement.type;
            for (int i = 1; i < totClipElements; ++i) {
                if (ClipElements[i].clipElement.type != sType) return false;
            }
            return true;
        }

        public static void RefreshSelectionsData()
        {
            if (totClipElements > 0) {
                for (int i = 0; i < totClipElements; ++i) ClipElements[i].Refresh();
            }
        }

        public static List<DOTweenClipElement> GetCleanSelectedClipElements()
        {
            if (ClipElements.Count == 0) return null;
            List<DOTweenClipElement> result = new List<DOTweenClipElement>();
            foreach (SelectedClipElement sel in ClipElements) result.Add(sel.clipElement);
            return result;
        }

        #endregion

        #region Methods

        static int IndexOf(DOTweenClipElement clipElement)
        {
            if (!containsClipElements) return -1;
            for (int i = 0; i < totClipElements; ++i) {
                if (ClipElements[i].clipElement == clipElement) return i;
            }
            return -1;
        }

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        public class SelectedClipElement
        {
            public readonly DOTweenClipElement clipElement;
            public float originalStartTime;
            public float originalDuration;
            public int originalLayerIndex;

            public SelectedClipElement(
                DOTweenClipElement clipElement,
                float? forcedOriginalStartTime = null, float? forcedOriginalDuration = null, int? forcedOriginalLayerIndex = null
            ){
                this.clipElement = clipElement;
                Refresh(forcedOriginalStartTime, forcedOriginalDuration, forcedOriginalLayerIndex);
            }

            public void Refresh(float? forcedOriginalStartTime = null, float? forcedOriginalDuration = null, int? forcedOriginalLayerIndex = null)
            {
                originalStartTime = forcedOriginalStartTime != null ? (float)forcedOriginalStartTime : clipElement.startTime;
                originalDuration = forcedOriginalDuration != null ? (float)forcedOriginalDuration : clipElement.duration;
                originalLayerIndex = forcedOriginalLayerIndex != null
                    ? (int)forcedOriginalLayerIndex
                    : _clip.FindClipElementLayerIndexByGuid(clipElement.guid);
            }
        }
    }
}