using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine.UI {
    public class Toolbelt : ConnectedBehaviour {

        [Required]
        public CanvasGroup CanvasGroup = null!;

        private void Awake() {
            CanvasGroup.alpha = 0;
        }

        protected override void OnSetup(GameState state, Selectors selectors) {
            LocalState.Instance.Session.Bind(session => {
                DOTween.Kill(this);
                if (session is SessionStatus.Ready) CanvasGroup.DOFade(1, 2f).SetTarget(this);
                else CanvasGroup.alpha = 0;
            }).AddTo(ResetToken);
        }
    }
}