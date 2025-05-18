using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePack.Unity;
using Shared;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using VoxelsEngine.Data;
using VoxelsEngine.Tools;

namespace VoxelsEngine {
    [Serializable]
    public class Configurator : MonoBehaviour {
        private static Configurator? _instance;
        private bool _isReady;

        [Title("Audio")]
        [Required, AssetsOnly]
        public AudioClip SFXFootstep = null!;

        public float SFXFootstepPitchRange = 0.5f;

        [Title("FX")]
        [Required, AssetsOnly]
        public GameObject PlaceFX = null!;

        [Required, AssetsOnly]
        public GameObject RemoveFX = null!;

        [Title("Rendering")]
        public int RenderDistance = 6;

        [Required]
        public Material OpaqueBlocksMaterial = null!;

        [ShowInInspector]
        public Dictionary<string, BlockRendering> BlocksRenderingLibrary = new();

        public IStreamAssets? StreamAssets;
        public Registry<MainTextureJson>? MainTextureRegistry;
        public Registry<FrameTextureJson>? FrameTextureRegistry;
        public SpriteRegistry? SpriteRegistry;
        public Registry<BlockConfigJson>? BlockRegistry;

        [Button(ButtonSizes.Large)]
        public void RegenerateAtlas() {
            RegenerateAtlasAsync().Forget();
        }

        public async UniTask RegenerateAtlasAsync() {
            try {
                if (StreamAssets is null) StreamAssets = StreamAssetsFetcherFactory.Create();

                // Create tasks for all registry loads/reloads
                var tasks = new List<UniTask>();

                // Main Texture Registry
                tasks.Add(MainTextureRegistry?.Reload() ?? Registry<MainTextureJson>.Build(Path.Combine("Textures", "Main"), "*.json", StreamAssets)
                    .ContinueWith(registry => MainTextureRegistry = registry));

                // Frame Texture Registry
                tasks.Add(FrameTextureRegistry?.Reload() ?? Registry<FrameTextureJson>.Build(Path.Combine("Textures", "Frame"), "*.json", StreamAssets)
                    .ContinueWith(registry => FrameTextureRegistry = registry));

                // Block Registry
                tasks.Add(BlockRegistry?.Reload() ?? Registry<BlockConfigJson>.Build(Path.Combine("Blocks"), "*.json", StreamAssets)
                    .ContinueWith(registry => BlockRegistry = registry));

                // Sprite Registry
                tasks.Add(SpriteRegistry?.Reload() ?? SpriteRegistry.Build("Sprites", "*.png", StreamAssets)
                    .ContinueWith(registry => SpriteRegistry = registry));

                // Wait for all tasks to complete
                await UniTask.WhenAll(tasks);

                var blockConfigs = BlockRegistry!.Get();
                BlocksRenderingLibrary.Clear();
                BlocksRenderingLibrary.Add("Air", BlockRendering.Air);
                tasks.Clear();
                foreach (var (blockPath, blockConfig) in blockConfigs) {
                    tasks.Add(BlockRendering.FromConfigAsync(StreamAssets, blockConfig, MainTextureRegistry!, FrameTextureRegistry!, SpriteRegistry!)
                        .ContinueWith(c => BlocksRenderingLibrary.Add(blockPath, c)));
                }

                await UniTask.WhenAll(tasks);

                // Generate Main Albedos
                List<Texture2D> mainAlbedoSources = new();
                List<Texture2D> mainNormalsSources = new();
                List<Texture2D> mainHeightsSources = new();
                List<Texture2D> frameAlbedoSources = new();
                List<Texture2D> frameNormalsSources = new();
                List<Texture2D> frameHeightsSources = new();
                int mainSourceSize = 0;
                int frameSourceSize = 0;

                foreach (var br in BlocksRenderingLibrary.Values) {
                    foreach (var side in br.Sides) {
                        side.MainTextureIndex = TryAddTexture(mainAlbedoSources, ref mainSourceSize, side.MainAlbedoTexture);
                        TryAddTexture(mainNormalsSources, ref mainSourceSize, side.MainNormalsTexture);
                        TryAddTexture(mainHeightsSources, ref mainSourceSize, side.MainHeightsTexture);
                        if (side.FrameAlbedoTexture != null && side.FrameNormalsTexture != null && side.FrameHeightsTexture != null) {
                            side.FrameTextureIndex = TryAddFramesTexture(frameAlbedoSources, ref frameSourceSize, side.FrameAlbedoTexture);
                            TryAddFramesTexture(frameNormalsSources, ref frameSourceSize, side.FrameNormalsTexture);
                            TryAddFramesTexture(frameHeightsSources, ref frameSourceSize, side.FrameHeightsTexture);
                        } else {
                            side.FrameTextureIndex = 0;
                        }
                    }
                }

                if (mainSourceSize > 0) {
                    _lastMainAlbedo = Create2DArrayTexture(mainSourceSize, mainAlbedoSources, "Assets/VoxelsEngine/Blocks/GeneratedMainAlbedo.asset", TextureFormat.RGB24, true, false);
                    _lastMainNormals = Create2DArrayTexture(mainSourceSize, mainNormalsSources, "Assets/VoxelsEngine/Blocks/GeneratedMainNormals.asset", TextureFormat.RGB24, true, true);
                    _lastMainHeights = Create2DArrayTexture(mainSourceSize, mainHeightsSources, "Assets/VoxelsEngine/Blocks/GeneratedMainHeights.asset", TextureFormat.R16, true, true);
                    _lastFrameAlbedo = Create2DArrayFrameTexture(frameSourceSize, frameAlbedoSources, "Assets/VoxelsEngine/Blocks/GeneratedFrameAlbedo.asset", TextureFormat.RGB24, true, false);
                    _lastFrameNormals = Create2DArrayFrameTexture(frameSourceSize, frameNormalsSources, "Assets/VoxelsEngine/Blocks/GeneratedFrameNormals.asset", TextureFormat.RGB24, true, true);
                    _lastFrameHeights = Create2DArrayFrameTexture(frameSourceSize, frameHeightsSources, "Assets/VoxelsEngine/Blocks/GeneratedFrameHeights.asset", TextureFormat.R16, true, true);

                    UploadTexturesToShader();
                }
                
                // problème: j'ai besoin d'i

                _isReady = true;
            } catch (Exception e) {
                Logr.LogException(e);
                throw;
            }
        }

        [Button(ButtonSizes.Large)]
        private void UploadTexturesToShader() {
            OpaqueBlocksMaterial.SetTexture(MainTex, _lastMainAlbedo);
            OpaqueBlocksMaterial.SetTexture(MainNormals, _lastMainNormals);
            OpaqueBlocksMaterial.SetTexture(MainHeights, _lastMainHeights);
            OpaqueBlocksMaterial.SetTexture(FrameTex, _lastFrameAlbedo);
            OpaqueBlocksMaterial.SetTexture(FrameNormals, _lastFrameNormals);
            OpaqueBlocksMaterial.SetTexture(FrameHeights, _lastFrameHeights);
        }

        private static Texture2DArray Create2DArrayTexture(int size, List<Texture2D> sources, string outputPath, TextureFormat textureFormat, bool mipChain, bool linear) {
            Texture2DArray outputTexture = new Texture2DArray(size, size, sources.Count, textureFormat, mipChain, linear);
            for (var iSource = 0; iSource < sources.Count; iSource++) {
                var source = sources[iSource];
                outputTexture.SetPixels(source.GetPixels(0), iSource, 0);
            }

            outputTexture.anisoLevel = 8;
            outputTexture.Apply();
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                AssetDatabase.CreateAsset(outputTexture, outputPath);
            }
#endif
            return outputTexture;
        }

        private static Texture2DArray Create2DArrayFrameTexture(int size, List<Texture2D> sources, string outputPath, TextureFormat textureFormat, bool mipChain, bool linear) {
            Texture2DArray outputTexture = new Texture2DArray(size, size, sources.Count * 55, textureFormat, mipChain, linear);
            for (var iSource = 0; iSource < sources.Count; iSource++) {
                var source = sources[iSource];

                for (int x = 0; x < 11; x++) {
                    for (int y = 0; y < 5; y++) {
                        outputTexture.SetPixels(
                            // For some reason GetPixels reads from bottom left coordinates
                            source.GetPixels(x * size, (4 - y) * size, size, size, 0),
                            iSource * 55 + x + y * 11
                        );
                    }
                }
            }

            // Use camp for the frame because they are mostly not tilable
            outputTexture.wrapMode = TextureWrapMode.Clamp;
            outputTexture.anisoLevel = 8;
            outputTexture.Apply();
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                AssetDatabase.CreateAsset(outputTexture, outputPath);
            }
#endif

            return outputTexture;
        }

        private static ushort TryAddTexture(List<Texture2D> sourceList, ref int expectedSize, Texture2D texture) {
            if (texture != null) {
                var idx = sourceList.IndexOf(texture);
                if (idx > 0) return (ushort) idx;
                sourceList.Add(texture);
                if (expectedSize == 0) expectedSize = texture.width;
                if (expectedSize != texture.width || expectedSize != texture.height) {
                    throw new ApplicationException($"Source main textures must be square and have the same size. Texture: {texture}, expected: {expectedSize}, loaded: {texture.width}x{texture.height}. ");
                }

                return (ushort) (sourceList.Count - 1);
            }

            return 0;
        }

        private static ushort TryAddFramesTexture(List<Texture2D> sourceList, ref int expectedSize, Texture2D texture) {
            if (texture != null) {
                var idx = sourceList.IndexOf(texture);
                if (idx > 0) return (ushort) idx;
                sourceList.Add(texture);
                if (expectedSize == 0) expectedSize = texture.width / 11;
                if (expectedSize != texture.width / 11 || expectedSize != texture.height / 5) {
                    throw new ApplicationException("Source frame textures must be a 11 by 5 grid of the same size");
                }

                return (ushort) (sourceList.Count - 1);
            }

            return 0;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitialize() {
            Initialize();
        }

        [Button]
        private void ForceReload() {
            Initialize();
        }
#endif

        public static bool IsInstanceReady() {
            return _instance != null && _instance.BlockRegistry?.IsLoaded == true;
        }

        public static Configurator Instance {
            get {
                if (_instance != null) return _instance;

#if UNITY_EDITOR
                // If we're in the editor find a ref in the scene
                _instance = FindFirstObjectByType<Configurator>();
                if (_instance != null) {
                    FillLibrary().Forget();
                }

#endif

                if (_instance == null) {
                    throw new InvalidOperationException("No Configurator found! Please add one in the scene and configure the required fields.");
                }

                return _instance;
            }
        }

        private async void Awake() {
            try {
                if (_instance != null && _instance.isActiveAndEnabled) {
                    // detroy self if it's not the current instance already
                    if (_instance.gameObject != gameObject) Destroy(gameObject);
                    return;
                }

                DontDestroyOnLoad(gameObject);

                _instance = this;
                await FillLibrary();

                Logr.Log("Configurator initialized");
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        private static async UniTask FillLibrary() {
            if (_instance == null) return;
            await _instance.RegenerateAtlasAsync();
        }

        public static MessagePackSerializerOptions MessagePackOptions = MessagePackSerializerOptions.Standard
            .WithResolver(UnityResolver.InstanceWithStandardResolver)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        private static readonly int MainTex = Shader.PropertyToID("_mainTex");
        private static readonly int MainNormals = Shader.PropertyToID("_mainNormals");
        private static readonly int MainHeights = Shader.PropertyToID("_mainHeights");
        private static readonly int FrameTex = Shader.PropertyToID("_frameTex");
        private static readonly int FrameNormals = Shader.PropertyToID("_frameNormals");
        private static readonly int FrameHeights = Shader.PropertyToID("_frameHeights");
        private Texture2DArray? _lastMainAlbedo;
        private Texture2DArray? _lastMainNormals;
        private Texture2DArray? _lastMainHeights;
        private Texture2DArray? _lastFrameAlbedo;
        private Texture2DArray? _lastFrameNormals;
        private Texture2DArray? _lastFrameHeights;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize() {
            DisableUnityAnalytics();
            MessagePackSerializer.DefaultOptions = MessagePackOptions;
            FillLibrary().Forget();
        }

        private static void DisableUnityAnalytics() {
            Analytics.initializeOnStartup = false;
            Analytics.deviceStatsEnabled = false;
            PerformanceReporting.enabled = false;
        }

        public UniTask IsReady() => UniTask.WaitUntil(this, e => e._isReady);
    }
}