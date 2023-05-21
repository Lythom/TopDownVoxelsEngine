using System;
using LoneStoneStudio.Tools;
using MessagePack;
using MessagePack.Resolvers;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace VoxelsEngine {
    [Serializable]
    public class BlocksDefinitionLibrary : SerializedDictionary<String, BlockDefinition> {
    }

    [Serializable]
    public class Configurator : MonoBehaviour {
        private static Configurator? _instance;

        [SerializeField]
        public BlocksDefinitionLibrary BlocksLibrary = new();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitialize() {
            Initialize();
        }
#endif

        [Button]
        private void ForceReload() {
            _serializerRegistered = false;
            Initialize();
        }

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
        }

        private static void FillLibrary() {
            if (_instance == null) return;
        }

        static bool _serializerRegistered = false;

        public static MessagePackSerializerOptions MessagePackOptions = MessagePackSerializerOptions.Standard
            .WithResolver(StaticCompositeResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            DisableUnityAnalytics();

            if (!_serializerRegistered) {
                StaticCompositeResolver.Instance.Register(
                    DynamicEnumAsStringResolver.Instance,
                    StandardResolver.Instance
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