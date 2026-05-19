using Fusion;
using UnityEngine;

public abstract class MainPhaseSpellBehaviorSO : ScriptableObject
{
    [Header("Behavior Settings")]
    public string behaviorId;

    public abstract void Execute(
        PlayerRef caster,
        HexCoord target,
        IUnitSubsystem unitSubsystem,
        IBoardSubsystem boardSubsystem,
        ICardLoadingManagerSubsystem cardLoading,
        IDebugLogger logger
    );
}
