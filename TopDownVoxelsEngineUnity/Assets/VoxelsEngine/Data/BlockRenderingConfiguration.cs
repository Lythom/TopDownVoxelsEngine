using System;
using System.Collections.Generic;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;

// Déclaration = Chemin dynamique dans les ressources via nom, sélecteur pour renseigner le bon path à partir des fichiers
// différence runtime et éditeur: en mode éditeur lien direct
// au moment de la compile export en json, export des textures dans les resources, lecture du json au runtime pour accéder aux fichiers resources
[Serializable]
public struct BlockRenderingSide {
    public DirectionFlags Directions;
    
    // A main texture is required
    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainAlbedoTexture;
    
    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainNormalsTexture;
    
    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainHeightsTexture;
    
    // TODO: 
    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameAlbedoTexture;
    
    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameNormalsTexture;
    
    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameHeightsTexture;
    
    // TODO: parfois je veux l'albedo du main mais le heights de la texture ? Bah non faut rajouter une frame
    // TODO: parfois je veux le normals du frame mais l'albedo du mains
}

[Serializable]
public struct BlockRenderingConfiguration {
    public static BlockRenderingConfiguration AIR = new(0);

    public BlockId Id;

    [ListDrawerSettings(ShowFoldout = false)]
    public List<BlockRenderingSide> Sides;

    public int FrameTextureIndex;
    public int FrameNormalIndex;
    public int MainTextureIndex;
    public int MainNormalIndex;

    public Texture? ItemPreview;

    public BlockRenderingConfiguration(BlockId id) {
        Id = id;
        ItemPreview = null;
        MainTextureIndex = -1;
        MainNormalIndex = -1;
        FrameTextureIndex = (int) id - 1;
        FrameNormalIndex = 0;
        Sides = new List<BlockRenderingSide>();
    }
}