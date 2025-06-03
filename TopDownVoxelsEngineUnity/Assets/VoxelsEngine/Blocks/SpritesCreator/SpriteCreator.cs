using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class SpriteCreator : MonoBehaviour {

        [Required]
        public Camera Cam = null!;

        [Required]
        public Material BlockMaterial = null!;

        [Button(ButtonSizes.Large), EnableIf("_readyForCapture")]
        public void CaptureAll() {
            CaptureAllAsync().Forget();
        }

        private async UniTaskVoid CaptureAllAsync() {
            try {
                // Ensure the directory exists
                string spritesDir = Path.Combine(Application.streamingAssetsPath, "Sprites");
                if (!Directory.Exists(spritesDir)) {
                    Directory.CreateDirectory(spritesDir);
                }

                // Ensure the configurator and block registry are loaded
                await Configurator.Instance.IsReady();
                await UniTask.WaitUntil(this, t => t._readyForCapture, cancellationToken: _cancellationTokenOnDestroy);

                if (Configurator.Instance.BlockRegistry == null) {
                    Debug.LogError("Block registry is not loaded!");
                    return;
                }

                // Get all blocks from the registry
                var blockRegistry = Configurator.Instance.BlockRegistry;
                var blocks = blockRegistry.Get();

                Debug.Log($"Starting sprite capture for {blocks.Count} blocks...");

                // Create a temporary texture to save the image
                RenderTexture renderTexture = Cam.targetTexture;
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

                // warmup ???
                Capture(1);
                _chunkRenderer.gameObject.SetActive(false);
                _chunkRenderer.gameObject.SetActive(true);

                // For each block in the registry
                var copy = blocks.ToDictionary(x => x.Key, x => x.Value);
                foreach (var blockEntry in copy) {
                    try {

                        string blockPath = blockEntry.Key;
                        BlockConfigJson blockConfig = blockEntry.Value;

                        // Extract the block ID from the path
                        if (!_blockMapping.BlockIdByPath.TryGetValue(blockPath, out var blockId)) continue;

                        if (blockId == 0) {
                            // Skip air block
                            continue;
                        }

                        Capture(blockId);

                        // Wait for a frame to ensure rendering is complete
                        await UniTask.Delay(50, cancellationToken: _cancellationTokenOnDestroy);

                        // Capture the screenshot
                        string fileName = $"{Path.GetFileNameWithoutExtension(blockPath)}.png";
                        string filePath = Path.Combine(spritesDir, fileName);

                        // Get the render texture from the camera


                        // Read the render texture
                        RenderTexture.active = renderTexture;
                        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                        texture.Apply();
                        ApplyGammaCorrection(texture);

                        RenderTexture.active = null;

                        // Convert to PNG
                        byte[] bytes = texture.EncodeToPNG();

                        // Save the file
                        await File.WriteAllBytesAsync(filePath, bytes, _cancellationTokenOnDestroy);

                        // Update the block config
                        blockConfig.ItemPreviewSprite = fileName;

                        // Save the updated JSON
                        blockRegistry.Editor_SaveToJson(blockPath, blockConfig);

                        Debug.Log($"Captured sprite for {blockPath} and saved to {filePath}");
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }

                // Clean up
                Destroy(texture);
                Debug.Log("Sprite capture complete. Indexes generated.");
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private CancellationToken _cancellationTokenOnDestroy;
        private ChunkRenderer _chunkRenderer = null!;
        private SpriteCreatorLevelMap _level = null!;
        private readonly BlockPathMapping _blockMapping = new();
        private bool _readyForCapture;

        private void Awake() {
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            _chunkRenderer = CreateChunkRenderer();
            _level = new SpriteCreatorLevelMap();
            Configurator.Instance.IsReady().ContinueWith(() => {
                _blockMapping.UpdateBlockMapping(Configurator.Instance.BlockRegistry!);
                Debug.Log("Block mapping updated. Ready for capturing");
                _readyForCapture = true;
            });
        }

        [Button]
        public void Capture(BlockId blockId) {
            _level.Clear();
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    for (int k = 0; k < 3; k++) {
                        _level.TrySetExistingCell(i, j, k, blockId);
                    }
                }
            }

            _chunkRenderer.UpdateMesh(_level, new ChunkKey("0", 0, 0), _blockMapping.BlockPathById);
        }

        private ChunkRenderer CreateChunkRenderer() {
            var go = new GameObject("Sprite Creator Chunk Renderer");
            go.transform.SetParent(transform);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();

            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = BlockMaterial;

            var chunkRenderer = go.AddComponent<ChunkRenderer>();

            return chunkRenderer;
        }

        private void ApplyGammaCorrection(Texture2D texture) {
            // Check if we're in linear color space
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++) {
                    // Convert from linear to sRGB space (approximation)
                    pixels[i].r = Mathf.LinearToGammaSpace(pixels[i].r);
                    pixels[i].g = Mathf.LinearToGammaSpace(pixels[i].g);
                    pixels[i].b = Mathf.LinearToGammaSpace(pixels[i].b);
                    // Alpha remains unchanged
                }

                texture.SetPixels(pixels);
                texture.Apply();
            }
        }
    }
}