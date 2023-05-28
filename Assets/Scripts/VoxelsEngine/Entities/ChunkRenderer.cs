using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelsEngine {
    [RequireComponent(typeof(MeshFilter))]
    public class ChunkRenderer : MonoBehaviour {
        public LevelData Level = null!;
        public ChunkKey ChunkKey;

        private Mesh _mesh = null!;
        private readonly List<int> _triangles = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector4> _uvs = new();

        private void Awake() {
            _mesh = GetComponent<MeshFilter>().mesh;
            if (_mesh == null) throw new Exception("No mesh found on ChunkRenderer");
        }

        public void ReCalculateMesh(LevelData level) {
            if (_mesh == null) return;
            var chunk = Level.Chunks[ChunkKey.ChX, ChunkKey.ChZ];
            if (!chunk.IsGenerated) throw new ApplicationException("Ensure Chunk is not null before drawing");

            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            foreach (var (x, y, z) in chunk.GetCellPositions()) {
                var cell = chunk.Cells[x, y, z];
                if (cell.BlockDef != BlockDefId.Air) {
                    var blockDef = Configurator.Instance.BlocksLibrary[(int) cell.BlockDef];
                    MakeCube(x, y, z, blockDef, chunk, level);
                }
            }
        }

        private void MakeCube(int x, int y, int z, BlockDefinition blockDef, ChunkData chunkData, LevelData level) {
            for (int i = 0; i < 6; i++) {
                var dir = (Direction) i;
                var n = level.GetNeighbor(x + chunkData.ChX * 16, y, z + chunkData.ChZ * 16, dir);
                if (n == null || n.Value.BlockDef == BlockDefId.Air) {
                    MakeFace(dir, x, y, z, (int) blockDef.Id);
                }
            }
        }

        private void MakeFace(Direction dir, int x, int y, int z, float textureIndex) {
            _vertices.AddRange(CubeMeshData.FaceVertices((int) dir, x % 16 - 8, y, z % 16 - 8));
            int vCount = _vertices.Count;
            _uvs.Add(new(0, 0, textureIndex));
            _uvs.Add(new(1, 0, textureIndex));
            _uvs.Add(new(1, 1, textureIndex));
            _uvs.Add(new(0, 1, textureIndex));

            _triangles.Add(vCount - 4);
            _triangles.Add(vCount - 4 + 1);
            _triangles.Add(vCount - 4 + 2);
            _triangles.Add(vCount - 4);
            _triangles.Add(vCount - 4 + 2);
            _triangles.Add(vCount - 4 + 3);
        }

        public void UpdateMesh() {
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetUVs(0, _uvs);
            _mesh.RecalculateNormals();
        }
    }
}