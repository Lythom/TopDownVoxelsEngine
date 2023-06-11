using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class ChangeBlockGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public short CharacterId;

        [Key(2)]
        public BlockId Block;

        public override int GetId() => Id;

        public ChangeBlockGameEvent(int id, short characterId, BlockId block) {
            Id = id;
            CharacterId = characterId;
            Block = block;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterId].SelectedBlock.Value = Block;
        }

        public override void AssertApplicationConditions(GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterId)) throw new ApplicationException("Character must exists");
        }
    }
}