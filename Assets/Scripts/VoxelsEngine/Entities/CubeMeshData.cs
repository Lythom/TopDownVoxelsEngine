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

        public static Vector3[] FaceVertices(int dir, int x, int y, int z) {
            Vector3[] faceVertices = new Vector3[4];
            for (int i = 0; i < 4; i++) {
                faceVertices[i] = Vertices[FaceTriangles[dir][i]] + new Vector3(x, y, z);
            }

            return faceVertices;
        }
    }
}