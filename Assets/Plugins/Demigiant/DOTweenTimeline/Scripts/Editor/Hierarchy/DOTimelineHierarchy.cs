// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/10/09

using System.Collections.Generic;
using DG.DemiEditor;
using DG.DOTweenEditor.UI;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DG.Tweening.TimelineEditor.Hierarchy
{
    [InitializeOnLoad]
    internal static class DOTimelineHierarchy
    {
        static DOTimelineEditorSettings _settings;
        static bool _connectedToSettings;
        static readonly Dictionary<int,bool> _InstanceIdToHasClipComponent = new Dictionary<int, bool>();

        static DOTimelineHierarchy()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Refresh();
        }

        static void OnHierarchyChanged()
        {
            Refresh();
        }

        static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            ConnectToSettings();
            if (!_settings.evidenceClipComponentInHierarchy) return;
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null) return;
            if (HasClipComponent(instanceId, go)) {
                Rect icoR = RectExtensions.SetCenterY(new Rect((int)(selectionRect.x) + 3, selectionRect.y, 9, 9), (int)(selectionRect.center.y));
                using (new DeGUI.ColorScope(null, null, go.activeInHierarchy ? Color.white : Color.white.SetAlpha(0.35f))) {
                    GUI.DrawTexture(icoR, EditorGUIUtils.miniIcon);
                }
            }
        }

        static void ConnectToSettings()
        {
            if (_connectedToSettings) return;
            _connectedToSettings = true;
            _settings = DOTimelineEditorSettings.Load();
        }

        public static void Refresh()
        {
            _InstanceIdToHasClipComponent.Clear();
        }

        static bool HasClipComponent(int instanceId, GameObject go)
        {
            if (!_InstanceIdToHasClipComponent.ContainsKey(instanceId)) {
                if (go == null) _InstanceIdToHasClipComponent.Add(instanceId, false);
                else {
                    bool hasClipComponent = go.GetComponent<DOTweenClipComponentBase>() != null
                        || go.GetComponent<DOTweenClipCollection>() != null;
                    if (!hasClipComponent && _settings.evidenceClipInHierarchy) {
                        // Look for clips in custom classes
                        Component[] comps = go.GetComponents<Component>();
                        int len = comps.Length;
                        for (int i = len - 1; i > 0; --i) { // Ignore first component because it's always a Transform or RectTransform
                            if (comps[i] == null) continue;
                            if (comps[i].GetType().FullName.StartsWith("UnityEngine.")) continue; // Ignore Unity components
                            if (!TimelineEditorUtils.ComponentContainsSerializedClip(comps[i])) continue;
                            hasClipComponent = true;
                            break;
                        }
                    }
                    _InstanceIdToHasClipComponent.Add(instanceId, hasClipComponent);
                }
            }
            return _InstanceIdToHasClipComponent[instanceId];
        }
    }
}