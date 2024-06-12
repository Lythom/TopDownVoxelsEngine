using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using MessagePack.Resolvers;
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

        public Registry<MainTextureJson> MainTextureRegistry = new(StreamAssets.GetPath("Textures", "Main"), "*.json");
        public Registry<FrameTextureJson> FrameTextureRegistry = new(StreamAssets.GetPath("Textures", "Frame"), "*.json");
        public SpriteRegistry SpriteRegistry = new(StreamAssets.GetPath("Sprites"), "*.png");
        public Registry<BlockConfigJson> BlockRegistry = new(StreamAssets.GetPath("Blocks"), "*.json");

        public GameObject? GrassProp;

        [Button(ButtonSizes.Large)]
        private void RegenerateAtlas() {
            MainTextureRegistry.Reload();
            FrameTextureRegistry.Reload();
            SpriteRegistry.Reload();
            BlockRegistry.Reload();

            var blockConfigs = BlockRegistry.Get();

            BlocksRenderingLibrary.Clear();
            BlocksRenderingLibrary.Add("Air", BlockRendering.Air);
            foreach (var (blockPath, blockConfig) in blockConfigs) {
                BlocksRenderingLibrary.Add(blockPath, new BlockRendering(blockConfig, MainTextureRegistry, FrameTextureRegistry, SpriteRegistry));
            }

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
                        side.FrameTextureIndex = -1;
                    }
                }
            }

            if (mainSourceSize > 0) {
                _lastMainAlbedo = Create2DArrayTexture(mainSourceSize, mainAlbedoSources, "Assets/Blocks/GeneratedMainAlbedo.asset", TextureFormat.RGB24, true, false);
                _lastMainNormals = Create2DArrayTexture(mainSourceSize, mainNormalsSources, "Assets/Blocks/GeneratedMainNormals.asset", TextureFormat.RGB24, true, true);
                _lastMainHeights = Create2DArrayTexture(mainSourceSize, mainHeightsSources, "Assets/Blocks/GeneratedMainHeights.asset", TextureFormat.R16, true, true);
                _lastFrameAlbedo = Create2DArrayFrameTexture(frameSourceSize, frameAlbedoSources, "Assets/Blocks/GeneratedFrameAlbedo.asset", TextureFormat.RGB24, true, false);
                _lastFrameNormals = Create2DArrayFrameTexture(frameSourceSize, frameNormalsSources, "Assets/Blocks/GeneratedFrameNormals.asset", TextureFormat.RGB24, true, true);
                _lastFrameHeights = Create2DArrayFrameTexture(frameSourceSize, frameHeightsSources, "Assets/Blocks/GeneratedFrameHeights.asset", TextureFormat.R16, true, true);

                UploadTexturesToShader();
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

        private static int TryAddTexture(List<Texture2D> sourceList, ref int expectedSize, Texture2D texture) {
            if (texture != null) {
                var idx = sourceList.IndexOf(texture);
                if (idx > -1) return idx;
                sourceList.Add(texture);
                if (expectedSize == 0) expectedSize = texture.width;
                if (expectedSize != texture.width || expectedSize != texture.height) {
                    throw new ApplicationException($"Source main textures must be square and have the same size. Texture: {texture}, expected: {expectedSize}, loaded: {texture.width}x{texture.height}. ");
                }

                return sourceList.Count - 1;
            }

            return -1;
        }

        private static int TryAddFramesTexture(List<Texture2D> sourceList, ref int expectedSize, Texture2D texture) {
            if (texture != null) {
                var idx = sourceList.IndexOf(texture);
                if (idx > -1) return idx;
                sourceList.Add(texture);
                if (expectedSize == 0) expectedSize = texture.width / 11;
                if (expectedSize != texture.width / 11 || expectedSize != texture.height / 5) {
                    throw new ApplicationException("Source frame textures must be a 11 by 5 grid of the same size");
                }

                return sourceList.Count - 1;
            }

            return -1;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitialize() {
            Initialize();
        }

        [Button]
        private void ForceReload() {
            _serializerRegistered = false;
            Initialize();
        }
#endif

        public static bool IsInstanceCreatedYet() {
            return _instance != null;
        }

        public static Configurator Instance {
            get {
                if (_instance != null) return _instance;

#if UNITY_EDITOR
                // If we're in the editor find a ref in the scene
                _instance = FindFirstObjectByType<Configurator>();
                if (_instance != null) {
                    FillLibrary();
                }

#endif

                if (_instance == null) {
                    throw new InvalidOperationException("No Configurator found! Please add one in the scene and configure the required fields.");
                }

                return _instance;
            }
        }

        private void Awake() {
            if (_instance != null && _instance.isActiveAndEnabled) {
                // detroy self if it's not the current instance already
                if (_instance.gameObject != gameObject) Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            _instance = this;
            FillLibrary();
            Logr.Log("Configurator initialized");
        }

        private static void FillLibrary() {
            if (_instance == null) return;
            _instance.RegenerateAtlas();
        }

        static bool _serializerRegistered = false;

        public static MessagePackSerializerOptions MessagePackOptions = MessagePackSerializerOptions.Standard
            .WithResolver(StaticCompositeResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        private static readonly int MainTex = Shader.PropertyToID("_mainTex");
        private static readonly int MainNormals = Shader.PropertyToID("_mainNormals");
        private static readonly int MainHeights = Shader.PropertyToID("_mainHeights");
        private static readonly int FrameTex = Shader.PropertyToID("_frameTex");
        private static readonly int FrameNormals = Shader.PropertyToID("_frameNormals");
        private static readonly int FrameHeights = Shader.PropertyToID("_frameHeights");
        private static readonly int FrameWithoutAlbedo = Shader.PropertyToID("_frameWithoutAlbedo");
        private Texture2DArray _lastMainAlbedo;
        private Texture2DArray _lastMainNormals;
        private Texture2DArray _lastMainHeights;
        private Texture2DArray _lastFrameAlbedo;
        private Texture2DArray _lastFrameNormals;
        private Texture2DArray _lastFrameHeights;
        private List<int> _frameIndexesWithoutAlbedo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            DisableUnityAnalytics();


            if (!_serializerRegistered) {
                try {
                    StaticCompositeResolver.Instance.Register(
                        StandardResolver.Instance,
                        GeneratedResolver.Instance
                    );
                } catch (Exception e) {
                    Logr.LogException(e);
                }

                MessagePackSerializer.DefaultOptions = MessagePackOptions;
                _serializerRegistered = true;
            }

            FillLibrary();
        }

        private static void DisableUnityAnalytics() {
            Analytics.initializeOnStartup = false;
            Analytics.deviceStatsEnabled = false;
            PerformanceReporting.enabled = false;
        }
    }
}