using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Shared {
    /// <summary>
    /// Manages the mapping between block paths and their numeric IDs.
    /// This class is responsible for maintaining the BlockPathById index.
    /// </summary>
    [MessagePackObject(true)]
    public partial class BlockPathMapping {
        /// <summary>
        /// Array mapping block IDs to their path strings. Index is the block ID.
        /// </summary>
        public string?[] BlockPathById { get; private set; }
        
        /// <summary>
        /// Dictionary mapping block paths to their IDs for faster lookups.
        /// This is not serialized but reconstructed from BlockPathById.
        /// </summary>
        [IgnoreMember]
        public Dictionary<string, ushort> BlockIdByPath { get; private set; } = new();

        // Required for MessagePack deserialization
        [SerializationConstructor]
        public BlockPathMapping(string?[] blockPathById) {
            BlockPathById = blockPathById;
            RebuildBlockIdByPath();
        }

        /// <summary>
        /// Creates a new BlockPathMapping with a default-sized array.
        /// </summary>
        public BlockPathMapping() : this(new string?[ushort.MaxValue]) {
        }

        /// <summary>
        /// Rebuilds the BlockIdByPath dictionary from the BlockPathById array.
        /// </summary>
        private void RebuildBlockIdByPath() {
            BlockIdByPath.Clear();
            if (BlockPathById[0] == null) BlockPathById[0] = "Air";
            BlockIdByPath["Air"] = 0;
            
            for (ushort blockId = 1; blockId < BlockPathById.Length; blockId++) {
                var blockPath = BlockPathById[blockId];
                if (blockPath != null) BlockIdByPath.TryAdd(blockPath, blockId);
                else break;
            }
        }

        /// <summary>
        /// Updates the block mapping based on the provided registry.
        /// Adds new blocks from the registry to the mapping and updates the inverted lookup.
        /// </summary>
        /// <param name="registry">Registry containing block configurations</param>
        public void UpdateBlockMapping(IRegistry<BlockConfigJson> registry) {
            BlockPathById[0] = "Air";

            foreach (var blockPath in registry.Get().Keys) {
                if (BlockPathById.Contains(blockPath)) continue;
                var nextIdx = Array.IndexOf(BlockPathById, null);
                if (nextIdx == -1) {
                    throw new InvalidOperationException("Le tableau BlockPathById est plein. Impossible d'ajouter de nouveaux éléments.");
                }

                // assign a new Id to a block present in registry that was unknown before
                BlockPathById[nextIdx] = blockPath;
            }

            foreach (var block in BlockPathById) {
                if (block != null && block != "Air" && registry.Get(block) == null) {
                    Logr.Log($"Le block {block} utilisé dans cette save n'est pas présent dans le BlockRegistry. Les instances sont rendus avec l'apparence du premier block trouvé dans le registre. Si un block nommé {block} est réintroduit plus tard, ils pourront être récupérés.", Tags.PlayerFeedbackRequired);
                }

                if (block == null) break;
            }

            // update inverted lookup
            RebuildBlockIdByPath();
        }

        /// <summary>
        /// Updates this BlockPathMapping with values from another instance.
        /// </summary>
        /// <param name="other">The BlockPathMapping to copy values from</param>
        public void UpdateValue(BlockPathMapping other) {
            for (var i = 0; i < BlockPathById.Length; i++) BlockPathById[i] = other.BlockPathById[i];
            RebuildBlockIdByPath();
        }
    }
}
