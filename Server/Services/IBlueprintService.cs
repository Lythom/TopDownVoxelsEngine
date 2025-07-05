using System;
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

    public UniTask<CellV0[,,]?> PlaceBlueprintAsync(
        Guid blueprintId,
        Vector3Int position,
        byte rotation,
        Symmetries flipOperations,
        LevelMap level
    );
}