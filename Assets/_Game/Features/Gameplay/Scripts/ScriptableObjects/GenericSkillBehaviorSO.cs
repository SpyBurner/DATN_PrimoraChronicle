using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GenericSkillBehavior", menuName = "Primora/Behaviors/GenericSkillBehavior")]
public class GenericSkillBehaviorSO : SkillBehaviorBaseSO
{
    [Header("Summon Parameters")]
    public string summonUnitCardId;
    public int summonHP;
    public float summonSpeed;
    public int summonMoveRange;
    public int summonDeathAnchor;
    public bool summonIsPersistent;

    [Header("Tile Effect Parameters")]
    public string tileEffectId;
    public int tileEffectDuration = 3;

    [Header("Damage/Heal")]
    public int directDamage;
    public int directHeal;
    public int maxHPModifier;

    [Header("Status Effect Application")]
    public string appliedStatusEffectId;
    public int appliedStatusDuration = 3;

    [Header("Growth")]
    public int growthStacksGranted;

    public void Execute(SkillExecutionContext context, IUnitSubsystem units, IBoardSubsystem board,
        ITileEffectSubsystem tileEffects, IDamagePipelineSubsystem damagePipeline)
    {
        Debug.Log($"[SkillExecution] {behaviorId} from {context.CasterUnitId} → ({context.TargetPosition.P},{context.TargetPosition.Q})");
    }

    public List<HexCoord> GetAffectedTiles(HexCoord center, IBoardSubsystem board)
    {
        var result = new List<HexCoord>();
        if (aoe <= 0)
        {
            result.Add(center);
            return result;
        }

        foreach (var tile in board.AllTiles)
        {
            if (board.Distance(center, tile) <= aoe)
            {
                result.Add(tile);
            }
        }
        return result;
    }
}
