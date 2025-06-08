using System;
using System.Collections.Generic;
using Shared;
using UnityEngine;
using Vector3Int = UnityEngine.Vector3Int;

namespace VoxelsEngine {
    public class ChunkGPUSynchronizer : IDisposable {
        private static readonly Lazy<ChunkGPUSynchronizer> _lazyInstance =
            new(() => {
                var instance = new ChunkGPUSynchronizer();
                Application.quitting += () => instance.Dispose();
                return instance;
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static bool _isDisposed = false;

        public static ChunkGPUSynchronizer Instance {
            get {
                if (_isDisposed) {
                    _isDisposed = false;
                    _lazyInstance.Value.Init();
                }

                return _lazyInstance.Value;
            }
        }

        // -- World Configuration --
        private const int MaxActiveChunks = 1024;
        public static readonly Vector3Int ChunkDimensions = new(16, 64, 16); // Taille d'un chunk en blocs+
        private static readonly int VoxelsPerChunk = ChunkDimensions.x * ChunkDimensions.y * ChunkDimensions.z;
        public Vector3Int WorldDimensionsInChunks = new(128, 1, 128); // Ex: 2048/16, 64/64, 2048/16

        // -- GPU Data --
        // Contient les données réelles des blocs des chunks actifs
        private ComputeBuffer _worldBlockDataSsbo;

        // Table d'indirection: map chunk coords (linéarisées) vers un slotID ou -1 si inactif
        // Mappe chunkCoord linéarisée => slotID dans worldBlockDataBuffer
        private ComputeBuffer _ssboSlotIdByChunkIdSsbo;

        // -- CPU Management --
        private HashSet<int> _activeChunks = new();
        private readonly Stack<int> _freeSlotsInSsbo = new();
        private int[] _ssboSlotIdByChunkId;

        private ChunkGPUSynchronizer() {
            (_worldBlockDataSsbo, _ssboSlotIdByChunkIdSsbo, _ssboSlotIdByChunkId) = InitializeBuffers();
            SetGlobalShaderProperties();
        }

        void Init() {
            (_worldBlockDataSsbo, _ssboSlotIdByChunkIdSsbo, _ssboSlotIdByChunkId) = InitializeBuffers();
            SetGlobalShaderProperties();
        }


        // Shader property IDs
        private static readonly int WorldBlockDataPropID = Shader.PropertyToID("_WorldBlockData");
        private static readonly int ChunkIndirectionTablePropID = Shader.PropertyToID("_ChunkIndirectionTable");
        private static readonly int ChunkDimensionsPropID = Shader.PropertyToID("_ChunkDimensions");
        private static readonly int WorldChunkCountsPropID = Shader.PropertyToID("_WorldChunkCounts"); // Pour l'indirection

        (ComputeBuffer, ComputeBuffer, int[]) InitializeBuffers() {
            // SSBO pour les données des blocs des chunks actifs
            // Taille : max chunks actifs * voxels par chunk * sizeof(uint)
            _worldBlockDataSsbo = new ComputeBuffer(MaxActiveChunks * VoxelsPerChunk, sizeof(uint), ComputeBufferType.Structured);

            // Taille : totalChunksInWorld * sizeof(int)
            int totalChunksInWorld = WorldDimensionsInChunks.x * WorldDimensionsInChunks.y * WorldDimensionsInChunks.z;
            _ssboSlotIdByChunkIdSsbo = new ComputeBuffer(totalChunksInWorld, sizeof(int), ComputeBufferType.Structured);

            _ssboSlotIdByChunkId = new int[totalChunksInWorld];
            for (int i = 0; i < totalChunksInWorld; i++) {
                _ssboSlotIdByChunkId[i] = -1; // -1 signifie chunk non chargé/vide
            }

            _ssboSlotIdByChunkIdSsbo.SetData(_ssboSlotIdByChunkId);

            // Initialiser la liste des slots libres
            for (int i = 0; i < MaxActiveChunks; i++) {
                _freeSlotsInSsbo.Push(i);
            }

            return (_worldBlockDataSsbo, _ssboSlotIdByChunkIdSsbo, _ssboSlotIdByChunkId);
        }

        void SetGlobalShaderProperties() {
            Shader.SetGlobalBuffer(WorldBlockDataPropID, _worldBlockDataSsbo);
            Shader.SetGlobalBuffer(ChunkIndirectionTablePropID, _ssboSlotIdByChunkIdSsbo);
            Shader.SetGlobalVector(ChunkDimensionsPropID, new Vector4(ChunkDimensions.x, ChunkDimensions.y, ChunkDimensions.z, 0));
            Shader.SetGlobalVector(WorldChunkCountsPropID, new Vector4(WorldDimensionsInChunks.x, WorldDimensionsInChunks.y, WorldDimensionsInChunks.z, 0));
        }

        public void UploadChunkData(IChunkRenderer chunk) {
            if (chunk.transform.position.x < 0
                || chunk.transform.position.x / Chunk.Size >= LevelMap.LevelChunkSize
                || chunk.transform.position.z < 0
                || chunk.transform.position.z / Chunk.Size >= LevelMap.LevelChunkSize) return;

            int linearChunkIndex = chunk.GetFlatIndex();

            if (chunk.GpuSlotID == -1) {
                if (_freeSlotsInSsbo.Count == 0) {
                    Logr.LogError("[SSBO] Plus de slots GPU disponibles pour les chunks !");
                    // TODO: unload furthest chunks to retrieve an ID
                    return;
                }

                chunk.GpuSlotID = _freeSlotsInSsbo.Pop();
                // Logr.Log($"[SSBO] ({linearChunkIndex}) Getting GpuSlotID={chunk.GpuSlotID}");
            }

            // 1. Uploader les données du chunk dans le grand SSBO
            //    Les données de chunk.BlockData sont un uint[] de taille voxelsPerChunk
            _worldBlockDataSsbo.SetData(chunk.BlockData, 0, chunk.GpuSlotID * VoxelsPerChunk, VoxelsPerChunk);
            // Logr.Log($"[SSBO] ({linearChunkIndex}) Data uploaded at index {chunk.GpuSlotID} ({chunk.GpuSlotID * VoxelsPerChunk}). Count = {VoxelsPerChunk}");

            // 2. Mettre à jour la table d'indirection
            // Logr.Log($"[SSBO] ({linearChunkIndex}) Uploading {chunk.BlockData}. SlotId={chunk.GpuSlotID} ({chunk.GpuSlotID * VoxelsPerChunk})");
            if (linearChunkIndex < _ssboSlotIdByChunkId.Length) {
                if (_ssboSlotIdByChunkId[linearChunkIndex] != chunk.GpuSlotID) // Si la valeur change réellement
                {
                    _ssboSlotIdByChunkId[linearChunkIndex] = chunk.GpuSlotID;
                    _ssboSlotIdByChunkIdSsbo.SetData(new[] {chunk.GpuSlotID}, 0, linearChunkIndex, 1);

                    // Logr.Log($"[SSBO] ({linearChunkIndex}) Data uploaded at index {chunk.GpuSlotID} ({chunk.GpuSlotID * VoxelsPerChunk})");
                }
            } else {
                Logr.LogError($"Index de chunk linéaire invalide: {linearChunkIndex} pour coords {chunk.transform.position / Chunk.Size}");
            }

            _activeChunks.Add(linearChunkIndex);
        }

        public void UnloadChunkData(IChunkRenderer chunk) {
            if (chunk.GpuSlotID == -1) return; // Pas sur le GPU

            // 1. Mettre à jour la table d'indirection pour marquer ce chunk comme inactif
            int linearChunkIndex = chunk.GetFlatIndex();
            if (linearChunkIndex < _ssboSlotIdByChunkId.Length) {
                if (_ssboSlotIdByChunkId[linearChunkIndex] == chunk.GpuSlotID) // S'assurer qu'on invalide le bon slot
                {
                    _ssboSlotIdByChunkId[linearChunkIndex] = -1;
                    _ssboSlotIdByChunkIdSsbo.SetData(new[] {-1}, 0, linearChunkIndex, 1);
                }
            }

            // 2. Rendre le slot SSBO disponible à nouveau
            if (!_freeSlotsInSsbo.Contains(chunk.GpuSlotID)) {
                _freeSlotsInSsbo.Push(chunk.GpuSlotID);
            }

            chunk.GpuSlotID = -1;
            _activeChunks.Remove(linearChunkIndex);
        }


        public void Dispose() {
            _worldBlockDataSsbo.Release();
            _ssboSlotIdByChunkIdSsbo.Release();
            _worldBlockDataSsbo = null!;
            _ssboSlotIdByChunkIdSsbo = null!;
            _isDisposed = true;
        }
    }
}