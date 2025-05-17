#ifndef VOXEL_DATA_UTILS_INCLUDED
#define VOXEL_DATA_UTILS_INCLUDED

// Ces déclarations doivent correspondre aux noms que vous utiliserez
// avec Material.SetBuffer() en C# et aux noms de référence
// des propriétés dans le Shader Graph Blackboard.

StructuredBuffer<uint> _WorldBlockData; // SSBO principal avec les données des blocs (packed)
StructuredBuffer<int> _ChunkIndirectionTable; // Table d'indirection: linearChunkCoord -> slotID

float3 _ChunkDimensions;    // A DÉCLARER PAR SHADER GRAPH VIA LE BLACKBOARD PROPERTY  // (chunkSizeX, chunkSizeY, chunkSizeZ) ex: (16, 64, 16)
float3 _WorldChunkCounts;   // A DÉCLARER PAR SHADER GRAPH VIA LE BLACKBOARD PROPERTY // Nombre de chunks dans chaque dimension du monde pour l'indirection table ex: (128, 1, 128)

#define NORMAL_OFFSET_MULTIPLIER 0.01f

// Fonction pour récupérer les données d'un bloc à une worldPos donnée
// Shader Graph gère mieux les sorties float pour les Vector1. On convertira uint en float.
void GetBlockDataAtWorldPos_float(float3 inputWorldPos, float3 worldNormal, out float outMainTexIdx_float, out float outFrameTexIdx_float)
{
    float3 worldPos = inputWorldPos - worldNormal * NORMAL_OFFSET_MULTIPLIER;
    
    uint outMainTexIdx_uint = 0; // Valeur par défaut (ex: air block)
    uint outFrameTexIdx_uint = 0;

    // 1. Calculer les coordonnées du chunk (chunkCoord_wc)
    float3 invChunkDim = float3(1.0 / _ChunkDimensions.x, 1.0 / _ChunkDimensions.y, 1.0 / _ChunkDimensions.z);
    int3 chunkCoord_wc = (int3)floor(worldPos * invChunkDim);

    // 2. Vérifier si chunkCoord_wc est dans les limites du monde (pour l'indirection table)
    // Note: _WorldChunkCounts est un float3, il faut le caster en int3 pour la comparaison.
    if (any(chunkCoord_wc < 0) || any(chunkCoord_wc >= (int3)_WorldChunkCounts))
    {
        outMainTexIdx_float = 0.0f;
        outFrameTexIdx_float = 0.0f;
        return; // Hors du monde mappé
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
        return;
    }
    // Pour une sécurité plus robuste, GetDimensions serait idéal, mais plus complexe à gérer ici.
    // Pour l'instant, on se fie à la vérification aux limites du monde (étape 2).

    // 4. Récupérer le slotID depuis la table d'indirection
    int chunkSlotID = _ChunkIndirectionTable[linearChunkIndex];

    if (chunkSlotID < 0) // Chunk non chargé ou vide
    {
        outMainTexIdx_float = 0.0f;
        outFrameTexIdx_float = 0.0f;
        return;
    }

    // 5. Calculer les coordonnées locales à l'intérieur du chunk
    // Il faut s'assurer que worldPos est correctement 'flooré' avant le modulo si ce n'est pas déjà un coin de voxel.
    int3 flooredWorldPos = (int3)floor(worldPos);
    int3 localCoord = int3(
        flooredWorldPos.x % (int)_ChunkDimensions.x,
        flooredWorldPos.y % (int)_ChunkDimensions.y,
        flooredWorldPos.z % (int)_ChunkDimensions.z
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

    // SÉCURITÉ : Vérifier les bornes du _WorldBlockData
    // uint ssboSize;
    // _WorldBlockData.GetDimensions(ssboSize);
    // if (finalSSBOIndex < 0 || finalSSBOIndex >= ssboSize) return;

    // 8. Récupérer les données packées et les dépacker
    uint packedData = _WorldBlockData[finalSSBOIndex];

    outMainTexIdx_uint = packedData >> 16;
    outFrameTexIdx_uint = packedData & 0xFFFF; // Masque pour les 16 bits inférieurs

    outMainTexIdx_float = (float)outMainTexIdx_uint;
    outFrameTexIdx_float = (float)outFrameTexIdx_uint;
}

#endif // VOXEL_DATA_UTILS_INCLUDED
