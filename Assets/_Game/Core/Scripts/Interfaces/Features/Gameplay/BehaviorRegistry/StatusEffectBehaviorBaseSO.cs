using UnityEngine;

public abstract class StatusEffectBehaviorBaseSO : ScriptableObject
{
    [Header("Identification")]
    public string effectId;

    [Header("Tick Behavior")]
    public int damagePerTurn;
    public int healPerTurn;

    [Header("Intercept Behavior")]
    public int interceptAmount;

    [Header("Flags")]
    public bool preventsHealing;
    public bool preventsMovement;
    public bool leavesTrailOnMove;

    [Header("Trail")]
    public string trailTileEffectId;
}
