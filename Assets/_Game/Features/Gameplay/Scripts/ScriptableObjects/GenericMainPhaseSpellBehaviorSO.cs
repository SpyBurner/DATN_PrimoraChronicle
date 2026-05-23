using UnityEngine;

[CreateAssetMenu(fileName = "GenericMainPhaseSpellBehavior", menuName = "Primora/Behaviors/GenericMainPhaseSpellBehavior")]
public class GenericMainPhaseSpellBehaviorSO : MainPhaseSpellBehaviorBaseSO
{
    [Header("Effect")]
    public string appliedTileEffectId;
    public int tileEffectDuration = 3;
    public string appliedStatusEffectId;
    public int statusEffectDuration = 3;
    public int directDamage;
    public int directHeal;

    public void Execute(HexCoord targetPosition, int casterOwnerIndex,
        IUnitSubsystem units, IBoardSubsystem board, ITileEffectSubsystem tileEffects)
    {
        Debug.Log($"[MainPhaseSpell] {behaviorId} cast at ({targetPosition.P},{targetPosition.Q})");
    }
}
