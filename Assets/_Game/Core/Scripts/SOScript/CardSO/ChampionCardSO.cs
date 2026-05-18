using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "ChampionCardSO", menuName = "ScriptableObjects/Cards/ChampionCardSO")]
    public class ChampionCardSO : UnitCardSO
    {
        private void Reset()
        {
            EnsureCardSetup();
        }

        private void OnValidate()
        {
            EnsureCardSetup();
        }
    }
}