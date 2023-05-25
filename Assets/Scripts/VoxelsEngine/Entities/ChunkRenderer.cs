using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelsEngine {
    [RequireComponent(typeof(MeshFilter))]
    public class ChunkRenderer : MonoBehaviour {
        public Chunk? Chunk;
        private Mesh _mesh = null!;
        private readonly List<int> _triangles = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector4> _uvs = new();
        private readonly List<Vector4> _uvs2 = new();

        private void Awake() {
            _mesh = GetComponent<MeshFilter>().mesh;
            if (_mesh == null) throw new Exception("No mesh found on ChunkRenderer");
        }

        public void Redraw(LevelData level) {
            if (_mesh == null) return;
            if (Chunk == null) throw new ApplicationException("Ensure Chunk is not null before drawing");

            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _uvs2.Clear();
            foreach (var (x, y, z) in Chunk.GetCellPositions()) {
                string key = $"{x}_{y}_{z}";
                var cell = Chunk.Cells[x, y, z];
                if (cell.BlockDefinition != "AIR") {
                    var blockDef = Configurator.Instance.BlocksLibrary[cell.BlockDefinition];
                    MakeCube(x, y, z, blockDef, level);
                }
            }

            UpdateMesh();
        }

        private void MakeCube(int x, int y, int z, BlockDefinition blockDef, LevelData level) {
            for (int i = 0; i < 6; i++) {
                var dir = (Direction) i;
                var n = level.GetNeighbor(x, y, z, dir);
                if (n == null || n.Value.BlockDefinition == "AIR") {
                    MakeFace(dir, x, y, z, blockDef.TextureIndex);
                }
            }
        }

        private void MakeFace(Direction dir, int x, int y, int z, float textureIndex) {
            _vertices.AddRange(CubeMeshData.FaceVertices((int) dir, x % 16, y, z % 16));
            int vCount = _vertices.Count;
            _uvs.Add(new(0, 0, textureIndex));
            _uvs.Add(new(1, 0, textureIndex));
            _uvs.Add(new(1, 1, textureIndex));
            _uvs.Add(new(0, 1, textureIndex));
            _uvs2.Add(new(textureIndex, textureIndex));
            _uvs2.Add(new(textureIndex, textureIndex));
            _uvs2.Add(new(textureIndex, textureIndex));
            _uvs2.Add(new(textureIndex, textureIndex));


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
            _mesh.RecalculateTangents();
        }
    }
}