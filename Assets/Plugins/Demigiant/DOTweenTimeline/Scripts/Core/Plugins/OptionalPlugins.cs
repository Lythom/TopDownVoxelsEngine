// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/02/21

using UnityEngine;
#if true // DOTWEENPRO_MARKER
using ABSAnimationComponent = DG.Tweening.Core.ABSAnimationComponent;
#endif
#if false // DEUNITYEXTENDED_MARKER
using Demigiant.DemiTools.DeUnityExtended.Components;
#endif
#if false // DEAUDIO_MARKER
using DG.DeAudio;
#endif

#pragma warning disable CS1522 // Empty switch block (can be caused by disabling of DOTween modules
namespace DG.Tweening.Timeline.Core.Plugins
{
    /// <summary>
    /// Plugins that are activated via DOTween Setup
    /// and that depend on other installed plugins
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    static class OptionalPlugins
    {
#if UNITY_EDITOR
        static OptionalPlugins()
        {
            // Used only to register plugins to be displayed in editor's timeline (runtime uses Register method directly)
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            DOVisualPluginsManager.RegisterActionPlugins(GetActionPlugin,
                "DOTweenOptional_Pro",
                "DOTweenOptional_DeAudio",
                "DOTweenOptional_DeUnityExtended"
            );
        }

        static DOVisualActionPlugin GetActionPlugin(string id)
        {
            switch (id) {
#if true // DOTWEENPRO_MARKER
            case "DOTweenOptional_Pro":
                return DOVisualPluginsManager.CacheAndReturnAction(id,
                    // DOTweenAnimation ---------------------------------------
                    new PlugDataAction("5bd7285b-b2eb-4ec9-945c-8c4d1e4e907c", "DOTweenAnimation/. Generate + Play", typeof(DOTweenAnimation),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((DOTweenAnimation)t).CreateTween(b),
                        boolOptionLabel: "Recreate If Already Exists"),
                    // DOTweenAnimation + Path --------------------------------
                    new PlugDataAction("f55e67ef-58c8-4058-bdb7-159373c8f755", "DOTweenAnimation + Path/Kill", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DOKill(b),
                        boolOptionLabel: "And Complete"),
                    new PlugDataAction("8b26712f-5fe6-4871-a3b1-fcccabd54b08", "DOTweenAnimation + Path/Rewind", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DORewind()),
                    new PlugDataAction("0e0db7ef-51ed-4ead-8056-43ec725a8949", "DOTweenAnimation + Path/Complete", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DOComplete()),
                    new PlugDataAction("55a034cc-bce6-4265-85ad-2fddee667011", "DOTweenAnimation + Path/Restart", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DORestart()),
                    new PlugDataAction("d716b3aa-a6a3-4cc5-ba68-544a8e6f70db", "DOTweenAnimation + Path/Restart From Here", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DORestart(true)),
                    new PlugDataAction("c4ed8034-02f4-4a8a-8111-97d42ddf372c", "DOTweenAnimation + Path/Play", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DOPlay()),
                    new PlugDataAction("0a92fcc6-82f7-47bc-ab53-0ad53d54f64e", "DOTweenAnimation + Path/Pause", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DOPause()),
                    new PlugDataAction("7860f877-df36-4d5c-8d05-51b7745408ff", "DOTweenAnimation + Path/Play Forward", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DOPlayForward()),
                    new PlugDataAction("9442aaf8-9ec0-4e19-a001-cd33d22c365b", "DOTweenAnimation + Path/Play Backwards", typeof(ABSAnimationComponent),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ABSAnimationComponent)t).DOPlayBackwards())
                );
#endif
#if false // DEAUDIO_MARKER
            case "DOTweenOptional_DeAudio":
                return DOVisualPluginsManager.CacheAndReturnAction(id,
                    new PlugDataAction("b2ea20e1-ca0f-41a6-8160-9b0b9553ff42", "DeAudio/Stop All", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Stop(),
                        null, "AudioClip"),
                    new PlugDataAction("6d69700f-a5bf-4ad0-9f63-a19f923ae466", "DeAudio/Stop All in Group", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Stop((DeAudioGroupId)i),
                        null, "AudioClip", intOptionLabel: "Group", intOptionAsEnumType:typeof(DeAudioGroupId)),
                    new PlugDataAction("1585e7ec-5ed2-455d-86e2-d67e8ab678b4", "DeAudio/Pause All", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Pause(),
                        null, "AudioClip"),
                    new PlugDataAction("eca9d437-c49c-470a-b47a-ee2c825d2383", "DeAudio/Pause All in Group", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Pause((DeAudioGroupId)i),
                        null, "AudioClip", intOptionLabel: "Group", intOptionAsEnumType:typeof(DeAudioGroupId)),
                    new PlugDataAction("386b9aad-1437-409d-a2dc-23e6acf8108c", "DeAudio/Resume All", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Resume(),
                        null, "AudioClip"),
                    new PlugDataAction("3ea23fa0-a013-4147-930f-0f70c2b772f8", "DeAudio/Resume All in Group", null,
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Resume((DeAudioGroupId)i),
                        null, "AudioClip", intOptionLabel: "Group", intOptionAsEnumType:typeof(DeAudioGroupId)),
                    new PlugDataAction("779883cf-c354-437a-8556-5b202fad2cf9", "DeAudio/Play Clip", typeof(AudioClip),
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.PlayFrom((DeAudioGroupId)i, (AudioClip)t, f0, f1, f2, b),
                        null, "AudioClip", intOptionLabel:"Group", intOptionAsEnumType:typeof(DeAudioGroupId), boolOptionLabel:"Loop", defBoolValue:false,
                        float0OptionLabel:"From Time", float1OptionLabel:"Volume", defFloat1Value:1, float2OptionLabel:"Pitch", defFloat2Value:1),
                    new PlugDataAction("30b1e388-6639-4507-b53e-d9e5ca89e6e8", "DeAudio/Stop Clip", typeof(AudioClip),
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.Stop((AudioClip)t),
                        null, "AudioClip"),
                    new PlugDataAction("f93423dc-b95f-4d31-b533-95afa0f5e46e", "DeAudio/FadeIn Clip", typeof(AudioClip),
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.PlayFrom((DeAudioGroupId)i, (AudioClip)t, f0, f1, f2, b).FadeFrom(0, f3),
                        null, "AudioClip", float3OptionLabel:"Duration", defFloat3Value:1.5f,
                        intOptionLabel: "Group", intOptionAsEnumType: typeof(DeAudioGroupId), boolOptionLabel:"Loop", defBoolValue:false,
                        float0OptionLabel:"From Time", float1OptionLabel:"Volume", defFloat1Value:1, float2OptionLabel:"Pitch", defFloat2Value:1),
                    new PlugDataAction("0ba1a23f-d10a-4267-a88b-640f8d71dd46", "DeAudio/FadeOut Clip", typeof(AudioClip),
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.FadeOut((AudioClip)t, f0),
                        null, "AudioClip", float0OptionLabel:"Duration", defFloat0Value:1.5f),
                    new PlugDataAction("f5160ffd-266a-4fe8-9e9a-161383925c3a", "DeAudio/Change Clip Volume", typeof(AudioClip),
                        (t,b,s,o,f0,f1,f2,f3,i) => DeAudioManager.FadeTo((AudioClip)t, f1, f0),
                        null, "AudioClip", float0OptionLabel:"Duration", defFloat0Value:1.5f, float1OptionLabel:"Volume", defFloat1Value:1f)
                );
#endif
#if false // DEUNITYEXTENDED_MARKER
            case "DOTweenOptional_DeUnityExtended":
                return DOVisualPluginsManager.CacheAndReturnAction(id,
                    new PlugDataAction("3a67211e-474c-4863-b8f9-f9c6b6bf2090", "ParticleSystemController/Deactivate", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            if (b) return;
                            ParticleSystemController psc = (ParticleSystemController)t;
                            psc.StopEmitting(true, true);
                            psc.Clear(true, true);
                        },
                        onCreation:(t,b,s,o,f0,f1,f2,f3,i) => {
                            if (!b) return;
                            ParticleSystemController psc = (ParticleSystemController)t;
                            psc.StopEmitting(true, true);
                            psc.Clear(true, true);
                        },
                        "PSC", boolOptionLabel:"Deactivate On Creation", defBoolValue:false),
                    new PlugDataAction("3c7d6a74-43da-4e81-89ad-cb010b26e802", "ParticleSystemController/Start Emitting", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ParticleSystemController)t).StartEmitting(),
                        null, "PSC"),
                    new PlugDataAction("2216e131-72c1-4d06-a6d4-ddb19a96c0bd", "ParticleSystemController/Stop Emitting", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ParticleSystemController)t).StopEmitting(true, b),
                        null, "PSC", boolOptionLabel:"And Clear", defBoolValue:false),
                    new PlugDataAction("c3b93f2e-d76f-4641-9546-e95ab8f087fd", "ParticleSystemController/Set Emission Multiplier", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ParticleSystemController)t).SetEmissionOverTimeMultiplier(f0),
                        null, "PSC", float0OptionLabel:"Multiplier", defFloat0Value:1),
                    new PlugDataAction("5ce19b1d-9498-46a3-bc5b-8e71d93a8255", "ParticleSystemController/Restart", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => ((ParticleSystemController)t).Restart(),
                        null, "PSC"),
                    new PlugDataAction("70246330-694f-4035-8dee-de5d55dfec89", "ParticleSystemController/Toggle Modules/ForceOverLifetime", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            ParticleSystemController psc = (ParticleSystemController)t;
                            ParticleSystem.ForceOverLifetimeModule module = psc.sys.forceOverLifetime;
                            module.enabled = b;
                        }, null, "PSC", boolOptionLabel:"Enabled", defBoolValue:true),
                    new PlugDataAction("f51316fb-2d7c-4c32-b25b-2c56acad8f5a", "ParticleSystemController/Toggle Modules/VelocityOverLifetime", typeof(ParticleSystemController),
                        (t,b,s,o,f0,f1,f2,f3,i) => {
                            ParticleSystemController psc = (ParticleSystemController)t;
                            ParticleSystem.VelocityOverLifetimeModule module = psc.sys.velocityOverLifetime;
                            module.enabled = b;
                        }, null, "PSC", boolOptionLabel:"Enabled", defBoolValue:true)
                );
#endif
            }
            return null;
        }
    }
}
