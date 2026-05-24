# Wiring — T1 Panel Loose-End Fixes (24-05 automation)

Legend: ⬜ todo · ✅ done · ⚠ verify

> Code-side fixes landed on branch `feat/T1-panel-loose-ends`.
> This doc lists the **Editor wiring** that you still need to do by hand.

---

## What changed in code (read me first)

| Change | File | Effect on wiring |
|---|---|---|
| `ICombatSubsystem` gained `CurrentActorCanMoveChanged` + `CurrentActorCanActChanged` events | `Core/Scripts/Interfaces/Features/Gameplay/Combat/ICombatSubsystem.cs` | No wiring change. |
| `UnitPublicData` gained `BaseCardId`, `MoveRange`, `NormalAttackDamage` | `Core/Scripts/Interfaces/Features/Gameplay/Unit/UnitPublicData.cs` | No wiring change — `UnitNetworkView.PushState()` now writes them. |
| New `SkillSlotUI` component (parallel to `FuseSlotUI`) | `Features/Gameplay/Scripts/UI/SkillSlotUI.cs` | **Wiring change required** — see §1. |
| `SkillPanel._skillSlotPrefab` field type changed from `GameObject` → `SkillSlotUI` | `Features/Gameplay/Scripts/UI/SkillPanel.cs` | **Wiring change required** — see §2. |
| `FusionPanel` gained 5 new innate-slot text fields + a `_defaultMoveRange` int | `Features/Gameplay/Scripts/UI/FusionPanel.cs` | **Wiring change required** — see §3. |

---

## 1. `SkillSlot.prefab` — attach `SkillSlotUI` component

Open `Assets/_Game/Features/Gameplay/UI/Component/SkillSlot.prefab` (or wherever the existing slot prefab lives — search for the one assigned to `SkillPanel._skillSlotPrefab` previously).

| Step | Action | Status |
|---|---|---|
| 1 | Add `SkillSlotUI` MonoBehaviour to the prefab **root** | ⬜ |
| 2 | Drag the slot's `Button` into `SkillSlotUI.Button` | ⬜ |
| 3 | Drag the `TMP_Text` displaying the skill name into `SkillSlotUI.NameText` | ⬜ |
| 4 | Drag the `TMP_Text` displaying cooldown / "Used" / "Done" into `SkillSlotUI.CooldownText` | ⬜ |
| 5 | Drag the slot's background `Image` into `SkillSlotUI.Background` | ⬜ |
| 6 | (Optional) Drag the icon `Image` into `SkillSlotUI.Icon` | ⬜ |

> Why this matters: the previous `SkillPanel` did `transform.Find("NameText")` which silently failed unless the prefab child was literally named "NameText". Using a component eliminates the name-coupling.

---

## 2. `PhaseInteractionPanel_Skill.prefab` — re-assign slot prefab

The `SkillPanel._skillSlotPrefab` field type is now `SkillSlotUI` (component), no longer `GameObject`. Unity will null out the old reference on first import.

| Step | Action | Status |
|---|---|---|
| 1 | Open `PhaseInteractionPanel_Skill.prefab` | ⬜ |
| 2 | Drag your updated `SkillSlot.prefab` (with the `SkillSlotUI` component) into `SkillPanel._skillSlotPrefab` | ⬜ |
| 3 | Confirm `_skillSlotContainer`, `_endTurnButton`, `_actorNameText` are still wired | ⬜ |

---

## 3. `PhaseInteractionPanel_Fusion.prefab` — wire innate-slot components

`_normalAttackSlot` and `_movementSlot` now use typed component references (`NormalAttackSlotUI` / `MovementSlotUI`) instead of raw TMP_Text fields. The 5 old inspector fields are gone; only 2 remain.

### 3a. `_normalAttackSlot` prefab root

| Step | Action | Status |
|---|---|---|
| 1 | Add `NormalAttackSlotUI` MonoBehaviour to `_normalAttackSlot` root | ⬜ |
| 2 | Drag child `TMP_Text` for the name into `NormalAttackSlotUI.NameText` | ⬜ |
| 3 | Drag child `TMP_Text` for damage into `NormalAttackSlotUI.DamageText` | ⬜ |
| 4 | Drag child `TMP_Text` for range into `NormalAttackSlotUI.RangeText` | ⬜ |

Then on the `FusionPanel` component on the panel root:

| Field | Assign | Status |
|---|---|---|
| `_normalAttackSlot` | drag the `_normalAttackSlot` GameObject (now has `NormalAttackSlotUI`) | ⬜ |

### 3b. `_movementSlot` prefab root

| Step | Action | Status |
|---|---|---|
| 1 | Add `MovementSlotUI` MonoBehaviour to `_movementSlot` root | ⬜ |
| 2 | Drag child `TMP_Text` for the name into `MovementSlotUI.NameText` | ⬜ |
| 3 | Drag child `TMP_Text` for range into `MovementSlotUI.RangeText` | ⬜ |

Then on the `FusionPanel` component:

| Field | Assign | Status |
|---|---|---|
| `_movementSlot` | drag the `_movementSlot` GameObject (now has `MovementSlotUI`) | ⬜ |
| `_defaultMoveRange` | Leave 2 unless GDS changes the unit MoveRange default | ⬜ |

> Any field left unwired is silently skipped (null-guarded) — but the panel will look empty if you don't wire them.

---

## 4. Smoke test (after Editor wiring is done)

| Test | Expected | Status |
|---|---|---|
| Enter Play mode (host + client) and start a match | Both panels appear | ⬜ |
| In Main Phase, drag a unit card into base slot | Innate Move / N_Atk slots show name+range+dmg | ⬜ |
| Equip a fuse spell | Fuse slot fills with spell name | ⬜ |
| Enter Combat Phase | Skill panel shows Move, Attack, and any granted skills (innate + equipped) with correct names | ⬜ |
| Click Move skill | Range of `MoveRange` empty tiles highlight | ⬜ |
| Click an empty tile in range | Unit moves; Move slot greys with "Done"; Attack still ready | ⬜ |
| Click Attack | Enemy-in-range tiles highlight | ⬜ |
| Click an enemy unit | Damage applied; Attack greys with "Done" | ⬜ |
| Click any granted skill | Range + display_pattern highlight as configured | ⬜ |
| On a skill with cooldown > 0, after firing | Cooldown text shows `CD: n` and decrements next turn | ⬜ |
| End Turn | Panel becomes non-interactive until your next unit's turn | ⬜ |

---

## 5. Targeting troubleshooting (if clicks don't register)

If clicking a highlighted tile does nothing:

1. **Camera tag**: `TargetingOverlay._mainCamera = Camera.main` — ensure the gameplay camera has tag `MainCamera`.
2. **Tile layer mask**: `TargetingOverlay._tileLayerMask` — must include the layer of `IM_Tile`'s collider. Default is `~0` (everything).
3. **Hex collider**: `IM_Tile.prefab` must have a `Collider` (e.g., MeshCollider) for `Physics.Raycast` to hit.
4. **UI blocking click**: a fullscreen UI canvas on top will eat the click. Check `GraphicRaycaster` / EventSystem hierarchy — the SkillPanel itself should not cover the board.
5. **`TileHighlight.prefab`** must be wired on `TargetingOverlay._tileHighlightPrefab`, otherwise highlights never render and you can't see range.

---

## 6. Reference: code-side fixes summary

- SkillPanel now subscribes to `IUnitSubsystem.OwnUnitSkillsChanged` → cooldown ticks and one-time flags refresh the UI automatically.
- SkillPanel subscribes to `ICombatSubsystem.CurrentActorCanMoveChanged` / `CurrentActorCanActChanged` → Move/Attack readiness flips without waiting for a full turn change.
- `_actorNameText` now resolves via `UnitPublicData.BaseCardId` → `_cardLoading.TryGetCardData()` (previously used the NetworkId as a card id, which always failed).
- Normal attack range now reads `n_atk_pattern` instead of hardcoded 1.
- Move range now reads `UnitPublicData.MoveRange` instead of hardcoded 2.
