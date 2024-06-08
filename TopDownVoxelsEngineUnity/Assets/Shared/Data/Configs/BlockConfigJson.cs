using System;
using System.Collections.Generic;
using MessagePack;
using Shared;
using Sirenix.OdinInspector;

[Serializable, MessagePackObject(true)]
public class BlockConfigJson {
    [ValueDropdown("@AssetsHelper.GetSpriteTextures()")]
    public string? ItemPreviewSprite;

    [ListDrawerSettings(ShowFoldout = false)]
    public List<BlockSideJson> Sides;

    public bool IgnoreFrameAlbedo = false;

    public BlockConfigJson() {
        ItemPreviewSprite = null;
        Sides = new List<BlockSideJson>();
    }
}

/// <summary>
/// 
/// </summary>
[Serializable, MessagePackObject(true)]
public class BlockSideJson {
    public DirectionFlag Directions;

    // A main texture is required
    [ValueDropdown("@AssetsHelper.GetMainTextures()")]
    public string MainTextureConfig = null!;

    // A frame is optional, it will overlay framing texture around the main texture and tty to blend according to heights
    [ValueDropdown("@AssetsHelper.GetFrameTextures()"), OnValueChanged("HandleFrameTextureConfigChange")]
    public string? FrameTextureConfig = null;

    public void HandleFrameTextureConfigChange() {
        if (FrameTextureConfig == "null") FrameTextureConfig = null;
    }
}