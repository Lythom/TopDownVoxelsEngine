using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class ChangeToolGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public ushort CharacterId;

        [Key(2)]
        public ToolId Tool;

        public override int GetId() => Id;

        public ChangeToolGameEvent(int id, ushort characterId, ToolId tool) {
            Id = id;
            CharacterId = characterId;
            Tool = tool;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters[CharacterId].SelectedTool.Value = Tool;
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterId)) throw new ApplicationException("Character must exists");
        }
    }
}