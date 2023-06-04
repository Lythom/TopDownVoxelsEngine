// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/17

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.DemiEditor;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using DG.Tweening.TimelineEditor;
using DG.Tweening.TimelineEditor.ExtraEditors;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DG.Tweening.TimelineEditor
{
    /// <summary>
    /// Global editor settings for the visual timeline
    /// </summary>
    internal class DOTimelineEditorSettings : ScriptableObject
    {
        #region Serialized
#pragma warning disable 0649

        public int secondToPixels = 100;
        public int layerHeight = 18;
        public int layersPanelWidth = 180;
        public bool forceFoldoutsOpen = false; // Forces DOTweenClip Inspector foldouts to stay open
        public float actionsLayoutDuration = 0.5f; // visual duration in Timeline of no-duration clipElements (like Actions and Events)
        public int maxSnapPixelDistance = 20; // Max distance at which we'll snap to other clipElements when dragging
        public int minPixelDragDistance = 10; // Min distance before a dragging activates
        public bool enforceTargetTypeInClipElement = true; // If TRUE doesn't allow to drag a different target type for an existing clipElement
        public bool evidenceClipComponentInHierarchy = false;
        public bool evidenceClipInHierarchy = false; // Used only if evidenceClipComponentInHierarchy is TRUE
        public Defaults defaults = new Defaults();
        public Experimental experimental = new Experimental();
        public RecorderData recorderData = new RecorderData();
        public CustomPluginsEditorData customPluginsData = new CustomPluginsEditorData();
        //
        [SerializeField] int _lastSelectedComponentId;
        [SerializeField] string _lastSelectedClipGuid;
        [SerializeField] List<LastSelectedData> _lastSelectedClipElementsData = new List<LastSelectedData>();

#pragma warning restore 0649
        #endregion

        // Fixed settings
        [NonSerialized] public bool activateCustomPluginsGenerator = false;
        //

        static DOTimelineEditorSettings I;
        public const int MinSecondToPixels = 20;
        public const int MaxSecondToPixels = 1600;
        public const int MinLayerHeight = 16;
        public const int MaxLayerHeight = 42;
        public const float MinClipElementSnapping = 0.01f;

        static readonly List<DOTweenClip> _TmpClips = new List<DOTweenClip>();

        #region Public Methods

        public static DOTimelineEditorSettings Load()
        {
            if (TimelineSession.isDevDebugMode) DOLog.DebugDev("Developer Debug Mode <color=#00ff00>ACTIVE</color>");
            if (I == null) I = DeEditorPanelUtils.ConnectToSourceAsset<DOTimelineEditorSettings>(TimelinePaths.ADB.DOTimelineSettings, true, true);
            return I;
        }

        #region Selected Methods

        public void RefreshSelected(Component component, DOTweenClip clip, bool undoable = true)
        {
            if (undoable) Undo.RecordObject(this, "DOTweenClip");
            _lastSelectedComponentId = component == null ? 0 : component.GetInstanceID();
            _lastSelectedClipGuid = component == null ? null : clip.guid;
            _lastSelectedClipElementsData.Clear();
            if (clip != null) {
                int len = TimelineSelection.ClipElements.Count;
                for (int i = 0; i < len; ++i) {
                    _lastSelectedClipElementsData.Add(new LastSelectedData(TimelineSelection.ClipElements[i]));
                }
            }
            if (undoable) EditorUtility.SetDirty(this);
            // Debug.Log("RefreshSelected " + component + (clip == null ? "" : " " + clip.name)
            //           + ", totSelected: " + _lastSelectedClipElementsData.Count);
        }

        // NOTE: tried using ref instead or out to keep clip/SO if they were already correct but for some reason
        // they're actually incorrect and it doesn't work
        public void ReapplySelected(out Component component, out DOTweenClip clip, out SerializedProperty spClip)
        {
//            Debug.Log("ReapplySelected " + _lastSelectedComponentId + "/" + _lastSelectedClipGuid + "/" + _lastSelectedClipElementsData.Count);
            component = _lastSelectedComponentId == 0 ? null : EditorUtility.InstanceIDToObject(_lastSelectedComponentId) as Component;
            if (component == null && _lastSelectedComponentId != 0) {
                // Component doesn't exist anymore, clear cache
                ResetSelected(out component, out clip, out spClip);
                return;
            }
            if (component == null) clip = null;
            else {
                _TmpClips.Clear();
                TimelineEditorUtils.FindSerializedClips(component, _TmpClips, _lastSelectedClipGuid);
                clip = _TmpClips.Count > 0 ? _TmpClips[0] : null;
                _TmpClips.Clear();
            }
            TimelineSelection.Clear();
            if (clip == null) {
                ResetSelected(out component, out clip, out spClip);
                return;
            }
            spClip = TimelineEditorUtils.GetSerializedClip(component, clip.guid);
            foreach (LastSelectedData sel in _lastSelectedClipElementsData) {
                DOTweenClipElement clipElement = clip.FindClipElementByGuid(sel.guid);
                if (clipElement != null) {
                    TimelineSelection.Select(clipElement, true, false, sel.originalStartTime, sel.originalDuration, sel.originalLayerIndex);
                }
            }
        }

        public void StoreSelectedSnapshot()
        {
            recorderData.lastSelectedComponentId = _lastSelectedComponentId;
            recorderData.lastSelectedClipGuid = _lastSelectedClipGuid;
            recorderData.lastSelectedClipElementsData.Clear();
            foreach (LastSelectedData sel in _lastSelectedClipElementsData) recorderData.lastSelectedClipElementsData.Add(sel);
        }

        public void ReapplySelectedSnapshot()
        {
            _lastSelectedComponentId = recorderData.lastSelectedComponentId;
            _lastSelectedClipGuid = recorderData.lastSelectedClipGuid;
            _lastSelectedClipElementsData.Clear();
            foreach (LastSelectedData sel in recorderData.lastSelectedClipElementsData) _lastSelectedClipElementsData.Add(sel);
        }

        void ResetSelected(out Component component, out DOTweenClip clip, out SerializedProperty spClip)
        {
            Undo.RecordObject(this, "DOTweenClip");
            component = null;
            clip = null;
            spClip = null;
            _lastSelectedComponentId = 0;
            _lastSelectedClipGuid = null;
            _lastSelectedClipElementsData.Clear();
            EditorUtility.SetDirty(this);
        }

        #endregion

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public class Defaults
        {
            public float duration = 1;
            public Ease ease = Ease.OutQuad;
            public LoopType loopType = LoopType.Yoyo;
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public class Experimental
        {
            public bool enableRecordMode = false;
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public struct LastSelectedData
        {
            public string guid;
            public float originalStartTime, originalDuration;
            public int originalLayerIndex;
            internal LastSelectedData(TimelineSelection.SelectedClipElement selection)
            {
                this.guid = selection.clipElement.guid;
                this.originalStartTime = selection.originalStartTime;
                this.originalDuration = selection.originalDuration;
                this.originalLayerIndex = selection.originalLayerIndex;
            }
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public class RecorderData
        {
            public List<DOTweenClip> recordedClips = new List<DOTweenClip>();
            public List<DOTweenClip> tmpClonedClips = new List<DOTweenClip>();
            public int undoIndexBeforeRecording;
            public int lastSelectedComponentId;
            public string lastSelectedClipGuid;
            public List<LastSelectedData> lastSelectedClipElementsData = new List<LastSelectedData>();
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        internal class CustomPluginsEditorData
        {
            public ParsedPluginsFile parsedPluginsFile;
            [SerializeField] string _pluginsFilePath, _pluginsADBFilePath, _pluginsFileLabel;
            [SerializeField] string _pluginsFileName; // Filename without extension

            public string pluginsFilePath {
                get { return _pluginsFilePath; }
                set {
                    _pluginsFilePath = value;
                    _pluginsADBFilePath = string.IsNullOrEmpty(value) ? value : DeEditorFileUtils.FullPathToADBPath(value);
                    _pluginsFileLabel = string.IsNullOrEmpty(value) ? value : FormatFilePathForLabel(_pluginsADBFilePath);
                    _pluginsFileName = string.IsNullOrEmpty(value) ? value : Path.GetFileNameWithoutExtension(_pluginsFilePath);
                }
            }
            public string pluginsADBFilePath { get { return _pluginsADBFilePath; } }
            public string pluginsFileLabel { get { return _pluginsFileLabel; } }
            public string pluginsFileName { get { return _pluginsFileName; } }

            public void Refresh()
            {
                pluginsFilePath = _pluginsFilePath;
            }

            string FormatFilePathForLabel(string filePath)
            {
                int lastSlashIndex = filePath.LastIndexOf('/');
                if (lastSlashIndex == -1) lastSlashIndex = filePath.LastIndexOf('/');
                if (lastSlashIndex == -1) return filePath;
                return "<color=#de9cff>" + filePath.Substring(0, lastSlashIndex + 1) 
                                         + "</color><b><color=#ffd800>" + filePath.Substring(lastSlashIndex + 1) + "</color></b>";
            }
        }
    }
}