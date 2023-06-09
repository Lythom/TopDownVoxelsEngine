﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/17

using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal class TimelineGlobalHeader : ABSTimelineElement
    {
        static readonly Color _GoNormalColor = new Color(0.49f, 0.49f, 0.65f);
        static readonly Color _ComponentNormalColor = new Color(0.04f, 0.5f, 0.73f);
        static readonly Color _ClipNormalColor = new Color(0.11f, 0.74f, 0.42f);
        static readonly Color _TimeBorderColor = new Color(0.52f, 0.52f, 0.52f);
        static readonly Color _TimeModdedBorderColor = new Color(0.96f, 0.67f, 0.03f);
        static readonly Color _CloseButtonColor = new DeSkinColor(1f);
        static readonly GUIContent _GcMidCrumb = new GUIContent("►");
        static readonly GUIContent _GcTimeScale = new GUIContent("×", "Clip's TimeScale");
        static readonly GUIContent _GcDurationOverload = new GUIContent("×", "Clip's Duration Overload (internal timeScale will be adjusted to achieve this total duration)");
        static readonly GUIContent _GcHelp = new GUIContent("?", "Help");
        static readonly GUIContent _GcClose = new GUIContent("×", "Close DOTweenClip");
        static GUIContent _gcSettings;
        bool _initialized;
        GUIContent _gcGo, _gcComponent, _gcClip;

        void Init()
        {
            if (_initialized) return;

            _initialized = true;

            _gcSettings = new GUIContent(DeStylePalette.ico_cog, "Settings");
        }

        #region GUI

        public void Refresh()
        {
            _gcGo = src == null ? null : new GUIContent(src.name);
            _gcComponent = src == null ? null : new GUIContent(TimelineEditorUtils.GetCleanType(src.GetType()));
            const string dropdownAdd = " ▾";
            _gcClip = clip == null ? null : new GUIContent(string.IsNullOrEmpty(clip.name)
                ? string.Format("[clip{0}]", dropdownAdd) : clip.name + dropdownAdd);
        }

        public override void Draw(Rect drawArea)
        {
            if (Event.current.type == EventType.Layout) return;

            Init();
            bool isRecordingOrPreviewing = DOTimelineRecorder.isRecording || DOTimelinePreviewManager.isPlayingOrPreviewing;

            base.Draw(drawArea);
            GUIContent gcTime;
            bool timeModded;
            switch (clip.timeMode) {
            case TimeMode.DurationOverload:
                gcTime = _GcDurationOverload;
                gcTime.text = clip.durationOverload.ToString("0.##");
                timeModded = true;
                break;
            default:
                gcTime = _GcTimeScale;
                gcTime.text = "×" + clip.timeScale.ToString("0.##");
                timeModded = !Mathf.Approximately(clip.timeScale, 1);
                break;
            }

            Rect contentR = area.Contract(1);
            Rect goR = contentR.SetWidth(DOEGUI.Styles.timeline.gHeaderCrumb.CalcSize(_gcGo).x);
            Rect div0R = goR.SetX(goR.xMax).SetWidth(10);
            Rect componentR = contentR.SetX(div0R.xMax - 2).SetWidth(DOEGUI.Styles.timeline.gHeaderCrumb.CalcSize(_gcComponent).x);
            Rect div1R = componentR.SetX(componentR.xMax).SetWidth(div0R.width);
            Rect clipR = contentR.SetX(div1R.xMax - 2).SetWidth(DOEGUI.Styles.timeline.gHeaderCrumbClip.CalcSize(_gcClip).x);
            Rect timeR = contentR.SetX(clipR.xMax + 2).SetWidth(DOEGUI.Styles.timeline.gHeaderTimeScaleLabel.CalcSize(gcTime).x);
            Rect closeR = area.SetX(area.xMax - 18).SetWidth(18);
            Rect helpR = closeR.ShiftX(-closeR.width);
            Rect settingsR = helpR.ShiftX(-closeR.width);
            Rect optionsR = area.ShiftXAndResize(timeR.xMax + 2).Shift(0, 0, -(closeR.xMax - settingsR.x) - 2, 0);
            if (optionsR.width > 300) {
                optionsR.width = 300;
                optionsR.x = area.xMax - optionsR.width - (closeR.xMax - settingsR.x) - 2;
            }
            Rect sliderHScaleR = new Rect(optionsR.x, optionsR.y, (int)(optionsR.width * 0.5f) - 4, optionsR.height);
            Rect sliderVScaleR = new Rect(sliderHScaleR.xMax + 8, sliderHScaleR.y, sliderHScaleR.width, sliderHScaleR.height);

            // Background
            DeGUI.DrawColoredSquare(area, new DeSkinColor(0.6f, 0.15f));
            // Ping Component/Clip + select clip dropdown
            if (DeGUI.ActiveButton(goR, _gcGo, _GoNormalColor, DOEGUI.Styles.timeline.gHeaderCrumb)) {
                Selection.activeGameObject = src.gameObject;
                EditorGUIUtility.PingObject(src);
            }
            GUI.DrawTexture(goR.SetWidth(goR.height).Contract(1), AssetPreview.GetMiniThumbnail(src.gameObject), ScaleMode.ScaleToFit);
            using (new DeGUI.ColorScope(null, _GoNormalColor)) GUI.Label(div0R, _GcMidCrumb, DOEGUI.Styles.timeline.gHeaderCrumbsMidLabel);
            if (DeGUI.ActiveButton(componentR,
                _gcComponent, _ComponentNormalColor, DOEGUI.Styles.timeline.gHeaderCrumb)
            ) {
                Selection.activeGameObject = src.gameObject;
                EditorGUIUtility.PingObject(src);
            }
            GUI.DrawTexture(componentR.SetWidth(componentR.height).Contract(1), AssetPreview.GetMiniThumbnail(src), ScaleMode.ScaleToFit);
            using (new DeGUI.ColorScope(null, _ComponentNormalColor)) GUI.Label(div1R, _GcMidCrumb, DOEGUI.Styles.timeline.gHeaderCrumbsMidLabel);
            using (new EditorGUI.DisabledScope(isRecordingOrPreviewing)) {
                if (DeGUI.ActiveButton(clipR,
                    _gcClip, _ClipNormalColor, DOEGUI.Styles.timeline.gHeaderCrumbClip)
                ) {
                    if (Event.current.button == 1) {
                        Selection.activeGameObject = src.gameObject;
                        EditorGUIUtility.PingObject(src);
                    } else {
                        TimelineEditorUtils.CM_SelectClipInScene(clipR, DOTweenClipTimeline.clip);
                    }
                }
                GUI.Label(clipR.SetWidth(clipR.height).Contract(1), "►", DOEGUI.Styles.timeline.gHeaderClipLabel);
            }
            using (new DeGUI.ColorScope(null, null, timeModded ? _TimeModdedBorderColor : _TimeBorderColor)) {
                GUI.Label(timeR, gcTime, DOEGUI.Styles.timeline.gHeaderTimeScaleLabel);
            }
            // Scale H/V
            using (new DeGUI.LabelFieldWidthScope(14, 0.001f)) {
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    int value = EditorGUI.IntSlider(
                        sliderHScaleR, "H", settings.secondToPixels, DOTimelineEditorSettings.MinSecondToPixels, DOTimelineEditorSettings.MaxSecondToPixels
                    );
                    if (check.changed) TimelineEditorUtils.UpdateSecondToPixels(value);
                }
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    int value = EditorGUI.IntSlider(
                        sliderVScaleR, "V", settings.layerHeight, DOTimelineEditorSettings.MinLayerHeight, DOTimelineEditorSettings.MaxLayerHeight
                    );
                    if (check.changed) TimelineEditorUtils.UpdateLayerHeight(value);
                }
            }
            // Settings + Help buttons
            if (DeGUI.ActiveButton(settingsR, _gcSettings, _CloseButtonColor, DOEGUI.Styles.button.tool)) {
                DOTweenClipTimeline.ShowSettingsPanel();
            }
            if (DeGUI.ActiveButton(helpR, _GcHelp, _CloseButtonColor, DOEGUI.Styles.button.tool)) {
                DOTweenClipTimeline.ShowHelpPanel();
            }
            // Close button
            if (DeGUI.ActiveButton(closeR, _GcClose, _CloseButtonColor, DOEGUI.Styles.button.tool)) {
                DOTweenClipTimeline.CloseCurrentClip();
            }
        }

        #endregion
    }
}