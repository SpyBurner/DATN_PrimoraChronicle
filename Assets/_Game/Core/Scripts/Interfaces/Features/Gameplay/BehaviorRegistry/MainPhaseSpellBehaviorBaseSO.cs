using UnityEngine;

public abstract class MainPhaseSpellBehaviorBaseSO : ScriptableObject
{
    [Header("Identification")]
    public string behaviorId;

    [Header("Targeting")]
    public int range = 1;
    public int aoe = 0;
    public int targetCondition = 4;
}
