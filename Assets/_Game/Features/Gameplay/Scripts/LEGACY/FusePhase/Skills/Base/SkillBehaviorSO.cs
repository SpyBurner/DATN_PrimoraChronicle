using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBehaviorSO : ScriptableObject
{
    [Header("Behavior Settings")]
    public string behaviorId;
    public bool one_time = false;
    public int cooldown = 3;

    /// <summary>
    /// Executes the gameplay effect of the skill.
    /// </summary>
    public abstract void Execute(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile);

    /// <summary>
    /// Checks if a tile is a valid target based on target conditions bitmask.
    /// Bitmask values: Enemy = 1, Ally = 2, EmptyTile = 4
    /// </summary>
    public virtual bool IsTileValidTarget(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile, int targetCondition)
    {
        if (targetTile == null) return false;

        // targetCondition 0: self-only — valid only when target is the caster's own tile
        if (targetCondition == 0)
            return targetTile.p == caster.P && targetTile.q == caster.Q;

        // Find unit at tile
        NetworkUnit targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);

        if (targetUnit != null)
        {
            if (targetUnit == caster)
            {
                // Self targeting check
                return (targetCondition & 2) != 0;
            }
            else if (targetUnit.Owner == caster.Owner)
            {
                // Ally targeting check
                return (targetCondition & 2) != 0;
            }
            else
            {
                // Enemy targeting check
                return (targetCondition & 1) != 0;
            }
        }
        else
        {
            // Empty tile check
            return (targetCondition & 4) != 0;
        }
    }
}
