using System;
using System.Collections.Generic;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine.Data;
using VoxelsEngine.Tools;

[Serializable]
public struct BlockRendering {
    public string Path;

    [ListDrawerSettings(ShowFoldout = false)]
    public List<BlockRenderingSide> Sides;

    public Texture? ItemPreview;

    public BlockRendering(string path) {
        Path = path;
        ItemPreview = null;
        Sides = new List<BlockRenderingSide>();
    }

    public BlockRendering(string path, BlockConfigJson blockConfig, Registry<MainTextureJson> mainTextures, Registry<FrameTextureJson> frameTextures, SpriteRegistry spritesRegistry) {
        Path = path;
        Sides = new();
        foreach (var side in blockConfig.Sides) {
            var mainJson = mainTextures.Get(side.MainTextureConfig);
            var frameJson = side.FrameTextureConfig == null ? null : frameTextures.Get(side.FrameTextureConfig);
            if (mainJson == null) throw new OperationCanceledException($"La mainTextureConfig {side.MainTextureConfig} n'a pas été trouvé pour le side {side.Directions} du block {path}.");
            Sides.Add(new BlockRenderingSide {
                Directions = side.Directions,
                MainAlbedoTexture = StreamAssets.FromRelativePath(mainJson.MainAlbedoTexture),
                MainNormalsTexture = StreamAssets.FromRelativePath(mainJson.MainNormalsTexture),
                MainHeightsTexture = StreamAssets.FromRelativePath(mainJson.MainHeightsTexture),
                FrameAlbedoTexture = frameJson == null ? null : StreamAssets.FromRelativePath(frameJson.FrameAlbedoTexture),
                FrameNormalsTexture = frameJson == null ? null : StreamAssets.FromRelativePath(frameJson.FrameNormalsTexture),
                FrameHeightsTexture = frameJson == null ? null : StreamAssets.FromRelativePath(frameJson.FrameHeightsTexture)
            });
        }

        var previewSpritePath = blockConfig.ItemPreviewSprite == null ? null : spritesRegistry.Get(blockConfig.ItemPreviewSprite);
        ItemPreview = previewSpritePath == null ? null : StreamAssets.FromRelativePath(previewSpritePath);
    }
}

[Serializable]
public class BlockRenderingSide {
    public DirectionFlag Directions;

    // A main texture is required
    public Texture2D MainAlbedoTexture = null!;

    public Texture2D MainNormalsTexture = null!;

    public Texture2D MainHeightsTexture = null!;

    // A frame is optional, it will overlay framing texture around the main texture and tty to blend according to heights
    public Texture2D? FrameAlbedoTexture;

    public Texture2D? FrameNormalsTexture;

    public Texture2D? FrameHeightsTexture;

    // index of the frame texture. Should be multiplied by 55 to get the 1st tile sprite of the tiles serie.
    public int FrameTextureIndex;

    // Index of the main texture.
    public int MainTextureIndex;
}