using UnityEngine;

public abstract class EvolutionBehaviorBaseSO : ScriptableObject
{
    [Header("Identification")]
    public string behaviorId;

    [Header("Evolution Chain")]
    public int requiredStacks = 4;
    // nextFormHP/Speed/MoveRange/DeathAnchor removed — read from ICardLoadingManagerSubsystem.TryGetCardData(nextFormCardId) at runtime
    public string nextFormCardId;
}
