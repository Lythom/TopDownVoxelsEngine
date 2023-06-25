namespace Shared {
    public enum SessionStatus {
        Disconnected,

        /// needs HelloNetworkMessage → characterJoinGameEvent
        NeedAuthentication,

        /// needs all characterJoinGameEvent and ChunkUpdateGameEvents
        GettingReady,

        /// can read all messages
        Ready
    }
}