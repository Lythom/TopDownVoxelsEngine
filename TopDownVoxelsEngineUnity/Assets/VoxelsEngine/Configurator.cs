using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Resolvers;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Analytics;

namespace VoxelsEngine {
    [Serializable]
    public class Configurator : MonoBehaviour {
        private static Configurator? _instance;

        public int RenderDistance = 6;

        [Required]
        public Material OpaqueBlocksMaterial;

        [SerializeField]
        public List<BlockData> BlocksLibrary = new();

        [SerializeField]
        public List<BlockRenderingConfiguration> BlocksRenderingLibrary = new();

        public GameObject GrassProp;


        [Button(ButtonSizes.Large)]
        private void RegenerateAtlas() {
            // Generate Main Albedos
            List<string> mainAlbedoSources = new();
            List<string> mainNormalsSources = new();
            List<string> mainHeightsSources = new();
            List<string> frameAlbedoSources = new();
            List<string> frameNormalsSources = new();
            List<string> frameHeightsSources = new();
            int mainSourceSize = 0;
            int frameSourceSize = 0;
            foreach (var brc in BlocksRenderingLibrary) {
                foreach (var side in brc.Sides) {
                    side.MainTextureIndex = TryAddTexture(mainAlbedoSources, ref mainSourceSize, side.MainAlbedoTexture);
                    side.MainNormalsIndex = TryAddTexture(mainNormalsSources, ref mainSourceSize, side.MainNormalsTexture);
                    side.MainHeightsIndex = TryAddTexture(mainHeightsSources, ref mainSourceSize, side.MainHeightsTexture);
                    side.FrameTextureIndex = TryAddFramesTexture(frameAlbedoSources, ref frameSourceSize, side.FrameAlbedoTexture);
                    side.FrameNormalsIndex = TryAddFramesTexture(frameNormalsSources, ref frameSourceSize, side.FrameNormalsTexture);
                    side.FrameHeightsIndex = TryAddFramesTexture(frameHeightsSources, ref frameSourceSize, side.FrameHeightsTexture);
                }
            }

            if (mainSourceSize > 0) {
                var mainAlbedo = Create2DArrayTexture(mainSourceSize, mainAlbedoSources, "Assets/Blocks/GeneratedMainAlbedo.asset", TextureFormat.RGB24, true);
                var mainNormals = Create2DArrayTexture(mainSourceSize, mainNormalsSources, "Assets/Blocks/GeneratedMainNormals.asset", TextureFormat.RGB24, true);
                var mainHeights = Create2DArrayTexture(mainSourceSize, mainHeightsSources, "Assets/Blocks/GeneratedMainHeights.asset", TextureFormat.R16, true);
                var frameAlbedo = Create2DArrayFrameTexture(frameSourceSize, frameAlbedoSources, "Assets/Blocks/GeneratedFrameAlbedo.asset", TextureFormat.RGB24, true);
                var frameNormals = Create2DArrayFrameTexture(frameSourceSize, frameNormalsSources, "Assets/Blocks/GeneratedFrameNormals.asset", TextureFormat.RGB24, true);
                var frameHeights = Create2DArrayFrameTexture(frameSourceSize, frameHeightsSources, "Assets/Blocks/GeneratedFrameHeights.asset", TextureFormat.R16, true);

                OpaqueBlocksMaterial.SetTexture(MainTex, mainAlbedo);
                OpaqueBlocksMaterial.SetTexture(MainNormals, mainNormals);
                OpaqueBlocksMaterial.SetTexture(MainHeights, mainHeights);
                OpaqueBlocksMaterial.SetTexture(FrameTex, frameAlbedo);
                OpaqueBlocksMaterial.SetTexture(FrameNormals, frameNormals);
                OpaqueBlocksMaterial.SetTexture(FrameHeights, frameHeights);
            }
        }

        private static Texture2DArray Create2DArrayTexture(int size, List<string> sources, string outputPath, TextureFormat textureFormat, bool mipChain) {
            Texture2DArray outputTexture = new Texture2DArray(size, size, sources.Count, textureFormat, mipChain);
            for (var iSource = 0; iSource < sources.Count; iSource++) {
                var sourceTexturePath = sources[iSource];
                var source = Resources.Load<Texture2D>(sourceTexturePath);
                outputTexture.SetPixels(source.GetPixels(0), iSource, 0);
            }

            outputTexture.anisoLevel = 8;
            outputTexture.Apply();
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEditor.AssetDatabase.CreateAsset(outputTexture, outputPath);
            }
#endif
            return outputTexture;
        }

        private static Texture2DArray Create2DArrayFrameTexture(int size, List<string> sources, string outputPath, TextureFormat textureFormat, bool mipChain) {
            Texture2DArray outputTexture = new Texture2DArray(size, size, sources.Count * 55, textureFormat, mipChain);
            for (var iSource = 0; iSource < sources.Count; iSource++) {
                var sourceTexturePath = sources[iSource];
                var source = Resources.Load<Texture2D>(sourceTexturePath);

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
                UnityEditor.AssetDatabase.CreateAsset(outputTexture, outputPath);
            }
#endif

            return outputTexture;
        }

        private static int TryAddTexture(List<string> sourceList, ref int expectedSize, string texturePath) {
            if (!string.IsNullOrEmpty(texturePath)) {
                var idx = sourceList.IndexOf(texturePath);
                if (idx > -1) return idx;
                sourceList.Add(texturePath);
                var res = Resources.Load<Texture2D>(texturePath);
                if (expectedSize == 0) expectedSize = res.width;
                if (expectedSize != res.width || expectedSize != res.height) {
                    throw new ApplicationException("Source main textures must be square and have the same size");
                }

                return sourceList.Count - 1;
            }

            return -1;
        }

        private static int TryAddFramesTexture(List<string> sourceList, ref int expectedSize, string texturePath) {
            if (!string.IsNullOrEmpty(texturePath)) {
                var idx = sourceList.IndexOf(texturePath);
                if (idx > -1) return idx;
                sourceList.Add(texturePath);
                var res = Resources.Load<Texture2D>(texturePath);
                if (expectedSize == 0) expectedSize = res.width / 11;
                if (expectedSize != res.width / 11 || expectedSize != res.height / 5) {
                    throw new ApplicationException("Source frame textures must be a 11 by 5 grid of the same size");
                }

                return sourceList.Count - 1;
            }

            return -1;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInitialize() {
            Initialize();
        }

        [Button]
        private void ForceReload() {
            _serializerRegistered = false;
            Initialize();
        }
#endif


        public static Configurator Instance {
            get {
                if (_instance != null) return _instance;

#if UNITY_EDITOR
                // If we're in the editor find a ref in the scene
                _instance = FindObjectOfType<Configurator>();
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
                Destroy(gameObject);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            DisableUnityAnalytics();


            if (!_serializerRegistered) {
                StaticCompositeResolver.Instance.Register(
                    StandardResolver.Instance,
                    GeneratedResolver.Instance
                );

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