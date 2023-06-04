using System;
using Shared;
using UnityEngine;

[Serializable]
public struct BlockRenderingConfiguration
{
    public static BlockRenderingConfiguration AIR = new(0);

    public BlockId Id;

    public int FrameTextureIndex;
    public int FrameNormalIndex;
    public int MainTextureIndex;
    public int MainNormalIndex;

    public Sprite? ItemPreview;

    public BlockRenderingConfiguration(BlockId id)
    {
        Id = id;
        ItemPreview = null;
        MainTextureIndex = -1;
        MainNormalIndex = -1;
        FrameTextureIndex = (int) id - 1;
        FrameNormalIndex = 0;
    }
}