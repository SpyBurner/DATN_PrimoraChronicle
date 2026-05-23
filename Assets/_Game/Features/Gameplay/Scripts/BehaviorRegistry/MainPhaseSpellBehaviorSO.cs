using Fusion;
using UnityEngine;

public abstract class MainPhaseSpellBehaviorSO : MainPhaseSpellBehaviorBaseSO
{
    public abstract void Execute(
        PlayerRef caster,
        HexCoord target,
        IUnitSubsystem unitSubsystem,
        IBoardSubsystem boardSubsystem,
        ICardLoadingManagerSubsystem cardLoading,
        IDebugLogger logger
    );
}
