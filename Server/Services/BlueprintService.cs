using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.EntityFrameworkCore;
using Server.DbModel;
using Shared;

namespace Server.Services {
    public class BlueprintService {
        private readonly GameSavesContext _context;
        private readonly ConcurrentDictionary<Guid, BlueprintV0> _cache;

        public BlueprintService(GameSavesContext context) {
            _context = context;
            _cache = new ConcurrentDictionary<Guid, BlueprintV0>();
        }

        public async Task<(bool success, string? error)> SaveBlueprintAsync(
            string creatorId,
            string name,
            Vector3Int anchorPosition,
            Vector3Int size,
            LevelMap level,
            BlockPathMapping levelBlockPathMapping
        ) {
            // Validate size (must be odd in X and Z)
            if (size.X % 2 == 0 || size.Z % 2 == 0)
                return (false, "Blueprint size must be odd in X and Z dimensions");

            HashSet<uint> mapped = new HashSet<uint>();

            try {
                // Extract cells from the level within the blueprint area
                var cells = new CellV0[size.X, size.Y, size.Z];
                var blockMapping = new BlockPathMapping();

                // Center is at the anchor point
                var startX = anchorPosition.X - (size.X - 1) / 2;
                var startZ = anchorPosition.Z - (size.Z - 1) / 2;

                for (int x = 0; x < size.X; x++)
                for (int y = 0; y < size.Y; y++)
                for (int z = 0; z < size.Z; z++) {
                    var worldPos = new Vector3Int(startX + x, anchorPosition.Y + y, startZ + z);
                    var foundCell = level.TryGetExistingCell(worldPos);
                    if (!foundCell.HasValue) return (false, $"Invalid cell coordinate: {worldPos}.");
                    var cell = foundCell.Value;
                    cells[x, y, z] = cell;
                    if (mapped.Add(cell.Block)) blockMapping.BlockPathById[cell.Block] = levelBlockPathMapping.BlockPathById[cell.Block];
                }

                var blueprint = new BlueprintV0 {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatorId = creatorId,
                    CreationDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Size = size,
                    Cells = new CellArrayV0(cells, true),
                    BlockMapping = blockMapping,
                    FloorHeight = 0, // Default value, can be modified later
                    PossibleSymmetries = Symmetries.None // Default value, can be modified later
                };

                var serialized = MessagePackSerializer.Serialize(blueprint);

                var dbBlueprint = new DbBlueprint {
                    Id = blueprint.Id,
                    Name = blueprint.Name,
                    CreatorId = blueprint.CreatorId,
                    CreationDate = blueprint.CreationDate,
                    LastModifiedDate = blueprint.LastModifiedDate,
                    SizeX = blueprint.Size.X,
                    SizeY = blueprint.Size.Y,
                    SizeZ = blueprint.Size.Z,
                    SerializedData = serialized,
                    FloorHeight = blueprint.FloorHeight,
                    PossibleSymmetries = (byte) blueprint.PossibleSymmetries
                };

                _context.Blueprints.Add(dbBlueprint);
                await _context.SaveChangesAsync();

                // Add to cache
                _cache[blueprint.Id] = blueprint;

                return (true, null);
            } catch (Exception ex) {
                return (false, ex.Message);
            }
        }

        public async Task<(BlueprintMetadataV0[] blueprints, int totalCount)> GetBlueprintListAsync(int page, int pageSize) {
            var totalCount = await _context.Blueprints.CountAsync();

            var blueprints = await _context.Blueprints
                .OrderByDescending(b => b.LastModifiedDate)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(b => new BlueprintMetadataV0 {
                    Id = b.Id,
                    Name = b.Name,
                    CreatorId = b.CreatorId,
                    CreationDate = b.CreationDate,
                    LastModifiedDate = b.LastModifiedDate,
                    Size = new Vector3Int(b.SizeX, b.SizeY, b.SizeZ)
                })
                .ToArrayAsync();

            return (blueprints, totalCount);
        }

        public async Task<BlueprintV0?> GetBlueprintAsync(Guid id) {
            // Try get from cache first
            if (_cache.TryGetValue(id, out var cached))
                return cached;

            var dbBlueprint = await _context.Blueprints.FindAsync(id);
            if (dbBlueprint == null)
                return null;

            try {
                var blueprint = MessagePackSerializer.Deserialize<BlueprintV0>(dbBlueprint.SerializedData);
                _cache[id] = blueprint;
                return blueprint;
            } catch {
                return null;
            }
        }

        public async Task<bool> PlaceBlueprintAsync(
            Guid blueprintId,
            Vector3Int position,
            byte rotation,
            Symmetries flipOperations,
            LevelMap level
        ) {
            var blueprint = await GetBlueprintAsync(blueprintId);
            if (blueprint == null)
                return false;

            try {
                // Apply transformations
                var cells = blueprint.Cells.Cells;
                var size = blueprint.Size;

                // Create transformed array
                var transformed = new CellV0[size.X, size.Y, size.Z];

                for (int x = 0; x < size.X; x++)
                for (int y = 0; y < size.Y; y++)
                for (int z = 0; z < size.Z; z++) {
                    var (tx, tz) = ApplyTransformations(x, z, size.X, size.Z, rotation, flipOperations);
                    transformed[x, y, z] = cells[tx, y, tz];
                }

                // Place in world
                for (int x = 0; x < size.X; x++)
                for (int y = 0; y < size.Y; y++)
                for (int z = 0; z < size.Z; z++) {
                    var worldPos = new Vector3Int(
                        position.X + x - (size.X - 1) / 2,
                        position.Y + y,
                        position.Z + z - (size.Z - 1) / 2
                    );

                    var cell = transformed[x, y, z];
                    if (cell.Block != BlockId.Air)
                        level.TrySetExistingCell(worldPos.X, worldPos.Y, worldPos.Z, cell.Block);
                }

                return true;
            } catch {
                return false;
            }
        }



        public static (int x, int z) ApplyTransformations(
            int x, int z,
            int sizeX, int sizeZ,
            byte rotation,
            Symmetries flipOperations)
        {
            if (sizeX % 2 == 0 || sizeZ % 2 == 0)
                throw new ArgumentException("Blueprint size must be odd in both X and Z dimensions");

            // Calculate center points
            var centerX = (sizeX - 1) / 2;
            var centerZ = (sizeZ - 1) / 2;

            // Convert to local coordinates (relative to center)
            var localX = x - centerX;
            var localZ = z - centerZ;

            // Apply flipping first (in local coordinates)
            if ((flipOperations & Symmetries.XAxis) != 0)
                localX = -localX;
            if ((flipOperations & Symmetries.ZAxis) != 0)
                localZ = -localZ;

            // Apply rotation (in local coordinates)
            var rotations = rotation % 4;
            for (int i = 0; i < rotations; i++)
            {
                var tempX = localX;
                localX = localZ;
                localZ = -tempX;
            }

            // Convert back to world coordinates
            return (localX + centerX, localZ + centerZ);
        }
    }
}