# Primora Chronicle — System Verification Plan

Use the Unity Console and MCP `get_console_logs` after each step to confirm expected output.  
All tests assume a 2-player session (1 human + 1 AI unless noted).

---

## 1. Board & Hex Tile Spawning

**What to check:** Board parent is a networked object; all 61 tiles spawn and register correctly.

**Steps:**
1. Enter Play mode.
2. Open the Hierarchy — confirm `Board` GameObject exists under the scene.
3. Expand `Board` — confirm it has a `NetworkObject` component and exactly 61 child `HexTile_P*_Q*` GameObjects.
4. Confirm BoardManager tile list count = 61 via log.

**Expected logs:**
```
[NetworkSpawner] Generated board with 61 hex tiles on state authority.
```

**Failure signs:**
- Fewer/more than 61 tiles → coordinate loop off-by-one
- No `NetworkObject` on Board → board not synced across clients

---

## 2. Player Spawn Position & Height

**What to check:** Players spawn at the surface of their starting hex tile, not floating above it.

**Steps:**
1. Enter Play mode (2 players or 1 + 1 AI).
2. Check Player 1's world position in Inspector.
3. Check Player 2's world position in Inspector.
4. Compare Y values to the child Renderer bounds.max.y of their respective tiles.

**Expected logs:**
```
[NetworkSpawner] Resolved player X position with surface height: P=4, Q=-4 -> (x, y, z)
[NetworkSpawner] Resolved player X position with surface height: P=-4, Q=4 -> (x, y, z)
```

**Pass criteria:**
- Player Y ≈ tile surface bounds.max.y (within 0.05 units)
- No players visually floating or clipped into board

**Failure signs:**
- `WARNING: BoardManager returned zero` → tile not registered at that coordinate
- `Using fallback spawn position` → BoardManager lookup failed; fallback Y = board + 1f

---

## 3. NetworkPlayerState — Deck / Hand / Discard

**What to check:** Deck populates correctly, DrawCards works, discard reshuffles.

**Steps:**
1. Enter Play mode.
2. After Start Phase, confirm in Inspector: `DeckCount` = number of cards set up, `HandCount` = 6.
3. Trigger Draw Phase (let Main Phase timer expire or end combat).
4. Confirm `HandCount` stays ≤ 6. `DiscardCount` increases for discarded cards.
5. Manually clear the Deck in Inspector (set `DeckCount = 0`) mid-draw — confirm `DiscardCount` drops to 0 and `DeckCount` rises (reshuffle triggered).

**Expected logs:**
```
[NetworkGameplayManager] Auto-confirmed decks for all players.
```

**Pass criteria:**
- HandCount is always 0–6
- DeckCount + DiscardCount + HandCount = total card pool (never loses cards)
- Reshuffle produces a non-empty deck from discard

---

## 4. Deploy Area Assignment

**What to check:** Each player has the correct Deploy Area coordinates.

**Steps:**
1. Enter Play mode.
2. Select each `NetworkPlayerState` in Inspector.
3. Read `DeployAreaP` and `DeployAreaQ` values.

**Pass criteria:**
| Player Index | DeployAreaP | DeployAreaQ |
|---|---|---|
| 0 (P1/Host) | 4 | -4 |
| 1 (P2/Client or AI) | -4 | 4 |

---

## 5. Gameplay Phase Transitions

**What to check:** Phases transition in the correct order; timers fire correctly.

**Steps:**
1. Enter Play mode. Observe `CurrentPhase` on `NetworkGameplayManager` in Inspector.
2. Wait for Start Phase timer (default 30s) to expire — phase should become `MainPhase`.
3. Wait for Main Phase timer (default 60s) — phase should become `CombatPhase`.
4. Observe combat turns executing (see Section 7).
5. After Board Clear — phase should become `DrawPhase`.
6. Wait 30s — phase should return to `MainPhase`.

**Expected phase sequence:**
```
Setup → StartPhase → MainPhase → CombatPhase → DrawPhase → MainPhase → ...
```

**Expected logs:**
```
[NetworkGameplayManager] Game Over! Winner: X
```
(eventually, when a player reaches 0 HP)

---

## 6. Combat Action Queue — Turn Order

**What to check:** Units are sorted Speed desc → HP asc → coin toss; queue executes in order.

**Steps:**
1. During CombatPhase, pause Unity and inspect `CombatActionQueue` on NetworkGameplayManager.
2. Find the `NetworkUnit` objects for each NetworkId in the queue.
3. Verify they are sorted: highest Speed first; ties broken by lowest HP first.
4. Resume — confirm each unit's `IsMyTurn` becomes true in queue order.

**Expected logs:**
```
[ParanoidMinimaxAI] AI Unit AICrownChampion executing turn logic.
```

**Pass criteria:**
- A unit with Speed=5 always acts before Speed=3
- A unit with HP=10 acts before HP=50 when speeds are equal

---

## 7. Unit Movement & Pathfinding

**What to check:** Unit moves only to reachable tiles; blocked paths are rejected.

**Steps:**
1. During a unit's turn (`IsMyTurn = true`), call `unit.MoveToTile(targetP, targetQ)` via context menu or RPC.
2. Test **valid move**: empty tile within `HexMovementRange`.
3. Test **blocked path**: place another unit on a tile between source and destination — move should fail.
4. Test **out-of-range**: tile farther than `HexMovementRange` — returns false.
5. Test **occupied destination**: tile that already has a unit — returns false.

**Pass criteria:**
- Valid move: unit's `P`/`Q` updates, physical position snaps to tile
- Blocked / out-of-range / occupied: returns `false`, position unchanged

---

## 8. Normal Attack — Enemy-Only Filtering

**What to check:** Normal attacks hit enemies only; allied units on adjacent tiles are ignored.

**Steps:**
1. Position an allied unit and an enemy unit each 1 tile from the attacker.
2. Call `unit.ExecuteNormalAttack(allyP, allyQ)` — should return `false`, ally HP unchanged.
3. Call `unit.ExecuteNormalAttack(enemyP, enemyQ)` — should return `true`, enemy HP decreases.

**Pass criteria:**
- Ally takes 0 damage
- Enemy HP decreases by 20 (default normal attack damage)

---

## 9. Skill Execution RPC

**What to check:** Client-to-host RPC validates range, targeting, cooldown, and executes.

**Steps:**
1. During a unit's turn, call `RPC_RequestSkillExecution(unitId, skillId, targetP, targetQ)`.
2. Test **out-of-range target** — no effect, warning log emitted.
3. Test **invalid target type** (e.g. ally tile for an enemy-only skill) — no effect, warning log.
4. Test **skill on cooldown** (`SkillCooldowns[i] > 0`) — no effect.
5. Test **valid execution** — skill effect applies.
6. After valid use, confirm `SkillCooldowns[i]` = 3.

**Expected logs:**
```
[SkillExecution] Caster X executing skb_* on tile (p, q)
[NetworkGameplayManager] Out of range for skill execution! Distance: N, Skill Range: M
[NetworkGameplayManager] Tile p,q is an invalid target for skb_*!
```

---

## 10. One-Time Skills

**What to check:** A skill with `one_time = true` fires exactly once per combat cycle.

**Steps:**
1. Assign a skill with `one_time = true` in a SkillBehaviorSO asset.
2. During CombatPhase, execute the skill — should succeed; `SkillUsedThisCycle[i]` = true.
3. Attempt to execute same skill again in same combat phase — should be blocked.
4. After Board Clear → next CombatPhase begins — `SkillUsedThisCycle` should be reset to false.
5. Execute skill again — should succeed.

---

## 11. Skill Cooldowns — Tick-Down

**What to check:** Cooldowns decrease by 1 at the start of each unit turn.

**Steps:**
1. Use a skill — confirm `SkillCooldowns[i]` = 3.
2. Wait for the unit's next turn — confirm `SkillCooldowns[i]` = 2.
3. Repeat until 0 — skill should be usable again.

**Pass criteria:**
- Cooldown never goes below 0
- Skill is unusable while cooldown > 0

---

## 12. Damage Pipeline — 3-Pass Verification

**What to check:** Tile effects modify damage before unit effects; barkskin_ward consumes on use.

**Steps:**
1. Place a unit on a `Corrupted` tile (enemy-owned). Apply 20 damage — expect 25 received (Corrupted adds 5).
2. Apply `barkskin_ward` status to a unit. Deal 20 damage — expect 5 (20 - 15). Confirm `barkskin_ward` removed after.
3. Combine both: Corrupted tile + barkskin_ward. Deal 20 damage — expect 10 (20 + 5 - 15). Tile effect applied first.

**Pass criteria:**
- Results match the arithmetic above
- `barkskin_ward` is removed after one hit

---

## 13. Death Anchor & Player Elimination

**What to check:** Unit death subtracts `DeathAnchor` from the owning player's HP; 0 HP eliminates.

**Steps:**
1. Set a unit's `DeathAnchor = 30`. Reduce its HP to 0.
2. Confirm owning player's `HP` decreases by 30 immediately.
3. Set player HP to exactly `DeathAnchor` value, then kill another unit — player HP should reach 0.
4. Confirm `IsAlive = false` on that player's `NetworkPlayerState`.
5. Confirm all of that player's units are despawned immediately.

---

## 14. Win Condition

**What to check:** Last surviving player is declared winner; time limit resolves by HP.

**Test A — Last player standing:**
1. Eliminate all players except one — `EndMatch(winner)` should fire.
2. Confirm log: `[NetworkGameplayManager] Game Over! Winner: X`
3. Confirm `CurrentPhase = GameOver`.

**Test B — Time limit:**
1. Temporarily set `matchTimeLimit = 10f` (10 seconds).
2. Let game run. After 10s, player with highest HP wins.
3. If HP tied, both get `PlayerRef.None` as winner (tie = both lose).

---

## 15. Board Clear & Persistent Units

**What to check:** Non-persistent units go to discard; persistent units survive; Deploy Area is cleared.

**Steps:**
1. Deploy a normal unit and a persistent unit (IsPersistent = true) on the board.
2. Trigger Board Clear (eliminate all but one player's units).
3. Confirm non-persistent unit is despawned.
4. Confirm persistent unit remains on board with its cooldowns intact.
5. Place a unit on a Deploy Area tile before board clear — confirm that unit is removed.

---

## 16. Tile Effects — Lingering Behavior

**What to check:** Tile effects persist through board clear; one per tile; same type refreshes, different replaces.

**Steps:**
1. Spawn a `Corrupted` tile effect at (0, 0).
2. Trigger board clear — confirm the effect is still at (0, 0) in the next Main Phase.
3. Spawn another `Corrupted` at (0, 0) — confirm `RemainingDuration` is refreshed (max of two durations).
4. Spawn a `Seeded` at (0, 0) — confirm `Corrupted` is despawned and `Seeded` replaces it.

---

## 17. Tile Effect Resolution on Unit Turn

**What to check:** Each tile effect triggers correct behavior when a unit starts its turn on it.

| Tile Effect | Expected Outcome |
|---|---|
| ScorchingGround | First entry: `smoldering`. Already smoldering: becomes `burning`. Already burning: refreshes duration. |
| Melting | Unit takes 20 damage immediately. |
| Seeded (ally) | Unit gains 1 Growth Stack. |
| Seeded (enemy) | No effect (ally immunity). |
| Corrupted (enemy-owned) | Unit takes 10 damage. |
| Corrupted (own faction) | No effect (ally immunity). |
| SeveredTail within range 2 | Unit takes 20 damage. |

**Steps:**
1. For each row, place unit on tile, add effect, start unit's turn.
2. Confirm HP change and status effect matches expected.

---

## 18. Verdant Evolution Chain

**What to check:** Growth stacks accumulate; evolution fires at 4 stacks; stats increase; stacks reset.

**Steps:**
1. Spawn a Seedling (IsPersistent = true, UnitID = "Seedling").
2. Call `unit.AddGrowthStack(1)` four times.
3. Confirm on the 4th call: Seedling is despawned and Sapling is spawned at same P/Q.
4. Repeat until Thorn Colossus — confirm each form's MaxHP is higher than previous.
5. Confirm GrowthStacks = 0 after each evolution.

---

## 19. Local Interaction — Tile Highlights

**What to check:** Selecting a skill colors range yellow; hovering shows AOE green; invalid targets show red.

**Steps:**
1. During a unit's turn in the local player's session, click a skill in the skill panel.
2. Confirm tiles within `skill.range` turn **yellow**.
3. Hover mouse over a yellow tile — confirm AOE tiles turn **green**.
4. Hover over a tile with an ally (for an enemy-targeting skill) — confirm it turns **red**.
5. Click a green tile — confirm `RPC_RequestSkillExecution` fires.
6. Cancel selection — confirm all tiles reset to original color.

**Expected logs:**
```
[LocalInteractionController] Dispatching RPC: skill=skb_* target=(p,q)
```

---

## 20. AI — Paranoid Minimax Turn Execution

**What to check:** AI executes a valid action each turn; searchDepth setting affects behavior.

**Steps:**
1. Start a 1 human vs 1 AI session.
2. When AI unit's turn starts, observe console logs for decision-making.
3. Confirm AI unit moves or attacks (HP changes on target, or AI position changes).
4. Set `searchDepth = 1` — AI plays faster but less optimally.
5. Set `searchDepth = 3` — AI takes longer to decide but plays smarter.

**Expected logs:**
```
[ParanoidMinimaxAI] AI Unit AICrownChampion executing turn logic.
[ParanoidMinimaxAI] Executing action: Move/Attack/Skill
```

**Pass criteria:**
- AI never gets stuck (always calls `aiUnit.EndTurn()` even if no action found)
- AI targets enemies, not allies

---

## 21. Champion Grants Framework

**What to check:** `AddGrantedCards` correctly appends cards to the deck and shuffles.

**Steps (runtime test via script or Inspector):**
1. After `SetupDeck` sets `DeckCount = 2`, call `AddGrantedCards` with a list: `[{string_id: "card_X", quantity: 3}]`.
2. Confirm `DeckCount = 5`.
3. Confirm the 3 new cards are present somewhere in the deck (not just at the end).
4. Confirm total card count is preserved (no cards lost or duplicated).

---

## Quick-Reference: Console Logs by System

| System | Key Log Pattern |
|---|---|
| Board | `Generated board with 61 hex tiles` |
| Player Spawn | `Resolved player X position with surface height` |
| Phase change | `CurrentPhase` change in NetworkGameplayManager |
| Skill blocked | `Out of range` / `invalid target` |
| Unit death | Handled by `HandleUnitDeath` |
| Win | `Game Over! Winner:` |
| AI turn | `AI Unit * executing turn logic` |
| RPC dispatch | `Dispatching RPC: skill=*` |

---

## Regression Checklist After Any Code Change

- [ ] Board spawns 61 tiles
- [ ] Players spawn at tile surface height
- [ ] Phase sequence runs without hanging
- [ ] Combat queue processes all units and reaches Board Clear
- [ ] No unit gets permanently stuck with `IsMyTurn = true`
- [ ] Win condition fires correctly
- [ ] Console has 0 errors at end of a full match cycle
