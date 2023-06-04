// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/17

using System;
using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal class TimelineLayers : ABSTimelineElement
    {
        public const int MinSize = 126;
        readonly Color _bgColor = new DeSkinColor(0.15f);
        readonly Color _lockedColor = new Color(0.99f, 0.76f, 0.02f);
        readonly Color _unlockedColor = new DeSkinColor(0.5f);
        readonly Color _activeColor = new Color(0.09f, 0.85f, 0.56f);
        readonly Color _inactiveColor = new Color(0.9f, 0.23f, 0.31f);
        readonly Color _nameBgColor = new DeSkinColor(0.3f);
        readonly Color _optionsDropdownColor = new DeSkinColor(0.5f);
        GUIContent _icoOptionsDropdown;
        string _lockedTooltip = "Locks/unlocks editing for this layer, has no effect at runtime";
        GUIContent _lockedContent, _unlockedContent;
        string _activeTooltip = "Activates/deactivates the layer. A deactivated layer will not create any tween/event at runtime";
        GUIContent _activeContent, _inactiveContent;

        #region GUI

        public void Refresh()
        {
            _icoOptionsDropdown = new GUIContent(DeStylePalette.ico_optionsDropdown);
            _lockedContent = new GUIContent(DeStylePalette.ico_lock, _lockedTooltip);
            _unlockedContent = new GUIContent(DeStylePalette.ico_lock_open, _lockedTooltip);
            _activeContent = new GUIContent(DeStylePalette.ico_visibility, _activeTooltip);
            _inactiveContent = new GUIContent(DeStylePalette.ico_visibility_off, _activeTooltip);
        }

        public override void Draw(Rect drawArea)
        {
            if (Event.current.type == EventType.Layout || drawArea.width < 50 || isRecorderStoppedPass) return;

            base.Draw(drawArea);

            // Background
            DeGUI.DrawColoredSquare(area, _bgColor);
            // Layers
            if (clip.layers != null) {
                for (int i = layout.firstVisibleLayerIndex; i < layout.visibleLayersDrawLoopLength; ++i) DrawLayer(i);
            }
        }

        void DrawLayer(int index)
        {
            Rect layerR = new Rect(
                0, layout.partialOffset.y + settings.layerHeight * (index - layout.firstVisibleLayerIndex), area.width, settings.layerHeight
            );
            Rect contentR = layerR.Contract(1);
            Rect colorR = layerR.SetWidth(20).Shift(0, 1, 0, -2);
            float contentRCenterY = contentR.center.y;
            Rect optionsSize = DeStylePalette.ico_optionsDropdown.GetRect().Fit(DeStylePalette.ico_optionsDropdown.width, contentR.height);
            Rect lockSize = DeStylePalette.ico_lock.GetRect().Fit(DeStylePalette.ico_lock.width, contentR.height);
            Rect activeSize = DeStylePalette.ico_visibility.GetRect().Fit(DeStylePalette.ico_visibility.width, contentR.height);
            Rect btOptionsR = new Rect(contentR.xMax - optionsSize.width, 0, optionsSize.width, optionsSize.height)
                .SetCenterY(contentRCenterY);
            Rect btLockR = new Rect(btOptionsR.x - lockSize.width - 2, 0, lockSize.width, lockSize.height)
                .SetCenterY(contentRCenterY);
            Rect btActiveR = new Rect(btLockR.x - activeSize.width - 2, 0, activeSize.width, activeSize.height)
                .SetCenterY(contentRCenterY);
            Rect nameR = contentR.ShiftXAndResize(colorR.width).Shift(0, 0, -(contentR.width - btActiveR.x + 2), 0).SetHeight(16)
                .SetCenterY(contentRCenterY);

            clip.layers[index].color = EditorGUI.ColorField(colorR, GUIContent.none, clip.layers[index].color, false, false, false);
            using (new DeGUI.ColorScope(null, DOEGUI.GetVisibleContentColorOn(clip.layers[index].color))) {
                GUI.Label(colorR, clip.layers[index].clipElementGuids.Length.ToString(), DOEGUI.Styles.timeline.layerTotClipElementsLabel);
            }
            using (new DeGUI.ColorScope(_nameBgColor)) {
                clip.layers[index].name = DeGUI.DoubleClickDraggableTextField(
                    nameR, editor, "layerName" + index, clip.layers[index].name, clip.layers, index,
                    DOEGUI.Styles.timeline.layerNameField, DOEGUI.Styles.timeline.layerNameFieldSelected
                );
            }
            using (new DeGUI.ColorScope(null, null, _optionsDropdownColor)) {
                if (EditorGUI.DropdownButton(btOptionsR, _icoOptionsDropdown, FocusType.Passive, DOEGUI.Styles.timeline.layerIcoToggle)) {
                    CM_Dropdown(index);
                }
            }
            bool locked = clip.layers[index].locked;
            bool active = clip.layers[index].isActive;
            using (new DeGUI.ColorScope(null, null, locked ? _lockedColor : _unlockedColor)) {
                if (GUI.Button(btLockR, locked ? _lockedContent : _unlockedContent, DOEGUI.Styles.timeline.layerIcoToggle)) {
                    clip.layers[index].locked = !locked;
                    DOTweenClip.ClipLayer layer = clip.layers[index];
                    for (int i = 0; i < layer.clipElementGuids.Length; ++i) {
                        TimelineSelection.Deselect(clip.FindClipElementByGuid(layer.clipElementGuids[i]));
                    }
                    GUI.changed = true;
                }
            }
            using (new DeGUI.ColorScope(null, null, active ? _activeColor : _inactiveColor)) {
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    if (GUI.Button(btActiveR, active ? _activeContent : _inactiveContent, DOEGUI.Styles.timeline.layerIcoToggle)) {
                        clip.layers[index].isActive = !active;
                        GUI.changed = true;
                    }
                    if (check.changed) {
                        // Set active value of all tweens in layer
                        var layer = clip.layers[index];
                        for (int i = 0; i < layer.clipElementGuids.Length; ++i) {
                            clip.FindClipElementByGuid(layer.clipElementGuids[i]).isActive = layer.isActive;
                        }
                    }
                }
            }
            if (DeGUIDrag.Drag(clip.layers, index, layerR).outcome == DeDragResultType.Accepted) GUI.changed = true;
        }

        #region Context Menus

        void CM_Dropdown(int layerIndex)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset Color"), false, () => {
                using (new DOScope.UndoableSerialization()) clip.layers[layerIndex].color = DOTweenClip.ClipLayer.DefColor;
            });
            menu.AddSeparator("");
            if (clip.layers.Length <= 1) menu.AddDisabledItem(new GUIContent("Delete Layer"));
            else {
                menu.AddItem(new GUIContent("Delete Layer"), false, () => {
                    using (new DOScope.UndoableSerialization()) {
                        TimelineEditorUtils.RemoveLayer(clip, layerIndex);
                        TimelineSelection.DeselectAll();
                    }
                });
            }
            menu.ShowAsContext();
        }

        #endregion

        #endregion
    }
}