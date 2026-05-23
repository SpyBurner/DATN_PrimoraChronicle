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

public class AIUnitController : UnitController
{
    private float _wP = 1.0f;
    private float _wD = 0.5f;
    private float _lambda = 1.5f;

    public AIUnitController(Unit unit, BoardController board) : base(unit, board)
    {
    }

    public override void TakeTurn()
    {
        var candidates = GenerateCandidateActions();

        AICandidateAction bestAction = null;
        float bestVal = float.MinValue;

        foreach (var action in candidates)
        {
            float val = EvaluateAction(action);
            if (val > bestVal)
            {
                bestVal = val;
                bestAction = action;
            }
        }

        if (bestAction != null)
            ExecuteAction(bestAction);

        Debug.Log($"AI Player {_unit.OwnerPlayer} chose {bestAction?.ActionType} (score: {bestVal:F1})");
        _onTurnCompleted?.Invoke();
    }

    private float EvaluateAction(AICandidateAction action)
    {
        int simP = action.MoveTile != null ? action.MoveTile.P : _unit.P;
        int simQ = action.MoveTile != null ? action.MoveTile.Q : _unit.Q;

        float pressure = CalculatePlayerPressure(simP, simQ, action);
        float distance = CalculateDistanceFactor(simP, simQ);

        return _wP * pressure + _wD * distance;
    }

    private float CalculatePlayerPressure(int simP, int simQ, AICandidateAction action)
    {
        float dPotential = 0f;
        if (action.ActionType == AIActionType.Attack && action.TargetUnit != null)
            dPotential = _unit.Attack;
        else if (action.ActionType == AIActionType.UseSkill && action.TargetUnit != null)
            dPotential = _unit.Attack * 1.5f;

        float dReceived = 0f;
        foreach (var tile in GetAllOccupiedEnemyTiles())
        {
            var enemy = tile.OccupiedBy;
            int dist = HexDistance(simP, simQ, enemy.P, enemy.Q);
            dReceived += enemy.Attack / (float)(dist + 1);
        }

        return dPotential - _lambda * dReceived;
    }

    private float CalculateDistanceFactor(int simP, int simQ)
    {
        int nearestDist = 999;
        foreach (var tile in GetAllOccupiedEnemyTiles())
        {
            int dist = HexDistance(simP, simQ, tile.OccupiedBy.P, tile.OccupiedBy.Q);
            if (dist < nearestDist) nearestDist = dist;
        }

        if (nearestDist >= 999) return 0f;
        return (10f - nearestDist) / 10f;
    }

    private List<AICandidateAction> GenerateCandidateActions()
    {
        var candidates = new List<AICandidateAction>();

        Unit nearestEnemy = FindNearestEnemy(out int nearestDist);

        if (nearestEnemy != null && nearestDist <= 1)
        {
            candidates.Add(new AICandidateAction(AIActionType.Attack, null, nearestEnemy));
        }

        if (nearestEnemy != null)
        {
            Tile bestMoveTile = null;
            int bestMoveDist = nearestDist;

            foreach (var coord in _unit.GetMoveTiles())
            {
                Tile tile = _board.GetTile(coord.x, coord.y, coord.z);
                if (tile == null || tile.OccupiedBy != null) continue;

                int distToEnemy = HexDistance(tile.P, tile.Q, nearestEnemy.P, nearestEnemy.Q);
                if (distToEnemy < bestMoveDist)
                {
                    bestMoveDist = distToEnemy;
                    bestMoveTile = tile;
                }
            }

            if (bestMoveTile != null)
            {
                if (bestMoveDist <= 1)
                    candidates.Add(new AICandidateAction(AIActionType.Attack, bestMoveTile, nearestEnemy));
                else
                    candidates.Add(new AICandidateAction(AIActionType.MoveOnly, bestMoveTile, null));
            }
            else if (nearestDist > 1)
            {
                // No better tile found but still move to any available tile
                foreach (var coord in _unit.GetMoveTiles())
                {
                    Tile tile = _board.GetTile(coord.x, coord.y, coord.z);
                    if (tile == null || tile.OccupiedBy != null) continue;
                    candidates.Add(new AICandidateAction(AIActionType.MoveOnly, tile, null));
                    break;
                }
            }
        }

        for (int i = 0; i < _unit.Skills.Length; i++)
        {
            var skill = _unit.Skills[i];
            if (skill == null || !skill.IsReady) continue;

            var targets = _unit.GetSkillTargets(i);
            foreach (var coord in targets)
            {
                Tile tile = _board.GetTile(coord.x, coord.y, coord.z);
                if (tile == null || tile.OccupiedBy == null) continue;
                if (tile.OccupiedBy.OwnerPlayer == _unit.OwnerPlayer) continue;

                candidates.Add(new AICandidateAction(AIActionType.UseSkill, null, tile.OccupiedBy, i));
                break;
            }
        }

        return candidates;
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
                if (action.TargetUnit != null && !action.TargetUnit.IsDead && action.SkillIndex >= 0)
                {
                    action.TargetUnit.TakeDamage(_unit.Attack);
                    _unit.UseSkill(action.SkillIndex);
                }
                break;
        }
    }

    private Unit FindNearestEnemy(out int nearestDist)
    {
        Unit nearest = null;
        nearestDist = 999;

        foreach (var tile in GetAllOccupiedEnemyTiles())
        {
            var enemy = tile.OccupiedBy;
            int dist = HexDistance(_unit.P, _unit.Q, enemy.P, enemy.Q);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private List<Tile> GetAllOccupiedEnemyTiles()
    {
        var result = new List<Tile>();
        int size = _board.Size;
        for (int p = -size; p <= size; p++)
        {
            int qMin = Mathf.Max(-size, -p - size);
            int qMax = Mathf.Min(size, -p + size);
            for (int q = qMin; q <= qMax; q++)
            {
                int r = -p - q;
                Tile tile = _board.GetTile(p, q, r);
                if (tile != null && tile.OccupiedBy != null
                    && tile.OccupiedBy.OwnerPlayer != _unit.OwnerPlayer
                    && !tile.OccupiedBy.IsDead)
                {
                    result.Add(tile);
                }
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
