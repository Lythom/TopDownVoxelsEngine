using System;

[Serializable]
public struct BlockDefinition {
    public static BlockDefinition AIR = new(-1);

    public float TextureIndex;
    public int MiningLevel;
    public Equipment RequiredEquipment;

    public BlockDefinition(float textureIndex, int miningLevel = 0, Equipment requiredEquipment = Equipment.None) {
        TextureIndex = textureIndex;
        MiningLevel = miningLevel;
        RequiredEquipment = requiredEquipment;
    }
}