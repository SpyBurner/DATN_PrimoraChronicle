using UnityEngine;

[CreateAssetMenu(fileName = "GenericEvolutionBehavior", menuName = "Primora/Behaviors/GenericEvolutionBehavior")]
public class GenericEvolutionBehaviorSO : EvolutionBehaviorBaseSO
{
    [Header("Visual")]
    public GameObject nextFormPrefab;

    public bool CanEvolve(int currentGrowthStacks)
    {
        return currentGrowthStacks >= requiredStacks;
    }

    public void Execute(string unitId, IUnitSubsystem units)
    {
        Debug.Log($"[Evolution] {behaviorId} evolving {unitId} → {nextFormCardId} (HP:{nextFormHP} Speed:{nextFormSpeed})");
    }
}
