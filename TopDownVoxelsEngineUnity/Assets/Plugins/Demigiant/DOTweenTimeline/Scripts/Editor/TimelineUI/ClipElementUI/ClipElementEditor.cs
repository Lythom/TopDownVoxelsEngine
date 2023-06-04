// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/20

using System;
using System.Collections.Generic;
using DG.DemiEditor;
using DG.DemiLib;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using DG.Tweening.Timeline.Core.Plugins;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace DG.Tweening.TimelineEditor.ClipElementUI
{
    internal class ClipElementEditor : ABSTimelineElement
    {
        public const int DefaultWidth = 250;
        public static bool showPlugTypeDropdown; // Set to TRUE when adding a new clipElement to the Timeline, so that we can immediately choose the plugType

        readonly GUIContent _gcDropdown = new GUIContent(" ▾");
        readonly GUIContent _gcEvent = new GUIContent("Events");
        readonly GUIContent _gcExecuteInEditMode = new GUIContent("Execute In Preview Mode",
            "If toggled executes this action even in preview mode." +
            "\nWarning: this can be risky if your action changes elements that are not in the Scene," +
            " because that won't be able to be undone when you exit the preview. Use it with care.");
        readonly GUIContent _gcStartTime = new GUIContent("At");
        readonly GUIContent _gcDuration = new GUIContent("Duration");
        readonly GUIContent _gcLoops = new GUIContent("Loops");
        readonly GUIContent _gcTarget = new GUIContent("Target");
        readonly GUIContent _gcEase = new GUIContent("Ease");
        readonly GUIContent _gcOvershoot = new GUIContent("Ovrsht");
        readonly GUIContent _gcAmplitude = new GUIContent("Ampl");
        readonly GUIContent _gcPeriod = new GUIContent("Period");
        readonly GUIContent _gcSnapping = new GUIContent("Snapping");
        readonly GUIContent _gcRichTextEnabled = new GUIContent("Rich-text");
        readonly GUIContent _gcScramble = new GUIContent("Scramble");
        readonly GUIContent _gcScrambleOptions = new GUIContent("Scramble Options");
        readonly GUIContent _gcIsRelative = new GUIContent("Relative");
        readonly GUIContent _gcIsNotRelative = new GUIContent("Absolute");
        readonly GUIContent _gcAlphaOnly = new GUIContent("Fade");
        readonly GUIContent _gcVibrato = new GUIContent("Vibrato");
        readonly GUIContent _gcElasticity = new GUIContent("Elasticity");
        readonly GUIContent _gcRandomness = new GUIContent("Rnd");
        readonly GUIContent _gcFadeOut = new GUIContent("Fade Out");
        readonly GUIContent _gcNumber = new GUIContent("N");
        readonly GUIContent _gcAngle = new GUIContent("Angle", "0° is top and positive values increase clockwise");
        GUIContent _gcVectorLocked, _gcVectorUnlocked;
        readonly Color _isRelativeColor = new Color(1f, 0.6f, 0f);
        Color _hDividerColor;
        const int _Padding = 2;
        float _lineHeight { get { return EditorGUIUtility.singleLineHeight; } }
        bool _initialized;
        readonly List<DOTweenClipElement> _clipElements = new List<DOTweenClipElement>();
        readonly List<DOVisualTweenPlugin> _tweenPlugins = new List<DOVisualTweenPlugin>();
        readonly List<DOVisualActionPlugin> _actionPlugins = new List<DOVisualActionPlugin>();
        readonly List<SerializedProperty> _spClipElements = new List<SerializedProperty>();
        readonly List<SerializedProperty> _spOnCompletes = new List<SerializedProperty>();
        readonly List<SerializedProperty> _spOnStepCompletes = new List<SerializedProperty>();
        readonly List<SerializedProperty> _spOnUpdates = new List<SerializedProperty>();
        DOTweenClipElement _mainClipElement { get { return _clipElements[0]; } }
        DOVisualTweenPlugin _mainTweenPlugin{ get { return _tweenPlugins.Count == 0 ? null : _tweenPlugins[0]; } }
        DOVisualActionPlugin _mainActionPlugin{ get { return _actionPlugins[0]; } }
        SerializedProperty _mainSpClipElement { get { return _spClipElements[0]; } }
        SerializedProperty _mainSpOnComplete { get { return _spOnCompletes[0]; } }
        SerializedProperty _mainSpOnStepComplete { get { return _spOnStepCompletes[0]; } }
        SerializedProperty _mainSpOnUpdates { get { return _spOnUpdates[0]; } }
        bool _forceShowOnCompletes, _forceShowOnStepCompletes, _forceShowOnUpdates;
        bool _allTargetsSet, _allTargetsTheSame; // True even in case of global tweens/actions
        bool _allPluginsSet, _allPluginsOfSameType, _allPluginsOfSameTypeAndSubtype;
        int _totClipElements;
        bool _isTweener, _isEvent, _isAction, _isInterval, _isGlobal;
        float _minLabelWidth = 46;
        Color _mainColor;
        AxisConstraint _lockAllAxesTo = AxisConstraint.None;
        AxisConstraint _disabledAxes = AxisConstraint.None;
        DeScrollView _scrollView;
        EaseSelectionWindow _easeSelectionWin = new EaseSelectionWindow();
        readonly List<Object> _currTargets = new List<Object>();
        Texture2D _easePreviewTex;
        EaseSnapshot _lastPreviewedEase;
        readonly List<SortedPlugData> _TmpSortedPlugDatas = new List<SortedPlugData>();

        #region GUI + INIT

        // Also called each time the target changes
        void Init(bool forceRefresh = false)
        {
            if (!_initialized) {
                _initialized = true;
                _gcVectorLocked = new GUIContent(DeStylePalette.ico_lock);
                _gcVectorUnlocked = new GUIContent(DeStylePalette.ico_lock_open);
                _hDividerColor = new DeSkinColor(new Color(0, 0, 0, 0.2f), new Color(1, 1, 1, 0.1f));
            }

            // Check full refresh and apply every-time refresh
            bool refreshRequired = false;
            _totClipElements = TimelineSelection.totClipElements;
            int len = _clipElements.Count;
            if (_totClipElements != len) {
                refreshRequired = true;
                _clipElements.Clear();
                for (int i = 0; i < _totClipElements; ++i) _clipElements.Add(TimelineSelection.ClipElements[i].clipElement);
            } else {
                for (int i = 0; i < len; ++i) {
                    if (!refreshRequired && TimelineSelection.ClipElements[i].clipElement == _clipElements[i]) continue;
                    refreshRequired = true;
                    _clipElements[i] = TimelineSelection.ClipElements[i].clipElement;
                }
            }
            if (refreshRequired) _forceShowOnCompletes = _forceShowOnStepCompletes = _forceShowOnUpdates = false;
            DOTweenClipElement.Type sType = _totClipElements > 0 ? _mainClipElement.type : DOTweenClipElement.Type.Tween;
            switch (sType) {
            case DOTweenClipElement.Type.Event:
                _isEvent = true;
                _isTweener = _isAction = _isInterval = _isGlobal = false;
                _mainColor = DOEGUI.Colors.timeline.sEvent;
                break;
            case DOTweenClipElement.Type.Action:
                // isGlobal is set after Init because it requires knowledge of the pluginData
                _isAction = true;
                _isTweener = _isEvent = _isInterval = false;
                _mainColor = DOEGUI.Colors.timeline.sAction;
                break;
            case DOTweenClipElement.Type.Interval:
                _isInterval = true;
                _isAction = _isTweener = _isGlobal = false;
                _mainColor = DOEGUI.Colors.timeline.sInterval;
                break;
            case DOTweenClipElement.Type.GlobalTween:
                _isTweener = _isGlobal = true;
                _isAction = _isEvent = _isInterval = false;
                _mainColor = DOEGUI.Colors.timeline.sGlobalTween;
                break;
            default:
                _isTweener = true;
                _isAction = _isEvent = _isInterval = _isGlobal = false;
                _mainColor = DOEGUI.Colors.timeline.sTween;
                break;
            }
            if (!refreshRequired && !forceRefresh) return;

            // Refresh -----------------------
            // Store serializedProperties
            _spClipElements.Clear();
            _spOnCompletes.Clear();
            _spOnStepCompletes.Clear();
            _spOnUpdates.Clear();
            for (int i = 0; i < _totClipElements; ++i) {
                SerializedProperty spClipElement = TimelineEditorUtils.GetSerializedClipElement(spClip, _clipElements[i].guid);
                _spClipElements.Add(spClipElement);
                _spOnCompletes.Add(spClipElement.FindPropertyRelative("onComplete"));
                _spOnStepCompletes.Add(spClipElement.FindPropertyRelative("onStepComplete"));
                _spOnUpdates.Add(spClipElement.FindPropertyRelative("onUpdate"));
            }
            //
            _tweenPlugins.Clear();
            _actionPlugins.Clear();
            if (_isEvent) {
                // Stop here if Event
                _allTargetsSet = _allTargetsTheSame = _allPluginsSet = _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                return;
            }
            //
            _allTargetsSet = _allTargetsTheSame = _allPluginsSet = _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = true;
            Object defTarget = null; // Used for targeted tweens/actions
            DOVisualTweenPlugin defPlug = null; // Used for targeted tweens/actions
            string defPlugId = null; // Used for global tweens/actions
            string defPlugDataGuid = null;
            int defPlugDataIndex = -1; // Legacy
            for (int i = 0; i < _totClipElements; ++i) {
                DOTweenClipElement clipElement = _clipElements[i];
                if (_isAction) {
                    if (clipElement == null) {
                        _allTargetsSet = _allTargetsTheSame = _allPluginsSet = _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                        _actionPlugins.Add(null);
                    } else {
                        DOVisualActionPlugin plug = DOVisualPluginsManager.GetActionPlugin(clipElement.plugId);
                        bool isValidPlugin = TimelineEditorUtils.ValidateClipElementPlugin(clipElement, plug);
                        if (!isValidPlugin) {
                            plug = null;
                            _allPluginsSet = _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                        }
                        string plugDataGuid = clipElement.plugDataGuid;
                        int plugDataIndex = clipElement.plugDataIndex;
                        if (i == 0) {
                            _isGlobal = !isValidPlugin || plug != null && !plug.GetPlugData(_mainClipElement).wantsTarget;
                            defPlugId = clipElement.plugId;
                            defPlugDataGuid = plugDataGuid;
                            defPlugDataIndex = plugDataIndex;
                        } else {
                            if (_allPluginsOfSameType) {
                                if (clipElement.plugId != defPlugId) _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                                else if (plugDataGuid != defPlugDataGuid || plugDataIndex != defPlugDataIndex) _allPluginsOfSameTypeAndSubtype = false;
                            }
                        }
                        _actionPlugins.Add(plug);
                    }
                } else if (_isTweener) {
                    if (clipElement == null) {
                        _allTargetsSet = _allTargetsTheSame = _allPluginsSet = _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                        _tweenPlugins.Add(null);
                    } else {
                        Object target = clipElement.target;
                        if (!_isGlobal && target == null) _allTargetsSet = false;
                        DOVisualTweenPlugin plug = _isGlobal
                            ? DOVisualPluginsManager.GetGlobalTweenPlugin(clipElement.plugId)
                            : target == null
                                ? DOVisualPluginsManager.Editor_GetTweenPluginByPlugDataGuid(_mainClipElement.plugDataGuid)
                                : DOVisualPluginsManager.GetTweenPlugin(target);
                        bool isValidPlugin = TimelineEditorUtils.ValidateClipElementPlugin(clipElement, plug, _isGlobal);
                        if (!isValidPlugin) {
                            plug = null;
                            _allPluginsSet = _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                        }
                        string plugDataGuid = clipElement.plugDataGuid;
                        int plugDataIndex = clipElement.plugDataIndex;
                        if (i == 0) {
                            defTarget = _isGlobal ? null : target;
                            defPlug = plug;
                            defPlugId = clipElement.plugId;
                            defPlugDataGuid = plugDataGuid;
                            defPlugDataIndex = plugDataIndex;
                        } else {
                            if (!_isGlobal && _allTargetsTheSame && target != defTarget) _allTargetsTheSame = false;
                            if (_allPluginsOfSameType) {
                                if (_isGlobal && clipElement.plugId != defPlugId || !_isGlobal && plug.targetType != defPlug.targetType) {
                                    _allPluginsOfSameType = _allPluginsOfSameTypeAndSubtype = false;
                                } else if (plugDataGuid != defPlugDataGuid || plugDataIndex != defPlugDataIndex) _allPluginsOfSameTypeAndSubtype = false;
                            }
                        }
                        _tweenPlugins.Add(plug);
                    }
                }
            }
//            Debug.Log("allTargetsSet: " + LogBool(_allTargetsSet) + ", _allTargetsTheSame: " + LogBool(_allTargetsTheSame)
//                      + ", allPluginsSet: " + LogBool(_allPluginsSet) + ", _allPluginsOfSameType: " + LogBool(_allPluginsOfSameType)
//                      + ", _allPluginsOfSameTypeAndSubtype: " + LogBool(_allPluginsOfSameTypeAndSubtype));
        }

        string LogBool(bool value)
        {
            return value ? "<color=#00ff00>TRUE</color>" : "<color=#ff0000>FALSE</color>";
        }

        public void Refresh() {}

        // Modifies area as it goes on
        public override void Draw(Rect drawArea)
        {
            if (Event.current.type == EventType.Layout) return;

            base.Draw(drawArea);
            if (area.width <= 0) return;

            Init(isUndoRedoPass || isRecorderStoppedPass);
            if (_totClipElements == 0 || isRecorderStoppedPass) return;
            if (_isAction) {
                _isGlobal = _actionPlugins.Count > 0 && _mainActionPlugin != null && !_mainActionPlugin.GetPlugData(_mainClipElement).wantsTarget;
            }

            // Special actions
            if (showPlugTypeDropdown) {
                showPlugTypeDropdown = false;
                switch (_mainClipElement.type) {
                case DOTweenClipElement.Type.Tween:
                    // BUG (UNITY) Opening a menu after another menu
                    // (like it would happen in this case, where the previous menu was to choose the Component type)
                    // weirdly makes Unity fire an Undo after setting things in the second menu, so it won't work.
                    // HACK menu after menu solution
                    // Solved by opening this second menu with a delayed call and collapsing its undo until the tween creation one
                    if (_mainClipElement.target != null) {
                        Vector2 mouseP = DeUnityEditorVersion.MajorVersion < 2020
                            ? new Vector2(editor.position.x, editor.position.y) + new Vector2(drawArea.x, drawArea.y) + Event.current.mousePosition
                            : new Vector2(drawArea.x, drawArea.y) + Event.current.mousePosition;
                        DeEditorUtils.DelayedCall(0.1f, ()=> CM_TweenPlugType(mouseP, -2));
                    }
                    break;
                case DOTweenClipElement.Type.GlobalTween:
                    CM_GlobalTweenOrActionPlugType(false);
                    break;
                case DOTweenClipElement.Type.Action:
                    CM_GlobalTweenOrActionPlugType(true);
                    break;
                }
            }

            // Background
            using (new DeGUI.ColorScope(null, null, _mainColor)) {
                GUI.Box(area, GUIContent.none, DOEGUI.Styles.timeline.sBg);
            }

            Rect scrollViewArea = area;
            if (_scrollView.fullContentArea.height > area.height) area = area.Shift(0, 0, -11, 0);
            _scrollView = DeGUI.BeginScrollView(scrollViewArea, _scrollView);
            using (new DeGUI.LabelFieldWidthScope(_minLabelWidth)) {
                _scrollView.IncreaseContentHeightBy(DrawHeaderAndTweenType());
                if (!_isEvent && !_isInterval && !_isGlobal && (!_isAction || _allPluginsOfSameTypeAndSubtype)) _scrollView.IncreaseContentHeightBy(DrawTarget());
                if (!_isEvent && !_isInterval && (!_allTargetsSet || !_allPluginsSet)) {
                    using (new DeGUI.ColorScope(DOEGUI.Colors.global.red)) {
                        GUI.Label(area.SetHeight(30),
                            _isGlobal || _isAction ? "Unset" : _allTargetsSet ? "Target is not supported" : "Target must be set",
                            DOEGUI.Styles.timeline.sMissingPluginBox
                        );
                    }
                } else if (_isEvent || _isInterval || _allTargetsSet) {
                    _scrollView.IncreaseContentHeightBy(DrawTimings());
                    if (_isTweener) {
                        if (CanHaveEase()) _scrollView.IncreaseContentHeightBy(DrawEase());
                        if (_allPluginsOfSameTypeAndSubtype) {
                            Divider();
                            _scrollView.IncreaseContentHeightBy(DrawToAndFrom());
                            _scrollView.IncreaseContentHeightBy(DrawExtraOptions());
                        }
                        Divider();
                    } else if (_isAction) {
                        Divider();
                        _scrollView.IncreaseContentHeightBy(DrawActionOptions());
                    }
                    if (_isTweener || _isEvent) _scrollView.IncreaseContentHeightBy(DrawEvents());
                }
            }
            _scrollView.IncreaseContentHeightBy(4);
            DeGUI.EndScrollView();

            // Deselect text field if mouseDown outside of it
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && GUIUtility.keyboardControl != 0) {
                GUI.FocusControl(null);
                editor.Repaint();
            }
        }

        void Divider()
        {
            DeGUI.DrawColoredSquare(new Rect(area.x + 4, area.y + 4, area.width - 8, 1), _hDividerColor);
            area = area.ShiftYAndResize(9);
            _scrollView.IncreaseContentHeightBy(9);
        }

        float DrawHeaderAndTweenType()
        {
            bool drawExecuteInEditMode = _isAction;

            Rect fullR = area.SetHeight(_lineHeight + 2);
            if (drawExecuteInEditMode) fullR = fullR.Shift(0, 0, 0, _lineHeight + 2);
            Rect contentR = fullR.Shift(_Padding, 1, -_Padding - 1, 0).SetHeight(_lineHeight);
            Rect typeR = contentR.SetWidth(_minLabelWidth + 1);
            Rect pluginR = contentR.ShiftXAndResize(typeR.width).ShiftYAndResize(1);
            pluginR = pluginR.SetWidth(pluginR.width - 1);

            DeGUI.DrawColoredSquare(fullR, _mainColor);
            if (_isInterval) GUI.Label(typeR, "Interval", DOEGUI.Styles.timeline.sIntervalTitle);
            else {
                GUI.Label(typeR,
                    _mainClipElement.type == DOTweenClipElement.Type.Action
                        ? "Action"
                        : _mainClipElement.type == DOTweenClipElement.Type.Event
                            ? "Event"
                            : "Tween",
                    DOEGUI.Styles.timeline.sTitle
                );
            }
            if (!_isInterval) {
                bool drawSelector = false;
                GUIContent gcPlugin = null;
                float pluginFullWidth = 0;
                if (_isAction) {
                    gcPlugin = _allPluginsOfSameTypeAndSubtype
                        ? _mainActionPlugin.Editor_GetClipElementHeaderLabelGUIContent(_mainClipElement, false)
                        : new GUIContent("–");
                    pluginFullWidth = DOEGUI.Styles.timeline.sBtTargetAndPlugType.CalcSize(gcPlugin).x;
                    drawSelector = true;
                } else if (!_isEvent && (_isGlobal || _allPluginsSet)) {
                    gcPlugin = _allPluginsOfSameTypeAndSubtype
                        ? _mainTweenPlugin.Editor_GetShortTypeAndAnimationNameGUIContent(_mainClipElement)
                        : _allPluginsOfSameType
                            ? new GUIContent(_mainTweenPlugin.Editor_GetShortTypeName(true) + " –")
                            : new GUIContent("–");
                    pluginFullWidth = DOEGUI.Styles.timeline.sBtTargetAndPlugType.CalcSize(gcPlugin).x;
                    drawSelector = true;
                }
                if (drawSelector) {
                    using (new DeGUI.ColorScope(new DeSkinColor(0.1f))) {
                        if (EditorGUI.DropdownButton(pluginR, gcPlugin, FocusType.Passive,
                            pluginFullWidth > pluginR.width
                                ? DOEGUI.Styles.timeline.sBtTargetAndPlugTypeRightAligned
                                : DOEGUI.Styles.timeline.sBtTargetAndPlugType
                        )) {
                            if (_isAction) CM_GlobalTweenOrActionPlugType(true);
                            else if (_isGlobal) CM_GlobalTweenOrActionPlugType(false);
                            else if (_allPluginsOfSameType) CM_TweenPlugType(Event.current.mousePosition);
                        }
                    }
                    GUI.Label(pluginR.ShiftXAndResize(pluginR.width - 19), _gcDropdown, DOEGUI.Styles.timeline.sBtTargetAndPlugTypeDropdownLabel);
                }
                if (drawExecuteInEditMode) {
                    Rect toggleR = pluginR.ShiftY(pluginR.height + _Padding);
                    DeGUI.MultiToggleButton(
                        toggleR, _gcExecuteInEditMode, "executeInEditMode", _clipElements,
                        DOEGUI.Colors.bg.critical, null, DOEGUI.Colors.content.toggleCritical
                    );
                }
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }

        float DrawTarget()
        {
            Rect fullR = area.SetHeight(_lineHeight + _Padding * 2);
            Rect contentR = fullR.Contract(_Padding);
            Rect prefixR = contentR.SetWidth(EditorGUIUtility.labelWidth + 2);
            Rect targetR = contentR.ShiftXAndResize(prefixR.width);

            Color bgColor = _allTargetsSet ? _mainColor : Color.red;
            DeGUI.DrawColoredSquare(fullR, bgColor.SetAlpha(0.5f));
            Type forcedTargetType = null;
            if (_isAction) {
                PlugDataAction plugData = _allPluginsOfSameTypeAndSubtype ? _mainActionPlugin.GetPlugData(_mainClipElement) : null;
                if (plugData != null && plugData.targetType != null) forcedTargetType = plugData.targetType;
                GUI.Label(prefixR,
                    plugData != null && plugData.targetLabel != null ? new GUIContent(plugData.targetLabel) : _gcTarget,
                    DOEGUI.Styles.timeline.sTargetPrefixLabel
                );
            } else {
                if (settings.enforceTargetTypeInClipElement && _mainTweenPlugin != null && _isTweener && !_isGlobal) {
                    forcedTargetType = _mainTweenPlugin.targetType;
                }
                GUI.Label(prefixR, _gcTarget, DOEGUI.Styles.timeline.sTargetPrefixLabel);
            }
            using (new DeGUI.ColorScope(null, null, _allTargetsSet ? Color.white : bgColor)) {
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    if (forcedTargetType != null) DeGUI.MultiObjectField(targetR, GUIContent.none, "target", _clipElements, forcedTargetType, true);
                    else {
                        CacheCurrTargets();
                        DeGUI.MultiObjectField(targetR, GUIContent.none, "target", _clipElements, true);
                    }
                    if (check.changed) {
                        bool isGameObject = _mainClipElement.target != null && _mainClipElement.target is GameObject;
                        if (isGameObject && forcedTargetType == null) {
                            GameObject target = _mainClipElement.target as GameObject;
                            RestoreCurrTargets();
                            TimelineEditorUtils.CM_SelectClipElementTargetFromGameObject(target, AssignTarget);
                        } else {
                            AssignTarget(_mainClipElement.target);
                        }
                    }
                }
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }
        void AssignTarget(Object target)
        {
            using (new DOScope.UndoableSerialization()) {
                for (int i = 0; i < _totClipElements; ++i) {
                    DOTweenClipElement clipElement = _clipElements[i];
                    clipElement.target = target;
                    if (!_isAction) {
                        if (clipElement.target == null) {
                            // Keep plugin and plugDataGuid because we'll use them to display/store the tween data even without target
                            // _tweenPlugins[i] = null;
                            // clipElement.plugDataGuid = null;
                            // clipElement.plugDataIndex = 0;
                        } else {
                            DOVisualTweenPlugin prevPlugin = _tweenPlugins[i];
                            _tweenPlugins[i] = DOVisualPluginsManager.GetTweenPlugin(clipElement.target);
                            if (_tweenPlugins[i] == null || _tweenPlugins[i] != prevPlugin) {
                                clipElement.plugDataGuid = null;
                                clipElement.plugDataIndex = 0;
                            }
                        }
                    }
                }
            }
            Init(true);
            DOTweenClipTimeline.Dispatch_OnClipChanged(clip);
        }

        float DrawTimings()
        {
            int rows = _isTweener ? 2 : 1;
            Rect fullR = area.SetHeight(_lineHeight * rows + _Padding * (rows + 1));
            Rect contentR = fullR.Contract(_Padding);
            Rect timeRowR = contentR.SetHeight(_lineHeight);
            Rect timeR = timeRowR.SetWidth(_minLabelWidth + 64);
            Rect durationR = timeRowR.ShiftXAndResize(timeR.width + 16);
            Rect loopsRowR = timeRowR.Shift(0, _lineHeight + _Padding, 0, 0);
            Rect loopsR = timeR.SetY(loopsRowR.y);
            Rect loopTypeR = loopsRowR.ShiftXAndResize(loopsR.width + _Padding);

            if (_isTweener || _isInterval) { // Time
                DeGUI.MultiFloatField(timeR, _gcStartTime, "startTime", _clipElements, 0, null);
                using (new DeGUI.LabelFieldWidthScope(GUI.skin.label.CalcSize(_gcDuration).x)) {
                    DeGUI.MultiFloatField(durationR, _gcDuration, "duration", _clipElements, 0);
                }
            } else { // Duration
                DeGUI.MultiFloatField(timeR, _gcStartTime, "startTime", _clipElements, 0, null);
            }
            if (_isTweener) { // Loops
                DeGUI.MultiIntField(loopsR, _gcLoops, "loops", _clipElements, 1, null);
                using (new EditorGUI.DisabledScope(!_mainClipElement.Editor_HasMultipleLoops())) {
                    DeGUI.MultiEnumPopup<LoopType>(loopTypeR, GUIContent.none, "loopType", _clipElements);
                }
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }

        float DrawEase()
        {
            Rect fullR = area.SetHeight(_lineHeight + _Padding * 2); // Increased below if necessary
            Rect contentR = fullR.Contract(_Padding);
            Rect easeRowR = contentR;
            Rect easePreviewR = contentR.Shift(0, _lineHeight + _Padding, 0, 0).SetHeight(_lineHeight * 2);
            Rect extraContentR = easePreviewR.Shift(0, easePreviewR.height + _Padding, 0, 0).SetHeight(_lineHeight);

            // Ease + eventual Snapping
            Rect snappingR = easeRowR.ShiftXAndResize(easeRowR.width + _Padding);
            if (_allPluginsOfSameTypeAndSubtype) {
                switch (_mainTweenPlugin.GetPlugData(_mainClipElement).propertyType) {
                case DOTweenClipElement.PropertyType.Float:
                case DOTweenClipElement.PropertyType.Vector2:
                case DOTweenClipElement.PropertyType.Vector3:
                case DOTweenClipElement.PropertyType.Vector4:
                case DOTweenClipElement.PropertyType.Rect:
                    snappingR = easeRowR.ShiftXAndResize(easeRowR.width - 64).Shift(0, 1, 0, 0);
                    DeGUI.MultiToggleButton(snappingR, _gcSnapping, "boolOption0", _clipElements, DOEGUI.Styles.timeline.sToggle);
                    break;
                }
            }
            Rect easeR = easeRowR.Shift(0, 0, -snappingR.width - _Padding, 0);
            if (GUI.Button(easeR, GUIContent.none, GUIStyle.none)) {
                _easeSelectionWin.Prepare(_clipElements);
                PopupWindow.Show(easeR, _easeSelectionWin);
            }
            bool mixedEases = DOEGUI.MultiFilteredEasePopup(easeR, _gcEase, "ease", _clipElements);
            if (!mixedEases) {
                // Ease preview
                EaseSnapshot currEaseSnapshot = new EaseSnapshot(_mainClipElement.ease, _mainClipElement.overshootOrAmplitude, _mainClipElement.period);
                if (currEaseSnapshot.ease == Ease.INTERNAL_Custom) {
                    extraContentR = extraContentR.ShiftY(-easePreviewR.height - _Padding);
                } else {
                    fullR = fullR.Shift(0, 0, 0, easePreviewR.height + _Padding);
                    easePreviewR = easePreviewR.ShiftXAndResize(EditorGUIUtility.labelWidth + 2);
                    if (GUI.Button(easePreviewR, GUIContent.none, GUIStyle.none)) {
                        _easeSelectionWin.Prepare(_clipElements);
                        PopupWindow.Show(easeR, _easeSelectionWin);
                    }
                    if (currEaseSnapshot != _lastPreviewedEase || _easePreviewTex == null) {
                        _lastPreviewedEase = currEaseSnapshot;
                        if (_easePreviewTex == null) _easePreviewTex = new Texture2D((int)easePreviewR.width, (int)easePreviewR.height);
                        TimelineEditorUtils.GenerateEaseTextureIn(
                            _easePreviewTex, currEaseSnapshot.ease, currEaseSnapshot.overshootOrAmplitude, currEaseSnapshot.period
                        );
                    }
                    GUI.DrawTexture(easePreviewR, _easePreviewTex);
                }
                // Extra ease options
                Rect overR;
                bool isBack = false, isElastic = false, isFlash = false, isCustom = false;
                switch (_mainClipElement.ease) {
                case Ease.InBack: case Ease.OutBack: case Ease.InOutBack:
                    isBack = true;
                    break;
                case Ease.InElastic: case Ease.OutElastic: case Ease.InOutElastic:
                    isElastic = true;
                    break;
                case Ease.Flash: case Ease.InFlash: case Ease.OutFlash: case Ease.InOutFlash:
                    isFlash = true;
                    break;
                case Ease.INTERNAL_Custom:
                    isCustom = true;
                    break;
                }
                if (isBack) {
                    fullR = fullR.Shift(0, 0, 0, _lineHeight + _Padding);
                    extraContentR = extraContentR.ShiftXAndResize(EditorGUIUtility.labelWidth + 2);
                    overR = extraContentR.SetWidth(_minLabelWidth + 64);
                    using (new DeGUI.LabelFieldWidthScope(EditorStyles.textField.CalcSize(_gcOvershoot).x)) {
                        DeGUI.MultiFloatField(overR, _gcOvershoot, "overshootOrAmplitude", _clipElements, 0);
                    }
                } else if (isElastic || isFlash) {
                    fullR = fullR.Shift(0, 0, 0, _lineHeight + _Padding);
                    extraContentR = extraContentR.ShiftXAndResize(EditorGUIUtility.labelWidth + 2);
                    overR = extraContentR.SetWidth(_minLabelWidth + 40);
                    Rect periodR = extraContentR.ShiftXAndResize(overR.width + 16);
                    using (new DeGUI.LabelFieldWidthScope(EditorStyles.textField.CalcSize(_gcAmplitude).x)) {
                        if (isFlash) DeGUI.MultiIntField(overR, _gcAmplitude, "overshootOrAmplitude", _clipElements, 1);
                        else DeGUI.MultiFloatField(overR, _gcAmplitude, "overshootOrAmplitude", _clipElements, 1, 10);
                    }
                    using (new DeGUI.LabelFieldWidthScope(EditorStyles.textField.CalcSize(_gcPeriod).x)) {
                        if (isFlash) DeGUI.MultiIntField(periodR, _gcPeriod, "period", _clipElements, -1, 1);
                        else DeGUI.MultiFloatField(periodR, _gcPeriod, "period", _clipElements, 0.05f, 1f);
                    }
                } else if (isCustom) {
                    fullR = fullR.Shift(0, 0, 0, _lineHeight + _Padding);
                    DeGUI.MultiCurveField(extraContentR.ShiftXAndResize(_minLabelWidth + 2), GUIContent.none, "easeCurve", _clipElements);
                }
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }

        float DrawToAndFrom()
        {
            if (!_allPluginsOfSameTypeAndSubtype) return 0;

            Rect fullR = area.SetHeight(_lineHeight + _Padding * 2); // Increased below if necessary
            Rect contentR = fullR.Contract(_Padding);
            Rect innerContentR = contentR;

            PluginTweenType tweenType = _mainTweenPlugin.GetPlugData(_mainClipElement).tweenType;
            bool canHaveFrom = CanHaveFrom(_mainClipElement, _mainTweenPlugin);
            // From
            if (canHaveFrom) {
                // Flip options
                Rect prefixR = innerContentR.SetWidth(_minLabelWidth);
                Rect flipR = prefixR.ShiftXAndResize(prefixR.width - DOEGUI.Styles.timeline.sBtFlip.fixedWidth + 2);
                innerContentR = innerContentR.ShiftXAndResize(flipR.xMax - innerContentR.x + 2);
                GUI.Label(prefixR, "From");
                using (new DeGUI.ColorScope(new DeSkinColor(0.2f, 1f))) {
                    if (GUI.Button(flipR, DeStylePalette.ico_flipV, DOEGUI.Styles.timeline.sBtFlip)) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            SwitchToFrom(_clipElements[i], _tweenPlugins[i]);
                        }
                        GUI.changed = true;
                    }
                }
                if (GUI.Button(prefixR, GUIContent.none, GUIStyle.none)) {
                    if (Event.current.button == 1) CM_CopyValuesFromHierarchyTarget(true);
                }
                //
                innerContentR = OptionalAlphaToggle(innerContentR);
                innerContentR = OptionalAxisConstraint(innerContentR, _mainClipElement.fromType);
                innerContentR = OptionalRelativeToggle(innerContentR, _mainClipElement.fromType);
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    DeGUI.MultiEnumPopup<DOTweenClipElement.ToFromType>(innerContentR, GUIContent.none, "fromType", _clipElements);
                    if (check.changed && _mainClipElement.fromType == DOTweenClipElement.ToFromType.Dynamic) {
                        for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].toType = DOTweenClipElement.ToFromType.Direct;
                    }
                }
                if (_mainClipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                    contentR = innerContentR = contentR.Shift(0, contentR.height + _Padding, 0, 0);
                    innerContentR = innerContentR.ShiftXAndResize(_minLabelWidth + 2);
                    innerContentR = ToFromPropertyValue(innerContentR, false);
                    contentR = contentR.SetHeight(innerContentR.height);
                    fullR = fullR.Shift(0, 0, 0, contentR.height + _Padding);
                }
            }
            // To
            if (canHaveFrom) {
                contentR = innerContentR = contentR.Shift(0, contentR.height + _Padding, 0, 0).SetHeight(_lineHeight);
                fullR = fullR.Shift(0, 0, 0, contentR.height + _Padding);
                Rect prefixR = innerContentR.SetWidth(_minLabelWidth);
                innerContentR = innerContentR.ShiftXAndResize(prefixR.width + 2);
                innerContentR = OptionalAlphaToggle(innerContentR);
                innerContentR = OptionalAxisConstraint(innerContentR, _mainClipElement.toType);
                innerContentR = OptionalRelativeToggle(innerContentR, _mainClipElement.toType);
                GUI.Label(prefixR, "To");
                if (GUI.Button(prefixR, GUIContent.none, GUIStyle.none)) {
                    if (Event.current.button == 1) CM_CopyValuesFromHierarchyTarget(false);
                }
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    DeGUI.MultiEnumPopup<DOTweenClipElement.ToFromType>(innerContentR, GUIContent.none, "toType", _clipElements);
                    if (check.changed && _mainClipElement.toType == DOTweenClipElement.ToFromType.Dynamic) {
                        for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].fromType = DOTweenClipElement.ToFromType.Direct;
                    }
                }
            }
            if (_mainClipElement.toType == DOTweenClipElement.ToFromType.Direct) {
                switch (tweenType) {
                case PluginTweenType.Punch:
                case PluginTweenType.Shake:
                    innerContentR = contentR;
                    GUI.Label(innerContentR.SetWidth(_minLabelWidth + 2), "Strn", DOEGUI.Styles.timeline.sPrefixLabel);
                    break;
                default:
                    contentR = innerContentR = contentR.Shift(0, contentR.height + _Padding, 0, 0);
                    break;
                }
                innerContentR = innerContentR.ShiftXAndResize(_minLabelWidth + 2);
                innerContentR = ToFromPropertyValue(innerContentR, true);
                contentR = contentR.SetHeight(innerContentR.height);
                if (canHaveFrom) fullR = fullR.Shift(0, 0, 0, contentR.height + _Padding);
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }

        float DrawActionOptions()
        {
            Rect fullR = area.SetHeight(0); // Increased below if necessary
            Rect contentR = new Rect(area.x + _Padding, area.y - _lineHeight, area.width - _Padding * 2, _lineHeight);
            Rect innerContentR = contentR;
            Rect prefixR = innerContentR.SetWidth(_minLabelWidth);
            PlugDataAction plugData = _mainActionPlugin.GetPlugData(_mainClipElement);
            using (new DeGUI.LabelFieldWidthScope(EditorGUIUtility.labelWidth + 20)) {
                if (!string.IsNullOrEmpty(plugData.objOptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    DeGUI.MultiObjectField(contentR, new GUIContent(plugData.objOptionLabel), "objOption", _clipElements, plugData.objOptionType, true);
                }
                if (!string.IsNullOrEmpty(plugData.boolOptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    DeGUI.MultiToggleButton(contentR, new GUIContent(plugData.boolOptionLabel), "boolOption0", _clipElements);
                }
                if (!string.IsNullOrEmpty(plugData.stringOptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    DeGUI.MultiTextField(contentR, new GUIContent(plugData.stringOptionLabel), "toStringVal", _clipElements);
                }
                if (!string.IsNullOrEmpty(plugData.intOptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    if (plugData.intOptionAsEnumType == null) {
                        contentR = contentR.SetWidth(EditorGUIUtility.labelWidth + 64);
                        DeGUI.MultiIntField(contentR, new GUIContent(plugData.intOptionLabel), "toIntVal", _clipElements);
                    } else { // int as enum popup
                        DeGUI.MultiEnumPopup(contentR,
                            new GUIContent(plugData.intOptionLabel), plugData.intOptionAsEnumType, "toIntVal", _clipElements
                        );
                    }
                }
                if (!string.IsNullOrEmpty(plugData.float0OptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    contentR = contentR.SetWidth(EditorGUIUtility.labelWidth + 64);
                    DeGUI.MultiFloatField(contentR, new GUIContent(plugData.float0OptionLabel), "toFloatVal", _clipElements);
                }
                if (!string.IsNullOrEmpty(plugData.float1OptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    contentR = contentR.SetWidth(EditorGUIUtility.labelWidth + 64);
                    DeGUI.MultiFloatField(contentR, new GUIContent(plugData.float1OptionLabel), "fromFloatVal", _clipElements);
                }
                if (!string.IsNullOrEmpty(plugData.float2OptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    contentR = contentR.SetWidth(EditorGUIUtility.labelWidth + 64);
                    DeGUI.MultiFloatField(contentR, new GUIContent(plugData.float2OptionLabel), "floatOption0", _clipElements);
                }
                if (!string.IsNullOrEmpty(plugData.float3OptionLabel)) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    contentR = contentR.SetWidth(EditorGUIUtility.labelWidth + 64);
                    DeGUI.MultiFloatField(contentR, new GUIContent(plugData.float3OptionLabel), "floatOption1", _clipElements);
                }
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }

        float DrawExtraOptions()
        {
            Rect fullR = area.SetHeight(0); // Increased below if necessary
            Rect contentR = new Rect(area.x + _Padding, area.y - _lineHeight, area.width - _Padding * 2, _lineHeight);
            Rect innerContentR = contentR;
            Rect prefixR = innerContentR.SetWidth(_minLabelWidth);
            Rect r0, r1;

            ITweenPluginData plugData = _mainTweenPlugin.GetPlugData(_mainClipElement);
            switch (plugData.propertyType) {
            case DOTweenClipElement.PropertyType.String:
                ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                r0 = innerContentR.SetWidth(innerContentR.width * 0.5f - _Padding * 0.5f).Shift(0, 1, 0, 0);
                r1 = r0.Shift(r0.width + _Padding, 0, 0, 0);
                GUI.Label(prefixR, "Options", DOEGUI.Styles.timeline.sPrefixLabel);
                DeGUI.MultiToggleButton(r0, _gcRichTextEnabled, "boolOption0", _clipElements, DOEGUI.Styles.timeline.sToggle);
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    DeGUI.MultiToggleButton(r1, _gcScramble, "intOption0", _clipElements, DOEGUI.Styles.timeline.sToggle);
                    if (check.changed) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            _clipElements[i].intOption1 = _clipElements[i].intOption0;
                            _clipElements[i].stringOption0 = null;
                        }
                    }
                }
                if (_mainClipElement.intOption0 == 1) {
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    GUI.Label(innerContentR, _gcScrambleOptions);
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        DeGUI.MultiEnumPopup<ScrambleMode>(innerContentR, GUIContent.none, "intOption1", _clipElements);
                        if (check.changed) {
                            ScrambleMode scrambleMode = (ScrambleMode)_mainClipElement.intOption1;
                            if (scrambleMode == ScrambleMode.None) {
                                for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].intOption0 = 0;
                            } else if (scrambleMode != ScrambleMode.Custom) {
                                for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].stringOption0 = null;
                            }
                        }
                    }
                    if ((ScrambleMode)_mainClipElement.intOption1 == ScrambleMode.Custom) {
                        ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                        DeGUI.MultiTextField(innerContentR, GUIContent.none, "stringOption0", _clipElements);
                    }
                }
                break;
            case DOTweenClipElement.PropertyType.Vector3:
                switch (plugData.tweenType) {
                case PluginTweenType.Punch:
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    r0 = prefixR.SetWidth(prefixR.width + 64);
                    r1 = new Rect(r0.xMax + 16, r0.y, 0, r0.height);
                    r1.width = innerContentR.xMax - r1.x;
                    float elasticitySize = GUI.skin.label.CalcSize(_gcElasticity).x + 6;
                    DeGUI.MultiIntField(r0, _gcVibrato, "intOption1", _clipElements, 0, null);
                    using (new DeGUI.LabelFieldWidthScope(elasticitySize)) {
                        DeGUI.MultiFloatField(r1, _gcElasticity, "floatOption0", _clipElements, 0, 1);
                    }
                    break;
                case PluginTweenType.Shake:
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    r0 = prefixR.SetWidth(prefixR.width + 64);
                    r1 = new Rect(r0.xMax + 16, r0.y, 0, r0.height);
                    r1.width = innerContentR.xMax - r1.x;
                    float randomnessSize = GUI.skin.label.CalcSize(_gcRandomness).x;
                    DeGUI.MultiIntField(r0, _gcVibrato, "intOption1", _clipElements, 0, null);
                    using (new DeGUI.LabelFieldWidthScope(randomnessSize)) {
                        DeGUI.MultiFloatField(r1, _gcRandomness, "floatOption0", _clipElements, 0, 180);
                    }
                    ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                    innerContentR = innerContentR.Shift(0, 1, 0, 0);
                    DeGUI.MultiToggleButton(innerContentR, _gcFadeOut, "intOption0", _clipElements, DOEGUI.Styles.timeline.sToggle);
                    break;
                }
                break;
            case DOTweenClipElement.PropertyType.Quaternion:
                ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                GUI.Label(prefixR, "Mode", DOEGUI.Styles.timeline.sPrefixLabel);
                DeGUI.MultiEnumPopup<RotateMode>(innerContentR, GUIContent.none, "intOption0", _clipElements);
                break;
            }
            switch (plugData.tweenType) {
            case PluginTweenType.StringOption:
                ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                DeGUI.MultiTextField(contentR, new GUIContent(plugData.stringOptionLabel), "stringOption0", _clipElements);
                break;
            case PluginTweenType.IntOption:
                ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                contentR = contentR.SetWidth(_minLabelWidth + 64);
                DeGUI.MultiIntField(contentR, new GUIContent(plugData.intOptionLabel), "intOption1", _clipElements);
                break;
            case PluginTweenType.ShapeCircle:
                ShiftExtraOptionsRectsBy(ref fullR, ref contentR, ref innerContentR, ref prefixR, _lineHeight + _Padding);
                r0 = innerContentR.Shift(0, 0, -66, 0);
                r1 = innerContentR.HangToRightAndResize(r0.xMax, 2);
                GUI.Label(prefixR, new GUIContent("Center"));
                DeGUI.MultiVector2Field(r0, GUIContent.none, "toVector2Val", _clipElements);
                DeGUI.MultiToggleButton(r1, _mainClipElement.intOption1 == 1 ? _gcIsRelative : _gcIsNotRelative, "intOption1", _clipElements,
                    DOEGUI.Colors.bg.toggleOn, _isRelativeColor, DOEGUI.Colors.content.toggleOn, Color.white,
                    DOEGUI.Styles.timeline.sToggle
                );
                break;
            }

            area = area.ShiftYAndResize(fullR.height);
            return fullR.height;
        }

        void ShiftExtraOptionsRectsBy(ref Rect fullR, ref Rect contentR, ref Rect innerContentR, ref Rect prefixR, float y)
        {
            if (fullR.height < 0) fullR = fullR.SetHeight(_lineHeight + _Padding * 2);
            fullR = fullR.Shift(0, 0, 0, y);
            contentR = innerContentR = contentR.Shift(0, y, 0, 0);
            innerContentR = innerContentR.ShiftXAndResize(prefixR.width + 2);
            prefixR = contentR.SetWidth(_minLabelWidth);
        }

        float DrawEvents()
        {
            Rect fullR = area.SetHeight(_lineHeight + _Padding * 2); // Increased below if necessary
            Rect contentR = fullR.Contract(_Padding);
            bool hasOnCompletes = _forceShowOnCompletes || _mainClipElement.onComplete.GetPersistentEventCount() > 0;
            bool hasOnStepCompletes = _forceShowOnStepCompletes || _mainClipElement.onStepComplete.GetPersistentEventCount() > 0;
            bool hasOnUpdates = _forceShowOnUpdates || _mainClipElement.onUpdate.GetPersistentEventCount() > 0;
            using (new EditorGUI.DisabledScope(_clipElements.Count > 1)) {
                if (_isEvent) {
                    fullR = fullR.Shift(0, 0, 0, -_lineHeight + _Padding);
                    contentR = contentR.SetHeight(-_Padding);
                } else {
                    // Toggles
                    Rect btOnCompletesR = contentR.SetWidth(contentR.width * 0.3f - _Padding);
                    Rect btOnStepCompletesR = btOnCompletesR.Shift(btOnCompletesR.width + _Padding, 0, 0, 0).SetWidth(contentR.width * 0.4f);
                    Rect btOnUpdatesR = btOnStepCompletesR.Shift(btOnStepCompletesR.width + _Padding, 0, 0, 0).SetWidth(btOnCompletesR.width);
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        DeGUI.MultiToggleButton(btOnCompletesR,
                            hasOnCompletes,
                            new GUIContent("OnComplete"), "onComplete", _clipElements, null, null, null, null, DOEGUI.Styles.global.toggleSmlLabel
                        );
                        if (check.changed) {
                            if (hasOnCompletes) {
                                // Remove
                                _forceShowOnCompletes = false;
                                for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].onComplete = new UnityEvent();
                            } else {
                                // Show
                                _forceShowOnCompletes = true;
                            }
                            Init(true);
                        }
                    }
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        DeGUI.MultiToggleButton(btOnStepCompletesR,
                            hasOnStepCompletes,
                            new GUIContent("OnStepComplete"), "onStepComplete", _clipElements, null, null, null, null, DOEGUI.Styles.global.toggleSmlLabel
                        );
                        if (check.changed) {
                            if (hasOnStepCompletes) {
                                // Remove
                                _forceShowOnStepCompletes = false;
                                for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].onStepComplete = new UnityEvent();
                            } else {
                                // Show
                                _forceShowOnStepCompletes = true;
                            }
                            Init(true);
                        }
                    }
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        DeGUI.MultiToggleButton(btOnUpdatesR,
                            hasOnUpdates,
                            new GUIContent("OnUpdate"), "onUpdate", _clipElements, null, null, null, null, DOEGUI.Styles.global.toggleSmlLabel
                        );
                        if (check.changed) {
                            if (hasOnUpdates) {
                                // Remove
                                _forceShowOnUpdates = false;
                                for (int i = 0; i < _clipElements.Count; ++i) _clipElements[i].onUpdate = new UnityEvent();
                            } else {
                                // Show
                                _forceShowOnUpdates = true;
                            }
                            Init(true);
                        }
                    }
                }
                // Events
                if (_clipElements.Count == 1) {
                    if (_isEvent || hasOnCompletes) {
                        float h = _mainSpOnComplete.GetUnityEventHeight();
                        fullR = fullR.Shift(0, 0, 0, h + _Padding);
                        contentR = contentR.Shift(0, contentR.height + _Padding, 0, 0).SetHeight(h);
                        DeGUI.MultiUnityEvent(contentR, _isEvent ? _gcEvent : GUIContent.none, "onComplete", _clipElements, _spOnCompletes);
                    }
                    if (!_isEvent && hasOnStepCompletes) {
                        float h = _mainSpOnStepComplete.GetUnityEventHeight();
                        fullR = fullR.Shift(0, 0, 0, h + _Padding);
                        contentR = contentR.Shift(0, contentR.height + _Padding, 0, 0).SetHeight(h);
                        DeGUI.MultiUnityEvent(contentR, GUIContent.none, "onStepComplete", _clipElements, _spOnStepCompletes);
                    }
                    if (!_isEvent && hasOnUpdates) {
                        float h = _mainSpOnUpdates.GetUnityEventHeight();
                        fullR = fullR.Shift(0, 0, 0, h + _Padding);
                        contentR = contentR.Shift(0, contentR.height + _Padding, 0, 0).SetHeight(h);
                        DeGUI.MultiUnityEvent(contentR, GUIContent.none, "onUpdate", _clipElements, _spOnUpdates);
                    }
                }
            }
            return fullR.height;
        }

        Rect OptionalAxisConstraint(Rect contentR, DOTweenClipElement.ToFromType toFromType)
        {
            if (toFromType != DOTweenClipElement.ToFromType.Direct) return contentR;
            ITweenPluginData plugData = _mainTweenPlugin.GetPlugData(_mainClipElement);
            if (plugData.tweenType != PluginTweenType.SelfDetermined) return contentR;
            if (plugData.propertyType == DOTweenClipElement.PropertyType.Quaternion) return contentR;
            const int width = 20;
            bool hasAxes = false, hasZ = false, hasW = false;
            switch (plugData.propertyType) {
            case DOTweenClipElement.PropertyType.Vector2:
                hasAxes = true;
                break;
            case DOTweenClipElement.PropertyType.Vector3:
                hasAxes = hasZ = true;
                break;
            case DOTweenClipElement.PropertyType.Vector4:
                hasAxes = hasZ = hasW = true;
                break;
            }
            if (!hasAxes) return contentR;
            Rect axisR = new Rect(contentR.xMax, contentR.y, width, contentR.height);
            if (hasW) {
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    axisR = axisR.Shift(-width, 0, 0, 0);
                    DeGUI.MultiToggleButton(axisR,
                        _mainClipElement.axisConstraint == AxisConstraint.W || _mainClipElement.axisConstraint == AxisConstraint.None,
                        new GUIContent("W"), "axisConstraint", _clipElements, null, null, null, null, DOEGUI.Styles.timeline.sAxisToggle
                    );
                    if (check.changed) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            _clipElements[i].axisConstraint = _clipElements[i].axisConstraint != AxisConstraint.W ? AxisConstraint.W : AxisConstraint.None;
                        }
                    }
                }
            }
            if (hasZ) {
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    axisR = axisR.Shift(-width, 0, 0, 0);
                    DeGUI.MultiToggleButton(axisR,
                        _mainClipElement.axisConstraint == AxisConstraint.Z || _mainClipElement.axisConstraint == AxisConstraint.None,
                        new GUIContent("Z"), "axisConstraint", _clipElements, null, null, null, null, DOEGUI.Styles.timeline.sAxisToggle
                    );
                    if (check.changed) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            _clipElements[i].axisConstraint = _clipElements[i].axisConstraint != AxisConstraint.Z ? AxisConstraint.Z : AxisConstraint.None;
                        }
                    }
                }
            }
            using (var check = new EditorGUI.ChangeCheckScope()) {
                axisR = axisR.Shift(-width, 0, 0, 0);
                DeGUI.MultiToggleButton(axisR,
                    _mainClipElement.axisConstraint == AxisConstraint.Y || _mainClipElement.axisConstraint == AxisConstraint.None,
                    new GUIContent("Y"), "axisConstraint", _clipElements, null, null, null, null, DOEGUI.Styles.timeline.sAxisToggle
                );
                if (check.changed) {
                    for (int i = 0; i < _clipElements.Count; ++i) {
                        _clipElements[i].axisConstraint = _clipElements[i].axisConstraint != AxisConstraint.Y ? AxisConstraint.Y : AxisConstraint.None;
                    }
                }
            }
            using (var check = new EditorGUI.ChangeCheckScope()) {
                axisR = axisR.Shift(-width, 0, 0, 0);
                DeGUI.MultiToggleButton(axisR,
                    _mainClipElement.axisConstraint == AxisConstraint.X || _mainClipElement.axisConstraint == AxisConstraint.None,
                    new GUIContent("X"), "axisConstraint", _clipElements, null, null, null, null, DOEGUI.Styles.timeline.sAxisToggle
                );
                if (check.changed) {
                    for (int i = 0; i < _clipElements.Count; ++i) {
                        _clipElements[i].axisConstraint = _clipElements[i].axisConstraint != AxisConstraint.X ? AxisConstraint.X : AxisConstraint.None;
                    }
                }
            }
            return contentR.SetWidth(axisR.x - contentR.x - _Padding);
        }

        Rect OptionalRelativeToggle(Rect contentR, DOTweenClipElement.ToFromType toFromType)
        {
            if (toFromType != DOTweenClipElement.ToFromType.Direct) return contentR;
            switch (_mainTweenPlugin.GetPlugData(_mainClipElement).tweenType) {
            case PluginTweenType.SelfDetermined:
            case PluginTweenType.ShapeCircle:
                break;
            default:
                return contentR;
            }
            Rect r = new Rect(contentR.xMax - 64, contentR.y, 64, contentR.height);
            DeGUI.MultiToggleButton(r, _mainClipElement.isRelative ? _gcIsRelative : _gcIsNotRelative, "isRelative", _clipElements,
                DOEGUI.Colors.bg.toggleOn, _isRelativeColor, DOEGUI.Colors.content.toggleOn, Color.white,
                DOEGUI.Styles.timeline.sToggle
            );
            return contentR.SetWidth(r.x - contentR.x - _Padding);
        }

        Rect OptionalAlphaToggle(Rect contentR)
        {
            switch (_mainTweenPlugin.GetPlugData(_mainClipElement).propertyType) {
            case DOTweenClipElement.PropertyType.Color:
                Rect r = new Rect(contentR.xMax - 40, contentR.y, 40, contentR.height);
                contentR = contentR.SetWidth(contentR.width - r.width - _Padding);
                DeGUI.MultiToggleButton(r, _gcAlphaOnly, "boolOption0", _clipElements, DOEGUI.Styles.timeline.sToggle);
                break;
            }
            return contentR;
        }

        Rect ToFromPropertyValue(Rect contentR, bool isTo)
        {
            // Editing fields
            ITweenPluginData plugData = _mainTweenPlugin.GetPlugData(_mainClipElement);
            switch (plugData.propertyType) {
            case DOTweenClipElement.PropertyType.Float:
                using (new DeGUI.LabelFieldWidthScope(12)) {
                    DeGUI.MultiFloatField(contentR.SetWidth(132), _gcNumber, isTo ? "toFloatVal" : "fromFloatVal", _clipElements);
                }
                break;
            case DOTweenClipElement.PropertyType.Int:
                using (new DeGUI.LabelFieldWidthScope(12)) {
                    DeGUI.MultiIntField(contentR.SetWidth(132), _gcNumber, isTo ? "toIntVal" : "fromIntVal", _clipElements);
                }
                break;
            case DOTweenClipElement.PropertyType.Uint:
                using (new DeGUI.LabelFieldWidthScope(12)) {
                    DeGUI.MultiIntField(contentR.SetWidth(132), _gcNumber, isTo ? "toUintVal" : "fromUintVal", _clipElements);
                }
                break;
            case DOTweenClipElement.PropertyType.String:
                contentR = contentR.SetHeight(Mathf.Max(contentR.height,
                    EditorStyles.textArea.CalcHeight(new GUIContent(isTo ? _mainClipElement.toStringVal : _mainClipElement.fromStringVal), contentR.width))
                );
                DeGUI.MultiTextArea(contentR, isTo ? "toStringVal" : "fromStringVal", _clipElements);
                break;
            case DOTweenClipElement.PropertyType.Vector2:
                switch (plugData.tweenType) {
                case PluginTweenType.ShapeCircle:
                    using (new DeGUI.LabelFieldWidthScope(38)) {
                        DeGUI.MultiFloatField(contentR.SetWidth(132), _gcAngle, isTo ? "toFloatVal" : "fromFloatVal", _clipElements);
                    }
                    break;
                default:
                    RefreshVectorLockedToAndDisabledAxes();
                    VectorLock(contentR, true);
                    DeGUI.MultiVector2FieldAdvanced(contentR, GUIContent.none, isTo ? "toVector2Val" : "fromVector2Val", _clipElements,
                        (_disabledAxes & AxisConstraint.X) == 0, (_disabledAxes & AxisConstraint.Y) == 0,
                        (_lockAllAxesTo & AxisConstraint.X) != 0, (_lockAllAxesTo & AxisConstraint.Y) != 0
                    );
                    break;
                }
                break;
            case DOTweenClipElement.PropertyType.Vector3:
                RefreshVectorLockedToAndDisabledAxes();
                VectorLock(contentR, false, true);
                DeGUI.MultiVector3FieldAdvanced(contentR, GUIContent.none, isTo ? "toVector3Val" : "fromVector3Val", _clipElements,
                    (_disabledAxes & AxisConstraint.X) == 0, (_disabledAxes & AxisConstraint.Y) == 0, (_disabledAxes & AxisConstraint.Z) == 0,
                    (_lockAllAxesTo & AxisConstraint.X) != 0, (_lockAllAxesTo & AxisConstraint.Y) != 0, (_lockAllAxesTo & AxisConstraint.Z) != 0
                );
                break;
            case DOTweenClipElement.PropertyType.Vector4:
                RefreshVectorLockedToAndDisabledAxes();
                VectorLock(contentR, false, false, true);
                DeGUI.MultiVector4FieldAdvanced(contentR, GUIContent.none, isTo ? "toVector4Val" : "fromVector4Val", _clipElements,
                    (_disabledAxes & AxisConstraint.X) == 0, (_disabledAxes & AxisConstraint.Y) == 0,
                    (_disabledAxes & AxisConstraint.Z) == 0, (_disabledAxes & AxisConstraint.W) == 0,
                    (_lockAllAxesTo & AxisConstraint.X) != 0, (_lockAllAxesTo & AxisConstraint.Y) != 0,
                    (_lockAllAxesTo & AxisConstraint.Z) != 0, (_lockAllAxesTo & AxisConstraint.W) != 0
                );
                break;
            case DOTweenClipElement.PropertyType.Quaternion:
                DeGUI.MultiVector3Field(contentR, GUIContent.none, isTo ? "toVector3Val" : "fromVector3Val", _clipElements);
                break;
            case DOTweenClipElement.PropertyType.Color:
                DeGUI.MultiColorFieldAdvanced(contentR, GUIContent.none, isTo ? "toColorVal" : "fromColorVal", _clipElements, _mainClipElement.boolOption0);
                break;
            case DOTweenClipElement.PropertyType.Rect:
                contentR = contentR.SetHeight(36);
                DeGUI.MultiRectField(contentR, GUIContent.none, isTo ? "toRectVal" : "fromRectVal", _clipElements);
                break;
            }
            return contentR;
        }

        void VectorLock(Rect contentR, bool isVector2, bool isVector3 = false, bool isVector4 = false)
        {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                Rect r = contentR.SetX(contentR.x - 20).SetWidth(20);
                _mainClipElement.editor_lockVector = DeGUI.ToggleButton(r,
                    _mainClipElement.editor_lockVector, _mainClipElement.editor_lockVector ? _gcVectorLocked : _gcVectorUnlocked,
                    DOEGUI.Styles.timeline.sLockToggle
                );
                if (check.changed && _mainClipElement.editor_lockVector) {
                    RefreshVectorLockedToAndDisabledAxes();
                    float value;
                    if (isVector2) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            value = (_lockAllAxesTo & AxisConstraint.X) != 0 ? _clipElements[i].toVector2Val.x : _clipElements[i].toVector2Val.y;
                            _clipElements[i].toVector2Val = new Vector2(value, value);
                            value = (_lockAllAxesTo & AxisConstraint.X) != 0 ? _clipElements[i].fromVector2Val.x : _clipElements[i].fromVector2Val.y;
                            _clipElements[i].fromVector2Val = new Vector2(value, value);
                        }
                    } else if (isVector3) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            value = (_lockAllAxesTo & AxisConstraint.X) != 0 ? _clipElements[i].toVector3Val.x
                                : (_lockAllAxesTo & AxisConstraint.Y) != 0 ? _clipElements[i].toVector3Val.y : _clipElements[i].toVector3Val.z;
                            _clipElements[i].toVector3Val = new Vector3(value, value, value);
                            value = (_lockAllAxesTo & AxisConstraint.X) != 0 ? _clipElements[i].fromVector3Val.x
                                : (_lockAllAxesTo & AxisConstraint.Y) != 0 ? _clipElements[i].fromVector3Val.y : _clipElements[i].fromVector3Val.z;
                            _clipElements[i].fromVector3Val = new Vector3(value, value, value);
                        }
                    } else if (isVector4) {
                        for (int i = 0; i < _clipElements.Count; ++i) {
                            value = (_lockAllAxesTo & AxisConstraint.X) != 0 ? _clipElements[i].toVector4Val.x
                                : (_lockAllAxesTo & AxisConstraint.Y) != 0 ? _clipElements[i].toVector4Val.y
                                : (_lockAllAxesTo & AxisConstraint.Z) != 0 ? _clipElements[i].toVector4Val.z : _clipElements[i].toVector4Val.w;
                            _clipElements[i].toVector4Val = new Vector4(value, value, value, value);
                            value = (_lockAllAxesTo & AxisConstraint.X) != 0 ? _clipElements[i].fromVector4Val.x
                                : (_lockAllAxesTo & AxisConstraint.Y) != 0 ? _clipElements[i].fromVector4Val.y
                                : (_lockAllAxesTo & AxisConstraint.Z) != 0 ? _clipElements[i].fromVector4Val.z : _clipElements[i].fromVector4Val.w;
                            _clipElements[i].fromVector4Val = new Vector4(value, value, value, value);
                        }
                    }
                }
            }
        }

        void RefreshVectorLockedToAndDisabledAxes()
        {
            DOTweenClipElement.PropertyType propertyType = _mainTweenPlugin.GetPlugData(_mainClipElement).propertyType;
            _lockAllAxesTo = AxisConstraint.None;
            _disabledAxes = AxisConstraint.None;
            bool useX, useY, useZ, useW;
            useX = _mainClipElement.axisConstraint == AxisConstraint.None || (_mainClipElement.axisConstraint & AxisConstraint.X) != 0;
            useY = _mainClipElement.axisConstraint == AxisConstraint.None || (_mainClipElement.axisConstraint & AxisConstraint.Y) != 0;
            useZ = _mainClipElement.axisConstraint == AxisConstraint.None || (_mainClipElement.axisConstraint & AxisConstraint.Z) != 0;
            useW = _mainClipElement.axisConstraint == AxisConstraint.None || (_mainClipElement.axisConstraint & AxisConstraint.W) != 0;
            if (!useX) _disabledAxes |= AxisConstraint.X;
            if (!useY) _disabledAxes |= AxisConstraint.Y;
            if (!useZ) _disabledAxes |= AxisConstraint.Z;
            if (!useW) _disabledAxes |= AxisConstraint.W;
            if (_mainClipElement.editor_lockVector) {
                switch (propertyType) {
                case DOTweenClipElement.PropertyType.Vector2:
                    if (useX) {
                        _lockAllAxesTo |= AxisConstraint.X;
                        _disabledAxes = ~AxisConstraint.X;
                    } else if (useY) {
                        _lockAllAxesTo |= AxisConstraint.Y;
                        _disabledAxes = ~AxisConstraint.Y;
                    }
                    break;
                case DOTweenClipElement.PropertyType.Vector3:
                    if (useX) {
                        _lockAllAxesTo |= AxisConstraint.X;
                        _disabledAxes = ~AxisConstraint.X;
                    } else if (useY) {
                        _lockAllAxesTo |= AxisConstraint.Y;
                        _disabledAxes = ~AxisConstraint.Y;
                    } else if (useZ) {
                        _lockAllAxesTo |= AxisConstraint.Z;
                        _disabledAxes = ~AxisConstraint.Z;
                    }
                    break;
                case DOTweenClipElement.PropertyType.Vector4:
                    if (useX) {
                        _lockAllAxesTo |= AxisConstraint.X;
                        _disabledAxes = ~AxisConstraint.X;
                    } else if (useY) {
                        _lockAllAxesTo |= AxisConstraint.Y;
                        _disabledAxes = ~AxisConstraint.Y;
                    } else if (useZ) {
                        _lockAllAxesTo |= AxisConstraint.Z;
                        _disabledAxes = ~AxisConstraint.Z;
                    } else if (useW) {
                        _lockAllAxesTo |= AxisConstraint.W;
                        _disabledAxes = ~AxisConstraint.W;
                    }
                    break;
                }
            }
        }

        void SwitchToFrom(DOTweenClipElement clipElement, DOVisualTweenPlugin plugin)
        {
            var toType = clipElement.toType;
            clipElement.toType = clipElement.fromType;
            clipElement.fromType = toType;
            ITweenPluginData plugData = plugin.GetPlugData(clipElement);
            switch (plugData.propertyType) {
            case DOTweenClipElement.PropertyType.Float:
                float toFloat = clipElement.toFloatVal;
                clipElement.toFloatVal = clipElement.fromFloatVal;
                clipElement.fromFloatVal = toFloat;
                break;
            case DOTweenClipElement.PropertyType.Int:
                int toInt = clipElement.toIntVal;
                clipElement.toIntVal = clipElement.fromIntVal;
                clipElement.fromIntVal = toInt;
                break;
            case DOTweenClipElement.PropertyType.Uint:
                uint toUint = clipElement.toUintVal;
                clipElement.toUintVal = clipElement.fromUintVal;
                clipElement.fromUintVal = toUint;
                break;
            case DOTweenClipElement.PropertyType.String:
                string toString = clipElement.toStringVal;
                clipElement.toStringVal = clipElement.fromStringVal;
                clipElement.fromStringVal = toString;
                break;
            case DOTweenClipElement.PropertyType.Vector2:
                switch (plugData.tweenType) {
                case PluginTweenType.ShapeCircle:
                    float toDegreesVal = clipElement.toFloatVal;
                    clipElement.toFloatVal = clipElement.fromFloatVal;
                    clipElement.fromFloatVal = toDegreesVal;
                    break;
                default:
                    Vector2 toVector2 = clipElement.toVector2Val;
                    clipElement.toVector2Val = clipElement.fromVector2Val;
                    clipElement.fromVector2Val = toVector2;
                    break;
                }
                break;
            case DOTweenClipElement.PropertyType.Vector3:
                Vector3 toVector3 = clipElement.toVector3Val;
                clipElement.toVector3Val = clipElement.fromVector3Val;
                clipElement.fromVector3Val = toVector3;
                break;
            case DOTweenClipElement.PropertyType.Vector4:
                Vector4 toVector4 = clipElement.toVector4Val;
                clipElement.toVector4Val = clipElement.fromVector4Val;
                clipElement.fromVector4Val = toVector4;
                break;
            case DOTweenClipElement.PropertyType.Color:
                Color toColor = clipElement.toColorVal;
                clipElement.toColorVal = clipElement.fromColorVal;
                clipElement.fromColorVal = toColor;
                break;
            case DOTweenClipElement.PropertyType.Rect:
                Rect toRect = clipElement.toRectVal;
                clipElement.toRectVal = clipElement.fromRectVal;
                clipElement.fromRectVal = toRect;
                break;
            }
            GUI.changed = true;
        }

        #region Context Menus

        void CM_TweenPlugType(Vector2 mousePosition, int collapseUndoBy = 0)
        {
            GenericMenu menu = new GenericMenu();
            DOVisualTweenPlugin plugin = _mainTweenPlugin;
            if (plugin == null) return;
            _TmpSortedPlugDatas.Clear();
            for (int i = 0; i < plugin.totPluginDatas; ++i) {
                _TmpSortedPlugDatas.Add(new SortedPlugData(plugin, null, plugin.editor_iPluginDatas[i], i));
            }
            CM_AddGenericPlugType(menu, DOTweenClipElement.Type.Tween, _TmpSortedPlugDatas, collapseUndoBy);
            menu.DropDown(new Rect(mousePosition.x, mousePosition.y, 1, 1)); // Necessary here becaue ShowAsContext won't work with a delayed call
        }

        void CM_GlobalTweenOrActionPlugType(bool isAction)
        {
            GenericMenu menu = new GenericMenu();
            List<string> allPluginsIds = isAction ? DOVisualPluginsManager.ActionPluginsIds : DOVisualPluginsManager.GlobalTweenPluginsIds;
            List<IPlugin> plugins = new List<IPlugin>();
            if (isAction) {
                for (int i = 0; i < allPluginsIds.Count; ++i) plugins.Add(DOVisualPluginsManager.GetActionPlugin(allPluginsIds[i]));
            } else {
                for (int i = 0; i < allPluginsIds.Count; ++i) plugins.Add(DOVisualPluginsManager.GetGlobalTweenPlugin(allPluginsIds[i]));
            }
            _TmpSortedPlugDatas.Clear();
            for (int i = 0; i < plugins.Count; ++i) {
                IPlugin plugin = plugins[i];
                if (plugin == null) continue;
                string pluginId = allPluginsIds[i];
                for (int j = 0; j < plugin.totPluginDatas; ++j) {
                    _TmpSortedPlugDatas.Add(new SortedPlugData(plugin, pluginId, plugin.editor_iPluginDatas[j], j));
                }
            }
            CM_AddGenericPlugType(menu, isAction ? DOTweenClipElement.Type.Action : DOTweenClipElement.Type.GlobalTween, _TmpSortedPlugDatas);
            menu.ShowAsContext();
        }

        void CM_AddGenericPlugType(GenericMenu toMenu, DOTweenClipElement.Type pluginType, List<SortedPlugData> sortedPlugDatas, int collapseUndoBy = 0)
        {
            sortedPlugDatas.Sort((a, b) => TimelineEditorUtils.SortPlugDataLabels(a.data.label, b.data.label));
            for (int i = 0; i < sortedPlugDatas.Count; ++i) {
                SortedPlugData sortedPData = sortedPlugDatas[i];
                toMenu.AddItem(
                    new GUIContent(
                        pluginType == DOTweenClipElement.Type.Tween
                            ? ((DOVisualTweenPlugin)sortedPData.plugin).Editor_GetShortTypeName() + " > " + sortedPData.data.label
                            : sortedPData.data.label
                    ),
                    false, () => {
                        using (new DOScope.UndoableSerialization()) {
                            for (int j = 0; j < _totClipElements; ++j) {
                                DOTweenClipElement clipElement = _clipElements[j];
                                string prevPlugDataGuid = clipElement.plugDataGuid;
                                int prevPlugDataIndex = clipElement.plugDataIndex;
                                clipElement.plugDataGuid = sortedPData.data.guid;
                                clipElement.plugDataIndex = sortedPData.originalIndex;
                                switch (pluginType) {
                                case DOTweenClipElement.Type.GlobalTween:
                                case DOTweenClipElement.Type.Action:
                                    clipElement.plugId = sortedPData.pluginId;
                                    break;
                                }
                                switch (pluginType) {
                                case DOTweenClipElement.Type.Tween:
                                case DOTweenClipElement.Type.GlobalTween:
                                    DOVisualTweenPlugin tweenPlugin = (DOVisualTweenPlugin)sortedPData.plugin;
                                    DOVisualTweenPlugin prevTweenPlugin = _tweenPlugins[j];
                                    bool forceReset = pluginType == DOTweenClipElement.Type.GlobalTween;
                                    ConditionalResetClipElement(clipElement, tweenPlugin, prevTweenPlugin, prevPlugDataGuid, prevPlugDataIndex, forceReset);
                                    break;
                                case DOTweenClipElement.Type.Action:
                                    DOVisualActionPlugin actionPlugin = (DOVisualActionPlugin)sortedPData.plugin;
                                    DOVisualActionPlugin prevActionPlugin = _actionPlugins[j];
                                    ConditionalResetClipElement(clipElement, actionPlugin, prevActionPlugin, prevPlugDataGuid, prevPlugDataIndex);
                                    break;
                                }
                            }
                        }
                        Init(true);
                        DOTweenClipTimeline.Dispatch_OnClipChanged(clip);
                        if (collapseUndoBy != 0) {
                            // HACK menu after menu solution part 2
                            Undo.CollapseUndoOperations(Undo.GetCurrentGroup() + collapseUndoBy);
                        }
                    }
                );
            }
        }

        void CM_CopyValuesFromHierarchyTarget(bool isFrom)
        {
            GenericMenu menu = new GenericMenu();
            bool canCopyValuesFromTarget = CanCopyValuesFromSelectedTarget();
            PlugDataTween plugData = _mainTweenPlugin.GetPlugData(_mainClipElement) as PlugDataTween;
            if (canCopyValuesFromTarget) {
                menu.AddItem(new GUIContent("Assign from Selected GameObject"), false, () => {
                    bool set = false;
                    using (new DOScope.UndoableSerialization()) {
                        foreach (DOTweenClipElement s in _clipElements) {
                            switch (plugData.label) {
                            case "AnchoredPosition":
                                set = true;
                                if (isFrom) s.fromVector2Val = Selection.activeTransform.GetComponent<RectTransform>().anchoredPosition;
                                else s.toVector2Val = Selection.activeTransform.GetComponent<RectTransform>().anchoredPosition;
                                break;
                            case "AnchoredPosition 3D":
                                set = true;
                                if (isFrom) s.fromVector3Val = Selection.activeTransform.GetComponent<RectTransform>().anchoredPosition3D;
                                else s.toVector3Val = Selection.activeTransform.GetComponent<RectTransform>().anchoredPosition3D;
                                break;
                            case "Position":
                                set = true;
                                if (isFrom) s.fromVector3Val = Selection.activeTransform.position;
                                else s.toVector3Val = Selection.activeTransform.position;
                                break;
                            case "Local Position":
                                set = true;
                                if (isFrom) s.fromVector3Val = Selection.activeTransform.localPosition;
                                else s.toVector3Val = Selection.activeTransform.localPosition;
                                break;
                            case "Rotation":
                                set = true;
                                if (isFrom) s.fromVector3Val = Selection.activeTransform.eulerAngles;
                                else s.toVector3Val = Selection.activeTransform.eulerAngles;
                                break;
                            case "Local Rotation":
                                set = true;
                                if (isFrom) s.fromVector3Val = Selection.activeTransform.localEulerAngles;
                                else s.toVector3Val = Selection.activeTransform.localEulerAngles;
                                break;
                            case "Scale":
                                set = true;
                                if (isFrom) s.fromVector3Val = Selection.activeTransform.localScale;
                                else s.toVector3Val = Selection.activeTransform.localScale;
                                break;
                            }
                            if (set) {
                                if (isFrom && s.fromType != DOTweenClipElement.ToFromType.Direct) s.fromType = DOTweenClipElement.ToFromType.Direct;
                                else if (!isFrom && s.toType != DOTweenClipElement.ToFromType.Direct) s.toType = DOTweenClipElement.ToFromType.Direct;
                            } 
                        }
                        if (!set) Debug.LogWarning("Nothing could be set: this shouldn't happen");
                    }
                    DOTweenClipTimeline.Dispatch_OnClipChanged(clip);
                });
            } else {
                menu.AddDisabledItem(new GUIContent("Assign from Selected GameObject (nothing valid selected)"));
            }
            menu.ShowAsContext();
        }

        #endregion

        #endregion

        #region Methods

        void CacheCurrTargets()
        {
            _currTargets.Clear();
            for (int i = 0; i < _totClipElements; ++i) _currTargets.Add(_clipElements[i].target);
        }

        void RestoreCurrTargets()
        {
            for (int i = 0; i < _totClipElements; ++i) _clipElements[i].target = _currTargets[i];
        }

        bool CanHaveEase()
        {
            switch (_mainTweenPlugin.GetPlugData(_mainClipElement).tweenType) {
            case PluginTweenType.Punch:
            case PluginTweenType.Shake:
                return false;
            default: return true;
            }
        }

        bool CanHaveFrom(DOTweenClipElement clipElement, DOVisualTweenPlugin plugin)
        {
            // switch (_mainTweenPlugin.GetPlugData(_mainClipElement).tweenType) {
            switch (plugin.GetPlugData(clipElement).tweenType) {
            case PluginTweenType.Punch:
            case PluginTweenType.Shake:
                return false;
            default: return true;
            }
        }

        // Assumes all clipElements have same-type plugins
        bool CanCopyValuesFromSelectedTarget()
        {
            if (!_isTweener || _isGlobal || Selection.activeTransform == null) return false;
            Type pluginTargetType = _mainTweenPlugin.targetType;
            if (pluginTargetType == typeof(RectTransform)) {
                return !_mainTweenPlugin.GetPlugData(_mainClipElement).label.Contains("AnchoredPosition")
                       || Selection.activeTransform.GetComponent<RectTransform>() != null;
            } else if (pluginTargetType == typeof(Transform)) return true;
            return false;
        }

        void ConditionalResetClipElement(
            DOTweenClipElement clipElement, DOVisualTweenPlugin plugin, DOVisualTweenPlugin prevPlugin,
            string prevPlugDataGuid, int prevPlugDataIndex, bool forceReset = false
        ){
            clipElement.toStringVal = clipElement.fromStringVal = null; // Reset strings to free up memory
            clipElement.objOption = null; // Reset obj to free up memory
            clipElement.axisConstraint = AxisConstraint.None; // Reset axis constraint (rotation doesn't use them)
            if (!CanHaveFrom(clipElement, plugin)) {
                clipElement.toType = DOTweenClipElement.ToFromType.Direct;
                clipElement.fromType = DOTweenClipElement.ToFromType.Dynamic;
            }
            PluginTweenType tweenType = plugin.GetPlugData(clipElement).tweenType;
            if (forceReset || prevPlugin != null && tweenType != prevPlugin.GetPlugData(prevPlugDataGuid, prevPlugDataIndex).tweenType) {
                switch (tweenType) {
                case PluginTweenType.Punch:
                case PluginTweenType.Shake:
                    clipElement.isRelative = false;
                    clipElement.boolOption0 = false;
                    clipElement.intOption0 = 0;
                    clipElement.intOption1 = 10;
                    clipElement.floatOption0 = tweenType == PluginTweenType.Shake ? 90 : 1;
                    clipElement.ease = Ease.Linear;
                    break;
                }
                clipElement.stringOption0 = null;
            }
        }
        void ConditionalResetClipElement(
            DOTweenClipElement clipElement, DOVisualActionPlugin plugin, DOVisualActionPlugin prevPlugin, string prevPlugDataGuid, int prevPlugDataIndex
        ){
            PlugDataAction plugData = plugin.GetPlugData(clipElement);
            PlugDataAction prevPlugData = null;
            bool resetTarget;
            if (prevPlugin == null) resetTarget = true;
            else {
                prevPlugData = prevPlugin.GetPlugData(prevPlugDataGuid, prevPlugDataIndex);
                resetTarget = prevPlugData == null
                              || prevPlugData.wantsTarget != plugData.wantsTarget || prevPlugData.targetType != plugData.targetType;
            }
            if (resetTarget) clipElement.target = null;
            if (prevPlugData == null || prevPlugData.boolOptionLabel != plugData.boolOptionLabel) clipElement.boolOption0 = plugData.defBoolValue;
            if (prevPlugData == null || prevPlugData.stringOptionLabel != plugData.stringOptionLabel) clipElement.toStringVal = plugData.defStringValue;
            if (prevPlugData == null || prevPlugData.float0OptionLabel != plugData.float0OptionLabel) clipElement.toFloatVal = plugData.defFloat0Value;
            if (prevPlugData == null || prevPlugData.float1OptionLabel != plugData.float1OptionLabel) clipElement.fromFloatVal = plugData.defFloat1Value;
            if (prevPlugData == null || prevPlugData.float2OptionLabel != plugData.float2OptionLabel) clipElement.floatOption0 = plugData.defFloat2Value;
            if (prevPlugData == null || prevPlugData.float3OptionLabel != plugData.float3OptionLabel) clipElement.floatOption1 = plugData.defFloat3Value;
            if (prevPlugData == null || prevPlugData.intOptionLabel != plugData.intOptionLabel) clipElement.toIntVal = plugData.defIntValue;
            if (prevPlugData == null || prevPlugData.objOptionLabel != plugData.objOptionLabel) clipElement.objOption = null;
        }

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        public struct EaseSnapshot
        {
            public Ease ease;
            public float overshootOrAmplitude, period;

            public EaseSnapshot(Ease ease, float overshootOrAmplitude, float period)
            {
                this.ease = ease;
                this.overshootOrAmplitude = overshootOrAmplitude;
                this.period = period;
            }

            public static bool operator ==(EaseSnapshot a, EaseSnapshot b)
            {
                return a.ease == b.ease
                       && Mathf.Approximately(a.overshootOrAmplitude, b.overshootOrAmplitude)
                       && Mathf.Approximately(a.period, b.period);
            }
            public static bool operator !=(EaseSnapshot a, EaseSnapshot b)
            {
                return a.ease != b.ease
                       || !Mathf.Approximately(a.overshootOrAmplitude, b.overshootOrAmplitude)
                       || !Mathf.Approximately(a.period, b.period);
            }

            public override bool Equals(object obj)
            { return base.Equals(obj); }
            public override int GetHashCode()
            { return base.GetHashCode(); }
        }

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        class SortedPlugData
        {
            public IPlugin plugin;
            public string pluginId;
            public IPluginData data;
            public int originalIndex;
            public SortedPlugData(IPlugin plugin, string pluginId, IPluginData pluginData, int originalIndex)
            {
                this.plugin = plugin;
                this.pluginId = pluginId;
                this.data = pluginData;
                this.originalIndex = originalIndex;
            }
        }
    }
}