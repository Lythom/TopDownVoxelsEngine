// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/02/01

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DG.Tweening.Timeline.Core.Plugins
{
    /// <summary>
    /// Contains/executes all possible action types
    /// </summary>
    public class DOVisualActionPlugin : IPlugin
    {
        public readonly PlugDataAction[] pluginDatas; // Always sorted by label in editor windows. Contains same content as _guidToPlugData
        public int totPluginDatas { get; }
        public IPluginData[] editor_iPluginDatas { get { return pluginDatas; } }

        readonly Dictionary<string, PlugDataAction> _guidToPlugData = new Dictionary<string, PlugDataAction>();

        public DOVisualActionPlugin(PlugDataAction[] pluginDatas)
        {
            this.pluginDatas = pluginDatas;
            totPluginDatas = this.pluginDatas.Length;

            for (int i = 0; i < totPluginDatas; ++i) {
                string plugDataGuid = this.pluginDatas[i].guid ?? "INVALID:" + Guid.NewGuid(); // Missing GUID, assign a unique one but marked as invalid
                if (_guidToPlugData.ContainsKey(plugDataGuid)) {
                    DOLog.Error(string.Format("Another PlugDataAction with the same guid already exists (guid: \"{0}\")", plugDataGuid));
                } else _guidToPlugData.Add(plugDataGuid, this.pluginDatas[i]);
            }
        }

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="ITweenPluginData"/> by the given clipElement plugDataGuid, and if it doesn't find it falls back on the clipElement plugDataIndex.
        /// Returns NULL if both retrieval methods fail.
        /// </summary>
        public PlugDataAction GetPlugData(DOTweenClipElement clipElement)
        {
            return GetPlugData(clipElement.plugDataGuid, clipElement.plugDataIndex);
        }
        /// <summary>
        /// Gets a <see cref="ITweenPluginData"/> by the given clipElement plugDataGuid, and if it doesn't find it falls back on the clipElement plugDataIndex.
        /// Returns NULL if both retrieval methods fail.
        /// </summary>
        public PlugDataAction GetPlugData(string plugDataGuid, int plugDataIndex)
        {
            return plugDataGuid != null && _guidToPlugData.TryGetValue(plugDataGuid, out PlugDataAction plugData)
                ? plugData
                : string.IsNullOrEmpty(plugDataGuid) && plugDataIndex < totPluginDatas ? pluginDatas[plugDataIndex] : null;
        }

        public bool HasPlugData(DOTweenClipElement clipElement)
        {
            if (!string.IsNullOrEmpty(clipElement.plugDataGuid)) return _guidToPlugData.ContainsKey(clipElement.plugDataGuid);
            return clipElement.plugDataIndex < totPluginDatas;
        }

#if UNITY_EDITOR
        Dictionary<string, GUIContent> _editor_guidToGc; // Used to cache GUIContent values
        Dictionary<string, GUIContent> _editor_guidToGc_timeline; // Used to cache GUIContent values
        GUIContent[] _editor_gcs; // Used to cache GUIContent values - fallback for legacy plugin index usage
        GUIContent[] _editor_gcs_timeline; // Used to cache GUIContent values - fallback for legacy plugin index usage
        readonly GUIContent _editor_missingPlugin = new GUIContent("<color=#ff0000>UNSUPPORTED</color>");

        public GUIContent Editor_GetClipElementHeaderLabelGUIContent(DOTweenClipElement clipElement, bool forTimeline)
        {
            if (forTimeline) {
                if (_editor_guidToGc_timeline == null) {
                    _editor_guidToGc_timeline = new Dictionary<string, GUIContent>();
                    _editor_gcs_timeline = new GUIContent[totPluginDatas];
                }
            } else {
                if (_editor_guidToGc == null) {
                    _editor_guidToGc = new Dictionary<string, GUIContent>();
                    _editor_gcs = new GUIContent[totPluginDatas];
                }
            }
            GUIContent gc = forTimeline
                ? GetGuiContent(clipElement, _editor_guidToGc_timeline, _editor_gcs_timeline)
                : GetGuiContent(clipElement, _editor_guidToGc, _editor_gcs);
            if (gc == null) {
                PlugDataAction plugData = GetPlugData(clipElement);
                // This can happen if clipElement was created with a now disabled module in legacy mode
                // (but shouldn't happen becasue if plugin is missing this method should never be called)
                if (plugData == null) return _editor_missingPlugin;
                gc = new GUIContent(Editor_GetClipElementHeaderLabel(plugData, forTimeline));
                if (forTimeline) {
                    if (!string.IsNullOrEmpty(plugData.guid))  _editor_guidToGc_timeline.Add(plugData.guid, gc);
                    _editor_gcs_timeline[clipElement.plugDataIndex] = gc;
                } else {
                    if (!string.IsNullOrEmpty(plugData.guid)) _editor_guidToGc.Add(plugData.guid, gc);
                    _editor_gcs[clipElement.plugDataIndex] = gc;
                }
            }
            return gc;
        }

        GUIContent GetGuiContent(DOTweenClipElement clipElement, Dictionary<string, GUIContent> guidToGcDict, GUIContent[] gcList)
        {
            return clipElement.plugDataGuid != null && guidToGcDict.TryGetValue(clipElement.plugDataGuid, out GUIContent gc)
                ? gc
                : clipElement.plugDataIndex < totPluginDatas ? gcList[clipElement.plugDataIndex] : null;
        }

        string Editor_GetClipElementHeaderLabel(PlugDataAction plugData, bool forTimeline)
        {
            string label = plugData.label;
            int lastSlashIndex = label.LastIndexOf('/');
            if (lastSlashIndex == -1)
                return forTimeline
                    ? string.Format("<color=#ffa047>{0}</color>", label)
                    : string.Format("<color=#ffa047>{0}</color>", label);
            return forTimeline
                ? string.Format("<color=#2e0020>{0}</color>→<color=#ffa047>{1}</color>",
                    label.Substring(0, lastSlashIndex), label.Substring(lastSlashIndex + 1)
                )
                : string.Format("<color=#d00093>{0}</color>→<color=#ffa047>{1}</color>",
                    label.Substring(0, lastSlashIndex), label.Substring(lastSlashIndex + 1)
                );
        }
#endif

        #endregion
    }
}