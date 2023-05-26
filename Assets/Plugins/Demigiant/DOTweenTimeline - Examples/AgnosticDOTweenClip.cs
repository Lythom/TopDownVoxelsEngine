// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/09/11

using DG.Tweening.Timeline;
using UnityEngine;
using UnityEngine.Serialization;

namespace DG.Tweening.TimelineExamples
{
    public class AgnosticDOTweenClip : MonoBehaviour
    {
        public DOTweenClip anthonyClip;
        public GameObject anthonyGO, camilleGO, skaterGO;

        DOTweenClip _camilleClip, _skaterClip;
        Tween _camilleTween, _skaterTween;

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // All the methods below are called via UI Button events ███████████████████████████████████████████████████████████████

        public void PlayOriginalClip()
        {
            // Generate the original clip or, if it already exists, restart it
            anthonyClip.GenerateTween();
        }

        public void ApplyToAllViaCloneAndReplace(float startupDelay)
        {
            // Kill and complete Camille and Skater tweens if they already exist
            if (_camilleTween.IsActive()) _camilleTween.Kill(true);
            if (_skaterTween.IsActive()) _skaterTween.Kill(true);
            // Clone Anthony DOTweenClip for each of the other sprites and replace the targets
            // (unless they had been cloned already)
            if (_camilleClip == null) {
                _camilleClip = anthonyClip.Clone()
                    .ReplaceTarget(anthonyGO.transform, camilleGO.transform)
                    .ReplaceTarget(anthonyGO.GetComponent<SpriteRenderer>(), camilleGO.GetComponent<SpriteRenderer>());
            }
            if (_skaterClip == null) {
                _skaterClip = anthonyClip.Clone()
                    .ReplaceTarget(anthonyGO.transform, skaterGO.transform)
                    .ReplaceTarget(anthonyGO.GetComponent<SpriteRenderer>(), skaterGO.GetComponent<SpriteRenderer>());
            }
            // Add delays
            _camilleClip.startupDelay = startupDelay;
            _skaterClip.startupDelay = startupDelay * 2;
            // Generate all
            anthonyClip.GenerateTween();
            _camilleTween = _camilleClip.GenerateTween();
            _skaterTween = _skaterClip.GenerateTween();
        }

        public void ApplyToAllViaGenerateIndependentTween(float startupDelay)
        {
            // Kill and complete Camille and Skater tweens if they already exist
            if (_camilleTween.IsActive()) _camilleTween.Kill(true);
            if (_skaterTween.IsActive()) _skaterTween.Kill(true);
            // Generate them from anthonyClip but as independent tweens (while replacing the targets)
            // This will also set the startup delay and immediately play the tweens (first parameter)
            _camilleTween = anthonyClip.GenerateIndependentTween(true, startupDelay,
                anthonyGO.transform, camilleGO.transform,
                anthonyGO.GetComponent<SpriteRenderer>(), camilleGO.GetComponent<SpriteRenderer>()
            );
            _skaterTween = anthonyClip.GenerateIndependentTween(true, startupDelay,
                anthonyGO.transform, skaterGO.transform,
                anthonyGO.GetComponent<SpriteRenderer>(), skaterGO.GetComponent<SpriteRenderer>()
            );
            // Generate the original clip or, if it already exists, restart it
            anthonyClip.GenerateTween();
        }
    }
}