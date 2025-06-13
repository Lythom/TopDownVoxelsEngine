using Cysharp.Threading.Tasks;
using DG.Tweening;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TinkState;
using TMPro;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class BlockPreview : ConnectedBehaviour {
        public int QueueElementOffset = 0;

        [Title("Bindings")]
        public TextMeshProUGUI? Text;

        [Required]
        public RawImage Sprite = null!;

        public BlockPreview? PrevBlock;
        public BlockPreview? NextBlock;

        private BlockId _lastBlock = BlockId.Air;
        private Vector3 _targetPos;
        private float _targetScale;

        private void Awake() {
            _targetPos = transform.localPosition;
            _targetScale = transform.localScale.x;
        }

        protected override void OnSetup(GameState state, Selectors selectors) {
            Observable.AutoRun(() => {
                var tool = Selectors.SelectedTool.Value;
                var isActive = tool?.Purpose is PlayerToolPurpose.PlaceBlock;
                this.SmartActive(isActive);
                if (!isActive) return;

                var blocksQueue = selectors.BlockQueue;
                if (blocksQueue.Count == 0) {
                    this.SmartActive(false);
                    return;
                }

                var offset = QueueElementOffset;
                while (offset < 0) offset += blocksQueue.Count;
                offset %= blocksQueue.Count;
                var block = blocksQueue[offset];
                if (block == BlockId.Air) block++;

                // if selecting the next block, animate from there
                var nextBlockId = (offset + 1) % blocksQueue.Count;
                var nextBlock = blocksQueue[nextBlockId];
                if (nextBlock == BlockId.Air) nextBlock++;
                // last displayed block became next block, it means we moved selection forward → animate from Previous
                if (_lastBlock == nextBlock) {
                    if (PrevBlock is not null) {
                        DOTween.Kill(this);
                        transform.localPosition = PrevBlock._targetPos;
                        transform.localScale = PrevBlock._targetScale * Vector3.one;
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
                    var prevBlockId = offset - 1 < 0 ? blocksQueue.Count - 1 : offset - 1;
                    var prevBlock = blocksQueue[prevBlockId];
                    if (prevBlock == BlockId.Air) prevBlock = blocksQueue[^1];
                    // last displayed block became previous block, it means we moved selection backward → animate from Next
                    if (_lastBlock == prevBlock) {
                        if (NextBlock is not null) {
                            // regular anim
                            DOTween.Kill(this);
                            transform.localPosition = NextBlock._targetPos;
                            transform.localScale = NextBlock._targetScale * Vector3.one;
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

                var blockPath = state.BlockPathById[block.Id];
                if (blockPath != null && Configurator.Instance.BlocksRenderingLibrary.TryGetValue(blockPath, out var b)) {
                    Sprite.texture = b.ItemPreview!;
                    if (Text is not null) Text.text = $"<+spread>{blockPath.Replace(".json", "")}";
                } else {
                    Sprite.texture = null;
                }

                _lastBlock = block;
            }).AddTo(ResetToken);
        }
    }
}