using System;
using Cysharp.Threading.Tasks;
using Popcron;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelsEngine.Tools;
using Ray = UnityEngine.Ray;
using Vector3 = Shared.Vector3;
using Vector3Int = Shared.Vector3Int;

namespace VoxelsEngine
{
    public class CharacterAgent : MonoBehaviour
    {
        [Required] public LevelRenderer Level = null!;

        [Required] public Transform CameraTransform = null!;

        private Plane _wkPlane;
        private Plane? _draggingPlane = null;
        private bool _isPlacing = false;

        private void Awake()
        {
        }


        private void Update()
        {
          
        }

        private (Vector3Int?, Vector3Int?) GetBlocksOnPlane(Ray mouseRay, Plane plane)
        {
            Vector3Int? facingCursorPos = null;
            Vector3Int? collidingBlockPos = null;
            var axis = plane.normal;
            int dir = UnityEngine.Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (plane.Raycast(mouseRay, out var enter))
            {
                UnityEngine.Vector3 collisionPos = mouseRay.GetPoint(enter);
                collidingBlockPos = LevelTools.WorldToCell((collisionPos + axis * (0.5f * dir)));
                facingCursorPos = LevelTools.WorldToCell((collisionPos - axis * (0.5f * dir)));
            }

            return (collidingBlockPos, facingCursorPos);
        }


        private (Vector3Int? collidingBlock, Vector3Int? facingBloc0k, Plane? plane) GetCollidedBlockPosition(LevelRenderer level, Ray mouseRay, Vector3 position,
            int radius = 4)
        {
            var (up, upC, upF, upPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.up, Mathf.RoundToInt(position.Y), radius);
            var (right, rightC, rightF, rightPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.right, Mathf.RoundToInt(position.X), radius);
            var (forward, forwardC, forwardF, forwardPlane) = GetCollidedBlockPositionOnPlane(level, mouseRay, Vector3.forward, Mathf.RoundToInt(position.Z), radius);
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
        private (float rayEnter, Vector3Int? collidingBlock, Vector3Int? facingBlock, Plane? p) GetCollidedBlockPositionOnPlane(LevelRenderer level, Ray mouseRay,
            Vector3 axis, int playerPosition, int radius = 4)
        {
            // Define the start and stop positions for the collision check based on the player's position and the specified radius.
            // the goal is to start with blocks that are closer to the camera (=mouse ray origin)
            var start = playerPosition - radius;
            var stop = playerPosition + radius;

            // Determine the direction of the ray along the specified axis.
            int dir = UnityEngine.Vector3.Dot(mouseRay.direction, axis) > 0 ? 1 : -1;
            if (dir < 0) (start, stop) = (stop, start);

            for (int pos = start; pos != stop; pos += dir)
            {
                _wkPlane.SetNormalAndPosition(axis, axis * (pos + dir * 0.5f));
                if (_wkPlane.Raycast(mouseRay, out var enter))
                {
                    Vector3 position = mouseRay.GetPoint(enter);
                    // Determine the position inside the block that the ray intersects.
                    var insidePosition = (position + axis * (0.5f * dir)).WorldToCell();
                    var inside = level.GetCellAt(insidePosition);
                    if (!inside.IsAir())
                    {
                        return (enter, insidePosition, (position - axis * (0.5f * dir)).WorldToCell(), _wkPlane);
                    }
                }
            }

            return (float.MaxValue, null, null, null);
        }
    }
}