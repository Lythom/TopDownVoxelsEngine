// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/08/07

using DG.Tweening.Timeline.Core;
using UnityEditor;

namespace DG.Tweening.TimelineEditor
{
    /// <summary>
    /// Session-only editor data
    /// </summary>
    [InitializeOnLoad]
    internal static class TimelineSession
    {
        public static bool isDevDebugMode {
            get { return _showClipGuid || _showClipElementPlugDataIndexAndGuid || _logMissingPlugDataGuidAssignment || _logUndoRedoEvents; }
        }

        public static bool showClipGuid {
            get { return _showClipGuid; }
            set { SetDebugModeSessionVar("ShowClipGuid", value, ref _showClipGuid); }
        }
        static bool _showClipGuid;

        public static bool showClipElementPlugDataIndexAndGuid {
            get { return _showClipElementPlugDataIndexAndGuid; }
            set { SetDebugModeSessionVar("ShowClipElementsPlugDataIndexAndGuid", value, ref _showClipElementPlugDataIndexAndGuid); }
        }
        static bool _showClipElementPlugDataIndexAndGuid;

        public static bool logMissingPlugDataGuidAssignment {
            get { return _logMissingPlugDataGuidAssignment; }
            set { SetDebugModeSessionVar("LogMissingPlugDataGuidAssignment", value, ref _logMissingPlugDataGuidAssignment); }
        }
        static bool _logMissingPlugDataGuidAssignment;

        public static bool logUndoRedoEvents {
            get { return _logUndoRedoEvents; }
            set { SetDebugModeSessionVar("LogUndoRedoEvents", value, ref _logUndoRedoEvents); }
        }
        static bool _logUndoRedoEvents;

        const string _Prefix = "DOTweenTimelineDebugMode";

        static TimelineSession()
        {
            _showClipGuid = SessionState.GetBool(_Prefix + "ShowClipGuid", false);
            _showClipElementPlugDataIndexAndGuid = SessionState.GetBool(_Prefix + "ShowClipElementsPlugDataIndexAndGuid", false);
            _logMissingPlugDataGuidAssignment = SessionState.GetBool(_Prefix + "LogMissingPlugDataGuidAssignment", false);
            _logUndoRedoEvents = SessionState.GetBool(_Prefix + "LogUndoRedoEvents", false);
        }

        #region Public Methods

        public static void ToggleAll(bool toggleOn)
        {
            showClipGuid = toggleOn;
            showClipElementPlugDataIndexAndGuid = toggleOn;
            logMissingPlugDataGuidAssignment = toggleOn;
            logUndoRedoEvents = toggleOn;
        }

        #endregion

        #region Methods

        static void SetDebugModeSessionVar(string id, bool value, ref bool var)
        {
            if (value == var) return;
            bool wasDevDebugModeActive = isDevDebugMode;
            SessionState.SetBool(_Prefix + id, value);
            var = value;
            if (wasDevDebugModeActive != isDevDebugMode) {
                DOLog.DebugDev("Developer Debug Mode ► " + (isDevDebugMode ? "<color=#00ff00>ACTIVATED</color>" : "<color=#ff0000>DEACTIVATED</color>"));
            }
        }

        #endregion

    }
}