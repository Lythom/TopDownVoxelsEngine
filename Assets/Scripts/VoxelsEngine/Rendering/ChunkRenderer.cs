using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelsEngine.Rendering;

namespace VoxelsEngine {
    
    /// <summary>
    /// Generate a mesh for the chunk ChunkKey of Level.
    /// The renderer game object should be placed at (chX * ChunkData.Size, 0, chY * ChunkData.Size).
    /// The rendered cells are centered, which means cell at (0,0,0) boundaries are visually at (-0.5,-0.5,-0.5)->(0.5,0.5,0.5).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class ChunkRenderer : MonoBehaviour {
        public LevelData Level = null!;
        public ChunkKey ChunkKey;

        private Mesh _mesh = null!;
        private readonly int[] _triangles = new int[20000];
        private int _trianglesCount = 0;
        private readonly Vector3[] _vertices = new Vector3[10000];
        private int _verticesCount = 0;
        private readonly Vector4[] _uvs = new Vector4[10000];
        private readonly Vector2[] _uvs2 = new Vector2[10000];
        private int _uvsCount = 0;
        private int _uvs2Count = 0;

        private void Awake() {
            _mesh = GetComponent<MeshFilter>().mesh;
            if (_mesh == null) throw new Exception("No mesh found on ChunkRenderer");
        }

        public void ReCalculateMesh(LevelData level) {
            if (_mesh == null) return;
            var chunk = Level.Chunks[ChunkKey.ChX, ChunkKey.ChZ];
            if (!chunk.IsGenerated) throw new ApplicationException("Ensure Chunk is not null before drawing");

            _trianglesCount = 0;
            _verticesCount = 0;
            _uvsCount = 0;
            _uvs2Count = 0;
            foreach (var (x, y, z) in chunk.GetCellPositions()) {
                var cell = chunk.Cells[x, y, z];
                if (cell.BlockDef != BlockDefId.Air) {
                    var blockDef = Configurator.Instance.BlocksLibrary[(int) cell.BlockDef];
                    MakeCube(x, y, z, blockDef, chunk, level);
                }
            }
        }

        private void MakeCube(int cX, int cY, int cZ, BlockDefinition blockDef, ChunkData chunkData, LevelData level) {
            for (int i = 0; i < 6; i++) {
                var dir = (Direction) i;
                var x = cX + chunkData.ChX * ChunkData.Size;
                var y = cY;
                var z = cZ + chunkData.ChZ * ChunkData.Size;
                var n = level.GetNeighbor(x, cY, z, dir);
                if (n == null || n.Value.BlockDef == BlockDefId.Air) {
                    var bitMask = Level.Get8SurroundingsBitmask(dir, x, y, z, blockDef.Id);
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
        /// <param name="textureIndex">Index of the texture to use</param>
        /// <param name="bitMask">positions of the neighbours cells of the same type</param>
        private void MakeFace(Direction dir, int x, int y, int z, BlockDefinition blockDefinition, int bitMask) {
            CubeMeshData.FaceVertices((int) dir, x % ChunkData.Size, y, z % ChunkData.Size, _vertices, ref _verticesCount);
            //
            // foreach (var faceVertex in CubeMeshData.FaceVertices((int) dir, x % 16 - 8, y, z % 16 - 8)) {
            //     _vertices[_verticesCount] = faceVertex;
            //     _verticesCount++;
            // }
            int blobIndex = AutoTile48Blob.GetBlobIndex(bitMask);

            _uvs[_uvsCount++] = new(1, 0, blockDefinition.MainTextureIndex, blockDefinition.FrameTextureIndex);
            _uvs[_uvsCount++] = new(0, 0, blockDefinition.MainTextureIndex, blockDefinition.FrameTextureIndex);
            _uvs[_uvsCount++] = new(0, 1, blockDefinition.MainTextureIndex, blockDefinition.FrameTextureIndex);
            _uvs[_uvsCount++] = new(1, 1, blockDefinition.MainTextureIndex, blockDefinition.FrameTextureIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, blockDefinition.FrameNormalIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, blockDefinition.FrameNormalIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, blockDefinition.FrameNormalIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, blockDefinition.FrameNormalIndex);

            _triangles[_trianglesCount++] = _verticesCount - 4;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 1;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 2;
            _triangles[_trianglesCount++] = _verticesCount - 4;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 2;
            _triangles[_trianglesCount++] = _verticesCount - 4 + 3;
        }

        public void UpdateMesh() {
            _mesh.Clear();
            _mesh.SetVertices(_vertices, 0, _verticesCount);
            _mesh.SetTriangles(_triangles, 0, _trianglesCount, 0);
            _mesh.SetUVs(0, _uvs, 0, _uvsCount);
            _mesh.SetUVs(1, _uvs2, 0, _uvs2Count);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
        }
    }
}