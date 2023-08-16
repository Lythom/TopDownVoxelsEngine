using System;
using System.Collections.Generic;
using System.Linq;
using LoneStoneStudio.Tools;
using Shared;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public struct PropPlacement {
        public Vector3 Position;
        public Vector3 Angle;
    }

    /// <summary>
    /// Generate a mesh for the chunk ChunkKey of Level.
    /// The renderer game object should be placed at (chX * ChunkData.Size, 0, chY * ChunkData.Size).
    /// The rendered cells are centered, which means cell at (0,0,0) boundaries are visually at (-0.5,-0.5,-0.5)->(0.5,0.5,0.5).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class ChunkRenderer : MonoBehaviour {
        public LevelMap Level = null!;

        private Mesh _mesh = null!;
        private readonly int[] _triangles = new int[20000];
        private int _trianglesCount = 0;
        private readonly Vector3[] _vertices = new Vector3[10000];
        private int _verticesCount = 0;
        private readonly Vector4[] _uvs = new Vector4[10000];
        private readonly Vector4[] _uvs2 = new Vector4[10000];
        private int _uvsCount = 0;
        private int _uvs2Count = 0;
        private Dictionary<GameObject, List<PropPlacement>> Props = new();
        private Transform _propsContainer = null!;

        private void Awake() {
            _mesh = GetComponent<MeshFilter>().mesh;
            if (_mesh == null) throw new Exception("No mesh found on ChunkRenderer");
            Props.Add(Configurator.Instance.GrassProp, new List<PropPlacement>());
            var pc = new GameObject();
            pc.name = "Props";
            pc.transform.parent = transform;
            _propsContainer = pc.transform;
        }

        public bool ReCalculateMesh(LevelMap level, ChunkKey chunkKey) {
            var chunk = Level.Chunks[chunkKey.ChX, chunkKey.ChZ];
            if (!chunk.IsGenerated) throw new ApplicationException("Ensure Chunk is not null before drawing");

            _trianglesCount = 0;
            _verticesCount = 0;
            _uvsCount = 0;
            _uvs2Count = 0;
            foreach (var (x, y, z) in chunk.GetCellPositions()) {
                var cell = chunk.Cells[x, y, z];
                if (cell.Block != BlockId.Air) {
                    var blockDef = Configurator.Instance.BlocksRenderingLibrary[(int) cell.Block];
                    MakeCube(x, y, z, chunkKey, blockDef, chunk, level);
                }
            }

            return true;
        }

        private void MakeCube(int cX, int cY, int cZ, ChunkKey chunkKey, BlockRenderingConfiguration blockDef, Chunk chunk, LevelMap level) {
            for (int i = 0; i < 6; i++) {
                var dir = (Direction) (i + 1);
                var x = cX + chunkKey.ChX * Chunk.Size;
                var y = cY;
                var z = cZ + chunkKey.ChZ * Chunk.Size;
                var n = level.GetNeighbor(x, cY, z, dir);
                if (n == null || n.Value.Block == BlockId.Air) {
                    var bitMask = AutoTile48Blob.Get8SurroundingsBitmask(dir, x, y, z, blockDef.Id, Level.CellMatchDefinition);
                    MakeFace(dir, x, y, z, blockDef, bitMask);
                }

                // TODO: use GPU instancing
                if (dir == Direction.Up) {
                    if (n != null && n.Value.Block == BlockId.Grass) {
                        if (Props.TryGetValue(Configurator.Instance.GrassProp, out var list)) {
                            for (int j = 0; j < 1; j++) {
                                // list.Add(new PropPlacement() {
                                //     Position = new Vector3(x + Random.Range(-0.4f, 0.4f), y + 1.48f, z + Random.Range(-0.4f, 0.4f)),
                                //     Angle = new Vector3(0, Random.Range(0, 359), 0)
                                // });
                            }
                        }
                    }
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
        /// <param name="bitMask">positions of the neighbours cells of the same type</param>
        private void MakeFace(Direction dir, int x, int y, int z, BlockRenderingConfiguration block, int bitMask) {
            if (block.Sides.Count == 0) return;
            CubeMeshData.FaceVertices((int) dir - 1, x % Chunk.Size, y, z % Chunk.Size, _vertices, ref _verticesCount);
            //
            // foreach (var faceVertex in CubeMeshData.FaceVertices((int) dir, x % 16 - 8, y, z % 16 - 8)) {
            //     _vertices[_verticesCount] = faceVertex;
            //     _verticesCount++;
            // }
            int blobIndex = AutoTile48Blob.GetBlobIndex(bitMask);
            var side = block.Sides.FirstOrDefault(s => s.Directions.HasFlagFast(dir)) ?? block.Sides[0];

            float mainTextureAlbedoNormals = CSharpToShaderPacking.PackTwo(side.MainTextureIndex, side.MainNormalsIndex);
            float frameAlbedoNormals = CSharpToShaderPacking.PackTwo(side.FrameTextureIndex, side.FrameNormalsIndex);
            float windFactor = CSharpToShaderPacking.PackThree(Mathf.FloorToInt(side.MainWindIntensity * 254), Mathf.FloorToInt(side.FrameWindIntensity * 254), 0);

            _uvs[_uvsCount++] = new(1, 0, mainTextureAlbedoNormals, side.MainHeightsIndex);
            _uvs[_uvsCount++] = new(0, 0, mainTextureAlbedoNormals, side.MainHeightsIndex);
            _uvs[_uvsCount++] = new(0, 1, mainTextureAlbedoNormals, side.MainHeightsIndex);
            _uvs[_uvsCount++] = new(1, 1, mainTextureAlbedoNormals, side.MainHeightsIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, windFactor, frameAlbedoNormals, side.FrameHeightsIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, windFactor, frameAlbedoNormals, side.FrameHeightsIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, windFactor, frameAlbedoNormals, side.FrameHeightsIndex);
            _uvs2[_uvs2Count++] = new(blobIndex, windFactor, frameAlbedoNormals, side.FrameHeightsIndex);

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
            _propsContainer.DestroyChildren();
            foreach (var (obj, placements) in Props) {
                foreach (var p in placements) {
                    var i = Instantiate(obj, _propsContainer);
                    i.transform.position = p.Position;
                    i.transform.eulerAngles = p.Angle;
                }
            }
        }
    }
}