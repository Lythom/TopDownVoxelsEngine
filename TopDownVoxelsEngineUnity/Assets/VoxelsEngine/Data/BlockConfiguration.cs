using System;
using System.Collections.Generic;
using MessagePack;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine.Data;

[Serializable]
[CreateAssetMenu(fileName = "Block", menuName = "BlockConfiguration", order = 1)]
public class BlockConfiguration : ScriptableObject {
    public static BlockConfiguration AIR = new(0);

    public BlockId Id;

    public Texture? ItemPreview;

    [ListDrawerSettings(ShowFoldout = false)]
    public List<BlockRenderingSide> Sides;

    public BlockConfiguration(BlockId id) {
        Id = id;
        ItemPreview = null;
        Sides = new List<BlockRenderingSide>();
    }
}


// Déclaration = Chemin dynamique dans les ressources via nom, sélecteur pour renseigner le bon path à partir des fichiers
// différence runtime et éditeur: en mode éditeur lien direct
// au moment de la compile export en json, export des textures dans les resources, lecture du json au runtime pour accéder aux fichiers resources
[Serializable]
public class BlockRenderingSide {
    public DirectionFlag Directions;

    // A main texture is required
    
    public string MainAlbedoTexture = null!;

    
    public string MainNormalsTexture = null!;

    
    public string MainHeightsTexture = null!;

    [ValueDropdown("@AssetsHelper.GetMainTextures()"), OnValueChanged("UpdateMainTexture")]
    public string MainTextureConfiguration = null!;

    private void UpdateMainTexture() {
        try {
            var conf = MessagePackSerializer.Deserialize<MainTextureConfiguration>(MessagePackSerializer.ConvertFromJson(MainTextureConfiguration));
            MainAlbedoTexture = conf.MainAlbedoTexture;
            MainNormalsTexture = conf.MainNormalsTexture;
            MainHeightsTexture = conf.MainHeightsTexture;
        } catch (Exception e) {
            Logr.LogError($"Couldn't Deserialize MainTextureConfiguration from {MainTextureConfiguration}. Exception: {e}.");
        }
    }

    // A frame is optional, it will overlay framing texture around the main texture and tty to blend according to heights
    
    public string? FrameAlbedoTexture;

    
    public string? FrameNormalsTexture;

    
    public string? FrameHeightsTexture;

    [ValueDropdown("@AssetsHelper.GetFrameTextures()"), OnValueChanged("UpdateFrameTexture")]
    public string? FrameTextureConfiguration = null;

    private void UpdateFrameTexture() {
        try {
            var conf = MessagePackSerializer.Deserialize<FrameTextureConfiguration>(MessagePackSerializer.ConvertFromJson(FrameTextureConfiguration));
            FrameAlbedoTexture = conf.FrameAlbedoTexture;
            FrameNormalsTexture = conf.FrameNormalsTexture;
            FrameHeightsTexture = conf.FrameHeightsTexture;
        } catch (Exception e) {
            Logr.LogError($"Couldn't Deserialize FrameTextureConfiguration from {FrameTextureConfiguration}. Exception: {e}.");
        }
    }

    // index of the frame texture. Should be multiplied by 55 to get the 1st tile sprite of the tiles serie.
    
    public int FrameTextureIndex;

    // Index of the main texture.
    
    public int MainTextureIndex;
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