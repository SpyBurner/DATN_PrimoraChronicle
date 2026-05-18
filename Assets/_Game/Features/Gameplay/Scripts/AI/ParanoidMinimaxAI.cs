using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ParanoidMinimaxAI : MonoBehaviour
{
    public static ParanoidMinimaxAI Instance { get; private set; }

    [Header("Difficulty / Search Settings")]
    [Range(1, 3)]
    public int searchDepth = 2; // 1: Easy, 2: Medium, 3: Hard (d_max limits)

    [Header("Evaluation Weights")]
    public float W_p = 1.0f;  // Player Pressure weight
    public float W_d = 0.5f;  // Distance Factor weight
    public float W_t = 0.8f;  // Tile Effect weight
    public float W_m = 0.5f;  // Mode Modifier weight
    public float lambda = 1.5f; // Penalty coefficient for received damage

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Executes host-authoritative AI decision logic on the server.
    /// </summary>
    public void ExecuteAILogic(NetworkUnit aiUnit)
    {
        if (aiUnit == null || NetworkGameplayManager.Instance == null) return;
        if (!NetworkGameplayManager.Instance.Runner.IsServer) return;

        Debug.Log($"[ParanoidMinimaxAI] AI Unit {aiUnit.UnitID} executing turn logic.");

        // Gather candidate moves
        List<AICandidateAction> candidates = GenerateCandidateActions(aiUnit);
        
        AICandidateAction bestAction = null;
        float bestVal = float.MinValue;

        // Perform search
        foreach (var action in candidates)
        {
            float val = MinimaxEvaluateAction(aiUnit, action, searchDepth);
            if (val > bestVal)
            {
                bestVal = val;
                bestAction = action;
            }
        }

        // Execute best chosen action
        if (bestAction != null)
        {
            ExecuteAction(aiUnit, bestAction);
        }
        else
        {
            Debug.LogWarning("[ParanoidMinimaxAI] No valid candidate action found. Ending turn.");
            aiUnit.EndTurn();
        }
    }

    private float MinimaxEvaluateAction(NetworkUnit aiUnit, AICandidateAction action, int depth)
    {
        // 1. Simulate the resulting state S'
        Vector3 simulatedPos = action.MoveTile != null 
            ? new Vector3(action.MoveTile.p, action.MoveTile.q, 0f) 
            : new Vector3(aiUnit.P, aiUnit.Q, 0f);

        int simP = (int)simulatedPos.x;
        int simQ = (int)simulatedPos.y;

        // Calculate evaluation score E(S') from LaTeX formulas
        float score = EvaluateState(aiUnit, simP, simQ, action);
        return score;
    }

    private float EvaluateState(NetworkUnit aiUnit, int simP, int simQ, AICandidateAction action)
    {
        // Treat all opponents as a single adversarial coalition (MIN)
        float playerPressure = CalculatePlayerPressure(aiUnit, simP, simQ, action);
        float distanceFactor = CalculateDistanceFactor(aiUnit, simP, simQ);
        float tileEffect = CalculateTileEffect(aiUnit, simP, simQ);
        float modeModifier = CalculateModeModifier(aiUnit);

        float score = W_p * playerPressure + W_d * distanceFactor + W_t * tileEffect + W_m * modeModifier;

        // Extreme absolute win/loss scoring guards
        if (aiUnit.HP <= 0) score = float.MinValue;
        
        return score;
    }

    private float CalculatePlayerPressure(NetworkUnit aiUnit, int simP, int simQ, AICandidateAction action)
    {
        // Potential damage AI can deal
        float d_potential = 0f;
        if (action.ActionType == AIActionType.Attack && action.TargetUnit != null)
        {
            d_potential += 20f; // Attack damage
        }
        else if (action.ActionType == AIActionType.UseSkill && action.TargetUnit != null)
        {
            d_potential += 30f; // Skill damage estimate
        }

        // Received damage aggregated from all opponents (treating all opponents as a single unified coalition MIN)
        float d_received_sum = 0f;
        foreach (var u in FindObjectsOfType<NetworkUnit>())
        {
            if (u.Owner != aiUnit.Owner && u.HP > 0)
            {
                int dist = NetworkUnit.GetDistance(simP, simQ, u.P, u.Q);
                // Potential damage drops with distance
                d_received_sum += 25f / (dist + 1);
            }
        }

        return d_potential - lambda * d_received_sum;
    }

    private float CalculateDistanceFactor(NetworkUnit aiUnit, int simP, int simQ)
    {
        float sum = 0f;
        int nearestDist = 999;
        
        foreach (var u in FindObjectsOfType<NetworkUnit>())
        {
            if (u.Owner != aiUnit.Owner && u.HP > 0)
            {
                int dist = NetworkUnit.GetDistance(simP, simQ, u.P, u.Q);
                if (dist < nearestDist) nearestDist = dist;
            }
        }

        if (nearestDist < 999)
        {
            sum = (10f - nearestDist) / 10f;
        }

        return sum;
    }

    private float CalculateTileEffect(NetworkUnit aiUnit, int simP, int simQ)
    {
        float value = 0f;
        var effect = NetworkGameplayManager.Instance.FindTileEffectAt(simP, simQ);
        if (effect != null)
        {
            string effectType = effect.EffectType.ToString();
            if (effectType == "Seeded" && aiUnit.Faction.ToString() == "Verdant")
            {
                value += 5f;
            }
            else if (effectType == "Corrupted" && aiUnit.Faction.ToString() == "Hollow")
            {
                value += 5f;
            }
            else if (effectType == "Corrupted" && aiUnit.Faction.ToString() != "Hollow")
            {
                value -= 5f;
            }
            else if (effectType == "AshCloud" || effectType == "Melting")
            {
                value -= 5f;
            }
            else if (effectType == "BannerOfCinders" && aiUnit.Faction.ToString() == "Ashen")
            {
                value += 5f;
            }
        }
        return value;
    }

    private float CalculateModeModifier(NetworkUnit aiUnit)
    {
        float score = 0f;
        int totalOppHP = 0;
        int oppCount = 0;

        foreach (var u in FindObjectsOfType<NetworkUnit>())
        {
            if (u.Owner != aiUnit.Owner && u.HP > 0)
            {
                totalOppHP += u.HP;
                oppCount++;
            }
        }

        if (oppCount > 0)
        {
            float avgOppHP = (float)totalOppHP / oppCount;
            if (aiUnit.HP > avgOppHP)
            {
                score += (aiUnit.HP - avgOppHP) * 2f; // Aggressive boost
            }
            else
            {
                score += (aiUnit.HP - avgOppHP) * 1.5f; // Defensive penalty
            }
        }

        return score;
    }

    private List<AICandidateAction> GenerateCandidateActions(NetworkUnit aiUnit)
    {
        List<AICandidateAction> candidates = new List<AICandidateAction>();
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return candidates;

        // Find nearest enemy unit
        NetworkUnit nearestEnemy = null;
        int nearestDist = 999;
        foreach (var u in FindObjectsOfType<NetworkUnit>())
        {
            if (u.Owner != aiUnit.Owner && u.HP > 0)
            {
                int d = NetworkUnit.GetDistance(aiUnit.P, aiUnit.Q, u.P, u.Q);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestEnemy = u;
                }
            }
        }

        // Action Candidate: Stay & Attack if in range
        if (nearestEnemy != null && nearestDist <= 1)
        {
            candidates.Add(new AICandidateAction(AIActionType.Attack, null, nearestEnemy));
        }

        // Action Candidates: Move towards enemy, then Attack/Use Skill
        if (nearestEnemy != null)
        {
            HexTile bestMoveTile = null;
            int bestMoveDist = nearestDist;

            foreach (var tile in board.GetComponentsInChildren<HexTile>())
            {
                int distToCaster = NetworkUnit.GetDistance(aiUnit.P, aiUnit.Q, tile.p, tile.q);
                if (distToCaster <= aiUnit.HexMovementRange && NetworkGameplayManager.Instance.FindUnitAtTile(tile.p, tile.q) == null)
                {
                    int distToEnemy = NetworkUnit.GetDistance(tile.p, tile.q, nearestEnemy.P, nearestEnemy.Q);
                    if (distToEnemy < bestMoveDist)
                    {
                        bestMoveDist = distToEnemy;
                        bestMoveTile = tile;
                    }
                }
            }

            if (bestMoveTile != null)
            {
                if (bestMoveDist <= 1)
                {
                    candidates.Add(new AICandidateAction(AIActionType.Attack, bestMoveTile, nearestEnemy));
                }
                else
                {
                    candidates.Add(new AICandidateAction(AIActionType.MoveOnly, bestMoveTile, null));
                }
            }
        }

        // Fallback: End Turn only
        candidates.Add(new AICandidateAction(AIActionType.EndTurn, null, null));

        return candidates;
    }

    private void ExecuteAction(NetworkUnit aiUnit, AICandidateAction action)
    {
        Debug.Log($"[ParanoidMinimaxAI] Executing action {action.ActionType} on caster {aiUnit.UnitID}");

        // 1. Execute Move if specified
        if (action.MoveTile != null)
        {
            aiUnit.MoveToTile(action.MoveTile.p, action.MoveTile.q, true); // Host-driven teleport/move
        }

        // 2. Execute combat action
        if (action.ActionType == AIActionType.Attack && action.TargetUnit != null)
        {
            aiUnit.ExecuteNormalAttack(action.TargetUnit.P, action.TargetUnit.Q);
        }
        else if (action.ActionType == AIActionType.UseSkill && action.TargetUnit != null)
        {
            // Execute skill directly on Host
            var firstSkill = aiUnit.ActiveSkills.Get(0).ToString();
            if (!string.IsNullOrEmpty(firstSkill) && NetworkGameplayManager.Instance != null)
            {
                var skill = NetworkGameplayManager.Instance.skillBehaviors.Find(s => s.behaviorId == firstSkill);
                if (skill != null)
                {
                    var targetTile = FindObjectOfType<BoardManager>().FindTile(action.TargetUnit.P, action.TargetUnit.Q);
                    if (targetTile != null)
                    {
                        skill.Execute(NetworkGameplayManager.Instance, aiUnit, targetTile);
                    }
                }
            }
        }

        // 3. End Turn
        aiUnit.EndTurn();
    }
}

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
    public HexTile MoveTile;
    public NetworkUnit TargetUnit;

    public AICandidateAction(AIActionType type, HexTile moveTile, NetworkUnit target)
    {
        ActionType = type;
        MoveTile = moveTile;
        TargetUnit = target;
    }
}
