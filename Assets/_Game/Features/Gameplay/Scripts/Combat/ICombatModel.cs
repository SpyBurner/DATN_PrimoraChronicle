using UnityObservables;

public interface ICombatModel : IModel
{
    Observable<string> CurrentAttackerId { get; }
    Observable<string> CurrentDefenderId { get; }
    Observable<string> CombatLog { get; }

    void ApplyState(CombatStateData data);
}

