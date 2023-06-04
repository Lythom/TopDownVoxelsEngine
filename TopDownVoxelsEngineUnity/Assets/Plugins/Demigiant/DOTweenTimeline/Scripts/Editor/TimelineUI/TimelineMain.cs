// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/17

using System;
using System.Collections.Generic;
using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using DG.Tweening.Timeline.Core.Plugins;
using DG.Tweening.TimelineEditor.ClipElementUI;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace DG.Tweening.TimelineEditor
{
    internal class TimelineMain : ABSTimelineElement
    {
        enum LayerPass
        {
            Main, Overlay
        }

        readonly Color _bgColor = new DeSkinColor(0.1f);
        readonly Color _prefabModeBgColor = new Color(0.04f, 0.18f, 0.31f);
        readonly Color _timeSeparatorColor0 = new Color(0, 0, 0, 0.4f);
        readonly Color _timeSeparatorColor1 = new Color(1, 1, 1, 0.2f);
        readonly Color _separatorFractionColor = new Color(0, 0, 0, 0.1f);
        readonly Color _rowColor0 = new DeSkinColor(0.2f);
        readonly Color _rowColor1 = new DeSkinColor(0.25f);
        readonly Color _prefabModeRowColor0 = new Color(0.08f, 0.27f, 0.45f);
        readonly Color _prefabModeRowColor1 = new Color(0.12f, 0.34f, 0.54f);
        readonly Color _inactiveOverlayColor = new Color(0.82f, 0.02f, 0.15f, 1f);
        readonly Color _lockedBgColor = new Color(0.99f, 0.76f, 0.02f, 0.5f);
        readonly Color _shadowColor = new Color(0, 0, 0, 0.6f);
        readonly Color _selectionColor = new Color(0f, 0.68f, 1f);
        readonly Color _snapToClipElementColor = new Color(1f, 0f, 0.99f);
        readonly Color _pinBgColor = new Color(0.38f, 0.02f, 0.7f);
        readonly Color _pinBorderColor = new Color(0.67f, 0.94f, 0.52f);

        Vector2 _dragStartP;
        Vector2 _dragCurrMouseP; // Used by continuous shift update
        DOTweenClipElement _currMouseOverClipElement; // Refreshed on every GUI call by DrawLayer
        bool _isDraggingTimeline, _isPreparingToDragClipElements;
        bool _isSnapDragging; // TRUE only if snapping to multiplier values, not to other elements
        SnapToClipElementData _snapToClipElementData = new SnapToClipElementData();
        bool _isPreparingToDragDuration, _durationDraggingWaitingForFirstStep;
        DOTweenClipElement _isPreparingToDragDurationMainTarget;
        int _draggedMainClipElementCurrLayerIndex, _draggedClipElementsMaxLayerShift, _draggedClipElementsMinLayerShift;
        Vector2Int _timelineShiftSnapshot;
        Rect _allSelectedR;
        Rect _dragSelectionR;
        bool _keyAlt;
        readonly List<Rect> _tmpPassRects = new List<Rect>(); // Used to determine if there are clipElement rects overlapping

        #region GUI

        public void Refresh() {}

        public override void Draw(Rect drawArea)
        {
            if (Event.current.type == EventType.Layout || isRecorderStoppedPass) return;

            base.Draw(drawArea);

            _keyAlt = DeGUIKey.alt;

            // Input - PRE
            switch (Event.current.rawType) {
            case EventType.MouseDown: // Raw so it can work even when scope is disabled during playmode
                switch (Event.current.button) {
                case 0: // LMB
                    if (Event.current.alt) {
                        GUI.FocusControl(null);
                        DragTimeline(true);
                        Event.current.Use();
                    }
                    break;
                case 2: // MMB
                    GUI.FocusControl(null);
                    DragTimeline(true);
                    break;
                }
                break;
            case EventType.MouseUp:
                switch (Event.current.button) {
                case 0: // LMB
                    if (_isDraggingTimeline) StopDragTimeline();
                    else if (TimelineSelection.isDraggingSelection) StopDragSelection();
                    else if (_isPreparingToDragClipElements || TimelineSelection.isDraggingClipElements) StopDraggingClipElements();
                    else if (TimelineSelection.isDraggingDuration || _isPreparingToDragDuration) StopDraggingClipElementsDuration();
                    break;
                case 2: // MMB
                    if (_isDraggingTimeline) StopDragTimeline();
                    break;
                }
                break;
            case EventType.MouseDrag: // Raw so it can work even when scope is disabled during playmode
                if (_isDraggingTimeline) DragTimeline();
                else if (TimelineSelection.isDraggingSelection) DragSelection();
                else if (_isPreparingToDragClipElements || TimelineSelection.isDraggingClipElements) DragClipElements();
                else if (TimelineSelection.isDraggingDuration) DragClipElementsDuration();
                break;
            case EventType.ScrollWheel: // Change zoom (secondToPixels or layerHeight) - Raw so it can work even when scope is disabled during playmode
                if (!area.Contains(Event.current.mousePosition)) break;
                Vector2 scroll = Event.current.delta;
                if (DeGUIKey.shift) {
                    int newLayerHeight = Mathf.Min(
                        DOTimelineEditorSettings.MaxLayerHeight,
                        Mathf.Max(DOTimelineEditorSettings.MinLayerHeight, (int)(settings.layerHeight - scroll.y))
                    );
                    if (newLayerHeight != settings.layerHeight) TimelineEditorUtils.UpdateLayerHeight(newLayerHeight, Event.current.mousePosition.y);
                } else {
                    int newSecondToPixels = Mathf.Min(
                        DOTimelineEditorSettings.MaxSecondToPixels,
                        Mathf.Max(DOTimelineEditorSettings.MinSecondToPixels, (int)(settings.secondToPixels - scroll.y * 4))
                    );
                    if (newSecondToPixels != settings.secondToPixels) TimelineEditorUtils.UpdateSecondToPixels(newSecondToPixels, Event.current.mousePosition.x);
                }
                editor.Repaint();
                break;
            }
            switch (Event.current.type) {
            case EventType.MouseDown:
                switch (Event.current.button) {
                case 0: // LMB
                    if (_isPreparingToDragDuration && area.Contains(Event.current.mousePosition)) {
                        GUI.FocusControl(null);
                        DragClipElementsDuration(true);
                        Event.current.Use();
                    }
                    break;
                }
                break;
            case EventType.DragUpdated:
                if (!IsValidDragAndDrop(DragAndDrop.objectReferences)) break;
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                break;
            case EventType.DragPerform:
                if (!IsValidDragAndDrop(DragAndDrop.objectReferences)) break;
                CompleteDragAndDrop(DragAndDrop.objectReferences[0]);
                DeGUI.ExitCurrentEvent();
                return;
            }
            //
            if (_isDraggingTimeline) EditorGUIUtility.AddCursorRect(area, MouseCursor.Pan);
            else if (TimelineSelection.isDraggingClipElements) EditorGUIUtility.AddCursorRect(area, MouseCursor.MoveArrow);
            else if (_isPreparingToDragDuration || TimelineSelection.isDraggingDuration) EditorGUIUtility.AddCursorRect(area, MouseCursor.ResizeHorizontal);

            // Draw
            _currMouseOverClipElement = null;
            bool hasLayers = clip.layers != null;
            DrawBackground(hasLayers);
            DrawTimeSeparators();
            if (hasLayers) {
                bool wasPreparingToDragDuration = _isPreparingToDragDuration;
                if (!TimelineSelection.isDraggingDuration) {
                    _isPreparingToDragDuration = false;
                    _isPreparingToDragDurationMainTarget = null;
                }
                for (int i = layout.firstVisibleLayerIndex; i < layout.visibleLayersDrawLoopLength; ++i) DrawLayer(LayerPass.Main, i);
                if (_isPreparingToDragDuration != wasPreparingToDragDuration) editor.Repaint();
            }
            if (TimelineSelection.isDraggingSelection || TimelineSelection.containsClipElements) {
                _allSelectedR = new Rect(99999999, 0, 0, 0);
                for (int i = layout.firstVisibleLayerIndex; i < layout.visibleLayersDrawLoopLength; ++i) DrawLayer(LayerPass.Overlay, i);
            }
            DrawOverlay();

            // Input - POST
            switch (Event.current.type) {
            case EventType.MouseDown:
                switch (Event.current.button) {
                case 0: // LMB
                    GUI.FocusControl(null);
                    if (TimelineSelection.containsClipElements) {
                        TimelineSelection.DeselectAll();
                        editor.Repaint();
                    }
                    DragSelection(true);
                    break;
                case 1: // RMB
                    if (_currMouseOverClipElement != null && !TimelineSelection.Contains(_currMouseOverClipElement)) {
                        DOTweenClip.ClipLayer layer = clip.Editor_GetClipElementLayer(_currMouseOverClipElement.guid);
                        if (layer.locked) editor.ShowNotification(new GUIContent(string.Format("Layer \"{0}\" is locked", layer.name)));
                        else {
                            TimelineSelection.Select(_currMouseOverClipElement);
                            editor.Repaint();
                        }
                    }
                    break;
                }
                break;
            case EventType.ContextClick:
                int layerIndex = layout.GetLayerIndexAtMouse();
                if (layerIndex != -1 && clip.layers[layerIndex].locked) {
                    editor.ShowNotification(new GUIContent(string.Format("Layer \"{0}\" is locked", clip.layers[layerIndex].name)));
                } else {
                    if (_currMouseOverClipElement == null) CM_EmptyArea();
                    else CM_ClipElement();
                }
                break;
            case EventType.KeyDown:
                if (isPlayingOrPreviewing) break;
                switch (Event.current.keyCode) {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    if (GUIUtility.hotControl > 0 || GUIUtility.keyboardControl > 0) return;
                    if (!TimelineSelection.containsClipElements) break;
                    if (_isPreparingToDragClipElements || TimelineSelection.isDraggingClipElements) StopDraggingClipElements();
                    for (int i = 0; i < TimelineSelection.ClipElements.Count; ++i) {
                        TimelineEditorUtils.RemoveClipElement(clip, TimelineSelection.ClipElements[i].clipElement.guid);
                    }
                    TimelineSelection.DeselectAll();
                    GUI.changed = true;
                    break;
                case KeyCode.Escape:
                    if (TimelineSelection.HasSelections()) {
                        TimelineSelection.DeselectAll();
                        editor.Repaint();
                    }
                    break;
                case KeyCode.A:
                    if (GUIUtility.hotControl > 0 || GUIUtility.keyboardControl > 0) return;
                    if (DeGUIKey.Exclusive.ctrl) {
                        TimelineSelection.SelectAllIn(clip, false);
                        editor.Repaint();
                    }
                    break;
                case KeyCode.D:
                    if (GUIUtility.hotControl > 0 || GUIUtility.keyboardControl > 0) return;
                    if (DeGUIKey.Exclusive.ctrl) {
                        TimelineSelection.DeselectAll();
                        editor.Repaint();
                    }
                    break;
                case KeyCode.C:
                    if (DeGUIKey.Exclusive.ctrl) CopySelectedClipElements(false);
                    break;
                case KeyCode.V:
                    if (DeGUIKey.Exclusive.ctrl || DeGUIKey.Exclusive.ctrlShift) {
                        PasteClipElementsFromClipboard(
                            layout.GetSecondsAtMouse(), layout.GetLayerIndexAtMouse(true), DeGUIKey.Exclusive.ctrlShift
                        );
                    }
                    break;
                case KeyCode.X:
                    if (DeGUIKey.Exclusive.ctrl) CopySelectedClipElements(true);
                    break;
                }
                break;
            }
        }

        void DrawBackground(bool hasLayers)
        {
            // Main bg
            if (DOTweenClipTimeline.stageMode == StageMode.PrefabEditingMode) {
                DeGUI.DrawTiledTexture(area.ShiftYAndResize(layout.visibleTimelineHeight), DeStylePalette.tileBars_slanted_alpha, 1f, _prefabModeBgColor);
            } else DeGUI.DrawTiledTexture(area.ShiftYAndResize(layout.visibleTimelineHeight), DeStylePalette.tileBars_slanted, 1f, _bgColor);
            // Layers bg
            if (hasLayers) {
                for (int i = layout.firstVisibleLayerIndex; i < layout.visibleLayersDrawLoopLength; ++i) {
                    DOTweenClip.ClipLayer layer = clip.layers[i];
                    Rect layerR = TimelineEditorUtils.GetLayerRect(i, area.width);
                    DeGUI.DrawColoredSquare(layerR, i % 2 == 0
                        ? DOTweenClipTimeline.stageMode == StageMode.PrefabEditingMode ? _prefabModeRowColor0 : _rowColor0
                        : DOTweenClipTimeline.stageMode == StageMode.PrefabEditingMode ? _prefabModeRowColor1 : _rowColor1);
                    if (layer.color != DOTweenClip.ClipLayer.DefColor) {
                        DeGUI.DrawColoredSquare(layerR, layer.color.SetAlpha(0.25f));
                    }
                }
            }
        }

        // Overlay pass happens only if elements are selected or if dragging selection
        void DrawLayer(LayerPass pass, int index)
        {
            bool isMainPass = pass == LayerPass.Main;
            bool isOverlayPass = !isMainPass;
            Vector2 mouseP = Event.current.mousePosition;
            DOTweenClip.ClipLayer layer = clip.layers[index];
            Rect layerR = TimelineEditorUtils.GetLayerRect(index, area.width);
            // ClipElement
            int len = layer.clipElementGuids.Length;
            // Buttons pass
            if (isMainPass) {
                _tmpPassRects.Clear();
                for (int i = len - 1; i > -1; --i) {
                    DOTweenClipElement clipElement = clip.FindClipElementByGuid(layer.clipElementGuids[i]);
                    if (clipElement == null) continue;
                    bool visible = clipElement.startTime < layout.lastVisibleTime
                                   && clipElement.startTime + clipElement.Editor_DrawDuration() * clipElement.Editor_PositiveLoopValue() > layout.firstVisibleTime;
                    if (!visible) continue;
                    Rect r = TimelineEditorUtils.GetClipElementRect(clipElement, layerR);
                    _tmpPassRects.Add(r);
                    if (r.Contains(mouseP) && _currMouseOverClipElement == null) _currMouseOverClipElement = clipElement;
                    // Drag-to-resize
                    if (!TimelineSelection.isDraggingDuration) {
                        float dragHalfW = Mathf.Min(r.width * 0.5f, 6);
                        Rect dragDurationR = r.Shift(r.width - dragHalfW, 0, 0, 0).SetWidth(dragHalfW * 2);
                        if (!isPlayingOrPreviewing && dragDurationR.Contains(mouseP) && clipElement.Editor_HasDuration()) {
                            _isPreparingToDragDuration = true;
                            _isPreparingToDragDurationMainTarget = clipElement;
                        }
                    }
                    // Button
                    if (EditorGUI.DropdownButton(r, GUIContent.none, FocusType.Passive, GUIStyle.none)) {
                        GUI.FocusControl(null);
                        if (layer.locked) editor.ShowNotification(new GUIContent(string.Format("Layer \"{0}\" is locked", layer.name)));
                        else {
                            if (DeGUIKey.shift) {
                                if (TimelineSelection.Contains(clipElement)) TimelineSelection.Deselect(clipElement);
                                else TimelineSelection.Select(clipElement, true);
                            } else {
                                TimelineSelection.Select(clipElement, TimelineSelection.Contains(clipElement));
                                if (!isPlayingOrPreviewing) {
                                    DragClipElements(true);
                                    // Move clipElement to back (so it appears above other elements in the same layer)
                                    string[] layerClipElementGuids = clip.layers[TimelineSelection.ClipElements[0].originalLayerIndex].clipElementGuids;
                                    int sIndex = Array.IndexOf(layerClipElementGuids, clipElement.guid);
                                    layerClipElementGuids.Shift(sIndex, layerClipElementGuids.Length - 1);
                                }
                            }
                        }
                    }
                }
            }
            // Graphics pass
            for (int i = 0; i < len; ++i) {
                DOTweenClipElement clipElement = clip.FindClipElementByGuid(layer.clipElementGuids[i]);
                if (clipElement == null) continue;
                bool visible = clipElement.startTime < layout.lastVisibleTime
                               && clipElement.startTime + clipElement.Editor_DrawDuration() * clipElement.Editor_PositiveLoopValue() > layout.firstVisibleTime;
                if (!visible && isMainPass) continue; // Selections are evaluated even if clipElement is not visible, so that multi-selection looks correct
                bool isGlobal = false, isEvent = false, isAction = false, isInterval = false, specialExecuteInEditMode = false;
                bool requiresPlugin = true;
                switch (clipElement.type) {
                case DOTweenClipElement.Type.GlobalTween:
                    isGlobal = true;
                    break;
                case DOTweenClipElement.Type.Event:
                    isEvent = true;
                    requiresPlugin = false;
                    break;
                case DOTweenClipElement.Type.Action:
                    isAction = true;
                    specialExecuteInEditMode = clipElement.executeInEditMode;
                    // isGlobal set later when plugin is loaded
                    break;
                case DOTweenClipElement.Type.Interval:
                    isInterval = true;
                    requiresPlugin = false;
                    break;
                }
                Rect r = TimelineEditorUtils.GetClipElementRect(clipElement, layerR);
                if (isMainPass) {
                    // Main
                    bool isTweener = true;
                    bool skipDuringPreview = false;
                    Color color = TimelineEditorUtils.GetClipElementBaseColor(clipElement);
                    switch (clipElement.type) {
                    case DOTweenClipElement.Type.Event:
                    case DOTweenClipElement.Type.GlobalTween:
                        skipDuringPreview = true;
                        break;
                    case DOTweenClipElement.Type.Action:
                        isTweener = false;
                        skipDuringPreview = !clipElement.executeInEditMode;
                        break;
                    }
                    if (isTweener && clipElement.Editor_HasMultipleLoops()) {
                        // Loops underlay
                        using (new DeGUI.ColorScope(null, null, DOEGUI.Colors.timeline.sTweenerLoop)) {
                            GUI.Box(
                                r.SetWidth(clipElement.Editor_DrawDuration() * clipElement.Editor_PositiveLoopValue() * settings.secondToPixels),
                                GUIContent.none, DOEGUI.Styles.timeline.clipElementLoop
                            );
                        }
                    }
                    // Button graphics
                    if (DOTimelinePreviewManager.isPlayingOrPreviewing && skipDuringPreview) {
                        using (new DeGUI.ColorScope(null, null, color)) {
                            GUI.Box(r, GUIContent.none, DOEGUI.Styles.timeline.btClipElementSkipped);
                        }
                    } else {
                        using (new DeGUI.ColorScope(null, null, color)) {
                            GUI.Box(r, GUIContent.none, DOEGUI.Styles.timeline.btClipElement);
                        }
                        using (new DeGUI.ColorScope(null, null, new Color(1f, 1f, 1f, 0.05f))) {
                            GUI.Box(r, GUIContent.none, DOEGUI.Styles.timeline.btClipElementOutline);
                        }
                    }
                    // Labels
                    Rect lineR = r.SetHeight(Mathf.Min(r.height, 16));
                    Rect icoR = new Rect(0, 0, 16, 16).SetCenter(lineR.x + 9, lineR.center.y);
                    Rect contentR = lineR.ShiftXAndResize(icoR.xMax + 2 - icoR.x);
                    bool hasTarget = clipElement.target != null;
                    DOVisualActionPlugin actionPlugin = null;
                    DOVisualTweenPlugin tweenPlugin = null;
                    bool hasPlugin = false;
                    if (isAction) {
                        actionPlugin = DOVisualPluginsManager.GetActionPlugin(clipElement.plugId);
                        PlugDataAction plugData = actionPlugin == null ? null : actionPlugin.GetPlugData(clipElement);
                        hasPlugin = actionPlugin != null && plugData != null;
                        isGlobal = !hasPlugin || plugData.wantsTarget == false;
                        if (hasPlugin && plugData != null && string.IsNullOrEmpty(clipElement.plugDataGuid) && !string.IsNullOrEmpty(plugData.guid)) {
                            // Legacy fix for plugData being stored by index instead of GUID:
                            // assign plugDataGuid to clipElement if missing
                            if (TimelineSession.logMissingPlugDataGuidAssignment) Debug.Log("Assign plugDataGuid ► " + plugData.guid);
                            clipElement.plugDataGuid = plugData.guid;
                            GUI.changed = true;
                        }
                    } else if (requiresPlugin) {
                        tweenPlugin = isGlobal
                            ? DOVisualPluginsManager.GetGlobalTweenPlugin(clipElement.plugId)
                            : hasTarget ? DOVisualPluginsManager.GetTweenPlugin(clipElement.target) : null;
                        hasPlugin = tweenPlugin != null;
                        if (hasPlugin && string.IsNullOrEmpty(clipElement.plugDataGuid)) {
                            // Legacy fix for plugData being stored by index instead of GUID:
                            // assign plugDataGuid to clipElement if missing
                            ITweenPluginData plugData = tweenPlugin.GetPlugData(clipElement);
                            if (plugData != null && !string.IsNullOrEmpty(plugData.guid)) {
                                if (TimelineSession.logMissingPlugDataGuidAssignment) Debug.Log("Assign plugDataGuid ► " + plugData.guid);
                                clipElement.plugDataGuid = plugData.guid;
                                GUI.changed = true;
                            }
                        }
                    }
                    bool hasMissingRequiredTargetError = hasPlugin && !isEvent && !isInterval && !isGlobal && !hasTarget;
                    bool hasError = requiresPlugin && !hasPlugin || hasMissingRequiredTargetError;
                    if (hasError) {
                        // Missing plugin or missing required target
                        using (new DeGUI.ColorScope(null, null, Color.black)) GUI.Box(r.Contract(2), GUIContent.none, DOEGUI.Styles.box.roundOutline02);
                        using (new DeGUI.ColorScope(null, null, Color.red)) GUI.Box(r, GUIContent.none, DOEGUI.Styles.box.roundOutline02);
                        GUI.DrawTexture(icoR, DeStylePalette.ico_alert, ScaleMode.ScaleToFit);
                    }
                    Rect labelR = contentR.SetHeight(r.height).ShiftY(2).Shift(0, 0, -1, -3);
                    GUIStyle labelStyle = labelR.height > 23 ? DOEGUI.Styles.timeline.clipElementLabelWordWrap : DOEGUI.Styles.timeline.clipElementLabel;
                    if (isInterval) {
                        if (!hasError) labelR = labelR.Shift(-icoR.width + 2, 0, icoR.width - 2, 0);
                        GUI.Label(labelR, "<color=#006666><b>INTERVAL</b></color>", labelStyle);
                    } else if (isEvent) {
                        if (!hasError) labelR = labelR.Shift(-icoR.width + 2, 0, icoR.width - 2, 0);
                        GUI.Label(labelR, "<color=#ffffff><b>EVENT</b></color>", labelStyle);
                    } else if (isGlobal) {
                        if (!hasError) labelR = labelR.Shift(-icoR.width + 2, 0, icoR.width - 2, 0);
                        GUI.Label(labelR,
                            !hasPlugin
                                ? "<color=#fff219><b>unset</b></color>"
                                : string.Format("<b>{0}</b>", isAction
                                    ? actionPlugin.Editor_GetClipElementHeaderLabelGUIContent(clipElement, true).text
                                    : tweenPlugin.Editor_GetAnimationNameGUIContent(clipElement).text
                                ),
                            labelStyle
                        );
                    } else {
                        if (hasTarget) {
                            if (hasPlugin) GUI.DrawTexture(icoR, AssetPreview.GetMiniThumbnail(clipElement.target), ScaleMode.ScaleToFit);
                            GUI.Label(labelR,
                                !hasPlugin
                                    ? string.Format(
                                        "{0}→<color=#fff219><b>{1} not supported</b></color>", clipElement.target.name, TimelineEditorUtils.GetCleanType(clipElement.target.GetType())
                                    )
                                    : string.Format(
                                        "{0}→<b>{1}</b>", clipElement.target.name,
                                        isAction
                                            ? actionPlugin.Editor_GetClipElementHeaderLabelGUIContent(clipElement, true).text
                                            : tweenPlugin.Editor_GetAnimationNameGUIContent(clipElement).text
                                    ),
                                labelStyle
                            );
                        } else if (hasMissingRequiredTargetError) {
                            GUI.Label(labelR,
                                string.Format(
                                    "→<b>{0}</b>",
                                    isAction
                                        ? actionPlugin.Editor_GetClipElementHeaderLabelGUIContent(clipElement, true).text
                                        : tweenPlugin.Editor_GetAnimationNameGUIContent(clipElement).text
                                ),
                                labelStyle
                            );
                        } else {
                            // Target missing (which caused non-action plugin to be set to NULL)
                            // Meaning this is a target tween with no target (and thus we can't determine the type)
                            GUI.Label(labelR, "<color=#fff219><b>missing target</b></color>", labelStyle);
                        }
                    }
                    // Special execute in edit mode icon
                    if (specialExecuteInEditMode) {
                        using (new DeGUI.ColorScope(null, null, Color.green)) {
                            GUI.DrawTexture(icoR.SetWidth(8).SetHeight(8), DeStylePalette.whiteDot_darkBorder);
                        }
                    }
                    // Developer Debug
                    if (TimelineSession.showClipElementPlugDataIndexAndGuid) {
                        labelR = labelR.height > 0 ? lineR.SetY(labelR.yMax - 4).ShiftXAndResize(4) : contentR;
                        GUI.Label(labelR,
                            string.Format("{0}►{1}", clipElement.plugDataIndex, string.IsNullOrEmpty(clipElement.plugDataGuid) ? "no plugDataGuid" : clipElement.plugDataGuid),
                            DOEGUI.Styles.timeline.clipElementLabel);
                    }
                    // Eventual pin
                    if (clipElement.pin > 0) {
                        Rect pinR = new Rect(r.xMax - 14 - 3, r.y + 2, 14, 14);
                        using (new DeGUI.ColorScope(null, null, _pinBorderColor)) {
                            GUI.DrawTexture(pinR.Expand(1), DeStylePalette.circle);
                        }
                        using (new DeGUI.ColorScope(null, null, _pinBgColor)) {
                            GUI.DrawTexture(pinR, DeStylePalette.circle);
                        }
                        GUI.Label(pinR, clipElement.pin.ToString(), DOEGUI.Styles.timeline.clipElementPin);
                    }
                } else {
                    // Selection
                    if (TimelineSelection.isDraggingSelection) {
                        // If dragging selection determine if this clipElement should be selected
                        if (!layer.locked) {
                            if (_dragSelectionR.Overlaps(r)) TimelineSelection.Select(clipElement, true);
                            else TimelineSelection.Deselect(clipElement);
                        }
                    }
                    if (TimelineSelection.Contains(clipElement)) {
                        Rect selectionR = r;
                        _allSelectedR = _allSelectedR.x > 9999 ? selectionR : _allSelectedR.Add(selectionR);
                        using (new DeGUI.ColorScope(null, null, Color.black)) {
                            GUI.Box(selectionR.Contract(1), GUIContent.none, DOEGUI.Styles.timeline.singleSelection);
                            GUI.Box(selectionR.Contract(2), GUIContent.none, DOEGUI.Styles.timeline.singleSelection);
                            GUI.Box(selectionR.Expand(1), GUIContent.none, DOEGUI.Styles.timeline.singleSelection);
                        }
                        using (new DeGUI.ColorScope(null, null, _selectionColor)) {
                            GUI.Box(selectionR, GUIContent.none, DOEGUI.Styles.timeline.singleSelection);
                        }
                    }
                }
            }
            if (isMainPass) {
                // Inactive
                if (!layer.isActive) {
                    // DeGUI.DrawColoredSquare(layerR, _inactiveOverlayColor);
                    DeGUI.DrawTiledTexture(layerR, DeStylePalette.tileBars_empty, 0.25f, _inactiveOverlayColor);
                }
                // Locked
                if (layer.locked) {
                    DeGUI.DrawTiledTexture(layerR, DeStylePalette.tileBars_empty, 0.5f, _lockedBgColor);
                }
                // Color
                if (layer.color != DOTweenClip.ClipLayer.DefColor) {
                    DeGUI.DrawColoredSquare(layerR.ShiftYAndResize(layerR.height - 2), layer.color);
                }
                // Draw eventual overlapping clipElement indicators
                len = _tmpPassRects.Count;
                for (int i = 0; i < len; ++i) {
                    Rect r = _tmpPassRects[i];
                    for (int j = i + 1; j < len; ++j) {
                        if (!r.Intersects(_tmpPassRects[j], out Rect intersectR)) continue;
                        // Overlap
                        using (new DeGUI.ColorScope(null, null, Color.yellow.SetAlpha(0.75f))) {
                            GUI.Box(intersectR.Contract(1), GUIContent.none, DOEGUI.Styles.timeline.btClipElement);
                        }
                    }
                }
                _tmpPassRects.Clear();
            }
        }

        void DrawOverlay()
        {
            if (TimelineSelection.isDraggingSelection) {
                // Selection drag area
                if (_dragSelectionR.width > 1 || _dragSelectionR.height > 1) {
                    using (new DeGUI.ColorScope(null, null, _selectionColor)) {
                        GUI.Box(_dragSelectionR, GUIContent.none, DOEGUI.Styles.timeline.selectionArea);
                    }
                }
            } else if (TimelineSelection.totClipElements > 1) {
                // Multi-selection box
                using (new DeGUI.ColorScope(null, null, _selectionColor)) {
                    GUI.Box(_allSelectedR, GUIContent.none, DOEGUI.Styles.timeline.multiSelection);
                }
            }
            const int overlayH = 16;
            if (TimelineSelection.isDraggingClipElements || TimelineSelection.isDraggingDuration) {
                // Time or duration overlay
                for (int i = 0; i < TimelineSelection.totClipElements; ++i) {
                    DOTweenClipElement clipElement = TimelineSelection.ClipElements[i].clipElement;
                    Rect clipElementR = TimelineEditorUtils.GetClipElementRect(clipElement, area.width);
                    float timeVal;
                    GUIStyle draggedLabelStyle;
                    if (TimelineSelection.isDraggingClipElements) {
                        timeVal = clipElement.startTime;
                        draggedLabelStyle = DOEGUI.Styles.timeline.draggedTimeLabel;
                    } else {
                        timeVal = clipElement.duration;
                        draggedLabelStyle = DOEGUI.Styles.timeline.draggedDurationLabel;
                    }
                    string label = TimelineEditorUtils.ConvertSecondsToTimeString(timeVal, true, true);
                    int w = (int)draggedLabelStyle.CalcSize(new GUIContent(label)).x;
                    Rect timeR = new Rect(clipElementR.x, clipElementR.y, Mathf.Max(w, clipElementR.width), Mathf.Max(overlayH, clipElementR.height));
                    if (TimelineSelection.isDraggingDuration) timeR.x = clipElementR.xMax - timeR.width;
                    using (new DeGUI.ColorScope(TimelineEditorUtils.GetClipElementBaseColor(clipElement).SetAlpha(0.65f))) {
                        GUI.Label(timeR, label, draggedLabelStyle);
                    }
                }
                // Snapping overlay
                if (_snapToClipElementData.isSnappingToClipElement) {
                    using (new DeGUI.ColorScope(_snapToClipElementColor)) {
                        Rect topR, bottomR;
                        if (_snapToClipElementData.selectedClipElementR.yMin < _snapToClipElementData.snapToClipElementR.yMin) {
                            topR = _snapToClipElementData.selectedClipElementR;
                            bottomR = _snapToClipElementData.snapToClipElementR;
                        } else {
                            topR = _snapToClipElementData.snapToClipElementR;
                            bottomR = _snapToClipElementData.selectedClipElementR;
                        }
                        GUI.Box(_snapToClipElementData.snapToClipElementR, GUIContent.none, DOEGUI.Styles.timeline.snapClipElement);
                        Rect lineR = new Rect(
                            (_snapToClipElementData.isSnappingToSelfDuration ? _snapToClipElementData.selectedClipElementR.xMax : _snapToClipElementData.selectedClipElementR.x)
                                + (TimelineSelection.isDraggingClipElements ? 0 : _snapToClipElementData.selectedClipElementR.width) - 1,
                            topR.y,
                            2, Mathf.Abs(topR.y - bottomR.yMax)
                        );
                        DeGUI.DrawColoredSquare(lineR, _snapToClipElementColor);
                    }
                }
            }
        }

        void DrawTimeSeparators()
        {
            float fullWidth = area.width - layout.partialOffset.x;
            int totColumns = Mathf.CeilToInt(fullWidth / settings.secondToPixels);
            int firstColIndex = -timelineShift.x / settings.secondToPixels;
            Rect colR = new Rect(layout.partialOffset.x, area.y, 1, area.height).SetHeight(layout.visibleTimelineHeight);
            for (int i = 0; i < totColumns; ++i) {
                DeGUI.DrawColoredSquare(colR, (firstColIndex + i) % 5 == 0 ? _timeSeparatorColor1 : _timeSeparatorColor0);
                // Fractions separator
                const float fractions = 4;
                Rect fractionR = colR;
                for (int j = 0; j < fractions - 1; ++j) {
                    fractionR = fractionR.Shift(settings.secondToPixels / fractions, 0, 0, 0);
                    DeGUI.DrawColoredSquare(fractionR, _separatorFractionColor);
                }
                //
                colR = colR.Shift(settings.secondToPixels, 0, 0, 0);
            }
        }

        void DragTimeline(bool begin = false)
        {
            if (begin) {
                _isDraggingTimeline = true;
                _dragStartP = Event.current.mousePosition;
                _timelineShiftSnapshot = timelineShift;
                editor.Repaint();
            } else {
                timelineShift = new Vector2Int(
                    Mathf.Min(0, _timelineShiftSnapshot.x + (int)(Event.current.mousePosition.x - _dragStartP.x)),
                    Mathf.Min(
                        0,
                        Mathf.Max(
                            clip.layers == null ? 0 : -clip.layers.Length * settings.layerHeight,
                            _timelineShiftSnapshot.y + (int)(Event.current.mousePosition.y - _dragStartP.y)
                    ))
                );
            }
        }

        void StopDragTimeline()
        {
            _isDraggingTimeline = false;
            editor.Repaint();
        }

        void DragSelection(bool begin = false)
        {
            if (begin) {
                TimelineSelection.isDraggingSelection = true;
                _dragStartP = Event.current.mousePosition;
                _dragSelectionR = new Rect(_dragStartP.x, _dragStartP.y, 0, 0);
                editor.Repaint();
            } else {
                Vector2 mouseP = Event.current.mousePosition;
                if (mouseP.x < 0) mouseP.x = 0;
                else if (mouseP.x > area.width) mouseP.x = area.width;
                if (mouseP.y < 0) mouseP.y = 0;
                else if (mouseP.y > area.height) mouseP.y = area.height;
                if (mouseP.x > _dragStartP.x) _dragSelectionR.x = _dragStartP.x;
                else _dragSelectionR.x = mouseP.x;
                if (mouseP.y > _dragStartP.y) _dragSelectionR.y = _dragStartP.y;
                else _dragSelectionR.y = mouseP.y;
                _dragSelectionR.width = Mathf.Abs(mouseP.x - _dragStartP.x);
                _dragSelectionR.height = Mathf.Abs(mouseP.y - _dragStartP.y);
                editor.Repaint();
            }
        }

        void StopDragSelection()
        {
            TimelineSelection.isDraggingSelection = false;
            editor.Repaint();
        }

        void DragClipElements(bool begin = false)
        {
            _snapToClipElementData.Reset();
            if (begin) {
                _isPreparingToDragClipElements = true;
                _dragStartP = _dragCurrMouseP = Event.current.mousePosition;
                editor.Repaint();
            } else {
                if (_isPreparingToDragClipElements) {
                    if (Vector2.Distance(Event.current.mousePosition, _dragStartP) > settings.minPixelDragDistance) {
                        _isPreparingToDragClipElements = false;
                        TimelineSelection.isDraggingClipElements = true;
                        _draggedMainClipElementCurrLayerIndex = TimelineSelection.ClipElements[0].originalLayerIndex;
                        EditorApplication.update += CheckShiftTimelineContinuous;
                        // Set min max shift for layers
                        int layersLen = clip.layers.Length;
                        for (int i = 0; i < TimelineSelection.ClipElements.Count; ++i) {
                            int layerIndex = TimelineSelection.ClipElements[i].originalLayerIndex;
                            int thisMax = layersLen - layerIndex - 1;
                            int thisMin = -layerIndex;
                            if (i == 0) {
                                _draggedClipElementsMinLayerShift = thisMin;
                                _draggedClipElementsMaxLayerShift = thisMax;
                            } else if (_draggedClipElementsMinLayerShift < thisMin) _draggedClipElementsMinLayerShift = thisMin;
                            else if (_draggedClipElementsMaxLayerShift > thisMax) _draggedClipElementsMaxLayerShift = thisMax;
                        }
                        // Refresh selection cache
                        TimelineSelection.RefreshSelectionsData();
                    }
                }
                if (TimelineSelection.isDraggingClipElements) {
                    _dragCurrMouseP = Event.current.mousePosition;
                    _isSnapDragging = DeGUIKey.ctrl;
                    // Vertical/layer shift
                    int currMouseLayerIndex = layout.GetLayerIndexAtMouse(true);
                    if (currMouseLayerIndex != _draggedMainClipElementCurrLayerIndex) {
                        // Switch layer for all selected
                        int shift = currMouseLayerIndex - _draggedMainClipElementCurrLayerIndex;
                        if (shift > _draggedClipElementsMaxLayerShift) shift = _draggedClipElementsMaxLayerShift;
                        if (shift < _draggedClipElementsMinLayerShift) shift = _draggedClipElementsMinLayerShift;
                        if (shift != 0) {
                            for (int i = 0; i < TimelineSelection.ClipElements.Count; ++i) {
                                TimelineSelection.SelectedClipElement sel = TimelineSelection.ClipElements[i];
                                int shiftTo = sel.originalLayerIndex + shift;
                                TimelineEditorUtils.ShiftClipElementToLayer(clip, sel.clipElement, sel.originalLayerIndex, shiftTo);
                                sel.originalLayerIndex = shiftTo;
                            }
                            _draggedMainClipElementCurrLayerIndex += shift;
                            _draggedClipElementsMinLayerShift -= shift;
                            _draggedClipElementsMaxLayerShift -= shift;
                        }
                    }
                    if (DeGUIKey.shift) {
                        // Lock horizontal shift
                        for (int i = 0; i < TimelineSelection.ClipElements.Count; ++i) {
                            TimelineSelection.ClipElements[i].clipElement.startTime = TimelineSelection.ClipElements[i].originalStartTime;
                        }
                    } else {
                        // Horizontal shift
                        // First find max offset so that no selected tween reaches a startTime below 0
                        float timeOffset = (_dragCurrMouseP.x - _dragStartP.x) / settings.secondToPixels;
                        ShiftDraggedClipElementsTime(timeOffset);
                    }
                    GUI.changed = true;
                }
            }
        }

        void StopDraggingClipElements()
        {
            TimelineSelection.isDraggingClipElements = _isPreparingToDragClipElements = _isSnapDragging = false;
            _snapToClipElementData.Reset();
            TimelineSelection.RefreshSelectionsData();
            EditorApplication.update -= CheckShiftTimelineContinuous;
            editor.Repaint();
        }

        void DragClipElementsDuration(bool begin = false)
        {
            _snapToClipElementData.Reset();
            if (begin) {
                TimelineSelection.isDraggingDuration = true;
                _durationDraggingWaitingForFirstStep = true;
                _dragStartP = _dragCurrMouseP = Event.current.mousePosition;
                if (!TimelineSelection.Contains(_isPreparingToDragDurationMainTarget)) {
                    TimelineSelection.Select(_isPreparingToDragDurationMainTarget);
                }
                TimelineSelection.RefreshSelectionsData();
                editor.Repaint();
            } else {
                if (_durationDraggingWaitingForFirstStep) {
                    if (Vector2.Distance(Event.current.mousePosition, _dragStartP) > settings.minPixelDragDistance) {
                        _durationDraggingWaitingForFirstStep = false;
                    }
                }
                if (!_durationDraggingWaitingForFirstStep) {
                    _dragCurrMouseP = Event.current.mousePosition;
                    _isSnapDragging = DeGUIKey.ctrl;
                    float timeOffset = (_dragCurrMouseP.x - _dragStartP.x) / settings.secondToPixels;
                    // Check eventual snapping with other elements
                    bool isSnappingToClipElement = false;
                    if (_keyAlt) {
                        TimelineSelection.SelectedClipElement mainSel = TimelineSelection.ClipElements[0];
                        float mainCurrTime = mainSel.clipElement.startTime + mainSel.originalDuration;
                        isSnappingToClipElement = CheckSnapToClipElementTimeOffset(mainCurrTime, ref timeOffset, -999999, ref _snapToClipElementData);
                    }
                    for (int i = 0; i < TimelineSelection.totClipElements; ++i) {
                        TimelineSelection.SelectedClipElement sel = TimelineSelection.ClipElements[i];
                        float toDuration = Mathf.Max(0, sel.originalDuration + timeOffset);
                        if (!isSnappingToClipElement && i == 0) {
                            // snap to 0.25 or 0.01 increments
                            float snapValue = _isSnapDragging ? 0.25f : 0.01f;
                            toDuration -= toDuration % snapValue;
                            timeOffset = toDuration - sel.originalDuration;
                        }
                        if (sel.clipElement.Editor_HasDuration()) sel.clipElement.duration = toDuration;
                        if (isSnappingToClipElement) {
                            _snapToClipElementData.selectedClipElementR = TimelineEditorUtils.GetClipElementRect(TimelineSelection.ClipElements[0].clipElement, area.width);
                        }
                    }
                    GUI.changed = true;
                }
            }
        }

        void StopDraggingClipElementsDuration()
        {
            _isPreparingToDragDuration = _durationDraggingWaitingForFirstStep = TimelineSelection.isDraggingDuration = false;
            TimelineSelection.RefreshSelectionsData();
            editor.Repaint();
        }

        void ShiftDraggedClipElementsTime(float timeOffset, bool isTimelineShift = false)
        {
            if (timeOffset < 0.0001f && timeOffset > -0.0001f) return;
            // First find max offset so that no selected tween reaches a startTime below 0
            float minTimeOffset = -999999;
            for (int i = 0; i < TimelineSelection.totClipElements; ++i) {
                TimelineSelection.SelectedClipElement sel = TimelineSelection.ClipElements[i];
                if (-(isTimelineShift ? sel.clipElement.startTime : sel.originalStartTime) > minTimeOffset) {
                    minTimeOffset = -(isTimelineShift ? sel.clipElement.startTime : sel.originalStartTime);
                }
            }
            timeOffset = Mathf.Max(minTimeOffset, timeOffset);
            // Check eventual snapping with other elements
            bool isSnappingToClipElement = false;
            if (_keyAlt) {
                TimelineSelection.SelectedClipElement mainSel = TimelineSelection.ClipElements[0];
                float mainCurrTime = isTimelineShift ? mainSel.clipElement.startTime : mainSel.originalStartTime;
                isSnappingToClipElement = CheckSnapToClipElementTimeOffset(mainCurrTime, ref timeOffset, minTimeOffset, ref _snapToClipElementData);
                if (!isSnappingToClipElement) {
                    // Check clipElement end position (start time + duration) VS others
                    isSnappingToClipElement = CheckSnapToClipElementTimeOffset(mainCurrTime + mainSel.clipElement.duration, ref timeOffset, minTimeOffset, ref _snapToClipElementData);
                    if (isSnappingToClipElement) _snapToClipElementData.isSnappingToSelfDuration = true;
                };
            }
            //
            for (int i = 0; i < TimelineSelection.totClipElements; ++i) {
                TimelineSelection.SelectedClipElement sel = TimelineSelection.ClipElements[i];
                float toTime = (isTimelineShift ? sel.clipElement.startTime : sel.originalStartTime) + timeOffset;
                if (!isSnappingToClipElement && i == 0) { // snap to 0.25 or 0.01 increments (unless we're snapping to another clipElement)
                    float snapValue = _isSnapDragging ? 0.25f : DOTimelineEditorSettings.MinClipElementSnapping;
                    toTime -= toTime % snapValue;
                    timeOffset = toTime - (isTimelineShift ? sel.clipElement.startTime : sel.originalStartTime);
                }
                sel.clipElement.startTime = toTime;
                if (isSnappingToClipElement) {
                    _snapToClipElementData.selectedClipElementR = TimelineEditorUtils.GetClipElementRect(TimelineSelection.ClipElements[0].clipElement, area.width);
                }
            }
        }

        void CheckShiftTimelineContinuous()
        {
            const int borderX = 100;
            const float maxShift = 7;
            Vector2Int prevTimelineShift = timelineShift;
            float borderRight = area.xMax - borderX;
            if (_dragCurrMouseP.x < borderX) {
                int power = (int)(maxShift * (borderX - _dragCurrMouseP.x) / borderX);
                timelineShift = new Vector2Int((int)Mathf.Min(0, timelineShift.x + power), timelineShift.y);
            } else if (_dragCurrMouseP.x > borderRight) {
                int power = (int)(maxShift * (_dragCurrMouseP.x - borderRight) / borderX);
                timelineShift = new Vector2Int(timelineShift.x - power, timelineShift.y);
            }
            if (prevTimelineShift != timelineShift) {
                Vector2 diff = prevTimelineShift - timelineShift;
                _dragStartP.x -= diff.x;
                float timeOffset = diff.x / settings.secondToPixels;
                ShiftDraggedClipElementsTime(timeOffset, true);
                GUI.changed = true;
                editor.Repaint();
            }
        }

        bool IsValidDragAndDrop(UnityEngine.Object[] allDragged)
        {
            if (allDragged.Length != 1) return false;
            if (layout.GetLayerIndexAtMouse() == -1) return false;
            UnityEngine.Object dragged = allDragged[0];
            if (dragged is GameObject) return ((GameObject)dragged).scene.rootCount != 0; // Ignore prefabs
            if (dragged is Component) return ((Component)dragged).gameObject.scene.rootCount != 0; // Ignore prefabs
            return false;
        }

        void CompleteDragAndDrop(Object dragged)
        {
            int layerIndex = layout.GetLayerIndexAtMouse();
            float atTime = layout.GetSecondsAtMouse();
            TimelineEditorUtils.CM_SelectClipElementTargetFromGameObject(
                dragged is GameObject ? (GameObject)dragged : ((Component)dragged).gameObject,
                (component) => {
                    using (new DOScope.UndoableSerialization()) {
                        DOTweenClipElement clipElement = new DOTweenClipElement(Guid.NewGuid().ToString(), DOTweenClipElement.Type.Tween, atTime) {
                            target = component
                        };
                        AddClipElement(clipElement, clip.layers[layerIndex]);
                    }
                    DOTweenClipTimeline.Dispatch_OnClipChanged(clip);
                }
            );
        }

        #region Context Menus

        void CM_EmptyArea()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(string.Format("Reset shift (current: {0},{1})", timelineShift.x, timelineShift.y)), false,
                () => {
                    using (new DOScope.UndoableSerialization()) timelineShift = new Vector2Int(0, 0);
                });
            if (!isPlayingOrPreviewing) {
                int layerIndex = layout.GetLayerIndexAtMouse();
                float timePosition = layout.GetSecondsAtMouse();
                menu.AddSeparator("");
                int totClipElementsInClipboard = TimelineClipboard.TotMemorizedClipElements();
                if (totClipElementsInClipboard > 0) {
                    menu.AddItem(
                        new GUIContent(string.Format("Paste {0} Clip{1} [Ctrl+V]", totClipElementsInClipboard, totClipElementsInClipboard > 1 ? "s" : "")),
                        false, () => PasteClipElementsFromClipboard(timePosition, layerIndex)
                    );
                    menu.AddItem(
                        new GUIContent(string.Format("Paste {0} Clip{1} At Original Time [Ctrl+Shift+V]", totClipElementsInClipboard, totClipElementsInClipboard > 1 ? "s" : "")),
                        false, () => PasteClipElementsFromClipboard(timePosition, layerIndex, true)
                    );
                } else menu.AddDisabledItem(new GUIContent("Paste [Ctrl+V]"));
                menu.AddSeparator("");
                if (layerIndex != -1) {
                    if (clip.layers[layerIndex].locked) {
                        menu.AddDisabledItem(new GUIContent("[Layer is locked]"));
                    } else {
                        menu.AddItem(new GUIContent("Add Tween"), false, () => {
                            AddClipElement(new DOTweenClipElement(
                                    Guid.NewGuid().ToString(), DOTweenClipElement.Type.Tween, timePosition), clip.layers[layerIndex]
                            );
                        });
                        menu.AddItem(new GUIContent("Add Global Tween"), false, () => {
                            AddClipElement(new DOTweenClipElement(
                                    Guid.NewGuid().ToString(), DOTweenClipElement.Type.GlobalTween, timePosition), clip.layers[layerIndex]
                            );
                        });
                        menu.AddItem(new GUIContent("Add Event"), false, () => {
                            AddClipElement(new DOTweenClipElement(
                                    Guid.NewGuid().ToString(), DOTweenClipElement.Type.Event, timePosition), clip.layers[layerIndex]
                            );
                        });
                        menu.AddItem(new GUIContent("Add Action"), false, () => {
                            AddClipElement(new DOTweenClipElement(
                                    Guid.NewGuid().ToString(), DOTweenClipElement.Type.Action, timePosition), clip.layers[layerIndex]
                            );
                        });
                        menu.AddItem(new GUIContent("Add Interval"), false, () => {
                            AddClipElement(new DOTweenClipElement(
                                    Guid.NewGuid().ToString(), DOTweenClipElement.Type.Interval, timePosition), clip.layers[layerIndex]
                            );
                        });
                    }
                }
            }
            menu.ShowAsContext();
            editor.Repaint();
        }

        void CM_ClipElement()
        {
            if (isPlayingOrPreviewing) return;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Cut [Ctrl+X]"), false, ()=> CopySelectedClipElements(true));
            menu.AddItem(new GUIContent("Copy [Ctrl+C]"), false, ()=> CopySelectedClipElements(false));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Remove Pin"), false, () => {
                using (new DOScope.UndoableSerialization()) {
                    for (int i = 0; i < TimelineSelection.totClipElements; ++i) TimelineSelection.ClipElements[i].clipElement.pin = -1;
                }
            });
            for (int i = 1; i < 21; ++i) {
                int pinVal = i;
                menu.AddItem(
                    new GUIContent("Pin/" + pinVal), TimelineSelection.totClipElements == 1 && TimelineSelection.ClipElements[0].clipElement.pin == pinVal,
                    () => {
                        using (new DOScope.UndoableSerialization()) {
                            for (int j = 0; j < TimelineSelection.totClipElements; ++j) TimelineSelection.ClipElements[j].clipElement.pin = pinVal;
                        }
                    }
                );
            }
            menu.ShowAsContext();
            editor.Repaint();
        }

        #endregion

        #endregion

        #region Methods

        void AddClipElement(DOTweenClipElement clipElement, DOTweenClip.ClipLayer layer, bool isCopy = false)
        {
            using (new DOScope.UndoableSerialization()) {
                DeEditorUtils.Array.ExpandAndAdd(ref layer.clipElementGuids, clipElement.guid);
                DeEditorUtils.Array.ExpandAndAdd(ref clip.elements, clipElement);
                if (!isCopy) {
                    ClipElementEditor.showPlugTypeDropdown = true;
                    clipElement.isActive = layer.isActive;
                    switch (clipElement.type) {
                    case DOTweenClipElement.Type.Action:
                    case DOTweenClipElement.Type.Event:
                        clipElement.duration = 0;
                        break;
                    case DOTweenClipElement.Type.Tween:
                        clipElement.duration = settings.defaults.duration;
                        DOVisualTweenPlugin plug = DOVisualPluginsManager.GetTweenPlugin(clipElement.target);
                        if (plug != null) {
                            ITweenPluginData plugData = plug.GetPlugData(clipElement);
                            if (plugData != null) clipElement.plugDataGuid = plugData.guid;
                        }
                        break;
                    default:
                        clipElement.duration = settings.defaults.duration;
                        break;
                    }
                    clipElement.ease = settings.defaults.ease;
                    clipElement.loopType = settings.defaults.loopType;
                }
            }
            using (new DOScope.UndoableSerialization()) TimelineSelection.Select(clipElement, isCopy);
        }

        void CopySelectedClipElements(bool cut)
        {
            TimelineClipboard.CopyClipElements(clip, TimelineSelection.GetCleanSelectedClipElements());
            if (cut) {
                using (new DOScope.UndoableSerialization()) {
                    for (int i = 0; i < TimelineSelection.ClipElements.Count; ++i) {
                        TimelineEditorUtils.RemoveClipElement(clip, TimelineSelection.ClipElements[i].clipElement.guid);
                    }
                    TimelineSelection.DeselectAll();
                }
            }
        }

        void PasteClipElementsFromClipboard(float timePosition, int layerIndex, bool pasteAtOriginalTimePosition = false)
        {
            if (!TimelineClipboard.HasMemorizedClipElements()) return;
            using (new DOScope.UndoableSerialization()) {
                // First determine if new layers need to be added
                int highestLayerIndex = 0;
                foreach (TimelineClipboard.ClipElementCopy sCopy in TimelineClipboard.ClipElementCopies) {
                    int sLayerIndex = layerIndex + sCopy.layerIndexOffsetFromUpper;
                    if (sLayerIndex > highestLayerIndex) highestLayerIndex = sLayerIndex;
                }
                if (highestLayerIndex > clip.layers.Length - 1) {
                    int totLayersToAdd = highestLayerIndex - (clip.layers.Length - 1);
                    bool proceed = EditorUtility.DisplayDialog("Paste Clips",
                        string.Format(
                            "{0} layer{1} need to be added in order to paste the clips correctly.\n\nProceed?",
                            totLayersToAdd, totLayersToAdd > 1 ? "s" : ""
                        ),
                        "Ok", "Cancel"
                    );
                    if (!proceed) return;
                    for (int i = 0; i < totLayersToAdd; ++i) {
                        DeEditorUtils.Array.ExpandAndAdd(ref clip.layers, new DOTweenClip.ClipLayer("Layer " + (clip.layers.Length + 1)));
                    }
                }
                TimelineSelection.DeselectAll();
                foreach (TimelineClipboard.ClipElementCopy sCopy in TimelineClipboard.ClipElementCopies) {
                    int sLayerIndex = layerIndex + sCopy.layerIndexOffsetFromUpper;
                    DOTweenClipElement sClone = sCopy.clipElement.Clone(true);
                    if (!pasteAtOriginalTimePosition) sClone.startTime = timePosition + sCopy.startTimeOffsetFromFirst;
                    AddClipElement(sClone, clip.layers[sLayerIndex], true);
                }
            }
            editor.Repaint();
        }

        // Returns TRUE if we should snap to another clipElement
        // Out Rects are filled correctly only if snapping is set
        bool CheckSnapToClipElementTimeOffset(
            float mainSelectedTargetTime, ref float dragTimeOffset, float minDragTimeOffset, ref SnapToClipElementData snapToClipElementData
        ){
            snapToClipElementData.Reset();
            float shortestSnapDiff = 99999999;
            float currSnapTimePos = 0;
            float mainSelectedTargetTimeWOffset = mainSelectedTargetTime + dragTimeOffset;
            DOTweenClipElement snapToClipElement = null;
            for (int i = 0; i < clip.elements.Length; ++i) { // Skip first, obviously
                DOTweenClipElement clipElement = clip.elements[i];
                if (TimelineSelection.Contains(clipElement)) continue;
                // Check VS start time
                float time = clipElement.startTime;
                float timeDiff = Mathf.Abs(time - mainSelectedTargetTimeWOffset);
                if (timeDiff < shortestSnapDiff) {
                    shortestSnapDiff = timeDiff;
                    currSnapTimePos = time;
                    snapToClipElement = clipElement;
                }
                // Check VS duration
                time = clipElement.startTime + clipElement.duration;
                timeDiff = Mathf.Abs(time - mainSelectedTargetTimeWOffset);
                if (timeDiff < shortestSnapDiff) {
                    shortestSnapDiff = timeDiff;
                    currSnapTimePos = time;
                    snapToClipElement = clipElement;
                }
            }
            if (shortestSnapDiff * settings.secondToPixels <= settings.maxSnapPixelDistance) {
                dragTimeOffset = Mathf.Max(minDragTimeOffset, currSnapTimePos - mainSelectedTargetTime);
                snapToClipElementData.isSnappingToClipElement = true;
                snapToClipElementData.snapToClipElementR = TimelineEditorUtils.GetClipElementRect(snapToClipElement, area.width);
                return true;
            }
            return false;
        }

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        struct SnapToClipElementData
        {
            public bool isSnappingToClipElement;
            public bool isSnappingToSelfDuration; // TRUE if snapping to the selected clipElement's end time (start time + duration)
            public Rect selectedClipElementR;
            public Rect snapToClipElementR;

            public void Reset()
            {
                isSnappingToClipElement = isSnappingToSelfDuration = false;
                selectedClipElementR = snapToClipElementR = new Rect();
            }
        }
    }
}