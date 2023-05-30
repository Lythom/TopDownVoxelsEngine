using UnityEngine;

namespace Popcron {
    public class BCubeDrawer : Drawer {
        public static void Cube(Vector3 center, Quaternion rotation, Vector3 size, Color? color = null, bool dashed = false) {
            Gizmos.Draw<BCubeDrawer>(color, dashed, center, rotation, size);
        }

        public override int Draw(ref Vector3[] buffer, params object[] values) {
            Vector3 position = (Vector3) values[0];
            Quaternion rotation = (Quaternion) values[1];
            Vector3 size = (Vector3) values[2];

            size *= 0.5f;

            Vector3 point1 = new Vector3(position.x - size.x, position.y - size.y, position.z - size.z);
            Vector3 point2 = new Vector3(position.x + size.x, position.y - size.y, position.z - size.z);
            Vector3 point3 = new Vector3(position.x + size.x, position.y + size.y, position.z - size.z);
            Vector3 point4 = new Vector3(position.x - size.x, position.y + size.y, position.z - size.z);

            Vector3 point5 = new Vector3(position.x - size.x, position.y - size.y, position.z + size.z);
            Vector3 point6 = new Vector3(position.x + size.x, position.y - size.y, position.z + size.z);
            Vector3 point7 = new Vector3(position.x + size.x, position.y + size.y, position.z + size.z);
            Vector3 point8 = new Vector3(position.x - size.x, position.y + size.y, position.z + size.z);

            point1 = rotation * (point1 - position);
            point1 += position;

            point2 = rotation * (point2 - position);
            point2 += position;

            point3 = rotation * (point3 - position);
            point3 += position;

            point4 = rotation * (point4 - position);
            point4 += position;

            point5 = rotation * (point5 - position);
            point5 += position;

            point6 = rotation * (point6 - position);
            point6 += position;

            point7 = rotation * (point7 - position);
            point7 += position;

            point8 = rotation * (point8 - position);
            point8 += position;

            //bottom
            buffer[0] = point1;
            buffer[1] = point2;

            buffer[2] = point2;
            buffer[3] = point3;

            buffer[4] = point3;
            buffer[5] = point4;

            buffer[6] = point4;
            buffer[7] = point1;

            //left
            buffer[8] = point1;
            buffer[9] = point5;

            buffer[10] = point5;
            buffer[11] = point8;

            buffer[12] = point8;
            buffer[13] = point4;

            buffer[14] = point4;
            buffer[15] = point8;

            //top
            buffer[16] = point8;
            buffer[17] = point7;

            buffer[18] = point7;
            buffer[19] = point6;

            buffer[20] = point6;
            buffer[21] = point5;

            buffer[22] = point5;
            buffer[23] = point6;

            // right face
            buffer[24] = point6;
            buffer[25] = point2;

            buffer[26] = point2;
            buffer[27] = point3;

            buffer[28] = point3;
            buffer[29] = point7;

            return 30;
        }
    }
}