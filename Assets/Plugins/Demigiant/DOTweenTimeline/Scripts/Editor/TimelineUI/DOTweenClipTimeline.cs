// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/16

using System;
using DG.DemiEditor;
using DG.DemiLib;
using DG.DOTweenEditor.UI;
using DG.Tweening.TimelineEditor.ClipElementUI;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using DG.Tweening.TimelineEditor.ExtraEditors;
using DG.Tweening.TimelineEditor.PropertyDrawers;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    class DOTweenClipTimeline : EditorWindow, IHasCustomMenu
    {
        public enum Mode
        {
            Default,
            Settings,
            Help
        }

        // ■■■ EVENTS
        public static event Action OnPrefabEditingModeChanged; // Dispatched when entering/exiting prefab editing mode (if the Timeline is open)
        public static event Action<DOTweenClip> OnClipOpened;
        public static event Action<DOTweenClip> OnClipChanged;
        public static event Action OnMouseDown;
        static void Dispatch_OnPrefabEditingModeChanged() { if (OnPrefabEditingModeChanged != null) OnPrefabEditingModeChanged(); }
        static void Dispatch_OnClipOpened(DOTweenClip clip) { if (OnClipOpened != null) OnClipOpened(clip); }
        public static void Dispatch_OnClipChanged(DOTweenClip clip) { if (OnClipChanged != null) OnClipChanged(clip); }
        static void Dispatch_OnMouseDown() { if (OnMouseDown != null) OnMouseDown(); }
        // ■■■

        [MenuItem("Tools/Demigiant/" + _Title)]
        static void ShowWindow() { ShowWindow(null, null, null); }
		
        public const int LayerHeight = 18;
        public static DOTweenClipTimeline editor { get; private set; }
        public static DOTimelineEditorSettings settings { get; private set; }
        public static DOTweenTimelineSettings runtimeSettings { get; private set; }
        public static readonly TimelineLayout Layout = new TimelineLayout();
        public static SerializedProperty spClip; // Used by ClipElementEditor to display UnityEvents
        public static Component src;
        public static DOTweenClip clip;
        public static Mode mode = Mode.Default;
        public static bool isPlayingOrPreviewing { get; private set; } // True if Application or DOTimelinePreviewManager are playing or previewing
        public static bool isUndoRedoPass { get; private set; } // TRUE during the first Repaint after an undoRedo
        public static bool isRecorderOrPreviewUndoPass { get; private set; } // TRUE during the first Repaint after TimelineRecorder has stopped
        public static bool isOrWillResizeLayersPanel { get { return _isPreparingToResizeLayersPanel || _isResizingLayersPanel; } }
        public static StageMode stageMode { get; private set; }
        public PrefabEditSaveMode prefabEditingSaveMode { get; private set; }

        const string _Title = "DOTween Timeline";
        static bool _isPreparingToResizeLayersPanel;
        static bool _isResizingLayersPanel;
        Vector2 _dragStartP;
        float _layersPanelDragStartWidth;
        readonly TimelineGlobalHeader _globalHeader = new TimelineGlobalHeader();
        readonly TimelineLayersHeader _layersHeader = new TimelineLayersHeader();
        readonly TimelineLayers _layers = new TimelineLayers();
        readonly TimelineHeader _timelineHeader = new TimelineHeader();
        readonly TimelineMain _timeline = new TimelineMain();
        readonly TimelineScrubber _scrubber = new TimelineScrubber();
        readonly ClipElementEditor _clipElementEditor = new ClipElementEditor();
        readonly TimelineSettingsUI _settingsEditor = new TimelineSettingsUI();
        readonly TimelineHelpUI _helpEditor = new TimelineHelpUI();
        readonly GUIContent _gcDisconnected = new GUIContent("<b>Select a DOTweenClip in the Inspector to edit it</b>" +
                                                      "\n(<i>or use the button below to select one of the existing DOTweenClips in the Scene</i>)");
        readonly GUIContent _gcSelectClip = new GUIContent("Select DOTweenClip in Scene ▾");
        readonly GUIContent _gcSettings = new GUIContent("Settings");
        readonly GUIContent _gcHelp = new GUIContent("Help");
        readonly GUIContent _gcCustomPluginsEditor = new GUIContent("Custom Plugins Editor");
        readonly GUIContent _gcPrefabEditingAutoSaveWarning = new GUIContent("DOTween Timeline requires <b>Prefab AutoSave</b> to be <b>disabled</b>" +
                                                                             " (<i>top-right toggle in Unity's Scene view</i>)");

        #region Unity and GUI Methods

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("DOTween Timeline > Cleanup current DOTweenClip"), false, () => {
                if (clip == null) {
                    EditorUtility.DisplayDialog("DOTween Timeline > Cleanup", "You must open a DOTweenClip to apply the cleanup", "Ok");
                    return;
                }
                TimelineEditorUtils.CleanupClip(clip);
            });
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/ON"), TimelineSession.isDevDebugMode, null);
            menu.AddSeparator("DOTween Timeline > Developer Debug Mode/");
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/ACTIVATE ALL"), false, () => {
                TimelineSession.ToggleAll(true);
            });
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/DEACTIVATE ALL"), false, () => {
                TimelineSession.ToggleAll(false);
            });
            menu.AddSeparator("DOTween Timeline > Developer Debug Mode/");
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/Log Undo\\Redo Events"), TimelineSession.logUndoRedoEvents, () => {
                TimelineSession.logUndoRedoEvents = !TimelineSession.logUndoRedoEvents;
            });
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/Log Missing PlugDataGuid Assignment"), TimelineSession.logMissingPlugDataGuidAssignment, () => {
                TimelineSession.logMissingPlugDataGuidAssignment = !TimelineSession.logMissingPlugDataGuidAssignment;
            });
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/Show Clips Guid"), TimelineSession.showClipGuid, () => {
                TimelineSession.showClipGuid = !TimelineSession.showClipGuid;
            });
            menu.AddItem(new GUIContent("DOTween Timeline > Developer Debug Mode/Show ClipElements PlugData[Index|Guid]"), TimelineSession.showClipElementPlugDataIndexAndGuid, () => {
                TimelineSession.showClipElementPlugDataIndexAndGuid = !TimelineSession.showClipElementPlugDataIndexAndGuid;
            });
        }

        void OnEnable()
        {
            SetTitle();
            ConnectToSettings();
            Refresh();
            this.wantsMouseMove = true;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            DOTimelineRecorder.OnStopRecording += OnRecorderOrPreviewStopped;
            DOTimelinePreviewManager.OnStopPreviewing += OnRecorderOrPreviewStopped;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            DOTimelineRecorder.OnStopRecording -= OnRecorderOrPreviewStopped;
            DOTimelinePreviewManager.OnStopPreviewing -= OnRecorderOrPreviewStopped;
        }

        void OnHierarchyChange()
        {
            Refresh();
            Repaint();
        }

        void OnUndoRedoPerformed()
        {
            if (TimelineSession.logUndoRedoEvents) Debug.Log("<color=#ff0000>OnUndoRedoPerformed</color>");
            isUndoRedoPass = true;
            settings.ReapplySelected(out src, out clip, out spClip);
            Refresh();
            TimelineSelection.RefreshSelectionsData();
            Repaint();
        }

        void SetTitle()
        {
            this.titleContent = new GUIContent("Timeline", EditorGUIUtils.logo);
        }

        void Update()
        {
            if (Application.isPlaying) Repaint();
        }

        void OnGUI()
        {
            ConnectToSettings();
            DOEGUI.BeginGUI();
            Rect area = position.ResetXY();
            isPlayingOrPreviewing = Application.isPlaying || DOTimelinePreviewManager.isPlayingOrPreviewing;
            bool isRecording = DOTimelineRecorder.isRecording;
            bool isRepaint = Event.current.type == EventType.Repaint;

            using (new EditorGUI.DisabledScope(isPlayingOrPreviewing)) {
                switch (mode) {
                case Mode.Settings:
                    using (new GUILayout.AreaScope(area)) _settingsEditor.Draw(area);
                    isUndoRedoPass = isRecorderOrPreviewUndoPass = false;
                    return;
                case Mode.Help:
                    using (new GUILayout.AreaScope(area)) _helpEditor.Draw(area);
                    isUndoRedoPass = isRecorderOrPreviewUndoPass = false;
                    return;
                }
            }

            if (src == null) {
                TimelineSelection.Clear();
                isUndoRedoPass = isRecorderOrPreviewUndoPass = false;
                Rect descrR = new Rect(area.x + 10, area.y + 10, area.width - 20,
                    DOEGUI.Styles.timeline.disconnectedLabel.CalcHeight(_gcDisconnected, area.width)
                );
                Rect btClipR = descrR.SetY(descrR.yMax + 6).SetWidth(GUI.skin.button.CalcSize(_gcSelectClip).x).SetHeight(22);
                Rect btSettingsR = btClipR.SetX(btClipR.xMax + 2).SetWidth(80);
                Rect btHelpR = btSettingsR.SetX(btSettingsR.xMax + 2);
                DeGUI.DrawTiledTexture(area, DeStylePalette.tileBars_slanted_alpha, 1f, new DeSkinColor(0.1f));
                EditorGUI.DropShadowLabel(descrR, _gcDisconnected, DOEGUI.Styles.timeline.disconnectedLabel);
                using (new DeGUI.ColorScope(DOEGUI.Colors.global.purple)) {
                    if (GUI.Button(btClipR, _gcSelectClip, DOEGUI.Styles.timeline.seqBtEdit)) TimelineEditorUtils.CM_SelectClipInScene(btClipR, clip);
                }
                if (GUI.Button(btSettingsR, _gcSettings)) mode = Mode.Settings;
                if (GUI.Button(btHelpR, _gcHelp)) mode = Mode.Help;
                if (settings.activateCustomPluginsGenerator) {
                    Rect btCustomPluginsEditorR = btSettingsR.ShiftY(btClipR.height + 10).SetWidth(btHelpR.xMax - btSettingsR.x);
                    if (GUI.Button(btCustomPluginsEditorR, _gcCustomPluginsEditor)) CustomPluginsEditor.ShowWindow();
                }
                return;
            }

            if (stageMode == StageMode.PrefabEditingMode) {
                RefreshPrefabStageData();
                if (prefabEditingSaveMode == PrefabEditSaveMode.AutoSave) {
                    DeGUI.DrawTiledTexture(area, DeStylePalette.tileBars_slanted, 1f, new Color(0.11f, 0.3f, 0.48f, 0.5f));
                    float warningH = DOEGUI.Styles.global.warningLabelBox.CalcHeight(_gcPrefabEditingAutoSaveWarning, 230);
                    Rect warningR = new Rect(0, 0, 230, warningH).SetCenter(area.center.x, area.center.y);
                    using (new DeGUI.ColorScope(new Color(0.91f, 0.42f, 0f))) {
                        GUI.Label(warningR, _gcPrefabEditingAutoSaveWarning, DOEGUI.Styles.global.warningLabelBox);
                    }
                    return;
                }
            }

            MarkForUndo();
//            Debug.Log(Time.frameCount + " ED > " + Event.current.type + ", undoPass: " + isUndoRedoPass + ", recOrPreviewUndoPass: " + isRecorderOrPreviewUndoPass);

            bool hasSameTypeSelection = TimelineSelection.HasSelections(true);
            bool showClipElementEditor = hasSameTypeSelection
                                       && !TimelineSelection.isDraggingSelection
                                       && !TimelineSelection.isDraggingClipElements
                                       && !TimelineSelection.isDraggingDuration;

            Rect headerR = area.SetHeight(EditorGUIUtility.singleLineHeight + 2);
            Rect contentR = area.ShiftYAndResize(headerR.height);
            Rect clipElementEditorR = showClipElementEditor
                ? contentR.ShiftXAndResize(contentR.width - ClipElementEditor.DefaultWidth)
                : contentR.ShiftXAndResize(contentR.width);
            Rect layersR = contentR.SetWidth(settings.layersPanelWidth);
            // Validate layers panel size
            int maxLayersPanelW = (int)(area.width - clipElementEditorR.width) - 50;
            if (settings.layersPanelWidth > maxLayersPanelW) {
                settings.layersPanelWidth = Mathf.Max(TimelineLayers.MinSize, maxLayersPanelW);
                MarkDirty(true, false);
                layersR.width = settings.layersPanelWidth;
                if (layersR.width <= TimelineLayers.MinSize && layersR.width > maxLayersPanelW) {
                    // Hide layers
                    layersR.width = 18;
                }
            }
            //
            Rect layersHeaderR = layersR.SetHeight(18);
            layersR = layersR.ShiftYAndResize(layersHeaderR.height);
            Rect timelineR = contentR.ShiftXAndResize(layersR.width).Shift(0, 0, -clipElementEditorR.width, 0);
            Rect timelineHeaderR = timelineR.SetHeight(18);
            timelineR = timelineR.ShiftYAndResize(timelineHeaderR.height);
            Rect scrubberR = timelineHeaderR.Shift(0, 0, 0, timelineR.height);
            Layout.Refresh(timelineR);

            // Check for layers panel resizing
            if (!_isResizingLayersPanel) {
                float dragHalfW = 4;
                Rect dragLayersPanelR = layersR.Shift(layersR.width - dragHalfW, 0, 0, 0).SetWidth(dragHalfW * 2);
                bool wasPreparingToResizeLayersPanel = _isPreparingToResizeLayersPanel;
                _isPreparingToResizeLayersPanel = dragLayersPanelR.Contains(Event.current.mousePosition);
                if (_isPreparingToResizeLayersPanel != wasPreparingToResizeLayersPanel) Repaint();
            }
            if (isOrWillResizeLayersPanel) {
                EditorGUIUtility.AddCursorRect(area, MouseCursor.ResizeHorizontal);
                switch (Event.current.rawType) {
                case EventType.MouseUp:
                    if (_isResizingLayersPanel) StopResizingLayersPanel();
                    break;
                }
                switch (Event.current.type) {
                case EventType.MouseDown:
                    switch (Event.current.button) {
                    case 0:
                        if (_isPreparingToResizeLayersPanel) DragResizeLayersPanel(true);
                        break;
                    }
                    break;
                case EventType.MouseDrag:
                    if (_isResizingLayersPanel) DragResizeLayersPanel(false);
                    break;
                }
            }

            // Not in disabledScope so clip selection can work at runtime
            using (new GUILayout.AreaScope(headerR)) _globalHeader.Draw(headerR);
            if (clip != null) { // Clip might have been closed by global header
                // Not in disabledScope so preview button can work while previewing
                using (new GUILayout.AreaScope(layersHeaderR)) _layersHeader.Draw(layersHeaderR);
                using (new EditorGUI.DisabledScope(isPlayingOrPreviewing && !isRecording)) {
                    if (hasSameTypeSelection) {
                        using (new GUILayout.AreaScope(clipElementEditorR)) _clipElementEditor.Draw(clipElementEditorR);
                    }
                }
                using (new EditorGUI.DisabledScope(isPlayingOrPreviewing)) {
                    using (new GUILayout.AreaScope(layersR)) _layers.Draw(layersR);
                    using (new GUILayout.AreaScope(timelineHeaderR)) _timelineHeader.Draw(timelineHeaderR);
                }
                using (new EditorGUI.DisabledScope(_isResizingLayersPanel)) {
                    using (new GUILayout.AreaScope(timelineR)) _timeline.Draw(timelineR);
                }
                // Not in disabledScope so scrubbing can work while in editor preview mode
                using (new GUILayout.AreaScope(scrubberR)) _scrubber.Draw(scrubberR);

                if (DOTimelineRecorder.isRecording) {
                    // Recording border
                    using (new DeGUI.ColorScope(null, null, Color.red)) {
                        GUI.Box(area, GUIContent.none, DOEGUI.Styles.timeline.previewBorderBox);
                    }
                } else if (DOTimelinePreviewManager.isPlayingOrPreviewing) {
                    // Preview border
                    using (new DeGUI.ColorScope(null, null, DOEGUI.Colors.global.yellow)) {
                        GUI.Box(area, GUIContent.none, DOEGUI.Styles.timeline.previewBorderBox);
                    }
                }
            }

            if (GUI.changed) {
                MarkDirty();
                Dispatch_OnClipChanged(clip);
            }
            if (isRepaint) {
                bool wasRecorderStoppedPass = isRecorderOrPreviewUndoPass;
                isUndoRedoPass = isRecorderOrPreviewUndoPass = false;
                if (wasRecorderStoppedPass) Repaint();
            } else if (Event.current.type == EventType.MouseDown) {
                Dispatch_OnMouseDown();
            }
        }

        void DragResizeLayersPanel(bool begin)
        {
            if (begin) {
                _isResizingLayersPanel = true;
                _isPreparingToResizeLayersPanel = false;
                _dragStartP = Event.current.mousePosition;
                _layersPanelDragStartWidth = settings.layersPanelWidth;
            } else {
                using (new DOScope.UndoableSerialization(true, false)) {
                    settings.layersPanelWidth = Mathf.Max(
                        (int)(_layersPanelDragStartWidth + (Event.current.mousePosition.x - _dragStartP.x)),
                        TimelineLayers.MinSize
                    );
                }
                Repaint();
            }
        }

        void StopResizingLayersPanel()
        {
            _isPreparingToResizeLayersPanel = _isResizingLayersPanel = false;
            Repaint();
        }

        #endregion

        #region Methods

        void Refresh()
        {
            editor = this;
            if (RefreshPrefabStageData()) return;

            if (src == null) settings.ReapplySelected(out src, out clip, out spClip);
            else settings.RefreshSelected(src, clip);
            if (clip != null) {
                if (TimelineEditorUtils.ValidateAndFixClip(clip, spClip)) EditorUtility.SetDirty(src);
            }
            _globalHeader.Refresh();
            _layersHeader.Refresh();
            _layers.Refresh();
            _timelineHeader.Refresh();
            _timeline.Refresh();
            _scrubber.Refresh();
            _clipElementEditor.Refresh();
            _settingsEditor.Refresh();
        }

        /// <summary>
        /// Returns TRUE if prefab editing mode changed
        /// </summary>
        bool RefreshPrefabStageData()
        {
            StageMode currStageMode = TimelineEditorUtils.IsEditingPrefab() ? StageMode.PrefabEditingMode : StageMode.Normal;
            bool prefabEditingModeChanged = stageMode != StageMode.Unset && currStageMode != stageMode;
            stageMode = currStageMode;
            prefabEditingSaveMode = TimelineEditorUtils.GetPrefabEditSaveMode();
            if (prefabEditingModeChanged) {
                DOTweenClipPropertyDrawer.ForceClear();
                CloseCurrentClip();
                Dispatch_OnPrefabEditingModeChanged();
                return true;
            }
            return false;
        }

        static void ConnectToSettings()
        {
            if (settings == null) settings = DOTimelineEditorSettings.Load();
            if (runtimeSettings == null) {
                runtimeSettings = DeEditorPanelUtils.ConnectToSourceAsset<DOTweenTimelineSettings>(TimelinePaths.ADB.DOTweenTimelineSettings, true, true);
            }
        }

        #endregion

        #region Public Methods

        public static void ShowWindow(Component component, DOTweenClip clip, SerializedProperty spClip)
        {
            if (spClip == null && clip != null) spClip = TimelineEditorUtils.GetSerializedClip(component, clip.guid);
            DOTweenClipTimeline.src = component;
            DOTweenClipTimeline.clip = clip;
            DOTweenClipTimeline.spClip = spClip;
            ConnectToSettings();
            if (component != null) settings.RefreshSelected(component, clip);
            TimelineSelection.Clear();
            editor = GetWindow<DOTweenClipTimeline>(false, _Title);
            editor.SetTitle();
            editor.Refresh();
            Dispatch_OnClipOpened(clip);
        }

        public static void CloseCurrentClip()
        {
            if (DOTimelineRecorder.isRecording) DOTimelineRecorder.ExitRecordMode(true);
            if (DOTimelinePreviewManager.isPlaying) DOTimelinePreviewManager.StopPreview();
            src = null;
            clip = null;
            spClip = null;
            ConnectToSettings();
            settings.RefreshSelected(null, null, false);
            if (editor != null) editor.Repaint();
        }

        public static void ShowSettingsPanel()
        {
            mode = Mode.Settings;
        }

        public static void ShowHelpPanel()
        {
            mode = Mode.Help;
        }

        public void MarkForUndo(bool markSettings = true, bool markComponent = true, bool markRuntimeSettings = false)
        {
            if (markSettings) Undo.RecordObject(settings, _Title);
            if (markComponent) {
                if (src != null) Undo.RecordObject(src, _Title);
                if (spClip != null) {
                    try {
                        spClip.serializedObject.Update();
                    } catch {
                        // Happens when deselecting the component that contains the clip: Unity clears the SO contents
                        // ► Regenerate it
                        spClip = TimelineEditorUtils.GetSerializedClip(src, clip.guid);
                        spClip.serializedObject.Update();
                    }
                }
            }
            if (markRuntimeSettings) Undo.RecordObject(runtimeSettings, _Title);
        }

        public void MarkDirty(bool markSettings = true, bool markComponent = true, bool markRuntimeSettings = false)
        {
            if (markSettings) EditorUtility.SetDirty(settings);
            if (markComponent) {
                if (spClip != null) spClip.serializedObject.ApplyModifiedProperties();
                if (src != null) EditorUtility.SetDirty(src);
            }
            if (markRuntimeSettings) EditorUtility.SetDirty(runtimeSettings);
        }

        #endregion

        #region Callbacks

        void OnRecorderOrPreviewStopped()
        {
            isRecorderOrPreviewUndoPass = true;
            string currClipGuid = clip.guid;
            settings.ReapplySelected(out src, out clip, out spClip);
            if (clip != null && clip.guid != currClipGuid) {
                Event.current.Use();
                ShowWindow(src, clip, spClip);
            }
        }

        #endregion
    }
}