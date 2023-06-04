// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/17

using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    /// <summary>
    /// Shared GUIContents
    /// </summary>
    internal static class TimelineGUIContent
    {
        public static readonly GUIContent KillOnDestroy = new GUIContent("Kill Tween On Destroy",
            "If TRUE will kill all the tweens created by this component when it's destroyed" +
            " (except for the ones that were created via GenerateIndependentTween)." +
            "\nIMPORTANT: The OnDestroy behaviour will work only if this component has been active in the Scene" +
            " (OnDestroy is never called if a component's GameObject was never activated)."
        );
    }
}