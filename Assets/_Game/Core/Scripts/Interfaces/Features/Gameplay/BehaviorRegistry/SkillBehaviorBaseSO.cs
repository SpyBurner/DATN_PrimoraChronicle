using UnityEngine;

public abstract class SkillBehaviorBaseSO : ScriptableObject
{
    [Header("Behavior Settings")]
    public string behaviorId;
    public bool oneTime;
    public int cooldown = 3;

    [Header("Targeting")]
    public int range = 1;
    public int aoe = 0;
    public int targetCondition = 1;
    public bool ignorePathfinding;
    public bool ignoreFriendlyFire;

    [Header("Prefab References")]
    public GameObject summonPrefab;
    public GameObject tileEffectPrefab;

    public bool IsSelfTargetOnly => targetCondition == 0;
}
