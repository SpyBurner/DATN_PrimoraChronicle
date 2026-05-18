using Fusion;
using UnityEngine;

public class NetworkUnit : NetworkBehaviour
{
    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int HP { get; set; }
    [Networked] public int MaxHP { get; set; }
    [Networked] public float Speed { get; set; }
    [Networked] public int DeathAnchor { get; set; }
    [Networked] public int HexMovementRange { get; set; }
    [Networked] public int P { get; set; }
    [Networked] public int Q { get; set; }
    [Networked] public NetworkString<_16> Faction { get; set; }
    [Networked] public NetworkString<_16> UnitID { get; set; }
    [Networked] public NetworkBool IsPersistent { get; set; }
    [Networked] public int GrowthStacks { get; set; }
    [Networked] public NetworkBool IsMyTurn { get; set; }

    [Networked, Capacity(4)] public NetworkArray<NetworkString<_16>> EquippedSpells { get; }
    [Networked, Capacity(4)] public NetworkArray<NetworkString<_16>> ActiveSkills { get; }
    [Networked, Capacity(4)] public NetworkArray<int> SkillCooldowns { get; }
    [Networked, Capacity(4)] public NetworkArray<NetworkString<_16>> ActiveStatusEffects { get; }
    [Networked, Capacity(4)] public NetworkArray<int> StatusEffectDurations { get; }

    [Header("Evolution Prefabs (Verdant dominion)")]
    public GameObject seedlingPrefab;
    public GameObject saplingPrefab;
    public GameObject youngTreantPrefab;
    public GameObject thornColossusPrefab;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsMyTurn = false;
        }
    }

    public void InitializeUnit(PlayerRef owner, string unitId, int hp, float speed, int deathAnchor, int moveRange, string faction, bool isPersistent = false)
    {
        if (!Object.HasStateAuthority) return;

        Owner = owner;
        HP = hp;
        MaxHP = hp;
        Speed = speed;
        DeathAnchor = deathAnchor;
        HexMovementRange = moveRange;
        Faction = faction;
        UnitID = unitId;
        IsPersistent = isPersistent;
        GrowthStacks = 0;
    }

    public void StartTurn()
    {
        if (!Object.HasStateAuthority) return;

        IsMyTurn = true;

        // At the start of a unit's turn, all its skill cooldowns tick down by 1.
        for (int i = 0; i < 4; i++)
        {
            if (SkillCooldowns.Get(i) > 0)
            {
                SkillCooldowns.Set(i, SkillCooldowns.Get(i) - 1);
            }
        }

        // Tick down status effects
        for (int i = 0; i < 4; i++)
        {
            string effect = ActiveStatusEffects.Get(i).ToString();
            if (!string.IsNullOrEmpty(effect))
            {
                int rem = StatusEffectDurations.Get(i) - 1;
                StatusEffectDurations.Set(i, rem);

                // Handle status effect tick damage/effects
                if (effect == "burning")
                {
                    TakeDamage(10, Owner); // Burning tick damage
                }

                if (rem <= 0)
                {
                    ActiveStatusEffects.Set(i, string.Empty);
                }
            }
        }

        // Resolve Tile Effects at start of turn
        if (NetworkGameplayManager.Instance != null)
        {
            var tileEffect = NetworkGameplayManager.Instance.FindTileEffectAt(P, Q);
            if (tileEffect != null)
            {
                string type = tileEffect.EffectType.ToString();
                if (type == "ScorchingGround")
                {
                    if (HasStatusEffect("burning"))
                    {
                        ApplyStatusEffect("burning", 2);
                    }
                    else if (HasStatusEffect("smoldering"))
                    {
                        RemoveStatusEffect("smoldering");
                        ApplyStatusEffect("burning", 2);
                    }
                    else
                    {
                        ApplyStatusEffect("smoldering", 2);
                    }
                }
                else if (type == "Melting")
                {
                    TakeDamage(20, Owner); // Melting tick
                }
                else if (type == "Seeded")
                {
                    if (tileEffect.OwnerPlayerRef == Owner) // Ally
                    {
                        AddGrowthStack(1);
                    }
                }
                else if (type == "Corrupted")
                {
                    if (tileEffect.OwnerPlayerRef != Owner) // Enemy
                    {
                        TakeDamage(10, Owner);
                    }
                }
            }

            // Severed Tail effect: deal damage to all units within 2-hex range of any SeveredTail tile effect!
            foreach (var effect in FindObjectsOfType<NetworkTileEffect>())
            {
                if (effect.EffectType.ToString() == "SeveredTail")
                {
                    int dist = GetDistance(P, Q, effect.TileP, effect.TileQ);
                    if (dist <= 2)
                    {
                        TakeDamage(20, Owner);
                    }
                }
            }
        }
    }

    public void ApplyStatusEffect(string effect, int duration)
    {
        if (!Object.HasStateAuthority) return;

        // Try to find if already exists to renew duration
        for (int i = 0; i < 4; i++)
        {
            if (ActiveStatusEffects.Get(i).ToString() == effect)
            {
                StatusEffectDurations.Set(i, Mathf.Max(StatusEffectDurations.Get(i), duration));
                return;
            }
        }

        // Find empty slot
        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrEmpty(ActiveStatusEffects.Get(i).ToString()))
            {
                ActiveStatusEffects.Set(i, effect);
                StatusEffectDurations.Set(i, duration);
                return;
            }
        }
    }

    public void RemoveStatusEffect(string effect)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < 4; i++)
        {
            if (ActiveStatusEffects.Get(i).ToString() == effect)
            {
                ActiveStatusEffects.Set(i, string.Empty);
                StatusEffectDurations.Set(i, 0);
                return;
            }
        }
    }

    public bool HasStatusEffect(string effect)
    {
        for (int i = 0; i < 4; i++)
        {
            if (ActiveStatusEffects.Get(i).ToString() == effect)
            {
                return true;
            }
        }
        return false;
    }

    public void Heal(int amount)
    {
        if (!Object.HasStateAuthority) return;

        // Rule: Decay prevents all kinds of healing
        if (HasStatusEffect("decay")) return;

        HP = Mathf.Min(HP + amount, MaxHP);
    }

    public void EndTurn()
    {
        if (!Object.HasStateAuthority) return;

        IsMyTurn = false;
        if (NetworkGameplayManager.Instance != null)
        {
            NetworkGameplayManager.Instance.StartNextCombatTurn();
        }
    }

    public bool MoveToTile(int targetP, int targetQ, bool ignorePathfinding = false)
    {
        if (!Object.HasStateAuthority) return false;
        if (!IsMyTurn) return false;

        var board = FindObjectOfType<BoardManager>();
        if (board == null) return false;

        // Check bounds / target tile validity
        HexTile targetTile = board.FindTile(targetP, targetQ);
        if (targetTile == null) return false;

        // Rule: Each hex tile holds a maximum of 1 unit at a time.
        if (NetworkGameplayManager.Instance.FindUnitAtTile(targetP, targetQ) != null) return false;

        // Hex distance check
        int dist = GetDistance(P, Q, targetP, targetQ);
        int maxRange = HasStatusEffect("rooted") ? 0 : HexMovementRange;
        if (dist > maxRange) return false;

        if (!ignorePathfinding)
        {
            // Rule: Movement paths only through empty tiles.
            // Simplified straight line path check for hex grid
            if (!IsPathClear(P, Q, targetP, targetQ)) return false;
        }

        int oldP = P;
        int oldQ = Q;

        // Update coordinate
        P = targetP;
        Q = targetQ;

        // Snap physical position to hex position
        Vector3 worldPos = board.ResolveCoordinateToPosition(P, Q);
        if (worldPos != Vector3.zero)
        {
            transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        }

        // Leave a trail of Scorching Ground if burning_trail is active
        if (HasStatusEffect("burning_trail") && NetworkGameplayManager.Instance != null)
        {
            NetworkGameplayManager.Instance.SpawnTileEffect(oldP, oldQ, "ScorchingGround", 3, Owner);
        }

        return true;
    }

    public bool ExecuteNormalAttack(int targetP, int targetQ)
    {
        if (!Object.HasStateAuthority) return false;
        if (!IsMyTurn) return false;

        // Find target unit
        var targetUnit = NetworkGameplayManager.Instance.FindUnitAtTile(targetP, targetQ);
        if (targetUnit == null) return false;

        // Rule: Normal attacks only affect enemy tiles. Allied tiles are ignored automatically.
        if (targetUnit.Owner == Owner) return false;

        // Deal standard damage
        targetUnit.TakeDamage(20, Owner); // Sample damage
        return true;
    }

    public void TakeDamage(int amount, PlayerRef attacker)
    {
        if (!Object.HasStateAuthority) return;

        // Modifiers aggregated / intercepted / committed
        // We will execute the three strict passes:
        // 1. Aggregate: Gather raw stats
        // 2. Intercept: Defensive changes
        // 3. Commit: Write to current stats
        int finalDamage = amount;

        // Check lingering tile effects or unit defensive status
        finalDamage = InterceptDamage(finalDamage);

        HP -= finalDamage;
        if (HP <= 0)
        {
            HP = 0;
            Die();
        }
    }

    private int InterceptDamage(int rawDamage)
    {
        // Sample intercept logic: Tile evaluation happens before unit evaluation
        var tileEffect = NetworkGameplayManager.Instance.FindTileEffectAt(P, Q);
        if (tileEffect != null)
        {
            // If enemy tile effect, it might amplify or affect it.
            // Rule: A unit is immune to negative effects generated by their allies.
            if (tileEffect.OwnerPlayerRef != Owner)
            {
                if (tileEffect.EffectType == "Corrupted")
                {
                    rawDamage += 5; // Extra damage from corruption
                }
            }
        }

        // Barkskin ward reduces incoming damage by 15
        if (HasStatusEffect("barkskin_ward"))
        {
            rawDamage = Mathf.Max(0, rawDamage - 15);
            RemoveStatusEffect("barkskin_ward"); // consumed
        }

        return rawDamage;
    }

    private void Die()
    {
        if (!Object.HasStateAuthority) return;

        if (NetworkGameplayManager.Instance != null)
        {
            NetworkGameplayManager.Instance.HandleUnitDeath(this);
        }

        Runner.Despawn(Object);
    }

    public void AddGrowthStack(int amount)
    {
        if (!Object.HasStateAuthority) return;

        GrowthStacks += amount;
        if (GrowthStacks >= 4)
        {
            Evolve();
        }
    }

    private void Evolve()
    {
        if (!Object.HasStateAuthority) return;

        GameObject nextFormPrefab = null;
        string nextUnitId = "";

        if (UnitID == "Seedling" && saplingPrefab != null)
        {
            nextFormPrefab = saplingPrefab;
            nextUnitId = "Sapling";
        }
        else if (UnitID == "Sapling" && youngTreantPrefab != null)
        {
            nextFormPrefab = youngTreantPrefab;
            nextUnitId = "Young Treant";
        }
        else if (UnitID == "Young Treant" && thornColossusPrefab != null)
        {
            nextFormPrefab = thornColossusPrefab;
            nextUnitId = "Thorn Colossus";
        }

        if (nextFormPrefab != null)
        {
            var nextObj = Runner.Spawn(nextFormPrefab, transform.position, transform.rotation, Owner);
            var nextUnit = nextObj.GetComponent<NetworkUnit>();
            if (nextUnit != null)
            {
                nextUnit.InitializeUnit(Owner, nextUnitId, MaxHP + 20, Speed, DeathAnchor + 1, HexMovementRange, Faction.ToString(), true);
                nextUnit.P = P;
                nextUnit.Q = Q;
            }
            Runner.Despawn(Object);
        }
    }

    // Hex helper methods
    public static int GetDistance(int p1, int q1, int p2, int q2)
    {
        int r1 = -p1 - q1;
        int r2 = -p2 - q2;
        return (Mathf.Abs(p1 - p2) + Mathf.Abs(q1 - q2) + Mathf.Abs(r1 - r2)) / 2;
    }

    private bool IsPathClear(int p1, int q1, int p2, int q2)
    {
        // Simple linear hex path check
        int dist = GetDistance(p1, q1, p2, q2);
        for (int i = 1; i < dist; i++)
        {
            float t = (float)i / dist;
            int currP = Mathf.RoundToInt(Mathf.Lerp(p1, p2, t));
            int currQ = Mathf.RoundToInt(Mathf.Lerp(q1, q2, t));

            if (NetworkGameplayManager.Instance.FindUnitAtTile(currP, currQ) != null)
            {
                return false;
            }
        }
        return true;
    }
}
