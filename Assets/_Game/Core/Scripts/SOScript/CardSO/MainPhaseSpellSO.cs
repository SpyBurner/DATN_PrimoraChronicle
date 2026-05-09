using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "MainPhaseSpellSO", menuName = "ScriptableObjects/Cards/MainPhaseSpellSO")]
    public class MainPhaseSpellSO : SpellCardSO
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
