using System;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared.Net {
    [MessagePackObject]
    public class CharacterLeaveGameEvent : GameEvent {
        [Key(0)]
        public int Id;

        [Key(1)]
        public ushort CharacterId;

        public override int GetId() => Id;

        public CharacterLeaveGameEvent(int id, ushort characterId) {
            Id = id;
            CharacterId = characterId;
        }

        protected internal override void DoApply(GameState gameState, SideEffectManager? sideEffectManager) {
            if (!gameState.IsApplyingEvent) throw new ApplicationException("Use GameState.ApplyEvent to apply an event. This enables post event side effects on state.");
            gameState.Characters.Remove(CharacterId);
        }

        public override void AssertApplicationConditions(in GameState gameState) {
            if (!gameState.Characters.ContainsKey(CharacterId)) throw new ApplicationException("Character already unknown.");
        }
    }
}