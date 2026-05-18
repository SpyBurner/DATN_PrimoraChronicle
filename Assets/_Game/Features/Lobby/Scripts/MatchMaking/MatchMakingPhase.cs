public enum MatchMakingPhase
{
    Idle,
    Searching,
    MatchFound,    // confirmation window open
    Connecting,    // StartGame in progress
    Connected,     // RunnerState == Running
    Cancelled,
    Failed
}
