using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct BlockDefinition {
    public static BlockDefinition AIR = new(0);

    [FormerlySerializedAs("TextureIndex")]
    public BlockDefId Id;

    public int MiningLevel;
    public Equipment RequiredEquipment;
    public Sprite? ItemPreview;

    public BlockDefinition(BlockDefId id, int miningLevel = 0, Equipment requiredEquipment = Equipment.None) {
        Id = id;
        MiningLevel = miningLevel;
        RequiredEquipment = requiredEquipment;
        ItemPreview = null;
    }
}

public enum BlockDefId {
    Air,
    Dirt,
    Grass,
    Stone,
    Snow,
    Wood,
}