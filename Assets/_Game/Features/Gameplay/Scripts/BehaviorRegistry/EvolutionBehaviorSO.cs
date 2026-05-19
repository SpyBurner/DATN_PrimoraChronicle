using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionBehavior", menuName = "Primora/Skills/EvolutionBehavior")]
public class EvolutionBehaviorSO : ScriptableObject
{
    [Header("Evolution Chain")]
    public List<EvolutionStage> evolutionChain = new();

    [System.Serializable]
    public struct EvolutionStage
    {
        public string fromCardId;
        public string toCardId;
        public int requiredGrowthStacks;
    }

    public bool TryGetNextForm(string currentCardId, int growthStacks, out string nextForm)
    {
        foreach (var stage in evolutionChain)
        {
            if (stage.fromCardId == currentCardId && growthStacks >= stage.requiredGrowthStacks)
            {
                nextForm = stage.toCardId;
                return true;
            }
        }
        nextForm = null;
        return false;
    }
}
