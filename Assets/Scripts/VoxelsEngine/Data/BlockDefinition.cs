using System;
using UnityEngine;

[Serializable]
public struct BlockDefinition {
    public static BlockDefinition AIR = new(null, null);

    public Texture2D? Texture;
    public GameObject? Prefab;
    public int MiningLevel;
    public Equipment RequiredEquipment;

    public BlockDefinition(Texture2D? texture, GameObject? prefab, int miningLevel = 0, Equipment requiredEquipment = Equipment.None) {
        Texture = texture;
        Prefab = prefab;
        MiningLevel = miningLevel;
        RequiredEquipment = requiredEquipment;
    }
}