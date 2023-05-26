// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/08/17

using DG.DemiEditor;
using DG.DOTweenEditor;
using DG.DOTweenEditor.UI;
using DG.Tweening.Timeline.Core;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{

    /// <summary>
    /// Used to react to DOTween Utility Panel commands
    /// </summary>
    [InitializeOnLoad]
    static class DOTweenToTimelineBridge
    {
        static DOTweenToTimelineBridge()
        {
            EditorUtils.OnRequestDOTweenTimelineVersion += OnRequestDOTweenTimelineVersion;
            DOTweenUtilityWindow.OnRequestDOTweenTimelineMissingScriptsFix += OnRequestDOTweenTimelineMissingScriptsFix;
        }

        static string OnRequestDOTweenTimelineVersion()
        {
            return DOTweenTimelineSettings.Version;
        }

        static void OnRequestDOTweenTimelineMissingScriptsFix(bool currentSceneOnly)
        {
            string clipCollectionGuid = DeEditorMetaFixer.RetrieveMetaGuid(TimelinePaths.Sys.DOTweenClipCollectionMeta);
            if (clipCollectionGuid == null) {
                Debug.LogWarning("OnRequestDOTweenTimelineMissingScriptsFix ► GUID for DOTweenClipCollection.cs couldn't be found");
                return;
            }
            DeEditorMetaFixer.ComponentData[] cDatas = new[] {
                new DeEditorMetaFixer.ComponentData("DOTweenClipCollection", clipCollectionGuid,
                    "clips", "autokill", "ignoreTimeScale", "durationOverload", "elements", "pin", "_guid", "easeCurve"
                ),
            };
            if (currentSceneOnly) DeEditorMetaFixer.FixComponentsGuidsInActiveScene(cDatas);
            else DeEditorMetaFixer.FixComponentsGuidsInAllScenesAndPrefabs(cDatas);
        }
    }
}