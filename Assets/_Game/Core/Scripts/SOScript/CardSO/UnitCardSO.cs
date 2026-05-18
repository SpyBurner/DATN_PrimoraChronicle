using UnityEngine;

namespace Core
{
    public abstract class UnitCardSO : CardSO
    {
        [Header("Audio Assets")]
        public AudioClip SummonSfx;
        public AudioClip AttackSfx;
        public AudioClip DeathSfx;
    }
}
