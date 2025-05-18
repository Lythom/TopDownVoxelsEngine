using System;
using System.Linq;
using LoneStoneStudio.Tools;
using Shared;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    /// <summary>
    /// Generate a mesh for the chunk ChunkKey of Level.
    /// The renderer game object should be placed at (chX * ChunkData.Size, 0, chY * ChunkData.Size).
    /// The rendered cells are centered, which means cell at (0,0,0) boundaries are visually at (-0.5,-0.5,-0.5)->(0.5,0.5,0.5).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class ChunkRenderer : MonoBehaviour {
        public LevelMap Level = null!;
        public ChunkGPUSynchronizer ChunkGPUSynchronizer = null!;

        private Mesh _mesh = null!;
        private readonly int[] _triangles = new int[20000];
        private int _trianglesCount = 0;
        private readonly Vector3[] _vertices = new Vector3[10000];
        private int _verticesCount = 0;
        private readonly Vector4[] _uvs = new Vector4[10000];
        private readonly Vector4[] _uvs2 = new Vector4[10000];
        private int _uvsCount = 0;
        private int _uvs2Count = 0;
        private Transform _propsContainer = null!;

        // used for rendering by ChunkGPUSynchronizer
        public int GpuSlotID = -1;

        public uint[] BlockData = new uint[Chunk.Size * Chunk.Height * Chunk.Size];

        private void Awake() {
            _mesh = GetComponent<MeshFilter>().mesh;
            if (_mesh == null) throw new Exception("No mesh found on ChunkRenderer");
            var pc = new GameObject();
            pc.name = "Props";
            pc.transform.parent = transform;
            _propsContainer = pc.transform;
        }

        public bool UpdateMesh(LevelMap level, ChunkKey chunkKey, string?[] blockPathById) {
            var chunk = Level.Chunks[chunkKey.ChX, chunkKey.ChZ];
            if (!chunk.IsGenerated) throw new ApplicationException("Ensure Chunk is not null before drawing");

            _trianglesCount = 0;
            _verticesCount = 0;
            _uvsCount = 0;
            _uvs2Count = 0;

            foreach (var (x, y, z) in chunk.GetCellPositions()) {
                var cell = chunk.Cells[x, y, z];
                var blockPath = blockPathById[cell.Block];
                if (cell.Block != BlockId.Air
                    && blockPath != null
                    && Configurator.Instance.BlocksRenderingLibrary.TryGetValue(blockPath, out var blockDef)) {
                    if (blockDef.Sides.Count == 0) {
                        // no texture, the important flag is the last one that indicated it's an air block
                        BlockData[GetLocalBlockId(x, y, z)] = 0;
                        continue;
                    }

                    MakeCube(x, y, z, chunkKey, blockDef, cell.Block, level);
                    BlockRenderingSide? up = null;
                    BlockRenderingSide? side = null;
                    foreach (var s in blockDef.Sides) {
                        if (s.Directions.HasFlagFast(DirectionFlag.Up)) up = s;
                        if (s.Directions.HasFlagFast(DirectionFlag.North)) side = s;
                    }

                    if (up is null && side is null) {
                        up = blockDef.Sides[0];
                        side = blockDef.Sides[0];
                    }

                    var mainTopTextureIndex = (uint) (up!.MainTextureIndex & 0x3FFF); // 14 bits
                    var mainSideTextureIndex = (uint) (side!.MainTextureIndex & 0x3FFF); // 14 bits
                    if (mainSideTextureIndex == 0) mainSideTextureIndex = mainTopTextureIndex;
                    var canBleed = blockDef.CanBleed ? 1u : 0u; // 1 bit
                    var acceptBleeding = blockDef.AcceptBleeding ? 1u : 0u; // 1 bit
                    var hasFrame = blockDef.HasFrameAlbedo ? 1u : 0u; // 1 bit

                    // Pack into 32 bits:
                    // [14 bits top texture][14 bits side texture][1 bit canBleed][1 bit acceptBleeding][1 bit hasFrame][1 bit hasTexture]
                    // 31                18 17                 4 3            2   2                   1   1              0   0
                    uint packedData =
                        (mainTopTextureIndex << 18) | // First 14 bits, shifted to top
                        (mainSideTextureIndex << 4) | // Next 14 bits
                        (canBleed << 3) | // First extra bit
                        (acceptBleeding << 2) |
                        (hasFrame << 1) |
                        1u; // hasTexture
                    BlockData[GetLocalBlockId(x, y, z)] = packedData;
                } else {
                    BlockData[GetLocalBlockId(x, y, z)] = 0;
                }
            }

            UpdateMesh();
            ChunkGPUSynchronizer.UploadChunkData(this);
            return true;
        }

        private void OnDisable() {
            ChunkGPUSynchronizer.UnloadChunkData(this);
        }

        public static int GetLocalBlockId(int cx, int cy, int cz) {
            // X,Z,Y order
            return cx +
                   cz * Chunk.Size +
                   cy * Chunk.Size * Chunk.Size;
        }

        private void MakeCube(int cX, int cY, int cZ, ChunkKey chunkKey, BlockRendering blockDef, ushort blockId, LevelMap level) {
            for (int i = 0; i < 6; i++) {
                var dir = (Direction) (i + 1);
                var x = cX + chunkKey.ChX * Chunk.Size;
                var y = cY;
                var z = cZ + chunkKey.ChZ * Chunk.Size;
                var n = level.GetNeighbor(x, cY, z, dir);
                if (n == null || n.Value.Block == BlockId.Air) {
                    var bitMask = AutoTile48Blob.Get8SurroundingsBitmask(dir, x, y, z, blockId, Level.CellMatchDefinition);
                    MakeFace(dir, x, y, z, blockDef, bitMask);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="block"></param>
        /// <param name="bitMask">positions of the neighbour cells of the same type</param>
        private void MakeFace(Direction dir, int x, int y, int z, BlockRendering block, int bitMask) {
            if (block.Sides.Count == 0) return;
            CubeMeshData.FaceVertices((int) dir - 1, x % Chunk.Size, y, z % Chunk.Size, _vertices, ref _verticesCount);
            //
            // foreach (var faceVertex in CubeMeshData.FaceVertices((int) dir, x % 16 - 8, y, z % 16 - 8)) {
            //     _vertices[_verticesCount] = faceVertex;
            //     _verticesCount++;
            // }
            int blobIndex = AutoTile48Blob.GetBlobIndex(bitMask);
            var side = block.Sides.FirstOrDefault(s => s.Directions.HasFlagFast(dir)) ?? block.Sides[0];

            _uvs[_uvsCount++] = new(1, 0, side.MainTextureIndex, side.FrameTextureIndex);
            _uvs[_uvsCount++] = new(0, 0, side.MainTextureIndex, side.FrameTextureIndex);
            _uvs[_uvsCount++] = new(0, 1, side.MainTextureIndex, side.FrameTextureIndex);
            _uvs[_uvsCount++] = new(1, 1, side.MainTextureIndex, side.FrameTextureIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, block.HasFrameAlbedo ? 0 : 1);
            _uvs2[_uvs2Count++] = new(blobIndex, block.HasFrameAlbedo ? 0 : 1);
            _uvs2[_uvs2Count++] = new(blobIndex, block.HasFrameAlbedo ? 0 : 1);
            _uvs2[_uvs2Count++] = new(blobIndex, block.HasFrameAlbedo ? 0 : 1);

            _triangles[_trianglesCount++] = _verticesCount - 4;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 1;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 2;
            _triangles[_trianglesCount++] = _verticesCount - 4;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 2;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 3;
        }

        private void UpdateMesh() {
            _mesh.Clear();
            _mesh.SetVertices(_vertices, 0, _verticesCount);
            _mesh.SetTriangles(_triangles, 0, _trianglesCount, 0);
            _mesh.SetUVs(0, _uvs, 0, _uvsCount);
            _mesh.SetUVs(1, _uvs2, 0, _uvs2Count);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            _propsContainer.DestroyChildren();
        }

        public int GetFlatIndex() {
            return Chunk.GetFlatIndex((int) transform.position.x / Chunk.Size, (int) transform.position.z / Chunk.Size);
        }
    }
}