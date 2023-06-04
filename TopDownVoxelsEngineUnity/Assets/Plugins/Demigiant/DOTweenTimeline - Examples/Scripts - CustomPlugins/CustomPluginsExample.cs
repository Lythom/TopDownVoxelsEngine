using System;
using DG.Tweening.Timeline.Core.Plugins;
using UnityEngine;
#nullable disable

/// <summary>
/// Copy paste this code to create a custom plugin (including all #if etc which are very important)
/// then change the code inside GetTweenPlugin to add your own custom plugins for your own custom Components.
/// SUPER IMPORTANT: every plugin is composed of multiple sub-plugins (one for each property you want to include).
/// ALL SUB-PLUGIN MUST HAVE A UNIQUE ID because it's used to distinguish, retrieve and cache them
/// (it can be just an string-integer as long as you make sure that that integer is never repeated)
/// </summary>
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
// Class name is not important, you can refactor it to whatever you prefer
public static class CustomPluginsExample
{
#if UNITY_EDITOR
    static CustomPluginsExample()
    {
        // Needed to register custom plugins for the editor's timeline (runtime uses Register method directly)
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) Register();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        // Registers custom plugins for runtime usage
        DOVisualPluginsManager.RegisterTweenPlugins(GetTweenPlugin);
    }
    
    // Create target-based tweens here
    static DOVisualTweenPlugin GetTweenPlugin(Type targetType, string targetTypeFullName)
    {
        // ██████████████████████████████████████████████████████████████
        // CUSTOMIZATION START ██████████████████████████████████████████

        // A plugin (and an IF) for each target type. Only one plugin per target type is allowed.

        // First target type
        if (targetType == typeof(CustomComponentA)) {
            return DOVisualPluginsManager.CacheAndReturn(targetType,
                // This first one is expanded so I can comment it and explain things clearer, while the others are one-liners
                new PlugDataTween(
                    // Unique ID used to assign and retrieve this sub-plugin.
                    // If you change it you will lose all references to this sub-plugin in all your DOTweenClips,
                    // so be sure you make it unique when you write it the first time and then avoid changing it.
                    // - You can set it to a GUID, like "4c65d6f3-e73f-4cb5-b8e1-bd196be4322b".
                    //   Here's the nifty GUID generator for Visual Studio that I use:
                    //   https://marketplace.visualstudio.com/items?itemName=MadsKristensen.insertguid
                    // - You can set it to the name of the property you're adding it for,
                    //   like "sampleFloat" in this case. Just be sure it's unique
                    // - You can set it to any string, really. Again, as long as it's unique
                    "4c65d6f3-e73f-4cb5-b8e1-bd196be4322b",
                    // Label displayed in the clip element's editor. You can change this at any time, it's just decoration
                    "sampleFloat",
                    // Getter for the property to tween (similar to DOTween.To getters),
                    // retrieves the current value of the property
                    (c,s,i) => ()=> ((CustomComponentA)c).sampleFloat,
                    // Setter for the property to tween (similar to DOTween.To setters),
                    // sets the current value of the property
                    (c,s,i) => x => ((CustomComponentA)c).sampleFloat = x
                ),
                new PlugDataTween("5cabee8c-a90a-4e03-b861-58c8b5aca3b6", "sampleVector2", (c,s,i) => ()=> ((CustomComponentA)c).sampleVector2, (c,s,i) => x => ((CustomComponentA)c).sampleVector2 = x),
                new PlugDataTween("71c78ae4-8976-48cd-8c94-793b1e454b5e", "sampleString", (c,s,i) => ()=> ((CustomComponentA)c).sampleString, (c,s,i) => x => ((CustomComponentA)c).sampleString = x)
            );
        }

        // Second target type
        if (targetType == typeof(CustomComponentB)) {
            return DOVisualPluginsManager.CacheAndReturn(targetType,
                new PlugDataTween("e6f9edc0-1d2f-4883-8fa8-ab3fb5c8e368", "sampleVector3", (c,s,i) => ()=> ((CustomComponentB)c).sampleVector3, (c,s,i) => x => ((CustomComponentB)c).sampleVector3 = x),
                new PlugDataTween("ce9f06a4-17ba-45ed-bc10-49ab10cbd9a4", "sampleColor", (c,s,i) => ()=> ((CustomComponentB)c).sampleColor, (c,s,i) => x => ((CustomComponentB)c).sampleColor = x)
            );
        }

        // CUSTOMIZATION END ████████████████████████████████████████████
        // ██████████████████████████████████████████████████████████████
    
        return null;
    }
}