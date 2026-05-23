using UnityEngine;

public abstract class EvolutionBehaviorBaseSO : ScriptableObject
{
    [Header("Identification")]
    public string behaviorId;

    [Header("Evolution Chain")]
    public int requiredStacks = 4;
    public string nextFormCardId;
    public int nextFormHP;
    public float nextFormSpeed;
    public int nextFormMoveRange;
    public int nextFormDeathAnchor;
}
