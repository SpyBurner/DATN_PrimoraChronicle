# Primora Chronicle Gameplay Wiring Finishing Plan

This document lists all prefabs, scriptable objects, and scene setups that require manual wiring in the Unity Editor to fully integrate the pure Photon Fusion gameplay phases.

## 1. Prefab Wiring & References

### NetworkSpawner Prefab
**Asset Path:** `Assets/_Game/Features/Gameplay/Prefabs/NetworkedPrefabs/NetworkSpawner - WorldAnchor.prefab`
- **Fields to Configure:**
  - `Player Piece Prefab`: Assign the default player prefab `SM_Hood.prefab`.
  - `Player 1 Piece Prefab`: Assign `SM_Hood Variant.prefab`.
  - `Player 2 Piece Prefab`: Assign `SM_Hood Variant 1.prefab`.
  - `Hex Tile Prefab`: Assign `IM_Tile.prefab`.
  - `Board Prefab`: Optional networked board parent prefab (can remain unassigned to let the spawner create a dynamic board container).

### NetworkUnit Prefabs
**Asset Paths:**
- `Assets/_Game/Features/Gameplay/Prefabs/NetworkedPrefabs/SM_Hood.prefab`
- `Assets/_Game/Features/Gameplay/Prefabs/NetworkedPrefabs/SM_Hood Variant.prefab`
- `Assets/_Game/Features/Gameplay/Prefabs/NetworkedPrefabs/SM_Hood Variant 1.prefab`
- **Fields to Configure on base `SM_Hood.prefab` (automatically inherited by variants):**
  - `Seedling Prefab`: Assign the Seedling prefab (Verdant dominion evolution chain).
  - `Sapling Prefab`: Assign the Sapling prefab.
  - `Young Treant Prefab`: Assign the Young Treant prefab.
  - `Thorn Colossus Prefab`: Assign the Thorn Colossus prefab.

## 2. Scriptable Object Setup (GDS Alignment)

To bind skills and main phase spells to their opaque logic execution paths:
- Create Scriptable Objects representing the specific **Skill Behaviors**, **Status Effect Behaviors**, and **Main Phase Spell Behaviors** mapping to the opaque ID strings specified in `primora-rulebook.md` (e.g., `skill_summon_seedling` mapping to the seedling summoning SO behavior).
- Ensure all Card Scriptable Objects (`CardSO`) are configured with appropriate:
  - `String ID` matched to their GDS specification.
  - Champion reference properties including the `grants_cards` references for matched deck generation.

## 3. Scene Setup

- Place the `NetworkGameplayManager` script onto a new GameObject in the main Gameplay scene.
- Ensure the scene has a valid **CinemachineBrain** and main cameras properly tracking the board centers.
