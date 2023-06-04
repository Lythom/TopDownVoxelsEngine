// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/02/10

using System;
using System.Collections.Generic;
using DG.Tweening.Timeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DG.Tweening.TimelineEditor
{
    /// <summary>
    /// Not really a recorder but more an edit-mode controller, but recorder is shorter
    /// </summary>
    internal static class DOTimelineRecorder
    {
        // ■■■ EVENTS
        public static event Action OnStopRecording;
        static void Dispatch_OnStopRecording() { if (OnStopRecording != null) OnStopRecording(); }
        // ■■■

        public static bool isRecording { get; private set; }

        static DOTweenClipTimeline _editor { get { return DOTweenClipTimeline.editor; } }
        static DOTimelineEditorSettings _settings { get { return DOTweenClipTimeline.settings; } }

        #region Public Methods

        public static void EnterRecordMode(DOTweenClip clip)
        {
            if (isRecording) return;
            isRecording = true;
            GUI.FocusControl(null);
            TimelineUndoUtils.RegisterSceneUndo();
            TimelineEditorUtils.RegisterCompleteSceneUndo("DOTimelineRecorder");
            int currUndoGroup = Undo.GetCurrentGroup();
            using (new DOScope.NonUndoableSettingsSerialization()) {
                _settings.recorderData.undoIndexBeforeRecording = currUndoGroup;
                _settings.recorderData.recordedClips.Clear();
                AddClip(clip);
            }
            AddCallbacks();
        }

        public static void ExitRecordMode(bool applyRecordedChanges)
        {
            if (!isRecording) return;
            isRecording = false;
            if (applyRecordedChanges) StoreSnapshot();
            if (DOTimelinePreviewManager.isPlayingOrPreviewing) DOTimelinePreviewManager.StopPreview();
//            Debug.Log("REC ► REVERT TO " + _settings.recorderData.undoIndexBeforeRecording);
            Undo.RevertAllDownToGroup(_settings.recorderData.undoIndexBeforeRecording);
            if (applyRecordedChanges) ApplySnapshot();
            using (new DOScope.NonUndoableSettingsSerialization()) {
                _settings.recorderData.recordedClips.Clear();
                _settings.recorderData.tmpClonedClips.Clear();
            }
            RemoveCallbacks();
            Dispatch_OnStopRecording();
            DOTweenClipTimeline.editor.Repaint();
            // Fix for UI elements not being reset correctly until you click them
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            foreach (Canvas c in canvases) {
                if (!c.gameObject.activeSelf) continue;
                c.gameObject.SetActive(false);
                c.gameObject.SetActive(true);
            }
            // Extra fixes
            TimelineUndoUtils.RestoreAndClearCache();
        }

        public static void StoreSnapshot()
        {
            using (new DOScope.NonUndoableSettingsSerialization()) {
                _settings.RefreshSelected(DOTweenClipTimeline.src, DOTweenClipTimeline.clip, false);
                _settings.StoreSelectedSnapshot();
                _settings.recorderData.tmpClonedClips.Clear();
                for (int i = 0; i < _settings.recorderData.recordedClips.Count; ++i) {
                    _settings.recorderData.tmpClonedClips.Add(_settings.recorderData.recordedClips[i].Clone(false));
                }
            }
        }

        public static void ApplySnapshot()
        {
            using (new DOScope.NonUndoableSettingsSerialization()) {
                for (int i = 0; i < _settings.recorderData.recordedClips.Count; ++i) {
                    _settings.recorderData.recordedClips[i].AssignPropertiesFrom(_settings.recorderData.tmpClonedClips[i], true);
                }
                _settings.ReapplySelectedSnapshot();
            }
        }

        #endregion

        #region Methods

        static void AddCallbacks()
        {
            RemoveCallbacks();
            DOTweenClipTimeline.OnClipOpened += OnClipOpened;
            EditorSceneManager.sceneOpening += OnSceneOpening;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static void RemoveCallbacks()
        {
            DOTweenClipTimeline.OnClipOpened -= OnClipOpened;
            EditorSceneManager.sceneOpening -= OnSceneOpening;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        static void AddClip(DOTweenClip clip)
        {
            if (_settings.recorderData.recordedClips.Contains(clip)) return;
            using (new DOScope.NonUndoableSettingsSerialization()) {
                _settings.recorderData.recordedClips.Add(clip);
            }
        }

        #endregion

        #region Callbacks

        static void OnClipOpened(DOTweenClip clip)
        {
            if (!isRecording) return;
            AddClip(clip);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            ExitRecordMode(true);
        }

        static void OnSceneOpening(string path, OpenSceneMode mode)
        {
            ExitRecordMode(true);
        }

        static void OnBeforeAssemblyReload()
        {
            ExitRecordMode(true);
        }

        #endregion
    }
}