using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine.Data;
using VoxelsEngine.Tools;

[Serializable]
public struct BlockRendering {
    [ListDrawerSettings(ShowFoldout = false)]
    public List<BlockRenderingSide> Sides;

    public Texture? ItemPreview;
    public readonly bool HasFrameAlbedo;

    // Texture can heightmap blend into adjacent blocks
    public readonly bool CanBleed;

    // Adjacent blocks can bleed into this block
    public readonly bool AcceptBleeding;

    public static BlockRendering Air = new(true, false, false);

    public static async UniTask<BlockRendering> FromConfigAsync(IStreamAssets streamAssets, BlockConfigJson blockConfig, Registry<MainTextureJson> mainTextures, Registry<FrameTextureJson> frameTextures, SpriteRegistry spritesRegistry) {
        var block = new BlockRendering(blockConfig.HasFrameAlbedo, blockConfig.CanBleed, blockConfig.AcceptBleeding) {
            ItemPreview = null
        };
        List<UniTask> globalTasks = new();

        foreach (var side in blockConfig.Sides) {
            List<UniTask<Texture2D>> tasks = new();
            var mainJson = mainTextures.Get(side.MainTextureConfig);
            var frameJson = side.FrameTextureConfig == null ? null : frameTextures.Get(side.FrameTextureConfig);
            if (mainJson == null) throw new OperationCanceledException($"La mainTextureConfig {side.MainTextureConfig} n'a pas été trouvé pour le side {side.Directions} du block.");
            tasks.Clear();
            tasks.Add(streamAssets.LoadTexture2DAsync(mainJson.MainAlbedoTexture));
            tasks.Add(streamAssets.LoadTexture2DAsync(mainJson.MainNormalsTexture));
            tasks.Add(streamAssets.LoadTexture2DAsync(mainJson.MainHeightsTexture));
            if (frameJson != null) tasks.Add(streamAssets.LoadTexture2DAsync(frameJson.FrameAlbedoTexture));
            if (frameJson != null) tasks.Add(streamAssets.LoadTexture2DAsync(frameJson.FrameNormalsTexture));
            if (frameJson != null) tasks.Add(streamAssets.LoadTexture2DAsync(frameJson.FrameHeightsTexture));
            globalTasks.Add(UniTask.WhenAll(tasks).ContinueWith(results => {
                block.Sides.Add(new BlockRenderingSide {
                    Directions = side.Directions,
                    MainAlbedoTexture = results[0],
                    MainNormalsTexture = results[1],
                    MainHeightsTexture = results[2],
                    FrameAlbedoTexture = results.Length < 4 ? null : results[3],
                    FrameNormalsTexture = results.Length < 5 ? null : results[4],
                    FrameHeightsTexture = results.Length < 6 ? null : results[5],
                });
            }));
        }

        var previewSpritePath = blockConfig.ItemPreviewSprite == null ? null : spritesRegistry.Get(blockConfig.ItemPreviewSprite);
        block.ItemPreview = null;
        if (previewSpritePath != null) globalTasks.Add(streamAssets.LoadTexture2DAsync(previewSpritePath).ContinueWith(r => block.ItemPreview = r));
        await UniTask.WhenAll(globalTasks);
        return block;
    }

    public BlockRendering(bool hasFrameAlbedo, bool canBleed, bool acceptBleeding) : this() {
        HasFrameAlbedo = hasFrameAlbedo;
        CanBleed = canBleed;
        AcceptBleeding = acceptBleeding;
        Sides = new();
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
    public ushort FrameTextureIndex;

    // Index of the main texture.
    public ushort MainTextureIndex;
}