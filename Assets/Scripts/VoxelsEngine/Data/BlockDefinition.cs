using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct BlockDefinition {
    public static BlockDefinition AIR = new(0);

    [FormerlySerializedAs("TextureIndex")]
    public BlockDefId Id;

    public int FrameTextureIndex;
    public int FrameNormalIndex;
    public int MainTextureIndex;
    public int MainNormalIndex;

    public int MiningLevel;
    public Equipment RequiredEquipment;
    public Sprite? ItemPreview;

    public BlockDefinition(BlockDefId id, int backgroundIndex = -1, int miningLevel = 0, Equipment requiredEquipment = Equipment.None) {
        Id = id;
        MiningLevel = miningLevel;
        RequiredEquipment = requiredEquipment;
        ItemPreview = null;
        MainTextureIndex = -1;
        MainNormalIndex = -1;
        FrameTextureIndex = (int) id - 1;
        FrameNormalIndex = 0;
    }
}

public enum BlockDefId {
    Air,
    Dirt,
    Grass,
    Stone,
    Snow,
    Wood,
    PavedStone,
}