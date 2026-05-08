using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "ChampionCardSO", menuName = "ScriptableObjects/Cards/ChampionCardSO")]
    public class ChampionCardSO : CardSO
    {
        [Header("Champion Info")]
        public List<TroopCardSO> ChampionTroopCards = new(10);
        public List<SpellCardSO> ChampionSpellCards = new(10);
        public int Speed;
        public int Hp;
        public int Damage;
        public int DeathAnchor;
        public int AttackRange;
        public int MoveRange;

        private void Reset()
        {
            EnsureCardSetup(CardType.Champion);
        }

        private void OnValidate()
        {
            EnsureCardSetup(CardType.Champion);
        }
    }
}