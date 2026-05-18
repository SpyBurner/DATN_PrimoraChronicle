using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum GameplayPhase
{
    Setup,
    StartPhase,
    MainPhase,
    CombatPhase,
    DrawPhase,
    GameOver
}

public class NetworkGameplayManager : NetworkBehaviour
{
    public static NetworkGameplayManager Instance { get; private set; }

    [Networked] public GameplayPhase CurrentPhase { get; set; }
    [Networked] public TickTimer PhaseTimer { get; set; }
    [Networked] public int CurrentRound { get; set; }
    [Networked] public NetworkBool IsTimerExpired { get; set; }

    [Header("Phase Durations")]
    public float startPhaseDuration = 30f;
    public float mainPhaseDuration = 60f;
    public float drawPhaseDuration = 30f;
    public float matchTimeLimit = 3600f; // 1 hour

    [Networked, Capacity(4)] public NetworkArray<NetworkId> PlayerStates { get; }
    [Networked] public int PlayerCount { get; set; }

    // Action Queue for Combat Phase
    [Networked, Capacity(20)] public NetworkArray<NetworkId> CombatActionQueue { get; }
    [Networked] public int CombatQueueSize { get; set; }
    [Networked] public int CurrentCombatTurnIndex { get; set; }

    private float _matchTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentPhase = GameplayPhase.Setup;
            CurrentRound = 1;
            _matchTimer = 0f;
        }
    }

    public void RegisterPlayerState(NetworkPlayerState state)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < PlayerStates.Length; i++)
        {
            if (!PlayerStates.Get(i).IsValid)
            {
                PlayerStates.Set(i, state.Object.Id);
                PlayerCount++;
                break;
            }
        }

        if (PlayerCount >= 2 && CurrentPhase == GameplayPhase.Setup)
        {
            StartMatch();
        }
    }

    private void StartMatch()
    {
        if (!Object.HasStateAuthority) return;
        CurrentPhase = GameplayPhase.StartPhase;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, startPhaseDuration);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Match Time Limit checks
        _matchTimer += Runner.DeltaTime;
        if (_matchTimer >= matchTimeLimit)
        {
            IsTimerExpired = true;
            EndMatchWithTimeLimit();
            return;
        }

        if (PhaseTimer.Expired(Runner))
        {
            HandlePhaseTimeout();
        }
    }

    private void HandlePhaseTimeout()
    {
        switch (CurrentPhase)
        {
            case GameplayPhase.StartPhase:
                // Auto-select deck if any player has not done so
                AutoConfirmDecks();
                TransitionToMainPhase();
                break;
            case GameplayPhase.MainPhase:
                TransitionToCombatPhase();
                break;
            case GameplayPhase.DrawPhase:
                AutoConfirmDraws();
                TransitionToMainPhase();
                break;
        }
    }

    private void AutoConfirmDecks()
    {
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null && !playerState.IsReady)
                {
                    // Fallback to default/most recent deck
                    string[] defaultCards = new string[] { "troop_scout", "troop_warrior", "equip_sword", "spell_fireball" };
                    playerState.SetupDeck("champ_hero", defaultCards, 100);
                    playerState.DrawCards(6);
                }
            }
        }
    }

    private void TransitionToMainPhase()
    {
        CurrentPhase = GameplayPhase.MainPhase;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, mainPhaseDuration);
        
        // Deploy Area is cleared at the end of each turn (which triggers transitioning to next phase)
        ClearDeployAreas();
    }

    private void TransitionToCombatPhase()
    {
        CurrentPhase = GameplayPhase.CombatPhase;
        PhaseTimer = TickTimer.None; // Combat runs until resolved, not by duration timer

        // Reset one_time skill usage flags for this combat cycle
        ResetOneTimeSkillsForCycle();

        BuildCombatActionQueue();
        StartNextCombatTurn();
    }

    private void ResetOneTimeSkillsForCycle()
    {
        foreach (var unit in FindObjectsByType<NetworkUnit>(FindObjectsSortMode.None))
        {
            if (unit.Object != null && unit.Object.HasStateAuthority)
            {
                for (int i = 0; i < 4; i++)
                {
                    unit.SkillUsedThisCycle.Set(i, false);
                }
            }
        }
    }

    private void TransitionToDrawPhase()
    {
        CurrentRound++;
        CurrentPhase = GameplayPhase.DrawPhase;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, drawPhaseDuration);

        // Deal 2 new cards to all players
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null && playerState.IsAlive)
                {
                    playerState.DrawCards(2);
                }
            }
        }
    }

    private void AutoConfirmDraws()
    {
        // Draw phase confirmation: keep up to 6 cards in hand. This will auto discard any extra.
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null)
                {
                    while (playerState.HandCount > 6)
                    {
                        playerState.DiscardCard(playerState.HandCount - 1);
                    }
                }
            }
        }
    }

    private void ClearDeployAreas()
    {
        // Clears Deploy Area tiles of all units and effects
        var board = FindFirstObjectByType<BoardManager>();
        if (board == null) return;

        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null)
                {
                    var deployTile = board.FindTile(playerState.DeployAreaP, playerState.DeployAreaQ);
                    if (deployTile != null)
                    {
                        // Clear units
                        var unit = FindUnitAtTile(playerState.DeployAreaP, playerState.DeployAreaQ);
                        if (unit != null)
                        {
                            Runner.Despawn(unit.Object);
                        }

                        // Clear tile effects
                        var tileEffect = FindTileEffectAt(playerState.DeployAreaP, playerState.DeployAreaQ);
                        if (tileEffect != null)
                        {
                            Runner.Despawn(tileEffect.Object);
                        }
                    }
                }
            }
        }
    }

    private void BuildCombatActionQueue()
    {
        List<NetworkUnit> units = new List<NetworkUnit>();
        foreach (var unit in FindObjectsByType<NetworkUnit>(FindObjectsSortMode.None))
        {
            if (unit.Object != null && unit.Object.IsValid)
            {
                units.Add(unit);
            }
        }

        // Sort by speed desc, then HP asc, then coin toss
        System.Random rand = new System.Random();
        units.Sort((a, b) =>
        {
            int speedCompare = b.Speed.CompareTo(a.Speed);
            if (speedCompare != 0) return speedCompare;

            int hpCompare = a.HP.CompareTo(b.HP);
            if (hpCompare != 0) return hpCompare;

            return rand.Next(-1, 2); // tie-breaker coin toss
        });

        CombatQueueSize = 0;
        for (int i = 0; i < units.Count; i++)
        {
            CombatActionQueue.Set(i, units[i].Object.Id);
            CombatQueueSize++;
        }

        CurrentCombatTurnIndex = 0;
    }

    public void StartNextCombatTurn()
    {
        if (!Object.HasStateAuthority) return;

        // Skip destroyed units
        while (CurrentCombatTurnIndex < CombatQueueSize)
        {
            NetworkId activeUnitId = CombatActionQueue.Get(CurrentCombatTurnIndex);
            if (Runner.TryFindObject(activeUnitId, out var unitObj))
            {
                var unit = unitObj.GetComponent<NetworkUnit>();
                if (unit != null && unit.HP > 0)
                {
                    // Start unit turn
                    unit.StartTurn();

                    if (IsAIUnit(unit))
                    {
                        if (ParanoidMinimaxAI.Instance != null)
                        {
                            ParanoidMinimaxAI.Instance.ExecuteAILogic(unit);
                        }
                        else
                        {
                            Debug.LogWarning("[NetworkGameplayManager] ParanoidMinimaxAI instance not found. Ending AI turn.");
                            unit.EndTurn();
                        }
                    }
                    return;
                }
            }
            CurrentCombatTurnIndex++;
        }

        // Action queue finished -> check Board Clear condition
        CheckBoardClear();
    }

    public bool IsAIUnit(NetworkUnit unit)
    {
        if (unit == null) return false;

        // None designate host-authoritative server spawned virtual units/AIs
        if (unit.Owner == PlayerRef.None) return true;

        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (stateId.IsValid && Runner.TryFindObject(stateId, out var stateObj))
            {
                var pState = stateObj.GetComponent<NetworkPlayerState>();
                if (pState != null && pState.Player == unit.Owner && pState.IsAI)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void CheckBoardClear()
    {
        if (!Object.HasStateAuthority) return;

        // Board clear triggers when only one player's units remain on the board (non-persistent units)
        var units = FindObjectsByType<NetworkUnit>(FindObjectsSortMode.None);
        HashSet<PlayerRef> uniquePlayers = new HashSet<PlayerRef>();
        List<NetworkUnit> playerUnits = new List<NetworkUnit>();

        foreach (var u in units)
        {
            if (!u.IsPersistent)
            {
                playerUnits.Add(u);
                uniquePlayers.Add(u.Owner);
            }
        }

        if (uniquePlayers.Count <= 1)
        {
            // Transition units to Discard Pile
            foreach (var u in playerUnits)
            {
                MoveUnitToDiscard(u);
                Runner.Despawn(u.Object);
            }

            // Lingering tile effects persist but no duration tick
            // Clear units on Deploy Areas
            ClearDeployAreas();

            // Check if game over
            CheckWinCondition();

            if (CurrentPhase != GameplayPhase.GameOver)
            {
                TransitionToDrawPhase();
            }
        }
        else
        {
            // Re-build queue for next round of combat if units remain
            BuildCombatActionQueue();
            StartNextCombatTurn();
        }
    }

    private void MoveUnitToDiscard(NetworkUnit unit)
    {
        // Find player state and send cards to Discard
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null && playerState.Player == unit.Owner)
                {
                    // Add troop and equip spell cards used in fusion to Discard
                    if (playerState.DiscardCount < 40)
                    {
                        playerState.Discard.Set(playerState.DiscardCount, unit.UnitID);
                        playerState.DiscardCount++;
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        string equip = unit.EquippedSpells.Get(j).ToString();
                        if (!string.IsNullOrEmpty(equip) && playerState.DiscardCount < 40)
                        {
                            playerState.Discard.Set(playerState.DiscardCount, equip);
                            playerState.DiscardCount++;
                        }
                    }
                }
            }
        }
    }

    public void HandleUnitDeath(NetworkUnit unit)
    {
        if (!Object.HasStateAuthority) return;

        // Immediately subtract death anchor from player's HP
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null && playerState.Player == unit.Owner)
                {
                    playerState.HP -= unit.DeathAnchor;
                    if (playerState.HP <= 0)
                    {
                        playerState.HP = 0;
                        playerState.IsAlive = false;
                        EliminatePlayer(playerState.Player);
                    }
                }
            }
        }

        CheckWinCondition();
    }

    private void EliminatePlayer(PlayerRef player)
    {
        // Destroy all units belonging to the player
        foreach (var u in FindObjectsByType<NetworkUnit>(FindObjectsSortMode.None))
        {
            if (u.Owner == player)
            {
                Runner.Despawn(u.Object);
            }
        }
    }

    private void CheckWinCondition()
    {
        int aliveCount = 0;
        NetworkPlayerState winner = null;

        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null && playerState.IsAlive)
                {
                    aliveCount++;
                    winner = playerState;
                }
            }
        }

        if (aliveCount == 1 && winner != null)
        {
            EndMatch(winner.Player);
        }
        else if (aliveCount == 0)
        {
            EndMatch(PlayerRef.None); // All tied/died -> ties loss
        }
    }

    private void EndMatchWithTimeLimit()
    {
        if (!Object.HasStateAuthority) return;

        // Player with highest HP wins
        NetworkPlayerState highestHPPlayer = null;
        int maxHPValue = -1;
        bool tie = false;

        for (int i = 0; i < PlayerStates.Length; i++)
        {
            NetworkId stateId = PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null && playerState.IsAlive)
                {
                    if (playerState.HP > maxHPValue)
                    {
                        maxHPValue = playerState.HP;
                        highestHPPlayer = playerState;
                        tie = false;
                    }
                    else if (playerState.HP == maxHPValue)
                    {
                        tie = true;
                    }
                }
            }
        }

        if (tie || highestHPPlayer == null)
        {
            EndMatch(PlayerRef.None);
        }
        else
        {
            EndMatch(highestHPPlayer.Player);
        }
    }

    private void EndMatch(PlayerRef winner)
    {
        CurrentPhase = GameplayPhase.GameOver;
        PhaseTimer = TickTimer.None;
        Debug.Log($"[NetworkGameplayManager] Game Over! Winner: {winner}");
    }

    // Helpers
    [Header("Skill Behavior Assets")]
    public List<GenericSkillBehaviorSO> skillBehaviors = new List<GenericSkillBehaviorSO>();
    public GameObject tileEffectPrefab;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSkillExecution(NetworkId unitId, string skillId, int targetP, int targetQ)
    {
        if (!Object.HasStateAuthority) return;

        // 1. Retrieve the unit
        if (!Runner.TryFindObject(unitId, out var unitObj)) return;
        var unit = unitObj.GetComponent<NetworkUnit>();
        if (unit == null) return;

        // 2. Validate turn
        if (!unit.IsMyTurn) return;

        // 3. Find target tile
        var board = FindFirstObjectByType<BoardManager>();
        if (board == null) return;
        var tile = board.FindTile(targetP, targetQ);
        if (tile == null) return;

        // 4. Resolve skill behavior
        GenericSkillBehaviorSO skill = skillBehaviors.Find(s => s.behaviorId == skillId);
        if (skill == null)
        {
            Debug.LogError($"[NetworkGameplayManager] Skill behavior not found: {skillId}");
            return;
        }

        // 5. Verify distance / range
        int dist = NetworkUnit.GetDistance(unit.P, unit.Q, targetP, targetQ);
        if (dist > skill.range)
        {
            Debug.LogWarning($"[NetworkGameplayManager] Out of range for skill execution! Distance: {dist}, Skill Range: {skill.range}");
            return;
        }

        // 6. Verify targeting restrictions
        if (!skill.IsTileValidTarget(this, unit, tile, skill.targetCondition))
        {
            Debug.LogWarning($"[NetworkGameplayManager] Tile {targetP},{targetQ} is an invalid target for {skillId}!");
            return;
        }

        // Find skill index for cooldown/one_time tracking
        int skillIndex = -1;
        for (int i = 0; i < 4; i++)
        {
            if (unit.ActiveSkills.Get(i).ToString() == skillId)
            {
                skillIndex = i;
                break;
            }
        }

        // 7. Check if one_time skill has already been used this cycle
        if (skill.one_time && skillIndex >= 0 && unit.SkillUsedThisCycle.Get(skillIndex))
        {
            Debug.LogWarning($"[NetworkGameplayManager] One-time skill {skillId} has already been used this cycle!");
            return;
        }

        // 8. Execute!
        skill.Execute(this, unit, tile);

        // Mark one_time skill as used and set cooldown
        if (skillIndex >= 0)
        {
            if (skill.one_time)
            {
                unit.SkillUsedThisCycle.Set(skillIndex, true);
            }
            unit.SkillCooldowns.Set(skillIndex, 3); // Standard 3-turn cooldown
        }
    }

    public NetworkUnit FindUnitAtTile(int p, int q)
    {
        foreach (var u in FindObjectsByType<NetworkUnit>(FindObjectsSortMode.None))
        {
            if (u.P == p && u.Q == q) return u;
        }
        return null;
    }

    public NetworkTileEffect FindTileEffectAt(int p, int q)
    {
        foreach (var e in FindObjectsByType<NetworkTileEffect>(FindObjectsSortMode.None))
        {
            if (e.TileP == p && e.TileQ == q) return e;
        }
        return null;
    }

    public void SpawnTileEffect(int p, int q, string effectType, int duration, PlayerRef owner)
    {
        if (!Object.HasStateAuthority) return;

        var existing = FindTileEffectAt(p, q);
        if (existing != null)
        {
            if (existing.EffectType.ToString() == effectType)
            {
                existing.RemainingDuration = Mathf.Max(existing.RemainingDuration, duration);
                return;
            }
            else
            {
                Runner.Despawn(existing.Object);
            }
        }

        if (tileEffectPrefab != null)
        {
            var effectObj = Runner.Spawn(tileEffectPrefab, Vector3.zero, Quaternion.identity, owner);
            var effect = effectObj.GetComponent<NetworkTileEffect>();
            if (effect != null)
            {
                effect.ApplyEffect(p, q, effectType, duration, owner);
            }
        }
    }
}
