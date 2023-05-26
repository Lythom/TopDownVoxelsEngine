// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/02/01

using UnityEngine;
#if true // UI_MARKER
using UnityEngine.UI;
#endif

#pragma warning disable CS1522 // Empty switch block (can be caused by disabling of DOTween modules
namespace DG.Tweening.Timeline.Core.Plugins
{
    public enum ActivationMode
    {
        Target,
        TargetAndFirstLevelChildren,
        TargetAndAllChildren,
        FirstLevelChildren
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    static class DefaultActionPlugins
    {
#if UNITY_EDITOR
        static DefaultActionPlugins()
        {
            // Used only to register plugins to be displayed in editor's timeline (runtime uses Register method directly)
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            DOVisualPluginsManager.RegisterActionPlugins(GetActionPlugin, "DOTweenDefaultActions");
#if true // UI_MARKER
            DOVisualPluginsManager.RegisterActionPlugins(GetActionPlugin, "DOTweenDefaultUIActions");
#endif
        }

        static DOVisualActionPlugin GetActionPlugin(string id)
        {
            switch (id) {
            case "DOTweenDefaultActions":
                return DOVisualPluginsManager.CacheAndReturnAction(id,
                    // DOTween --------------------------------------------------
                    new PlugDataAction("f9bdfc60-1b58-49b0-9447-c980f3bf2fb2", "DOTween/All Tweens/Kill", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DOTween.KillAll()),
                    new PlugDataAction("c66265c2-fe5a-4246-9d5c-16c14e33821a", "DOTween/All Tweens/Rewind", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DOTween.RewindAll()),
                    new PlugDataAction("66f622c5-e200-4a15-90b0-a300e0af65b2", "DOTween/All Tweens/Complete", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DOTween.CompleteAll()),
                    new PlugDataAction("0ae7ce1f-ab19-4eb5-b9df-074c2739b4c6", "DOTween/All Tweens/Restart", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DOTween.RestartAll()),
                    // -
                    new PlugDataAction("8ad01378-5009-4e8f-b93d-1fed7f0e209b", "DOTween/Tweens By Id/Kill", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Kill(s, b); },
                        stringOptionLabel:"ID", boolOptionLabel:"And Complete"),
                    new PlugDataAction("e7807b8a-4ca6-4e60-bc82-96a5fdbeed1f", "DOTween/Tweens By Id/Rewind", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Rewind(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("68d2218e-b496-4f3f-960a-c869362491ec", "DOTween/Tweens By Id/Complete", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Complete(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("ff7e2443-fcc5-486e-8a2f-982a7ac0487a", "DOTween/Tweens By Id/Restart", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Restart(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("74d18085-0e19-4c98-a0cd-9c0094c66290", "DOTween/Tweens By Id/Play", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Play(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("89ebf400-c994-45ff-ac41-dbe22717984a", "DOTween/Tweens By Id/Pause", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Pause(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("de9cd77f-daa2-49b9-8c34-5d9e12091608", "DOTween/Tweens By Id/Play Forward", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.PlayForward(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("4be7e067-b1f0-4a1a-bc7b-bce4727a7328", "DOTween/Tweens By Id/Play Backwards", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.PlayBackwards(s); },
                        stringOptionLabel:"ID"),
                    new PlugDataAction("0fe2ad17-4305-404e-abdf-0015e9d024bb", "DOTween/Tweens By Id/Flip", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => { if (!string.IsNullOrEmpty(s)) DOTween.Flip(s); },
                        stringOptionLabel:"ID"),
                    // DOTweenClipComponent + DOTweenClipVariant -----------------------
                    new PlugDataAction("a5d77503-28de-462e-b3f7-03f553a2d2f0", "DOTweenClip/. Generate + Play", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).Play(b),
                        defBoolValue: true, boolOptionLabel: "Restart If Already Generated"),
                    new PlugDataAction("48c644bc-bf49-47ac-8e9b-42bcf28ee13c", "DOTweenClip/Kill", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).KillTween(b),
                        boolOptionLabel:"And Complete"),
                    new PlugDataAction("88b618c8-4e49-4167-b28a-a69e872bcd78", "DOTweenClip/Rewind", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).RewindTween()),
                    new PlugDataAction("750a46b9-6d72-47be-92bf-ccbea635d452", "DOTweenClip/Complete", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).CompleteTween()),
                    new PlugDataAction("7e8c05a3-a380-4a16-ab53-69d20ec805d6", "DOTweenClip/Restart", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).RestartTween()),
                    new PlugDataAction("d3c0d910-9bf6-4805-ae59-08b4864dc86d", "DOTweenClip/Restart From Here", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).RestartTweenFromHere()),
                    new PlugDataAction("f6c5fa15-9d15-471e-9111-f3ed71434769", "DOTweenClip/Play", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).PlayTween()),
                    new PlugDataAction("f8fb3cc5-8340-4b2f-b320-90c73ef002d7", "DOTweenClip/Pause", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).PauseTween()),
                    new PlugDataAction("b3acf812-e467-4fe9-bd4c-99ffe45e0b7f", "DOTweenClip/Play Forward", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).PlayTweenForward()),
                    new PlugDataAction("1cbaa66f-7021-4deb-853e-0fd6aa7350ce", "DOTweenClip/Play Backwards", typeof(DOTweenClipComponentBase),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipComponentBase)t).PlayTweenBackwards()),
                    // DOTweenClipCollection -------------------------------------------
                    new PlugDataAction("0a526438-d6fc-4c5b-8567-09abed97e9c0", "DOTweenClipCollection/All in Collection/. Generate + Play", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).PlayAll(b),
                        defBoolValue: true, boolOptionLabel: "Restart If Already Generated"),
                    new PlugDataAction("0adc7f56-fe52-416e-bd47-44d9c98377c7", "DOTweenClipCollection/All in Collection/Kill", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).KillAllTweens(b),
                        boolOptionLabel:"And Complete"),
                    new PlugDataAction("522d7202-d1e7-4a7d-8518-ed6450126825", "DOTweenClipCollection/All in Collection/Rewind", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).RewindAllTweens()),
                    new PlugDataAction("2d6fd991-8ff4-4ee6-9491-0ca8893818d2", "DOTweenClipCollection/All in Collection/Complete", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).CompleteAllTweens()),
                    new PlugDataAction("844a76b4-ed51-4637-8dae-9e12c48851ae", "DOTweenClipCollection/All in Collection/Restart", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).RestartAllTweens()),
                    new PlugDataAction("e64192d2-1dad-4678-b9d7-5251d9261f47", "DOTweenClipCollection/All in Collection/Restart From Here", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).RestartAllTweensFromHere()),
                    new PlugDataAction("63f40333-3d06-4600-8d15-2764b8482ed8", "DOTweenClipCollection/All in Collection/Play", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).PlayAllTweens()),
                    new PlugDataAction("ff9600c7-f2b0-4e85-8a20-e84831c0c2c5", "DOTweenClipCollection/All in Collection/Pause", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).PauseAllTweens()),
                    new PlugDataAction("0ab30b25-0b4b-4d29-9c5e-4f444246717b", "DOTweenClipCollection/All in Collection/Play Forward", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).PlayAllTweensForward()),
                    new PlugDataAction("4927cdcb-613f-40a8-9143-1a9ce8749c6f", "DOTweenClipCollection/All in Collection/Play Backwards", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenClipCollection)t).PlayAllTweensBackwards()),
                    // -
                    new PlugDataAction("0ac7cc18-2b4f-4203-abae-226351b3d4f2", "DOTweenClipCollection/Clips by Name/. Generate + Play", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.Play(b);
                        }, stringOptionLabel: "Name/ID", defBoolValue: true, boolOptionLabel: "Restart If Already Generated"),
                    new PlugDataAction("6089e494-4182-424b-93c5-9fd3e79d247b", "DOTweenClipCollection/Clips by Name/Kill", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.KillTween(b);
                        }, stringOptionLabel: "Name/ID", boolOptionLabel:"And Complete"),
                    new PlugDataAction("ca0e3b00-5900-46c6-9692-acca479e1ae1", "DOTweenClipCollection/Clips by Name/Rewind", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.RewindTween();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("85bed672-f14c-4eaa-a5f1-534184f2d795", "DOTweenClipCollection/Clips by Name/Complete", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.CompleteTween();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("d3b15264-11cb-463b-b7e8-f93aae37317e", "DOTweenClipCollection/Clips by Name/Restart", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.RestartTween();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("0e43dbab-59b7-40ba-9901-c99660b6b147", "DOTweenClipCollection/Clips by Name/Restart From Here", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.RestartTweenFromHere();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("a9e43e12-be41-45fc-9f44-82774acb5ba6", "DOTweenClipCollection/Clips by Name/Play", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.PlayTween();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("0eb03a4e-b6b1-44f1-820b-f39f545963d3", "DOTweenClipCollection/Clips by Name/Pause", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.PauseTween();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("3c9f9d3f-c00b-43a4-a5a5-d77770010d3d", "DOTweenClipCollection/Clips by Name/Play Forward", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.PlayTweenForward();
                        }, stringOptionLabel: "Name/ID"),
                    new PlugDataAction("2c480ecb-1774-43c1-9cff-7a68550287f4", "DOTweenClipCollection/Clips by Name/Play Backwards", typeof(DOTweenClipCollection),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            DOTweenClip clip = ((DOTweenClipCollection)t).GetClipByName(s);
                            if (clip != null) clip.PlayTweenBackwards();
                        }, stringOptionLabel: "Name/ID"),
                    // Debug --------------------------------------------------
                    new PlugDataAction("e79e07aa-f95c-485e-b0d4-d418cf2ab6c1", "Debug/Log", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => Debug.Log(s),
                        stringOptionLabel:"Message"),
                    // GameObject ---------------------------------------------
                    new PlugDataAction("20fcef9c-105e-4eff-a423-b16fa8d56bfc", "GameObject/Destroy", typeof(GameObject),
                        (t,b,s,o,f0,f1,f2,f3,i) => Object.Destroy((GameObject)t),
                        null, "GO"),
                    new PlugDataAction("39422273-8414-4354-b309-bb28d6d6b0e7", "GameObject/Activate", typeof(GameObject),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            if (i == 0) ((GameObject)t).SetActive(b);
                            else {
                                Transform mainTrans = ((GameObject)t).transform;
                                Transform[] allTrans = mainTrans.GetComponentsInChildren<Transform>(true);
                                switch (i) {
                                case 1:
                                    foreach (Transform trans in allTrans) {
                                        if (trans == mainTrans || trans.parent == mainTrans) trans.gameObject.SetActive(b);
                                    }
                                    break;
                                case 2:
                                    foreach (Transform trans in allTrans) trans.gameObject.SetActive(b);
                                    break;
                                case 3:
                                    foreach (Transform trans in allTrans) {
                                        if (trans.parent == mainTrans) trans.gameObject.SetActive(b);
                                    }
                                    break;
                                }
                            }
                        },
                        null, "GO", boolOptionLabel:"Activate", defBoolValue:true,
                        defIntValue: 0, intOptionLabel: "Mode", intOptionAsEnumType: typeof(ActivationMode))
                );
#if true // UI_MARKER
            case "DOTweenDefaultUIActions":
                return DOVisualPluginsManager.CacheAndReturnAction(id,
                    // UI -----------------------------------------------------
                    new PlugDataAction("066a359b-d8eb-4735-825e-b05ade7e3007", "UI Image/Set Sprite", typeof(Image),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            ((Image)t).sprite = (Sprite)o;
#if UNITY_EDITOR
                            EditorPreviewUIFixer((Component)t);
#endif
                        },
                        null, "Image", objOptionLabel: "New Sprite", objOptionType: typeof(Sprite))
                );
#endif
            }
            return null;
        }

#if true // UI_MARKER
#if UNITY_EDITOR

        static void EditorPreviewUIFixer(Component c)
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            // Fix for Unity bug where UI Texts and Images set to simple won't change color/content correctly in the editor preview
            // (simulate a movement so the editor refreshes the image)
            // https://issuetracker.unity3d.com/issues/image-color-cannot-be-changed-via-script-when-image-type-is-set-to-simple
            RectTransform rt = c.GetComponent<RectTransform>();
            Vector3 anchoredP = rt.anchoredPosition3D;
            rt.anchoredPosition3D = anchoredP + new Vector3(0, 0, 0.001f);
            rt.anchoredPosition3D = anchoredP;
        }

#endif
#endif
    }
}