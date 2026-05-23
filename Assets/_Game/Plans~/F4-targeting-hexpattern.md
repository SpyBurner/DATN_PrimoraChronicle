# F4 — HexPattern Targeting Inconsistency Audit

## Context

GDS `target_pattern` and `display_pattern` use the `{n, p, q}` format defined in `cardSchema.md`.
The existing codebase has partial targeting stubs that predate this schema. This file documents
all inconsistencies and the full implementation plan for F4 execution.

---

## GDS HexPattern `{n, p, q}` — Discriminator Table

| Condition | Meaning |
|---|---|
| `n=0, p=0, q=0` | Center tile only (self-target, no AoE) |
| `n>0, p=0, q=0` | **Shorthand disc** — all tiles within radius `n` of target |
| `n=-1, p=0, q=0` | **Global** — all tiles on the board |
| `n=-1, p≠0 or q≠0` | **Infinite line** — all tiles in direction `(p,q)` from target |
| `n>0, p≠0 or q≠0` | **Stepped explicit** — `n` steps in direction `(p,q)`; tiles at target + k*(p,q) for k=1..n |

`target_pattern` determines valid target tiles (range + reach).
`display_pattern` determines highlighted AoE tiles shown on hover.

---

## Inconsistency 1 — No `HexPatternResolver` utility

**Problem**: No utility class exists to convert a GDS `List<HexCoord>` (pattern entries) into a
`List<HexCoord>` of resolved board tiles. `GenericSkillBehaviorSO.GetAffectedTiles()` used the
old integer `aoe` field (now removed) and is gone.

**Required**: A static `HexPatternResolver` class in `Core.Interfaces` or `GameplayFeatures`.

```csharp
public static class HexPatternResolver
{
    // Returns all board tiles matched by one HexCoord pattern entry relative to pivot.
    public static List<HexCoord> Resolve(HexCoord pivot, HexCoord pattern, IBoardSubsystem board);

    // Returns all board tiles matched by a full pattern list (union of all entries).
    public static List<HexCoord> ResolveAll(HexCoord pivot, List<HexCoord> patterns, IBoardSubsystem board);

    // Returns the effective range (max steps) from a target_pattern list — used for range ring display.
    public static int GetRange(List<HexCoord> patterns);
}
```

`Resolve` dispatch logic per discriminator:
- `n=0, p=0, q=0` → `[pivot]`
- `n>0, p=0, q=0` → all tiles where `board.Distance(pivot, tile) <= n`
- `n=-1, p=0, q=0` → `board.AllTiles`
- `n=-1, p≠0 or q≠0` → cast ray from pivot in direction `(p,q)` until off-board
- `n>0, p≠0 or q≠0` → `[pivot + k*(p,q) for k in 1..n]` intersected with `board.AllTiles`

---

## Inconsistency 2 — Wrong range derivation in `SkillPanel`

**File**: [Features/Gameplay/Scripts/UI/SkillPanel.cs](../Features/Gameplay/Scripts/UI/SkillPanel.cs)

**Problem**: `range = skillData.target_pattern.Count` — uses list *length*, not semantic range.
A disc-3 pattern `{n=3, p=0, q=0}` has Count=1 but range=3.

**Fix**: Replace with `HexPatternResolver.GetRange(skillData.target_pattern)`.

---

## Inconsistency 3 — `IgnorePathfinding` hardcoded in `SkillPanel`

**File**: [Features/Gameplay/Scripts/UI/SkillPanel.cs](../Features/Gameplay/Scripts/UI/SkillPanel.cs)

**Problem**: `IgnorePathfinding = true` is hardcoded regardless of the skill.

**Fix**: `IgnorePathfinding = skillData.ignore_pathfinding`

---

## Inconsistency 4 — `TargetingSubsystem` stubs are empty

**File**: [Features/Gameplay/Scripts/Targeting/TargetingSubsystem.cs](../Features/Gameplay/Scripts/Targeting/TargetingSubsystem.cs)

**Problem**: `RefreshRangeHighlights()` and `HoverTile()` are empty stubs.

**Fix (RefreshRangeHighlights)**:
1. Look up caster position via `_unit.TryGetPublic(_currentRequest.Caster, out var data)` → `data.Position`
2. Call `_board.GetTilesInRange(casterPos, _currentRequest.Range)` — `Range` was already computed by `HexPatternResolver.GetRange` in `SkillPanel`
3. Populate `_highlighted`, fire `HighlightedTilesChanged`

**Fix (HoverTile)**:
1. Look up skill data via `_cardLoading.TryGetSkillData(_currentRequest.DisplayPattern, out skillData)`
2. Resolve `display_pattern` via `HexPatternResolver.ResolveAll(hoveredTile, skillData.display_pattern, board)`
3. Populate `_highlighted`, fire `HighlightedTilesChanged` — `TargetingOverlay` then colors hovered tile green, AoE tiles yellow

---

## Inconsistency 5 — `TargetingRequest.CasterUnitId` is a string; main plan uses `NetworkId`

**File**: [Core/Scripts/Interfaces/Features/Gameplay/Targeting/TargetingRequest.cs](../Core/Scripts/Interfaces/Features/Gameplay/Targeting/TargetingRequest.cs)

**Problem**: Original code stored `string CasterUnitId = _currentActor.ToString()`. The main plan §3.7
specifies `NetworkId Caster`. Adding `HexCoord CasterPosition` is unnecessary — `TargetingSubsystem`
injects `IUnitSubsystem` and resolves position at `BeginTargeting` time from the `Caster` NetworkId.

**Fix**: Rename `CasterUnitId` → `Caster` of type `NetworkId` (aligns with §3.7). Inject `IUnitSubsystem`
and `ICardLoadingManagerSubsystem` into `TargetingSubsystem`. Do NOT add `CasterPosition` to `TargetingRequest`.

---

## Inconsistency 6 — No `IBoardSubsystem.ContainsTile`

**Problem**: `HexPatternResolver` ray-cast (infinite line) and stepped-explicit modes need to
know when a coordinate falls off the board.

**Fix**: Add `bool ContainsTile(HexCoord coord)` to `IBoardSubsystem` and implement in `BoardSubsystem`.

---

## Implementation Order (F4 execution) — ✅ COMPLETE

1. ✅ Add `ContainsTile` to `IBoardSubsystem` + `BoardSubsystem`
2. ✅ Rename `TargetingRequest.CasterUnitId` (string) → `Caster` (NetworkId) — aligns with §3.7; no `CasterPosition` field needed
3. ✅ Add `ignore_pathfinding` to `SkillData` in `GDSModels.cs`
4. ✅ Implement `HexPatternResolver` static utility
5. ✅ Fix `SkillPanel.OnSkillClicked` range derivation + `IgnorePathfinding`
6. ✅ Implement `TargetingSubsystem.RefreshRangeHighlights()` and `HoverTile()` — inject `IUnitSubsystem` + `ICardLoadingManagerSubsystem`

---

## Key Files

| File | Role |
|---|---|
| `Core/Scripts/Interfaces/Features/Gameplay/Board/IBoardSubsystem.cs` | add `ContainsTile` |
| `Features/Gameplay/Scripts/Board/BoardSubsystem.cs` | implement `ContainsTile` |
| `Core/Scripts/Interfaces/Features/Gameplay/Targeting/TargetingRequest.cs` | `CasterUnitId` → `Caster` (NetworkId) |
| `Core/Scripts/Data/GDSModels.cs` | add `ignore_pathfinding` to `SkillData` |
| `Features/Gameplay/Scripts/Targeting/TargetingSubsystem.cs` | implement stubs; inject `IUnitSubsystem` + `ICardLoadingManagerSubsystem` |
| `Features/Gameplay/Scripts/UI/SkillPanel.cs` | fix range + IgnorePathfinding + Caster field |
| `Features/Gameplay/Scripts/Targeting/HexPatternResolver.cs` | _(new)_ static utility class |
