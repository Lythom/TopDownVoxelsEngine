// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/15
#nullable disable

using System;
using System.Collections;
using DG.Tweening.Timeline;
using UnityEngine;

namespace DG.Tweening.TimelineExamples
{
    public class ClipVariant : MonoBehaviour
    {
        public DOTweenClipVariant camilleVariant;
        public DOTweenClipVariant skaterVariant;

        void Start()
        {
            // Generate the variants tweens.
            // Notice that the original clip (Anthony) has a startup delay of 1 second,
            // which will be applied to the variants too
            camilleVariant.Play();
            skaterVariant.Play();
        }

        void OnDestroy()
        {
            // Kill the tweens when this component is destroyed
            camilleVariant.tween.Kill(); // No need to check for NULL, it's extensions magic
            skaterVariant.tween.Kill(); // No need to check for NULL, it's extensions magic
        }
    }
}