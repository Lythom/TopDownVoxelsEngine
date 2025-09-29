namespace Shared {
    public enum SessionStatus {
        Disconnected,

        /// needs RegisterPlayerCommand → characterJoinGameEvent
        NeedAuthentication,

        /// needs all characterJoinGameEvent and ChunkUpdateGameEvents
        GettingReady,

        /// can read all messages
        Ready
    }
}