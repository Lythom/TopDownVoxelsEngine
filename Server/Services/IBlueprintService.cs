using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Shared;

namespace Server.Services;

public interface IBlueprintService {
    public UniTask<(bool success, string? error)> SaveBlueprintAsync(
        string creatorId,
        string name,
        Vector3Int anchorPosition,
        Vector3Int size,
        LevelMap level,
        BlockPathMapping levelBlockPathMapping
    );

    public UniTask<(BlueprintMetadataV0[] blueprints, int totalCount)> GetBlueprintListAsync(int page, int pageSize);

    public UniTask<BlueprintV0?> GetBlueprintAsync(Guid id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="blueprintId"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="rotation"></param>
    /// <param name="flipOperations"></param>
    /// <param name="level">from State, LevelMap to be updated</param>
    /// <returns>Modified chunks</returns>
    public UniTask<IReadOnlySet<ChunkKey>> PlaceBlueprintAsync(
        Guid blueprintId,
        short x,
        short y,
        short z,
        byte rotation,
        Symmetries flipOperations,
        LevelMap level
    );
}