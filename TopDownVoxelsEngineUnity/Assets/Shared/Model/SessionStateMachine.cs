using System;
using LoneStoneStudio.Tools;

namespace Shared {
    public class SessionStateMachine {
        public readonly Reactive<SessionStatus> Status = new(SessionStatus.Disconnected);

        public SessionStateMachine() {
            Status.Value = SessionStatus.Disconnected;
        }

        public void TransitionTo(SessionStatus newStatus) {
            ValidateTransition(Status.Value, newStatus);
            switch (Status.Value, newStatus) {
                case (SessionStatus.Disconnected, SessionStatus.Helloing):
                    break;
                case (_, SessionStatus.Disconnected):
                    break;
                default: break;
            }

            Status.Value = newStatus;
        }

        private void ValidateTransition(SessionStatus current, SessionStatus next) {
            // can be disconnected anytime
            if (next == SessionStatus.Disconnected) return;

            if (current == SessionStatus.Disconnected && next != SessionStatus.Helloing) {
                throw new InvalidOperationException("From Disconnected, only Hello is allowed");
            }

            if (current == SessionStatus.Helloing && next != SessionStatus.Identified) {
                throw new InvalidOperationException("From Hello, only Identified is allowed");
            }
        }
    }
}