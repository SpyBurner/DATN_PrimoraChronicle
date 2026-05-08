# Person A — Unity Client: Gameplay & Game AI

> **Scope:** All in-match gameplay subsystems, the hex board, combat AI (Minimax), Start/Main/Combat/Draw/Win phases, card data population, graphical polish, and the replay system.
> **Codebase root:** `Assets/_Game/Features/Gameplay/`, `Assets/_Game/Core/Scripts/SOScript/CardSO/`

---

## Current Status (as of 2026-05-08)

### ✅ Already Implemented
| Area | What Exists | Notes |
|---|---|---|
| **Subsystem Shells** | `BoardSubsystem`, `CombatSubsystem`, `HandSubsystem`, `FusePhaseSubsystem`, `DrawPhaseSubsystem`, `MatchResultSubsystem`, `GameStateSubsystem` | All have Interface + Controller + Model + Panel stubs |
| **Start Phase** | `IStartPhaseController`, `IStartPhaseModel` (interfaces only) | No concrete implementation, no subsystem class |
| **Card SO Hierarchy** | `CardSO` → `TroopCardSO`, `ChampionCardSO`, `SpellCardSO` | Base fields exist (Name, Mana Cost, Rarity, Type, Illustration) |
| **Card Ideas Data** | `TestBE~/card_ideas.csv` (34 KB) | Raw card designs from the Excel sheet |
| **Gameplay Scene** | `Assets/_Game/Scenes/Gameplay.unity` in build index 3 | Scene exists but is largely empty |

### ❌ Not Yet Implemented (from Report §5, §7.2, §7.3)
- Hex grid generation & TileView rendering
- Start Phase Ban/Pick UI & Champion selection logic
- Main Phase: Fuse mechanic UI & card drag-to-deploy
- Combat Phase: Speed-based turn order, attack/skill/move action execution
- Draw Phase: deck-draw + discard logic
- Win Phase: HP-to-zero detection, reward calculation
- AI Main Phase (weighted card evaluation, mana-constrained selection)
- AI Combat Phase (Minimax + Alpha-Beta pruning)
- Pattern system runtime (axial `⟨r,p,q⟩` expansion on hex grid)
- Command logging for replay
- Post-processing / graphical atmosphere
- Card ScriptableObject asset creation from CSV

---

## Execution Plan

### Phase A1: Card Data & Board Foundation (Week 1–2)

#### A1.1 — Card ScriptableObject Population
> Ref: `Complete plans/card_creation_plan.md`

- [ ] **Refine CardSO fields** to match report §6.2:
  - `TroopCardSO` / `ChampionCardSO`: add `HP`, `Speed`, `DeathAnchor`, `NormalAttackDmg`, `NormalAttackPattern` (list of `⟨r,p,q⟩`), `NormalAttackEffectPattern`, `NormalAttackCD`, `Skills` (list of `SkillSO`)
  - `SpellCardSO`: discriminated sub-types via `EnumSpellType { Skill, Action, Equipment }`
  - Add `EquipCardSO` with delta fields (`DeltaHP`, `DeltaSpeed`, `DeltaNAtk`, etc.)
  - Create `SkillSO` ScriptableObject (`ATK`, `AtkPattern`, `AtkEffectPattern`, `AtkCD`, `Effect`)
  - Create `PatternElementSO` or serializable struct for `⟨r,p,q⟩` data
- [ ] **Create `CardDataImporter.cs`** Editor script to parse `card_ideas.csv` → generate SO assets under `Assets/_Game/Data/Cards/{Faction}/`
- [ ] **Run importer** and verify all card SOs have correct stats
- [ ] **Create `ChampionRegistrySO`** listing all available Champions (Ancient Lich, Groveheart, Kharvax)
- [ ] **Sync IDs** with TestBE seed data (`seed.py`)

#### A1.2 — Hex Board System
> Ref: Report §5.5 (Bàn đấu)

- [ ] **Implement `HexGridGenerator`** inside `BoardController`:
  - Pointy-top hexagonal grid using axial coordinates `(p, q)`
  - Configurable grid size (report says deploy area per player)
  - Each tile stores: position, occupant reference, tile effects, region faction
- [ ] **Create `TileView` MonoBehaviour** for each hex tile:
  - Visual states: Idle (faint cyan pulse 10%), Hover (purple glow + particles), Selected (green/red highlight for valid/invalid)
  - Ref: `Complete plans/graphical.md` §3
- [ ] **Implement `DeployArea`** logic: mark valid placement zones per player
- [ ] **Wire `BoardPanel`** (currently a stub) to render the grid in Gameplay scene
- [ ] **Setup Cinemachine** cameras: top-down strategic view + unit-focus view

---

### Phase A2: Start Phase & Hand Management (Week 2–3)

#### A2.1 — Start Phase (Ban/Pick)
> Ref: Report §5.7 (Start Phase), §7.2 (UI phác thảo)

- [ ] **Implement `StartPhaseSubsystem`** (concrete class — currently only interfaces exist):
  - `StartPhaseController`: manages Ban → Pick → Deck Select → Draw Initial Hand flow
  - `StartPhaseModel`: stores champion selection state, ready flags, ban list
- [ ] **Create `BanPickPanel` UI** (currently placeholder):
  - Champion selection grid with countdown timer
  - Secret simultaneous selection (reveal on both ready or timeout)
  - Display opponent's Champion after reveal
- [ ] **Deck selection UI**: show user's saved decks (fetch from `IDeckBuildSubsystem`)
- [ ] **Initial hand draw**: draw 4 cards from selected deck into `HandSubsystem`
- [ ] **Network sync** via Photon Fusion: `[Networked]` champion selection, ready states

#### A2.2 — Hand Subsystem
> Ref: Report US-G03, US-G04

- [ ] **Flesh out `HandController`** (currently a stub):
  - `DrawCards(int count)`: pull from deck, check hand limit (7 cards max)
  - Overflow → auto-discard to Discard Pile
  - Deck exhaustion → shuffle Discard Pile back into deck
- [ ] **`HandPanel`**: card fan layout, drag-to-deploy interaction
- [ ] **Mana tracking**: per-turn mana (start 2, cap 8, +1 per turn)

---

### Phase A3: Main Phase & Fuse Mechanics (Week 3–4)

#### A3.1 — Main Phase (Fuse)
> Ref: Report §5.7 (Main Phase), US-G04/G05

- [ ] **Flesh out `FusePhaseController`**:
  - Fuse logic: Unit card + up to 4 Spell cards → create enhanced unit
  - Mana cost validation: `Σ Cost(c) ≤ Mana_current`
  - Equipment delta application (add `DeltaHP`, `DeltaSpeed`, etc.)
  - Skill card → adds new skill to unit's skill list
  - Action card → immediate effect (mana/HP manipulation)
- [ ] **`FusePhasePanel`**: 
  - Large Unit slot + 4 small Spell/Equip slots
  - "READY" and "SKIP TURN" buttons
  - Drag from hand to fusion slots
- [ ] **Deploy to board**: place fused unit from FusePhase onto DeployArea tile
- [ ] **Network**: all deployments → `[Networked]` model updates via Photon Fusion

---

### Phase A4: Combat Phase & Turn System (Week 4–6)

#### A4.1 — Combat Core
> Ref: Report §5.7 (Combat Phase), §7.3 (AI Combat Phase)

- [ ] **Implement Speed-based `ActionQueue`** in `CombatController`:
  - Sort all units by Speed → determine action order per cycle
  - Each unit: Move OR Attack OR Use Skill (one action per turn)
- [ ] **`TurnOrderPanel`**: horizontal bar showing unit action order (matches report UI)
- [ ] **`SkillPanel`**: show available actions for the current active unit
- [ ] **Movement system**:
  - Validate moves against unit's Move Pattern on hex grid
  - Animate unit movement between tiles
- [ ] **Attack system**:
  - Check attack pattern against hex grid for valid targets
  - Apply damage: reduce target HP
  - If HP ≤ 0 → unit dies → apply DeathAnchor damage to owning player's HP
- [ ] **Skill system**:
  - Check skill pattern, CD, and effect
  - Apply AtkPattern + AtkEffectPattern on hex grid
  - Cooldown tracking per skill
- [ ] **Cycle resolution**: combat ends when only one player's units remain → clear board
- [ ] **Animation triggers**: `Idle`, `Walk`, `Attack`, `Hurt`, `Death`, `SkillCast`

#### A4.2 — Pattern Runtime Engine
> Ref: Report §6.2 (Pattern và Effect)

- [ ] **Implement `PatternResolver`**:
  - Input: unit position `(p,q)`, pattern list `⟨r,p,q⟩`
  - Output: set of affected tile coordinates
  - For each element: expand `(kp, kq)` for k=1..r, stop at board edge
- [ ] **Pattern visualization**: highlight affected tiles on hover/selection
- [ ] **Integrate with attack, skill, and move systems**

---

### Phase A5: Draw & Win Phases (Week 5–6)

#### A5.1 — Draw Phase
> Ref: Report §5.7 (Draw Phase), US-G07

- [ ] **Flesh out `DrawPhaseController`**:
  - Draw 1 card per player after combat
  - Hand limit check (7 max → excess to discard)
  - Deck exhaustion → shuffle discard pile
- [ ] **`DrawPhasePanel`**: show drawn card(s), "CONFIRM" to finalize

#### A5.2 — Win Phase
> Ref: Report §5.7 (Win Phase), US-G08

- [ ] **HP-to-zero check** in `GameStateController` after each phase cycle
- [ ] **`MatchResultPanel`** (stub exists): display Win/Lose + rewards (Gold, XP)
- [ ] **POST results** to backend via `POST /api/matches/result`
- [ ] **Return to Lobby** scene after match ends

---

### Phase A6: Game AI (Week 6–8)

#### A6.1 — AI Main Phase
> Ref: Report §7.2 (AI Main Phase)

- [ ] **Create `AIMainPhaseAgent`**:
  - Card evaluation: `Score(c) = (Tactical, Aggressive, Defensive)` per card
  - Weighted sum: `Value = Σ(w_t·Tactical + w_a·Aggressive + w_d·Defensive)`
  - Dynamic weight adjustment based on `ΔHP = HP_AI - HP_opp`
  - Mana-constrained optimization: select best card subset under mana budget
  - Execute fuse actions on behalf of AI player

#### A6.2 — AI Combat Phase (Minimax)
> Ref: Report §7.3 (AI Combat Phase)

- [ ] **Create `AICombatAgent`**:
  - State representation: player HPs, unit positions/HPs/skills, tile effects
  - Legal move generation: move, attack, skill (per unit constraints)
  - Minimax with Alpha-Beta pruning
  - Depth limit `d_max` for real-time feasibility
- [ ] **Evaluation function `E(S)`**:
  - Terminal check: `+∞` (AI wins), `-∞` (AI loses)
  - `PlayerPressure = D_potential - λ·D_received`
  - `DistanceFactor = Σ (Range - Dist) / Range`
  - `TileEffect = Σ TileValue(AI_pos) - Σ TileValue(opp_pos)`
  - `ModeModifier`: aggressive boost if HP advantage, defensive penalty if disadvantaged
  - Multi-player modifier `μ` for 3-player mode
- [ ] **Integrate AI agents** into `GameStateSubsystem` for AI player slots

---

### Phase A7: Replay & Command Logging (Week 7–8)

> Ref: Report §6.3 Stage 3–4 (Command logging for replay)

- [ ] **Create `CommandLogSubsystem`** (match-scoped):
  - Record all gameplay Commands (ChampionPick, Deploy, Move, Attack, UseSkill, FuseCard, SkipTurn, etc.)
  - Timestamp + sequential ordering
  - Serialize to JSON (ActionLog format)
- [ ] **Inject into Controllers**: each gameplay controller records its commands
- [ ] **Match end**: bundle CommandLog → upload via `POST /api/matches/result` actionLogData
- [ ] **Replay playback** (basic): re-apply Command sequence from JSON against initial state

---

### Phase A8: Graphical Polish (Week 8–9)

> Ref: `Complete plans/graphical.md`

- [ ] **Gameplay Scene Post-Processing Volume (URP)**:
  - Bloom (threshold 1.0, intensity 1.5)
  - Vignette (intensity 0.45)
  - Color Grading (ACES, blue lift, purple/teal gain)
  - Film Grain (low)
- [ ] **Environment**:
  - Dark gradient ambient (Navy/Purple/Black)
  - Exponential Squared fog (dark grey/purple, density 0.015–0.02)
  - Low-intensity cool main light with soft shadows
- [ ] **Unit VFX** (per faction):
  - Hollow: black smoke/mist particles
  - Ashen: ember sparks, heat distortion
  - Verdant: glowing teal leaves/vines
- [ ] **Spell VFX**: high-energy additive-shader impact particles
- [ ] **Tile pulse animations**: idle glow + hover particles

---

## Dependencies on Person B
- **Backend API** must be live for: `POST /api/matches/result`, `GET /api/config` (SystemConfig for reward calc)
- **Card data sync**: Person A generates CardSO IDs → Person B seeds them in backend
- **Deck data**: `GET /api/decks` needed for Start Phase deck selection
- **User data**: `GET /api/users/me` needed for HP initialization from Champion

## Deliverables Checklist
- [ ] Playable single-player match (vs AI) from Start Phase to Win Phase
- [ ] All 3 Factions with at least 1 Champion + 10 cards each (SO assets)
- [ ] AI opponent for both Main Phase and Combat Phase
- [ ] Command replay (basic playback)
- [ ] Post-processing and VFX atmosphere
