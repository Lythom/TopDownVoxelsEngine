using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TinkState;
using TMPro;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine.UI {

    public class ToolPreview : ConnectedBehaviour {
        public int QueueElementOffset = 0;

        [Title("Bindings")]
        public TextMeshProUGUI? Text;

        [Required]
        public RawImage Sprite = null!;

        public ToolPreview? PrevTool;
        public ToolPreview? NextTool;

        private string _lastToolName = "";
        private Vector3 _targetPos;
        private float _targetScale;

        private void Awake() {
            _targetPos = transform.localPosition;
            _targetScale = transform.localScale.x;
        }

        protected override void OnSetup(GameState state, Selectors selectors) {
            Observable.AutoRun(() => {
                var toolsQueue = selectors.ToolQueue;
                if (toolsQueue.Count == 0) return;

                var offset = QueueElementOffset;
                while (offset < 0) offset += toolsQueue.Count;
                offset %= toolsQueue.Count;
                var tool = toolsQueue[offset];
                
                if (Text is not null) Text.SmartActive(tool != null);
                Sprite.SmartActive(tool != null);
                if (tool == null) return;

                // if selecting next tool, animate from there
                var nextToolId = (offset + 1) % toolsQueue.Count;
                var nextTool = toolsQueue[nextToolId];
                // last displayed tool became next tool, it means we moved selection forward → animate from Previous
                if (_lastToolName == nextTool.Name) {
                    if (PrevTool is not null) {
                        DOTween.Kill(this);
                        transform.localPosition = PrevTool._targetPos;
                        transform.localScale = PrevTool._targetScale * Vector3.one;
                        transform.DOLocalMove(_targetPos, 0.2f).SetTarget(this);
                        transform.DOScale(_targetScale, 0.2f).SetTarget(this);
                    } else {
                        // no previous ? It's the beggining and we went forward, just pop the thing
                        DOTween.Kill(this);
                        transform.localPosition = _targetPos;
                        transform.localScale = Vector3.zero;
                        transform.DOScale(_targetScale, 0.2f).SetTarget(this);
                    }
                } else {

                    var prevToolId = offset - 1 < 0 ? toolsQueue.Count - 1 : offset - 1;
                    var prevTool = toolsQueue[prevToolId];
                    // last displayed tool became previous tool, it means we moved selection backward → animate from Next
                    if (_lastToolName == prevTool.Name) {
                        if (NextTool is not null) {
                            // regular anim
                            DOTween.Kill(this);
                            transform.localPosition = NextTool._targetPos;
                            transform.localScale = NextTool._targetScale * Vector3.one;
                            transform.DOLocalMove(_targetPos, 0.2f).SetTarget(this);
                            transform.DOScale(_targetScale, 0.2f).SetTarget(this);
                        } else {
                            // no next ? It's the end and we went backward, just pop the thing
                            DOTween.Kill(this);
                            transform.localPosition = _targetPos;
                            transform.localScale = Vector3.zero;
                            transform.DOScale(_targetScale, 0.2f).SetTarget(this);
                        }
                    }
                }

                if (Text is not null) Text.text = $"<+spread>{tool.Name}";
                Sprite.texture = tool.Sprite;
                _lastToolName = tool.Name;
            }).AddTo(ResetToken);
        }
    }
}