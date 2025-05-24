using UnityEngine;

namespace VoxelsEngine {
    internal static class CubeMeshData {
        //        4-------5
        //          `.    |
        //     2-------3  |
        //     |  7-------6
        //     |          
        //     1-------0 
        public static readonly Vector3[] Vertices = {
            new(0.5f, -0.5f, -0.5f),
            new(-0.5f, -0.5f, -0.5f),
            new(-0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(-0.5f, 0.5f, 0.5f),
            new(0.5f, 0.5f, 0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(-0.5f, -0.5f, 0.5f),
        };

        public static readonly int[][] FaceTriangles = {
            new[] {0, 1, 2, 3}, // North
            new[] {7, 6, 5, 4}, // South
            new[] {1, 7, 4, 2}, // East
            new[] {6, 0, 3, 5}, // West
            new[] {3, 2, 4, 5}, // Up
            new[] {6, 7, 1, 0} // Down
        };

        /// <summary>
        /// Will provide the vertices for the face. The provided array must have enough room.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="vertices">Array where to add the faces to.</param>
        /// <param name="verticesCount">current count of vertices = location where to add the new vertices. Will be updated by this function.</param>
        public static void FaceVertices(int dir, int x, int y, int z, Vector3[] vertices, ref int verticesCount) {
            for (int i = 0; i <= 3; i++) {
                if (verticesCount >= vertices.Length) {
                    return;
                }

                vertices[verticesCount++] = Vertices[FaceTriangles[dir][i]] + new Vector3(x, y, z);
            }
        }
    }
}