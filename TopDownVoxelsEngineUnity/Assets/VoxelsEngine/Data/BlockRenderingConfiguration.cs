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
    public string MainAlbedoTexture = null!;

    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainNormalsTexture = null!;

    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainHeightsTexture = null!;

    public float MainWindIntensity = 0;

    // A frame is optional, it will overlay framing texture around the main texture and tty to blend according to heights
    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameAlbedoTexture = null!;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameNormalsTexture = null!;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameHeightsTexture = null!;

    public float FrameWindIntensity = 0;

    // index of the frame texture. Should be multiplied by 55 to get the 1st tile sprite of the tiles serie.
    [HideInInspector]
    public int FrameTextureIndex;

    [HideInInspector]
    public int FrameNormalsIndex;

    [HideInInspector]
    public int FrameHeightsIndex;

    // Index of the main texture.
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