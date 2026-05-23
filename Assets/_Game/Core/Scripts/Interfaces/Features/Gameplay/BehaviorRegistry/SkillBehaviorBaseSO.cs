using UnityEngine;

public abstract class SkillBehaviorBaseSO : ScriptableObject
{
    public string behaviorId;

    [Header("Prefab References")]
    public GameObject summonPrefab;
    public GameObject tileEffectPrefab;
}
