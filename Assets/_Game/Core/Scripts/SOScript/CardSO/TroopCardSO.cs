using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "TroopCardSO", menuName = "ScriptableObjects/Cards/TroopCardSO")]
    public class TroopCardSO : CardSO
    {
        [Header("Troop Info")]
        public int Speed;
        public int Hp;
        public int Damage;

        private void Reset()
        {
            EnsureCardSetup(CardType.Troop);
        }

        private void OnValidate()
        {
            EnsureCardSetup(CardType.Troop);
        }
    }
}