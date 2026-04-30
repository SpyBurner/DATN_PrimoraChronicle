using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "SpellCardSO", menuName = "ScriptableObjects/Cards/SpellCardSO")]
    public class SpellCardSO : CardSO
    {
        private void Reset()
        {
            EnsureCardSetup(CardType.Spell);
        }

        private void OnValidate()
        {
            EnsureCardSetup(CardType.Spell);
        }
    }
}