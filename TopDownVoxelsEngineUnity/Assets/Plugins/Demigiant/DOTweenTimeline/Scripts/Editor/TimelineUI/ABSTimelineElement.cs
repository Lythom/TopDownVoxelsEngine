// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/17

using DG.DemiEditor;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal class ABSTimelineElement
    {
        protected DOTweenClipTimeline editor { get { return DOTweenClipTimeline.editor; } }
        protected TimelineLayout layout { get { return DOTweenClipTimeline.Layout; } }
        protected DOTimelineEditorSettings settings { get { return DOTweenClipTimeline.settings; } }
        protected DOTweenTimelineSettings runtimeSettings { get { return DOTweenClipTimeline.runtimeSettings; } }
        protected Component src { get { return DOTweenClipTimeline.src; } }
        protected DOTweenClip clip { get { return DOTweenClipTimeline.clip; } }
        protected SerializedProperty spClip { get { return DOTweenClipTimeline.spClip; } }
        protected Vector2Int timelineShift {
            get { return clip.editor.roundedAreaShift; }
            set {
                clip.editor.roundedAreaShift = value;
                GUI.changed = true;
            }
        }
        protected bool isPlayingOrPreviewing { get { return DOTweenClipTimeline.isPlayingOrPreviewing; } }
        protected bool isUndoRedoPass { get { return DOTweenClipTimeline.isUndoRedoPass; } }
        protected bool isRecorderStoppedPass { get { return !DOTimelineRecorder.isRecording && DOTweenClipTimeline.isRecorderOrPreviewUndoPass; } }
        protected Rect area { get; set; }

        public virtual void Draw(Rect drawArea)
        {
            this.area = drawArea.ResetXY();
        }
    }
}