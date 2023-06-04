// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/17
#nullable disable

using DG.Tweening.Timeline;
using UnityEngine;

namespace DG.Tweening.TimelineExamples
{
    public class CustomComponentWithClip : MonoBehaviour
    {
        public DOTweenClip myCustomClip;

        void Start()
        {
            // Since it's a custom component, it will be up to us to generate the tween on startup (if we want to)
            myCustomClip.GenerateTween();
        }

        void OnDestroy()
        {
            // Kill the tween when this component is destroyed
            myCustomClip.tween.Kill(); // No need to check for NULL, it's extensions magic
        }
    }
}