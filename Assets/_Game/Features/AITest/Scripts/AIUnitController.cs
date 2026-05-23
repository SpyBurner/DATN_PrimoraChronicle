using System.Collections.Generic;
using UnityEngine;

public enum AIActionType
{
    MoveOnly,
    Attack,
    UseSkill,
    EndTurn
}

public class AICandidateAction
{
    public AIActionType ActionType;
    public Tile MoveTile;
    public Unit TargetUnit;
    public int SkillIndex;

    public AICandidateAction(AIActionType type, Tile moveTile, Unit target, int skillIndex = -1)
    {
        ActionType = type;
        MoveTile = moveTile;
        TargetUnit = target;
        SkillIndex = skillIndex;
    }
}

public struct SimUnit
{
    public int P, Q;
    public int HP, MaxHP;
    public int Attack;
    public int OwnerPlayer;
    public int[] SkillCooldowns;
    public bool IsDead => HP <= 0;

    public SimUnit(Unit unit)
    {
        P = unit.P;
        Q = unit.Q;
        HP = unit.HP;
        MaxHP = unit.MaxHP;
        Attack = unit.Attack;
        OwnerPlayer = unit.OwnerPlayer;
        SkillCooldowns = new int[unit.Skills.Length];
        for (int i = 0; i < unit.Skills.Length; i++)
            SkillCooldowns[i] = unit.Skills[i] != null ? unit.Skills[i].CurrentCooldown : 999;
    }

    public SimUnit Clone()
    {
        var copy = this;
        copy.SkillCooldowns = (int[])SkillCooldowns.Clone();
        return copy;
    }
}

public struct SimAction
{
    public AIActionType Type;
    public int MoveP, MoveQ;
    public int SkillIndex;
    public int TargetIdx;

    public static SimAction MakeMove(int p, int q) => new SimAction { Type = AIActionType.MoveOnly, MoveP = p, MoveQ = q, SkillIndex = -1, TargetIdx = -1 };
    public static SimAction MakeAttack(int targetIdx, int moveP = -99, int moveQ = -99) => new SimAction { Type = AIActionType.Attack, TargetIdx = targetIdx, MoveP = moveP, MoveQ = moveQ, SkillIndex = -1 };
    public static SimAction MakeSkill(int skillIdx, int targetIdx) => new SimAction { Type = AIActionType.UseSkill, SkillIndex = skillIdx, TargetIdx = targetIdx, MoveP = -99, MoveQ = -99 };
    public static SimAction MakeEndTurn() => new SimAction { Type = AIActionType.EndTurn, SkillIndex = -1, TargetIdx = -1, MoveP = -99, MoveQ = -99 };
}

public class AIUnitController : UnitController
{
    private const int MAX_DEPTH = 5;
    private EffectController _effects;

    public AIUnitController(Unit unit, BoardController board, EffectController effects) : base(unit, board)
    {
        _effects = effects;
    }

    public override void TakeTurn()
    {
        _effects.ApplyTileEffectsToUnit(_unit);

        if (_unit.IsDead)
        {
            _onTurnCompleted?.Invoke();
            return;
        }

        var units = GatherAllUnits();
        int myIdx = -1;
        var simUnits = new SimUnit[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            simUnits[i] = new SimUnit(units[i]);
            if (units[i] == _unit) myIdx = i;
        }

        if (myIdx < 0)
        {
            _onTurnCompleted?.Invoke();
            return;
        }

        var bestAction = MinimaxRoot(simUnits, myIdx);
        var realAction = ConvertToRealAction(bestAction, units);

        if (realAction != null)
            ExecuteAction(realAction);

        _onTurnCompleted?.Invoke();
    }

    private SimAction MinimaxRoot(SimUnit[] units, int currentIdx)
    {
        var actions = GenerateSimActions(units, currentIdx);
        if (actions.Count == 0) return SimAction.MakeEndTurn();

        float bestVal = float.MinValue;
        SimAction bestAction = actions[0];
        float alpha = float.MinValue;
        float beta = float.MaxValue;

        foreach (var action in actions)
        {
            var nextState = ApplySimAction(units, currentIdx, action);
            int nextIdx = GetNextUnitIdx(nextState, currentIdx);
            bool nextIsMax = nextState[nextIdx].OwnerPlayer == units[currentIdx].OwnerPlayer;

            float val = Minimax(nextState, nextIdx, MAX_DEPTH - 1, alpha, beta, nextIsMax, units[currentIdx].OwnerPlayer);

            if (val > bestVal)
            {
                bestVal = val;
                bestAction = action;
            }
            alpha = Mathf.Max(alpha, bestVal);
        }

        return bestAction;
    }

    private float Minimax(SimUnit[] units, int currentIdx, int depth, float alpha, float beta, bool isMaximizing, int rootPlayer)
    {
        if (depth <= 0 || IsTerminal(units))
            return Evaluate(units, rootPlayer);

        var actions = GenerateSimActions(units, currentIdx);
        if (actions.Count == 0)
            return Evaluate(units, rootPlayer);

        if (isMaximizing)
        {
            float maxEval = float.MinValue;
            foreach (var action in actions)
            {
                var nextState = ApplySimAction(units, currentIdx, action);
                int nextIdx = GetNextUnitIdx(nextState, currentIdx);
                bool nextIsMax = nextState[nextIdx].OwnerPlayer == rootPlayer;

                float eval = Minimax(nextState, nextIdx, depth - 1, alpha, beta, nextIsMax, rootPlayer);
                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            float minEval = float.MaxValue;
            foreach (var action in actions)
            {
                var nextState = ApplySimAction(units, currentIdx, action);
                int nextIdx = GetNextUnitIdx(nextState, currentIdx);
                bool nextIsMax = nextState[nextIdx].OwnerPlayer == rootPlayer;

                float eval = Minimax(nextState, nextIdx, depth - 1, alpha, beta, nextIsMax, rootPlayer);
                minEval = Mathf.Min(minEval, eval);
                beta = Mathf.Min(beta, eval);
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    private float Evaluate(SimUnit[] units, int rootPlayer)
    {
        float myHP = 0f, myMaxHP = 0f;
        float enemyHP = 0f, enemyMaxHP = 0f;
        int myAlive = 0, enemyAlive = 0;

        // Find positions for distance calc
        int myP = 0, myQ = 0, enP = 0, enQ = 0;
        foreach (var u in units)
        {
            if (u.IsDead) continue;
            if (u.OwnerPlayer == rootPlayer) { myP = u.P; myQ = u.Q; myHP += u.HP; myMaxHP += u.MaxHP; myAlive++; }
            else { enP = u.P; enQ = u.Q; enemyHP += u.HP; enemyMaxHP += u.MaxHP; enemyAlive++; }
        }

        if (enemyAlive == 0) return 10000f;
        if (myAlive == 0) return -10000f;

        float hpAdvantage = (myHP / myMaxHP) - (enemyHP / enemyMaxHP);
        float absoluteAdvantage = myHP - enemyHP;
        int dist = HexDistance(myP, myQ, enP, enQ);
        float distFactor = (10f - dist) / 10f;

        return absoluteAdvantage * 2f + hpAdvantage * 50f + distFactor * 5f;
    }

    private bool IsTerminal(SimUnit[] units)
    {
        bool hasPlayer0 = false, hasPlayer1 = false;
        foreach (var u in units)
        {
            if (u.IsDead) continue;
            if (u.OwnerPlayer == 0) hasPlayer0 = true;
            else hasPlayer1 = true;
            if (hasPlayer0 && hasPlayer1) return false;
        }
        return true;
    }

    private List<SimAction> GenerateSimActions(SimUnit[] units, int unitIdx)
    {
        var actions = new List<SimAction>();
        var u = units[unitIdx];
        if (u.IsDead) return actions;

        // Find enemy
        int enemyIdx = -1;
        int enemyDist = 999;
        for (int i = 0; i < units.Length; i++)
        {
            if (i == unitIdx || units[i].IsDead || units[i].OwnerPlayer == u.OwnerPlayer) continue;
            int d = HexDistance(u.P, u.Q, units[i].P, units[i].Q);
            if (d < enemyDist) { enemyDist = d; enemyIdx = i; }
        }

        if (enemyIdx < 0)
        {
            actions.Add(SimAction.MakeEndTurn());
            return actions;
        }

        // Attack if adjacent
        if (enemyDist <= 1)
            actions.Add(SimAction.MakeAttack(enemyIdx));

        // Move options (up to 3 best toward enemy + 1 away for retreat)
        var moveOffsets = GetMoveOffsets();
        var moveCandidates = new List<(int p, int q, int dist)>();
        foreach (var off in moveOffsets)
        {
            int mp = u.P + off.x;
            int mq = u.Q + off.y;
            int mr = -mp - mq;
            Tile tile = _board.GetTile(mp, mq, mr);
            if (tile == null || (tile.OccupiedBy != null && tile.OccupiedBy != _unit)) continue;

            // Check not occupied by another sim unit
            bool occupied = false;
            for (int i = 0; i < units.Length; i++)
            {
                if (i == unitIdx || units[i].IsDead) continue;
                if (units[i].P == mp && units[i].Q == mq) { occupied = true; break; }
            }
            if (occupied) continue;

            int d = HexDistance(mp, mq, units[enemyIdx].P, units[enemyIdx].Q);
            moveCandidates.Add((mp, mq, d));
        }

        moveCandidates.Sort((a, b) => a.dist.CompareTo(b.dist));
        int moveCount = Mathf.Min(3, moveCandidates.Count);
        for (int i = 0; i < moveCount; i++)
        {
            var mc = moveCandidates[i];
            if (mc.dist <= 1)
                actions.Add(SimAction.MakeAttack(enemyIdx, mc.p, mc.q));
            else
                actions.Add(SimAction.MakeMove(mc.p, mc.q));
        }
        // Add retreat option (farthest)
        if (moveCandidates.Count > moveCount)
        {
            var retreat = moveCandidates[moveCandidates.Count - 1];
            actions.Add(SimAction.MakeMove(retreat.p, retreat.q));
        }

        // Skills
        for (int i = 0; i < u.SkillCooldowns.Length; i++)
        {
            if (u.SkillCooldowns[i] > 0) continue;
            actions.Add(SimAction.MakeSkill(i, enemyIdx));
        }

        if (actions.Count == 0)
            actions.Add(SimAction.MakeEndTurn());

        return actions;
    }

    private SimUnit[] ApplySimAction(SimUnit[] state, int unitIdx, SimAction action)
    {
        var next = new SimUnit[state.Length];
        for (int i = 0; i < state.Length; i++)
            next[i] = state[i].Clone();

        ref var actor = ref next[unitIdx];

        // Tick cooldowns
        for (int i = 0; i < actor.SkillCooldowns.Length; i++)
        {
            if (actor.SkillCooldowns[i] > 0) actor.SkillCooldowns[i]--;
        }

        if (action.MoveP != -99 && action.MoveQ != -99)
        {
            actor.P = action.MoveP;
            actor.Q = action.MoveQ;
        }

        switch (action.Type)
        {
            case AIActionType.Attack:
                if (action.TargetIdx >= 0 && action.TargetIdx < next.Length)
                {
                    next[action.TargetIdx].HP -= actor.Attack;
                    if (next[action.TargetIdx].HP < 0) next[action.TargetIdx].HP = 0;
                }
                break;
            case AIActionType.UseSkill:
                if (action.TargetIdx >= 0 && action.SkillIndex >= 0 && action.SkillIndex < actor.SkillCooldowns.Length)
                {
                    // Estimate skill damage as 1.8x attack
                    int skillDmg = Mathf.RoundToInt(actor.Attack * 1.8f);
                    next[action.TargetIdx].HP -= skillDmg;
                    if (next[action.TargetIdx].HP < 0) next[action.TargetIdx].HP = 0;
                    actor.SkillCooldowns[action.SkillIndex] = GetSkillCooldown(unitIdx, action.SkillIndex);
                }
                break;
        }

        return next;
    }

    private int GetSkillCooldown(int unitIdx, int skillIdx)
    {
        var skills = _unit.Skills;
        if (skillIdx >= 0 && skillIdx < skills.Length && skills[skillIdx] != null)
            return skills[skillIdx].Cooldown;
        return 3;
    }

    private int GetNextUnitIdx(SimUnit[] units, int currentIdx)
    {
        for (int i = 1; i <= units.Length; i++)
        {
            int idx = (currentIdx + i) % units.Length;
            if (!units[idx].IsDead) return idx;
        }
        return currentIdx;
    }

    private List<Vector2Int> GetMoveOffsets()
    {
        return new List<Vector2Int>
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, -1), new Vector2Int(-1, 1)
        };
    }

    private AICandidateAction ConvertToRealAction(SimAction simAction, List<Unit> units)
    {
        switch (simAction.Type)
        {
            case AIActionType.Attack:
                {
                    Tile moveTile = null;
                    if (simAction.MoveP != -99 && simAction.MoveQ != -99)
                    {
                        int r = -simAction.MoveP - simAction.MoveQ;
                        moveTile = _board.GetTile(simAction.MoveP, simAction.MoveQ, r);
                    }
                    Unit target = simAction.TargetIdx >= 0 && simAction.TargetIdx < units.Count ? units[simAction.TargetIdx] : null;
                    return new AICandidateAction(AIActionType.Attack, moveTile, target);
                }
            case AIActionType.MoveOnly:
                {
                    int r = -simAction.MoveP - simAction.MoveQ;
                    Tile moveTile = _board.GetTile(simAction.MoveP, simAction.MoveQ, r);
                    return new AICandidateAction(AIActionType.MoveOnly, moveTile, null);
                }
            case AIActionType.UseSkill:
                {
                    Unit target = simAction.TargetIdx >= 0 && simAction.TargetIdx < units.Count ? units[simAction.TargetIdx] : null;
                    return new AICandidateAction(AIActionType.UseSkill, null, target, simAction.SkillIndex);
                }
            default:
                return null;
        }
    }

    private void ExecuteAction(AICandidateAction action)
    {
        if (action.MoveTile != null)
            _unit.PlaceOnTile(action.MoveTile);

        switch (action.ActionType)
        {
            case AIActionType.Attack:
                if (action.TargetUnit != null && !action.TargetUnit.IsDead)
                    action.TargetUnit.TakeDamage(_unit.Attack);
                break;
            case AIActionType.UseSkill:
                if (action.SkillIndex >= 0 && action.SkillIndex < _unit.Skills.Length)
                {
                    var skill = _unit.Skills[action.SkillIndex];
                    if (skill?.Behavior != null)
                    {
                        Tile targetTile = action.TargetUnit != null ? action.TargetUnit.CurrentTile : null;
                        skill.Behavior.Execute(_unit, action.TargetUnit, targetTile, _board, _effects);
                    }
                    else if (action.TargetUnit != null && !action.TargetUnit.IsDead)
                    {
                        action.TargetUnit.TakeDamage(_unit.Attack);
                    }
                    _unit.UseSkill(action.SkillIndex);
                }
                break;
        }
    }

    private List<Unit> GatherAllUnits()
    {
        var result = new List<Unit>();
        int size = _board.Size;
        for (int p = -size; p <= size; p++)
        {
            int qMin = Mathf.Max(-size, -p - size);
            int qMax = Mathf.Min(size, -p + size);
            for (int q = qMin; q <= qMax; q++)
            {
                int r = -p - q;
                Tile tile = _board.GetTile(p, q, r);
                if (tile != null && tile.OccupiedBy != null && !tile.OccupiedBy.IsDead)
                    result.Add(tile.OccupiedBy);
            }
        }
        return result;
    }

    private int HexDistance(int p1, int q1, int p2, int q2)
    {
        int dp = p1 - p2;
        int dq = q1 - q2;
        int dr = (-p1 - q1) - (-p2 - q2);
        return (Mathf.Abs(dp) + Mathf.Abs(dq) + Mathf.Abs(dr)) / 2;
    }
}
