using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelsEngine {
    public class ChunkGenerator : MonoBehaviour {
        public Chunk? Chunk;
        public Dictionary<string, GameObject> Blocks = new();

        public void Redraw() {
            if (Chunk == null) throw new ApplicationException("Ensure Chunk is not null before drawing");
            for (var x = 0; x < Chunk.Cells.GetLength(0); x++) {
                for (var y = 0; y < Chunk.Cells.GetLength(1); y++) {
                    for (var z = 0; z < Chunk.Cells.GetLength(2); z++) {
                        string key = $"{x}_{y}_{z}";
                        var cell = Chunk.Cells[x, y, z];
                        GameObject? block = Blocks.ContainsKey(key) ? Blocks[key] : null;
                        if (block == null && cell.BlockDefinition != "AIR") // Assuming you have a BlockDefinition class with an ALL array defined somewhere
                        {
                            var blockDef = Configurator.Instance.BlocksLibrary[cell.BlockDefinition];
                            if (blockDef.Prefab != null) {
                                block = Instantiate(blockDef.Prefab, transform);
                                Blocks.Add(key, block);
                            }
                        }

                        if (block != null) {
                            block.transform.localPosition = new(x, z, y);
                        }
                    }
                }
            }
        }
    }
}