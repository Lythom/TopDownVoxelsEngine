using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class PlaceBlocksGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public short CharacterId;

        /// <summary>
        /// Use StoreValues / RetrieveValues to get the 5 positions stored here
        /// </summary>
        // [Key(2)]
        // public long FiveBlocksStorage;
        //
        // [Key(3)]
        // public short ChunkPosition;
        [Key(2)]
        public short X;

        [Key(3)]
        public short Y;

        [Key(4)]
        public short Z;

        [Key(5)]
        public BlockId Block;

        public override int GetId() => Id;

        public PlaceBlocksGameEvent(int id, short characterId, short x, short y, short z, BlockId block) {
            Id = id;
            CharacterId = characterId;
            X = x;
            Y = y;
            Z = z;
            Block = block;
        }
        //
        // public void SetFiveBlocks(Span<uint> values) {
        //     if (values.Length > 5) throw new ApplicationException("Can't store more than 5 values per packet.");
        //
        //     FiveBlocksStorage = long.MaxValue;
        //     for (int i = 0; i < values.Length; i++) {
        //         long value = values[i];
        //         // Décale la valeur à la position correcte et l'ajoute au stockage
        //         FiveBlocksStorage &= value << (12 * i);
        //     }
        // }
        //
        // public void GetFiveBLocks(Span<uint> buffer) {
        //     if (buffer.Length < 5) throw new ApplicationException("At least 5 slots must be available in buffer.");
        //     for (int i = 0; i < 5; i++) {
        //         // Crée un masque pour isoler les bits à la position souhaitée
        //         long mask = 0xFFF << (12 * i);
        //
        //         // Utilise le masque pour isoler les bits et les décale à la position 0
        //         uint value = (uint) ((FiveBlocksStorage & mask) >> (12 * i));
        //
        //         // Stocke la valeur dans le buffer
        //         buffer[i] = value;
        //     }
        // }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            var (chX, chZ) = LevelTools.GetChunkPosition(X, Z);
            var level = gameState.Characters[CharacterId].Level.Value;
            var chunk = gameState.Levels[level!].Chunks[chX, chZ];
            var (cx, cy, cz) = LevelTools.WorldToCellInChunk(X, Y, Z);
            chunk.Cells![cx, cy, cz].Block = Block;
        }

        public override void AssertApplicationConditions(GameState gameState) {
            var (chX, chZ) = LevelTools.GetChunkPosition(X, Z);
            if (!gameState.Characters.ContainsKey(CharacterId)) throw new ApplicationException("Unknown level");
            var level = gameState.Characters[CharacterId].Level;
            if (level.Value == null || !gameState.Levels.ContainsKey(level.Value)) throw new ApplicationException("Unknown level");
            var chunk = gameState.Levels[level.Value].Chunks[chX, chZ];
            if (!chunk.IsGenerated) throw new ApplicationException("Can't set blocks in non ready chunks");
            var (cx, cy, cz) = LevelTools.WorldToCellInChunk(X, Y, Z);
            chunk.Cells![cx, cy, cz].Block = Block;
        }
    }
}