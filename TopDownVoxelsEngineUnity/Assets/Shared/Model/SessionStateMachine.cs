using System;
using LoneStoneStudio.Tools;

namespace Shared {
    public class SessionStateMachine {
        public readonly Reactive<SessionStatus> Status = new(SessionStatus.NeedAuthentication);

        public SessionStateMachine() {
            Status.Value = SessionStatus.NeedAuthentication;
        }

        public void TransitionTo(SessionStatus newStatus) {
            ValidateTransition(Status.Value, newStatus);
            switch (Status.Value, newStatus) {
                case (SessionStatus.NeedAuthentication, SessionStatus.GettingReady):
                    break;
                case (_, SessionStatus.NeedAuthentication):
                    break;
                default: break;
            }

            Status.Value = newStatus;
        }

        private void ValidateTransition(SessionStatus current, SessionStatus next) {
            // can be disconnected anytime
            if (next == SessionStatus.NeedAuthentication) return;

            if (current == SessionStatus.NeedAuthentication && next != SessionStatus.GettingReady) {
                throw new InvalidOperationException("From Disconnected, only Hello is allowed");
            }

            if (current == SessionStatus.GettingReady && next != SessionStatus.Ready) {
                throw new InvalidOperationException("From Hello, only Identified is allowed");
            }
        }
    }
}