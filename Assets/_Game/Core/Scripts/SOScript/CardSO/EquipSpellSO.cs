using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "EquipSpellSO", menuName = "ScriptableObjects/Cards/EquipSpellSO")]
    public class EquipSpellSO : SpellCardSO
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
