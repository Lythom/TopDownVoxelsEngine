using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelsEngine.Rendering;

namespace VoxelsEngine {
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
        private int _uvsCount = 0;

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
                var x = cX + chunkData.ChX * 16;
                var y = cY;
                var z = cZ + chunkData.ChZ * 16;
                var n = level.GetNeighbor(x, cY, z, dir);
                if (n == null || n.Value.BlockDef == BlockDefId.Air) {
                    var bitMask = Level.Get8SurroundingsBitmask(dir, x, y, z, blockDef.Id);
                    var textureIndex = (int) blockDef.Id - 1; // air as no texture, skip
                    MakeFace(dir, x, y, z, textureIndex, bitMask);
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
        private void MakeFace(Direction dir, int x, int y, int z, float textureIndex, int bitMask) {
            CubeMeshData.FaceVertices((int) dir, x % 16, y, z % 16, _vertices, ref _verticesCount);
            //
            // foreach (var faceVertex in CubeMeshData.FaceVertices((int) dir, x % 16 - 8, y, z % 16 - 8)) {
            //     _vertices[_verticesCount] = faceVertex;
            //     _verticesCount++;
            // }
            int blobIndex = AutoTile48Blob.GetBlobIndex(bitMask);

            _uvs[_uvsCount++] = new(1, 0, textureIndex, blobIndex);
            _uvs[_uvsCount++] = new(0, 0, textureIndex, blobIndex);
            _uvs[_uvsCount++] = new(0, 1, textureIndex, blobIndex);
            _uvs[_uvsCount++] = new(1, 1, textureIndex, blobIndex);

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
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
        }
    }
}