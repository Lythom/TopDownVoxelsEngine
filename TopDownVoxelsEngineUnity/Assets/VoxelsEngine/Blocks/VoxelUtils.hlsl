#ifndef VOXEL_DATA_UTILS_INCLUDED
#define VOXEL_DATA_UTILS_INCLUDED

// Ces déclarations doivent correspondre aux noms que vous utiliserez
// avec Material.SetBuffer() en C# et aux noms de référence
// des propriétés dans le Shader Graph Blackboard.

StructuredBuffer<uint> _WorldBlockData; // SSBO principal avec les données des blocs (packed)
StructuredBuffer<int> _ChunkIndirectionTable; // Table d'indirection: linearChunkCoord -> slotID

float3 _ChunkDimensions; // (chunkSizeX, chunkSizeY, chunkSizeZ) ex: (16, 64, 16)
float3 _WorldChunkCounts; // Nombre de chunks dans chaque dimension du monde pour l'indirection table ex: (128, 1, 128)

#define NORMAL_OFFSET_MULTIPLIER 0.001f

// Fonction pour récupérer les données d'un bloc à une worldPos donnée
// Shader Graph gère mieux les sorties float pour les Vector1. On convertira uint en float.
void _GetBlockDataAtWorldPos_Internal(float3 queryWorldPos, float3 queryWorldNormal, out float outMainTexIdx_float, out float outFrameTexIdx_float)
{
    // Apply offset to queryWorldPos to sample *inside* the block if a normal is provided
    float3 effectiveWorldPos = queryWorldPos - queryWorldNormal * NORMAL_OFFSET_MULTIPLIER;

    uint outMainTexIdx_uint = 0; // Default to air/empty
    uint outFrameTexIdx_uint = 0;

    // 1. Calculer les coordonnées du chunk (chunkCoord_wc)
    float3 invChunkDim = float3(1.0 / _ChunkDimensions.x, 1.0 / _ChunkDimensions.y, 1.0 / _ChunkDimensions.z);
    int3 chunkCoord_wc = (int3)floor(effectiveWorldPos * invChunkDim);

    // 2. Vérifier si chunkCoord_wc est dans les limites du monde (pour l'indirection table)
    // Note: _WorldChunkCounts est un float3, il faut le caster en int3 pour la comparaison.
    if (any(chunkCoord_wc < 0) || any(chunkCoord_wc >= (int3)_WorldChunkCounts))
    {
        outMainTexIdx_float = 0.0f;
        outFrameTexIdx_float = 0.0f;
        return;
    }

    // 3. Calculer l'index linéaire pour la table d'indirection (X, Z, Y order)
    int linearChunkIndex = chunkCoord_wc.x +
        chunkCoord_wc.z * (int)_WorldChunkCounts.x +
        chunkCoord_wc.y * (int)_WorldChunkCounts.x * (int)_WorldChunkCounts.z;


    // SÉCURITÉ : Vérifier les bornes de la table d'indirection
    // Cela suppose que vous avez un moyen de connaître la taille de _ChunkIndirectionTable.
    // Si _ChunkIndirectionTable est toujours dimensionné exactement pour _WorldChunkCounts:
    int maxLinearIndex = (int)_WorldChunkCounts.x * (int)_WorldChunkCounts.y * (int)_WorldChunkCounts.z - 1;
    if (linearChunkIndex < 0 || linearChunkIndex > maxLinearIndex)
    {
        outMainTexIdx_float = 0.0f; // Should be caught by world bounds check, but good safety.
        outFrameTexIdx_float = 0.0f;
        return;
    }
    // Pour une sécurité plus robuste, GetDimensions serait idéal, mais plus complexe à gérer ici.
    // Pour l'instant, on se fie à la vérification aux limites du monde (étape 2).

    // 4. Récupérer le slotID depuis la table d'indirection
    int chunkSlotID = _ChunkIndirectionTable[linearChunkIndex];

    if (chunkSlotID < 0)
    {
        outMainTexIdx_float = 0.0f;
        outFrameTexIdx_float = 0.0f;
        return;
    }

    int3 flooredEffectiveWorldPos = (int3)floor(effectiveWorldPos);
    int3 localCoord = int3(
        flooredEffectiveWorldPos.x % (int)_ChunkDimensions.x,
        flooredEffectiveWorldPos.y % (int)_ChunkDimensions.y,
        flooredEffectiveWorldPos.z % (int)_ChunkDimensions.z
    );
    // Assurer que les résultats du modulo sont positifs (HLSL % peut donner du négatif si l'opérande de gauche est négatif)
    localCoord.x = (localCoord.x + (int)_ChunkDimensions.x) % (int)_ChunkDimensions.x;
    localCoord.y = (localCoord.y + (int)_ChunkDimensions.y) % (int)_ChunkDimensions.y;
    localCoord.z = (localCoord.z + (int)_ChunkDimensions.z) % (int)_ChunkDimensions.z;


    // 6. Calculer l'index local à l'intérieur des données du chunk (X, Z, Y order)
    int localBlockIndex = localCoord.x +
        localCoord.z * (int)_ChunkDimensions.x +
        localCoord.y * (int)_ChunkDimensions.x * (int)_ChunkDimensions.z;

    // 7. Calculer l'offset final dans le grand SSBO
    int voxelsPerChunk = (int)_ChunkDimensions.x * (int)_ChunkDimensions.y * (int)_ChunkDimensions.z;
    int finalSSBOIndex = chunkSlotID * voxelsPerChunk + localBlockIndex;

    // Basic SSBO bounds check (assuming _WorldBlockData is non-empty and correctly sized)
    // A more robust check would involve getting buffer dimensions, but that's heavier.
    // if (finalSSBOIndex < 0) { /* handle error or default */ } // Max check not easily done here without buffer size.

    // 8. Récupérer les données packées et les dépacker
    uint packedData = _WorldBlockData[finalSSBOIndex];

    outMainTexIdx_uint = packedData >> 16;
    outFrameTexIdx_uint = packedData & 0xFFFF;

    outMainTexIdx_float = (float)outMainTexIdx_uint;
    outFrameTexIdx_float = (float)outFrameTexIdx_uint;
}

// Main function to get data for blending
void GetVoxelDataForBlending_float(
    float3 hitWorldPos, // The world position of the ray-surface intersection
    float3 hitWorldNormal, // The normal of the surface at hitWorldPos

    out float outMainTexId_Current,
    out float outFrameTexId_Current, // Also return frameTexId for current block

    out float outMainTexId_PlaneXSide, // Neighbor along the plane's "X" axis
    out float outMainTexId_PlaneYSide, // Neighbor along the plane's "Y" axis
    out float outMainTexId_PlaneDiagonal, // Diagonal neighbor on the plane

    out float outDistToWorldXFace, // Squared distance from the world X-aligned face of the current voxel
    out float outDistToWorldYFace, // Distance from the world Y-aligned face of the current voxel
    out float outDistToWorldDiagFace // Distance from the world Diagonal-aligned face of the current voxel
)
{
    // 1. Get data for the current block (the one whose face was hit)
    _GetBlockDataAtWorldPos_Internal(hitWorldPos, hitWorldNormal, outMainTexId_Current, outFrameTexId_Current);

    // This is the world position *inside* the current block, used for determining its integer coords and fractional position
    float3 currentBlockSamplePos = hitWorldPos - hitWorldNormal * NORMAL_OFFSET_MULTIPLIER;
    int3 currentBlockIntegerCoords = (int3)floor(currentBlockSamplePos);

    // 4. Calculate distances to WORLD-ALIGNED faces of the *current* voxel
    // These distances are based on the fractional part of currentBlockSamplePos (point inside the current block).
    // frac_pos.x is the distance from the min-X face of the current voxel.
    // frac_pos.y is the distance from the min-Y face of the current voxel.
    // frac_pos.z is the distance from the min-Z face of the current voxel.
    float3 frac_pos = frac(currentBlockSamplePos);

    // 2. Determine plane axes based on the dominant component of hitWorldNormal.
    // These define the directions for "X side" and "Y side" *on the surface plane*.
    float3 absNormal = abs(hitWorldNormal);
    float3 plane_axis_x_dir; // Integer offset vector for "X-side" relative to the plane
    float3 plane_axis_y_dir; // Integer offset vector for "Y-side" relative to the plane

    if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z) // Normal is primarily Y (e.g., floor/ceiling)
    {
        plane_axis_x_dir = float3(frac_pos.x < 0.5 ? -1 : 1, 0, 0); // World X
        plane_axis_y_dir = float3(0, 0, frac_pos.z < 0.5 ? -1 : 1); // World Z
        // outDistToWorldXFace = 0.5 - (frac_pos.x < 0.5 ? frac_pos.x : 1.0 - frac_pos.x);
        outDistToWorldXFace = abs(frac_pos.x - 0.5);
        outDistToWorldYFace = abs(frac_pos.z - 0.5);
    }
    else if (absNormal.x >= absNormal.y && absNormal.x >= absNormal.z) // Normal is primarily X (e.g., west/east wall)
    {
        plane_axis_x_dir = float3(0, 0, frac_pos.z < 0.5 ? -1 : 1); // World Z
        plane_axis_y_dir = float3(0, frac_pos.y < 0.5 ? -1 : 1, 0); // World Y
        outDistToWorldXFace = abs(frac_pos.z - 0.5);
        outDistToWorldYFace = abs(frac_pos.y - 0.5);
    }
    else // Normal is primarily Z (e.g., north/south wall)
    {
        plane_axis_x_dir = float3(frac_pos.x < 0.5 ? -1 : 1, 0, 0); // World X
        plane_axis_y_dir = float3(0, frac_pos.y < 0.5 ? -1 : 1, 0); // World Y
        outDistToWorldXFace = abs(frac_pos.x - 0.5);
        outDistToWorldYFace = abs(frac_pos.y - 0.5);
    }
    outDistToWorldDiagFace = (outDistToWorldXFace + outDistToWorldYFace) * 1.412f;


    // 3. Get data for neighbor blocks
    // For these lookups, we use (0,0,0) as the normal, so no NORMAL_OFFSET_MULTIPLIER is applied.
    // We are querying by the block's base integer coordinate.
    float dummyFrameTexId; // We only need mainTexId for neighbors

    // Neighbor along the plane's "X" axis
    float3 queryPos_PlaneXSide = (float3)currentBlockIntegerCoords + plane_axis_x_dir;
    _GetBlockDataAtWorldPos_Internal(queryPos_PlaneXSide, float3(0, 0, 0), outMainTexId_PlaneXSide, dummyFrameTexId);

    // Neighbor along the plane's "Y" axis
    float3 queryPos_PlaneYSide = (float3)currentBlockIntegerCoords + plane_axis_y_dir;
    _GetBlockDataAtWorldPos_Internal(queryPos_PlaneYSide, float3(0, 0, 0), outMainTexId_PlaneYSide, dummyFrameTexId);

    // Diagonal neighbor on the plane
    float3 queryPos_PlaneDiagonal = (float3)currentBlockIntegerCoords + plane_axis_x_dir + plane_axis_y_dir;
    _GetBlockDataAtWorldPos_Internal(queryPos_PlaneDiagonal, float3(0, 0, 0), outMainTexId_PlaneDiagonal, dummyFrameTexId);
}

void GetHighestIndex_float(UnityTexture2DArray heightmaps, UnitySamplerState samplerState, float4 indexes, float4 weights, float2 uv, out float outHighestTexIdx, out float outHeight)
{
    float4 samples;
    samples.x = SAMPLE_TEXTURE2D_ARRAY(heightmaps, samplerState, uv, indexes.x).r * (weights.x + 0.5);
    samples.y = SAMPLE_TEXTURE2D_ARRAY(heightmaps, samplerState, uv, indexes.y).r * (weights.y + 0.5);
    samples.z = SAMPLE_TEXTURE2D_ARRAY(heightmaps, samplerState, uv, indexes.z).r * (weights.z - 0.4);
    samples.w = SAMPLE_TEXTURE2D_ARRAY(heightmaps, samplerState, uv, indexes.w).r * weights.w;

    outHeight = 0;
    
    if (indexes.x > 0 && samples.x >= samples.y && samples.x >= samples.w)
    {
        outHighestTexIdx = indexes.x;
        outHeight = samples.x;
    }
    else if (indexes.y > 0 && samples.y >= samples.x && samples.y >= samples.w)
    {
        outHighestTexIdx = indexes.y;
        outHeight = samples.y;
    }
    else if (indexes.z > 0 && samples.z >= samples.y && samples.z >= samples.w)
    {
        outHighestTexIdx = indexes.z;
        outHeight = samples.z;
    }
    else
    {
        outHighestTexIdx = indexes.w;
        outHeight = samples.w;
    }
}

#endif // VOXEL_DATA_UTILS_INCLUDED
