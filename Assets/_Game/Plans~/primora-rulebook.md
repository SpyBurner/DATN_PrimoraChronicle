# Primora Chronicle — Master Rulebook

---

## 1. Game Overview & Factions

Primora Chronicle is a tactical, hex-grid turn-based game where players lead a faction as a Monarch. Players deploy custom-fused units and manipulate the battlefield to reduce opponents' HP to zero.

The `faction` tag on cards is purely narrative and thematic. It does **not** restrict deck building.

### Factions

**Hollow Procession**
Cursed undead entities. Playstyle revolves around sacrifice, resource control via Corrupted terrain, and amplifying damage through the Decay mechanic.

**Verdant Dominion**
Ancient nature spirits. Playstyle revolves around sustain, healing, and accumulating strength over time through Growth Stacks and a persistent NPC evolution chain.

**Ashen Legion**
Creatures born of fire and ash. Playstyle focuses on strong offense, burst damage, and trading resources for immediate advantages.

---

## 2. The Game Board & Grid System

**Hexagonal Grid**
The board uses an axial `(n, p, q)` offset coordinate system. The targeted tile acts as the `(0, 0)` pivot point for calculating AOE and effect ranges. `n` defines the radius or pattern shape.

**Tile Capacity**
A single hex tile can hold a maximum of 1 unit at a time.

**Map Boundaries**
The battlefield is finite. Any movement or knockback that would push a unit beyond the outermost hex stops the unit at the board edge.

**Deploy Area**
Each player has a designated corner tile. This tile is permanently purified and immune to all environmental debuffs, guaranteeing a safe zone to deploy units each turn. At the start of each board clear, the Deploy Area tile is forcibly cleared of any unit or effect, including persistent NPCs. Any unit destroyed this way is sent to the Discard Pile.

---

## 3. Card Types & Deck Building

A valid deck consists of exactly **20 support cards** and exactly **1 Champion**. There is no Mana Cost economy.

### Card Types

**Champion**
The player's avatar and core unit. Pre-selected during deck building. Always available for deployment during the Main Phase. A player's starting HP equals their chosen Champion's HP value.

**Troop**
A deployable unit card. The base of any fusion assembly.

**MainPhaseSpell**
An action card played from hand during the Main Phase. Does not resolve immediately — it is queued and resolves during the Pre-Combat Spell Clash at the start of the Combat Phase.

**EquipSpell**
A card fused onto a unit during the Main Phase, permanently granting it skills or status effects.

### Rarity

**Common**
Freely collectable support cards. Can always be added to any deck. No copy limits.

**Champion**
Cards exclusively granted by a Champion's `grants_cards` list. A Champion-rarity card with `is_summonable: true` can be collected and added to a deck. A Champion-rarity card with `is_summonable: false` is an engine-only stat reference — it never enters a player's hand and only exists as a board NPC spawned by a skill.

### Granted Cards Rule
At the start of a match, a Champion's granted cards are shuffled directly into the player's 20-card deck, increasing the total deck size. This is an intentional balancing mechanic: Champions with powerful granted cards may also grant a higher volume of weaker cards, reducing the probability of cycling back to strong cards each round.

### Fusion Slots
Each unit has exactly **4 fusion slots**. If the unit has an innate skill (`grants_skill`), that skill automatically occupies 1 slot, leaving 3 open for EquipSpells. Units with no innate skill have all 4 slots available. Each EquipSpell card fused occupies exactly 1 slot. Duplicate EquipSpells can be fused onto the same unit.

### Card Lifecycle vs. Unit Lifecycle
A card and the unit it creates are distinct. When a unit is deployed via fusion, all cards involved in that fusion (the Troop card and all fused EquipSpells) immediately become part of the in-combat unit and are no longer in "card state." At the end of the Combat Phase, all those cards go to the Discard Pile — regardless of whether the unit survived or died on the board.

### Persistent NPCs
Some units are created exclusively by skills (`is_summonable: false`). These NPCs are never in card state; they exist only as board entities. Unlike player-deployed units, persistent NPCs survive across combat cycles. See Section 4 for their behavior during board clear.

Persistent NPCs cannot receive new fusions after being summoned. All their stats and skills are fixed at spawn.

---

## 4. Match Phases & Gameplay Loop

A standard match consists of recurring turns, each broken into the following phases.

---

### Phase 1: Start Phase

- **Deck Selection:** Players choose one of their pre-built decks. If a player fails to confirm in time, their most recently played deck is auto-selected.
- **Initial State:** The Champion's Granted Cards are shuffled into the deck. Each player's HP is set to their Champion's HP value.
- **Opening Hand:** The game proceeds directly to Draw Phase logic, dealing the player a starting hand of 6 cards.

---

### Phase 2: Main Phase (Fusion & Setup)

- **Deployment Limit:** A player may manually assemble and deploy exactly 1 unit per turn.
- **The Fusion Mechanic:** The player selects 1 Troop or Champion card and fuses it with up to 4 EquipSpell cards from their hand. Duplicates are allowed. If the base unit has an innate skill, it occupies 1 of the 4 slots. The fully assembled unit is placed on the player's Deploy Area tile.
- **Action Spells:** MainPhaseSpell cards can be played from hand during this phase. They do not resolve immediately — they are queued for the Pre-Combat Spell Clash.
- **Board State During Main Phase:** The board carries over from the previous Combat Phase. Persistent NPCs and lingering tile effects are still present. Queued Main Phase Spells can interact with this live board state.

---

### Phase 3: Combat Phase

**Pre-Combat Spell Clash**
Queued MainPhaseSpells resolve at the very start of this phase, before any unit acts. Execution order is determined by the Speed stat of the unit the player is deploying this turn:
- The player deploying the fastest unit resolves their spell first.
- A player who deploys no unit this turn resolves their spell last.
- Speed ties: lowest HP goes first. Remaining ties: coin toss.

The spell clash uses the same Aggregate → Intercept → Commit modifier pipeline as normal combat (see Section 5). If a player suffers lethal damage during the spell clash, they are immediately eliminated. Their unit is not deployed, and the match ends.

**Action Queue**
All units currently on the board are sorted strictly by their Speed stat. Ties: lowest HP goes first. Remaining ties: coin toss. AI-summoned units added mid-combat are appended to the end of the queue.

**Unit Actions**
On a unit's turn, it may:
1. **Move** up to its allowed hex range, pathing only through empty tiles.
2. **Perform ONE action:** a Normal Attack or use an Active Skill.

Movement and action may happen in any order on the unit's turn. A unit is not required to move or act.

**Cooldowns**
Skill cooldowns tick down by 1 at the precise start of that unit's turn in the queue.

**No Friendly Fire**
AOE attacks and skills strictly ignore allied units, with the following exception: skills marked `ignore_friendly_fire: true` (e.g., Mastery of Flame) are explicitly designed to affect allied units and bypass this rule.

**Jump / Teleport Skills**
Skills marked `ignore_pathfinding: true` (e.g., Molten Dive, Severed Tail) bypass normal hex pathing. The unit teleports directly to the destination tile without checking tiles in between. Only the destination tile must be valid (empty for movement, valid target for projectiles). Using a jump skill counts as the unit's action for that turn. The unit does not also gain a separate free movement.

**Death Anchor**
When a unit's HP reaches 0, it is destroyed. Its `death_anchor` value is immediately subtracted from the owning player's HP. If a player's HP reaches 0 at any point during combat, they are eliminated and all their remaining units on the board are destroyed.

**AI-Controlled NPCs**
Units spawned by skill effects are controlled by a state-machine AI: they move toward the closest enemy unit and execute a random valid attack or skill against it.

**Board Clear**
The cycle continues until only one player's units remain on the board. After this, board clear occurs:
1. All player-deployed units (surviving or not) go to the Discard Pile.
2. Persistent NPCs remain on the board and carry into the next Main Phase.
3. Lingering tile effects (Corrupted, Seeded, Melting) persist into the next cycle.
4. All standard tile effects and non-lingering unit buffs/debuffs are cleared.
5. The Deploy Area tile is forcibly cleared of any unit or effect. Any unit destroyed this way goes to the Discard Pile.

---

### Phase 4: Draw Phase

- **The Draw:** The player is presented with 2 new cards drawn from their deck.
- **Hand Management:** The player uses a drag-and-drop interface to manage their hand. They may keep any combination of newly drawn and existing hand cards, up to a strict maximum of 6. Excess cards must be moved to the Discard Pile.
- **Recycling:** If a player must draw but their deck is empty, the Discard Pile is immediately shuffled back into a new deck.

---

### Phase 5: Win Condition & Edge Cases

**Win Condition**
Player elimination is checked continuously during the Combat Phase. When a player's HP drops to 0 (from Death Anchor or other damage), they are immediately eliminated. Their remaining units are destroyed and removed from the board. The last player with HP above 0 is the winner.

**Simultaneous Elimination**
If multiple players' HP drops to 0 at the exact same moment, all tied players receive a Loss. They suffer an XP penalty and receive reduced Gold to discourage draw-farming.

**Time Limit (Anti-Stall)**
Matches enforce a strict 1-hour time limit. If time expires, the player with the highest remaining HP wins. If multiple players are tied for the highest HP, all tied players receive a Loss.

---

## 5. Combat, Damage & The Modifier Pipeline

All stat modifications and overlapping effects are resolved through a strict three-pass pipeline to prevent race conditions.

**No Base Armor**
All damage is applied directly to HP.

**Targeting**
All targeting is tile-based. Targeting an enemy means targeting the tile that enemy occupies. The `target_condition` bitmask controls valid tile types: `Enemy = 1`, `Ally = 2`, `EmptyTile = 4`. Combine with bitwise OR for multi-condition targeting (e.g., `7` = any tile).

**The Modifier Pipeline**
Before any action resolves, the system evaluates effects in strict order:

1. **Aggregate Pass:** All active stat changes (incoming damage, range reductions, buffs, etc.) are gathered into a temporary buffer.
2. **Intercept Pass:** Defensive status effects (e.g., Barkskin Ward) evaluate the buffer and cancel or modify specific incoming effects before they apply.
3. **Commit Pass:** The final resolved values are applied to the unit's actual stats.

Apply order within the pipeline: `Tile` effects → `Unit` effects.

**Max HP Changes**
Any effect that modifies Max HP (increase or decrease) also applies the same delta to current HP. If a unit's current HP would exceed the new Max HP, it is capped at the new Max HP.

---

## 6. Terrain & Tile Effects

**Lingering Faction Effects (Corrupted, Seeded, Melting)**
These are permanent environmental hazards that persist through board clear into the next Combat Phase.

- **Override Rule:** If a new Lingering Effect is applied to a tile already holding a different one, the old effect is completely replaced.
- **Creator Immunity:** A player is immune to the negative effects of Lingering Effects generated by their own faction.

**Decay (Unit Effect)**
Decay is a Unit-type status effect that travels with the afflicted unit. When a unit with Decay stacks stands on a Corrupted tile, the damage dealt by Corrupted is increased by 1 per Decay stack. Decay does not stay on a tile when the unit moves — it follows the unit.

**Standard Effects**
All other tile effects (e.g., Ash Cloud, Scorching Ground) and unit buffs/debuffs function normally. They stack on top of existing hex states and can be removed by specific Purify effects.

---

## 7. The Verdant Evolution Chain

The Verdant faction has a unique persistent NPC progression: Seedling → Sapling → Young Treant → Thorn Colossus.

**Summoning**
Seedlings are spawned by the `skill_summon_seedling` skill, which is granted by the `card_summon_seedling` EquipSpell. The Seedling is placed as an NPC on a random empty tile adjacent to the unit that activated the skill. The summoning card and host unit go to the Discard Pile at end of combat as normal.

**Persistence**
Seedlings and their evolutions are persistent NPCs. They survive board clear and remain on the board across combat cycles. Their skill cooldowns also persist between cycles.

**Evolution**
The Seedling possesses the `skill_sprout` passive. At the start of its turn, if standing on a Seeded tile, it gains 1 Growth Stack. Upon accumulating 4 Growth Stacks, the unit immediately evolves to the next form and all Growth Stacks are cleared. The evolved unit retains its board position and any remaining tile effects on its tile.

**AI Control**
All forms of the evolution chain are AI-controlled. They move toward the nearest enemy and use a valid attack or skill.

---

## 8. Main Phase Spell Clash — Detailed Resolution

The Pre-Combat Spell Clash uses the same modifier pipeline as normal combat. This means:

- Damage from multiple spells is aggregated before being committed.
- Defensive effects (e.g., Barkskin Ward) applied by one spell can intercept damage from another spell in the same clash batch.
- Tile-manipulation spells (e.g., Transplant) that target the same tile are resolved with a mutex: the first player to confirm their target locks the affected tiles until the effect animation completes. The second player's spell then resolves on the post-effect board state.
- If only one player queues a spell, it resolves alone with no ordering concern.

---

## 9. Data Integrity Rules (for Implementors)

- All `behavior_id` fields (`skill_behavior_id`, `status_effect_behavior_id`, `main_phase_spell_behavior_id`) reference Scriptable Object implementations. The engine treats behavior IDs as opaque strings; validation is done at asset load time.
- Passive skills do not have a `display_pattern` field. This is by design — no player targeting step occurs, so no preview is needed.
- `is_summonable: false` cards must never appear in a player's hand, deck, or fusion slot UI. They are loaded as stat references only.
- Trailing whitespace in `type` fields (`"Tile "`, `"Unit "`) is a known legacy data artifact. All new entries must use clean values (`Tile`, `Unit`).
- `target_condition: 0` is the canonical form for self-targeting passives. Do not use `target_condition: 2` with `n=0` pattern as an alternative for self-targeting.
