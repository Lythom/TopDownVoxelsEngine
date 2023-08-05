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

    // TODO: 
    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameAlbedoTexture;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameNormalsTexture;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
    public string FrameHeightsTexture;

    public int FrameTextureIndex;
    public int FrameNormalsIndex;
    public int FrameHeightsIndex;
    public int MainTextureIndex;
    public int MainNormalsIndex;
    public int MainHeightsIndex;

    // TODO: parfois je veux l'albedo du main mais le heights de la texture ? Bah non faut rajouter une frame
    // TODO: parfois je veux le normals du frame mais l'albedo du mains

    public static float Pack(int a, int b, int c) {
        return a * 1023f * 1023f + b * 1023f + c;
    }

    public static Vector3Int Unpack(double f) {
        int r = (int) Math.Floor(f / 1023 / 1023);
        int g = (int) Math.Floor((f - r * 1023 * 1023) / 1023);
        int b = (int) Math.Floor(f - r * 1023 * 1023 - g * 1023);
        return new Vector3Int(r, g, b);
    }
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