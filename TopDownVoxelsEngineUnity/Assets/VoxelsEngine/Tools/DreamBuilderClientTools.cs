using Shared;
using UnityEngine;
using Gizmos = Popcron.Gizmos;
using Ray = UnityEngine.Ray;
using Vector3 = UnityEngine.Vector3;
using Vector3Int = UnityEngine.Vector3Int;

namespace VoxelsEngine.VoxelsEngine.Tools {
    public static class DreamBuilderClientTools {
        public static (Vector3Int?, Vector3Int?) GetBlocksOnPlane(this Ray mouseRay, Plane plane) {
            Vector3Int? facingCursorPos = null;
            Vector3Int? collidingBlockPos = null;
            Vector3 axis = plane.normal;
            int dir = Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (plane.Raycast(mouseRay, out var enter)) {
                Vector3 collisionPos = mouseRay.GetPoint(enter);
                collidingBlockPos = LevelTools.WorldToCell((collisionPos + axis * (0.5f * dir)));
                facingCursorPos = LevelTools.WorldToCell((collisionPos - axis * (0.5f * dir)));
            }

            return (collidingBlockPos, facingCursorPos);
        }

        public static Vector3 ClosestPointToRay(Vector3Int point, Ray ray) {
            Vector3 lhs = point - ray.origin;
            float dotP = Vector3.Dot(lhs, ray.direction);
            return ray.origin + ray.direction * dotP;
        }

        public static (Vector3Int?, Vector3Int?) GetBlocksOnLine(this Ray mouseRay, Plane plane, Vector3Int draggingStartPosition) {
            int dir = Vector3.Dot(mouseRay.direction, plane.normal) > 0 ? 1 : -1;
            Vector3Int? collidingBlockPos = LevelTools.WorldToCell(draggingStartPosition + plane.normal * dir);
            Vector3Int? facingCursorPos = draggingStartPosition;
            var closestPointToRay = ClosestPointToRay(facingCursorPos.Value, mouseRay);
            float minDistance = Vector3.Distance(facingCursorPos.Value, closestPointToRay);

            var axis = facingCursorPos.Value - collidingBlockPos.Value;
           //  Debug.Log(collidingBlockPos);

            for (int i = 0; i < 31; i++) {
                closestPointToRay = ClosestPointToRay(facingCursorPos.Value + axis, mouseRay);
                var nextDistance = Vector3.Distance(facingCursorPos.Value + axis, closestPointToRay);
                if (nextDistance < minDistance) {
                    collidingBlockPos = facingCursorPos.Value;
                    facingCursorPos = facingCursorPos.Value + axis;
                    minDistance = nextDistance;
                } else {
                    Gizmos.Line(closestPointToRay, facingCursorPos.Value, Color.gray);
                    break;
                }
            }

            return (collidingBlockPos, facingCursorPos);
        }


        public static (Shared.Vector3Int? collidingBlock, Shared.Vector3Int? facingBloc0k, Plane? plane) GetCollidedBlockPosition(
            this Ray mouseRay,
            LevelMap level,
            Vector3 position,
            int radius = 4
        ) {
            var (up, upC, upF, upPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.up, Mathf.RoundToInt(position.y), radius);
            var (right, rightC, rightF, rightPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.right, Mathf.RoundToInt(position.x), radius);
            var (forward, forwardC, forwardF, forwardPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.forward, Mathf.RoundToInt(position.z), radius);
            var min = Mathf.Min(up, right, forward);
            if (up > 0 && up == min) return (upC, upF, upPlane);
            if (right > 0 && right == min) return (rightC, rightF, rightPlane);
            if (forward > 0 && forward == min) return (forwardC, forwardF, forwardPlane);
            return (null, null, null);
        }

        /// <summary>
        /// Determines the block position that a ray collides with in a given level.
        /// </summary>
        /// <param name="level">The level in which to check for collisions.</param>
        /// <param name="mouseRay">The ray to check for collisions.</param>
        /// <param name="axis">Should be the positive vector indicating the Plane orientation. ie. Vector3.up, Vector3.right or Vector3.forward.</param>
        /// <param name="playerPosition">The position of the player in the level.</param>
        /// <param name="radius">The radius within which to check for collisions. Default is 4.</param>
        /// <returns>The distance at which the ray enters the block, the position of the block that the ray collides with, and the position of the block that faces the colliding face.</returns>
        public static (float rayEnter, Shared.Vector3Int? collidingBlock, Shared.Vector3Int? facingBloc, Plane? p) GetCollidedBlockPositionOnPlane(
            LevelMap level,
            Ray mouseRay,
            Vector3 axis,
            int playerPosition,
            int radius = 4
        ) {
            // Define the start and stop positions for the collision check based on the player's position and the specified radius.
            // the goal is to start with blocks that are closer to the camera (=mouse ray origin)
            var start = playerPosition - radius;
            var stop = playerPosition + radius;
            Plane plane = new Plane();

            // Determine the direction of the ray along the specified axis.
            int dir = UnityEngine.Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (dir < 0) (start, stop) = (stop, start);

            for (int pos = start; pos != stop; pos += dir) {
                // put the plane aligned with the backface
                plane.SetNormalAndPosition(axis, axis * (pos + dir * 0.5f));
                if (plane.Raycast(mouseRay, out var enter)) {
                    Vector3 position = mouseRay.GetPoint(enter);
                    // Determine the position inside the block that the ray intersects.
                    var insidePosition = LevelTools.WorldToCell(position + axis * (0.5f * dir));
                    var inside = level.TryGetExistingCell(insidePosition);
                    if (!inside.IsAir()) {
                        return (enter, insidePosition, LevelTools.WorldToCell(position - axis * (0.5f * dir)), plane);
                    }
                }
            }

            return (float.MaxValue, null, null, null);
        }
    }
}