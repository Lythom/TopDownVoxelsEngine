using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MessagePack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.DbModel;
using Shared;

namespace Server.Services {
    public class BlueprintService : IBlueprintService {
        private readonly ConcurrentDictionary<Guid, BlueprintV0> _cache;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BlueprintService(IServiceScopeFactory serviceScopeFactory) {
            _serviceScopeFactory = serviceScopeFactory;
            _cache = new ConcurrentDictionary<Guid, BlueprintV0>();
        }

        public async UniTask<(bool success, string? error)> SaveBlueprintAsync(
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


            try {
                var (blueprint, error) = LevelTools.CreateBlueprint(creatorId, name, anchorPosition, size, level, levelBlockPathMapping);
                
                if (blueprint == null || !string.IsNullOrEmpty(error)) return (false, error ?? "Unknown error during blueprint creation");

                var serialized = MessagePackSerializer.Serialize(blueprint);

                var dbBlueprint = new DbBlueprint {
                    Id = blueprint.Id,
                    Name = blueprint.Name,
                    CreatorId = new Guid(blueprint.CreatorId),
                    CreationDate = blueprint.CreationDate,
                    LastModifiedDate = blueprint.LastModifiedDate,
                    SizeX = blueprint.Size.X,
                    SizeY = blueprint.Size.Y,
                    SizeZ = blueprint.Size.Z,
                    SerializedData = serialized,
                    FloorHeight = blueprint.FloorHeight,
                    PossibleSymmetries = (byte) blueprint.PossibleSymmetries
                };

                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();
                context.Blueprints.Add(dbBlueprint);
                await context.SaveChangesAsync();

                // Add to cache
                _cache[blueprint.Id] = blueprint;

                return (true, null);
            } catch (Exception ex) {
                return (false, ex.Message);
            }
        }

        public async UniTask<(BlueprintMetadataV0[] blueprints, int totalCount)> GetBlueprintListAsync(int page, int pageSize) {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();

            var totalCount = await context.Blueprints.AsNoTracking().CountAsync();

            var blueprints = await context.Blueprints
                .AsNoTracking()
                .OrderByDescending(b => b.LastModifiedDate)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(b => new BlueprintMetadataV0 {
                    Id = b.Id,
                    Name = b.Name,
                    CreatorId = b.CreatorId.ToString(),
                    CreationDate = b.CreationDate,
                    LastModifiedDate = b.LastModifiedDate,
                    Size = new Vector3Int(b.SizeX, b.SizeY, b.SizeZ)
                })
                .ToArrayAsync();

            return (blueprints, totalCount);
        }

        public async UniTask<BlueprintV0?> GetBlueprintAsync(Guid id) {
            // Try get from cache first
            if (_cache.TryGetValue(id, out var cached))
                return cached;

            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameSavesContext>();

            var dbBlueprint = await context.Blueprints.FindAsync(id);
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

        public async UniTask<IReadOnlySet<ChunkKey>> PlaceBlueprintAsync(
            Guid blueprintId,
            short anchorX,
            short anchorY,
            short anchorZ,
            byte rotation,
            Symmetries flipOperations,
            LevelMap level
        ) {
            HashSet<ChunkKey> modifiedChunks = new HashSet<ChunkKey>();

            var blueprint = await GetBlueprintAsync(blueprintId);
            if (blueprint == null)
                return modifiedChunks;

            try {
                level.PlaceBlueprint(anchorX, anchorY, anchorZ, rotation, flipOperations, blueprint, modifiedChunks);
                return modifiedChunks;
            } catch {
                return modifiedChunks;
            }
        }
    }
}