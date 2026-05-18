using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "TroopCardSO", menuName = "ScriptableObjects/Cards/TroopCardSO")]
    public class TroopCardSO : UnitCardSO
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