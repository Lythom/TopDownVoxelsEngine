using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class ChangeToolGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public ushort CharacterShortId;

        [Key(2)]
        public byte Tool;

        public override int GetId() => Id;

        public ChangeToolGameEvent(int id, ushort characterShortId, byte tool) {
            Id = id;
            CharacterShortId = characterShortId;
            Tool = tool;
        }

        protected override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterShortId].SelectedTool.Value = Tool;
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterShortId)) throw new ApplicationException("Character must exists");
        }
    }
}