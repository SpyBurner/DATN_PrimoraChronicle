using Fusion;

public struct GameStateData
{
    public GameplayPhase Phase;
    public float PhaseTimeRemaining;
    public float MatchElapsed;
    public int RoundNumber;
    public PlayerRef CurrentCombatActor;
}
