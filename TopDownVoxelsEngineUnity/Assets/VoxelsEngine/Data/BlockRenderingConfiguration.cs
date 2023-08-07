using System;
using System.Collections.Generic;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3Int = Shared.Vector3Int;

// Déclaration = Chemin dynamique dans les ressources via nom, sélecteur pour renseigner le bon path à partir des fichiers
// différence runtime et éditeur: en mode éditeur lien direct
// au moment de la compile export en json, export des textures dans les resources, lecture du json au runtime pour accéder aux fichiers resources
[Serializable]
public class BlockRenderingSide {
    public DirectionFlag Directions;

    // A main texture is required
    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainAlbedoTexture;

    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainNormalsTexture;

    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainHeightsTexture;

    public float MainWindIntensity = 0;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameAlbedoTexture;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameNormalsTexture;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameHeightsTexture;

    public float FrameWindIntensity = 0;

    [HideInInspector]
    public int FrameTextureIndex;

    [HideInInspector]
    public int FrameNormalsIndex;

    [HideInInspector]
    public int FrameHeightsIndex;

    [HideInInspector]
    public int MainTextureIndex;

    [HideInInspector]
    public int MainNormalsIndex;

    [HideInInspector]
    public int MainHeightsIndex;
}

[Serializable]
public struct BlockRenderingConfiguration {
    public static BlockRenderingConfiguration AIR = new(0);

    public BlockId Id;

    [ListDrawerSettings(ShowFoldout = false)]
    public List<BlockRenderingSide> Sides;


    public Texture? ItemPreview;

    public BlockRenderingConfiguration(BlockId id) {
        Id = id;
        ItemPreview = null;
        Sides = new List<BlockRenderingSide>();
    }
}