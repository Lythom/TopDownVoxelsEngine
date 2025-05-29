using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Shared;
using Shared.SideEffects;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class SpriteCreator : ConnectedBehaviour {
        
        private ChunkGPUSynchronizer? _gpuSynchronizer;

        [Required]
        public Camera Cam = null!;

        [Required]
        public Material BlockMaterial = null!;

        private CancellationToken _cancellationTokenOnDestroy;

        private void Awake() {
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            _gpuSynchronizer = new ChunkGPUSynchronizer();
        }

        protected override void OnSetup(GameState state) {
        }

    }
}