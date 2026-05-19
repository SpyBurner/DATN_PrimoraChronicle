# Gameplay Deck-Choose Implementation Plan

Tracks the StartPhase deck-choosing feature that reuses `IDeckSubsystem`
and follows the networked-subsystem-guideline architecture.

---

## Subsystem Stack

### Interfaces
- [x] `GameplayDeckChooseStateData` struct
- [x] `IGameplayDeckChooseSubsystem` — public facade (intent + bridge + sync)
- [x] `IGameplayDeckChooseController` — internal
- [x] `IGameplayDeckChooseNetworkBridge` — public seam interface
- [x] `IGameplayDeckChooseModel` — internal

### Implementation
- [x] `GameplayDeckChooseModel` — observables + ApplyState only
- [x] `GameplayDeckChooseController` — staged input, nullable bridge, HTTP deck-detail fetch
- [x] `GameplayDeckChooseSubsystem` — facade, wires observables → events
- [x] `GameplayDeckChooseNetworkView` — NetworkBehaviour, RPCs, Render → PushState
- [x] `GameplayDeckChoosePanel` — MonoBehaviour view, reuses IDeckSubsystem + DeckButton

---

## Remaining Work

- [x] **Move interfaces to Core.Interfaces** — all 5 interface/data files are now in
      `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`

- [x] **DI Installer**: bind IGameplayDeckChooseModel, IGameplayDeckChooseController,
      IGameplayDeckChooseSubsystem as AsSingle in the Gameplay SceneContext installer
- [x] **Assembly reference**: `GameplayFeatures.asmdef` now references LobbyFeatures GUID
      `5fd32f5c21e5e4c4a94a512f561f79e7`
- [x] **Spawn trigger**: `NetworkSpawner.SpawnPlayerPiece` spawns `deckChooseViewPrefab`
      per player with inputAuthority = that player; removed premature `SetupDeck` call
- [x] **AutoConfirmDecks** integration: `NetworkGameplayManager.AutoConfirmDecks()` now finds
      each player's `GameplayDeckChooseNetworkView` and calls `ServerAutoConfirm(playerIndex)`
- [ ] **Prefab wiring** *(hand-wire in Unity Editor)*: open `PhaseInteractionPanel_DeckChoose.prefab`,
      add `GameplayDeckChoosePanel` component to root, wire:
        - `_deckListContainer` → `Panel (1)` child transform
        - `_deckButtonPrefab` → `Assets/_Game/Features/Lobby/UI/Component/DeckButton.prefab`
        - `_confirmButton` → `Button_Confirm` child (has Button component)
      Then delete the static `DeckButton` instance already embedded in `Panel (1)`.
- [ ] **NetworkView prefab** *(hand-wire in Unity Editor)*: create a NetworkObject prefab for
      `GameplayDeckChooseNetworkView`:
        1. Create empty GameObject → add `NetworkObject` component
        2. Add `GameObjectContext` + empty `MonoInstaller` (for Zenject injection)
        3. Add `GameplayDeckChooseNetworkView` script
        4. Register in Fusion `NetworkObjectPrefabTable` (Project Settings → Fusion)
        5. Assign GUID to `NetworkSpawner.deckChooseViewPrefab` field in the scene
- [ ] **Phase-aware panel show/hide** *(hand-wire)*: the panel already hides itself on
      `IsReady = true`. For *showing* at StartPhase start, add a phase-listener MonoBehaviour
      that subscribes to `NetworkGameplayManager.CurrentPhase` changes and
      `SetActive(true/false)` on the panel based on `CurrentPhase == StartPhase`.

---

## Architecture Reference

```
GameplayDeckChoosePanel
    ↓ IDeckSubsystem          (loads deck list — reused from Lobby)
    ↓ IGameplayDeckChooseSubsystem (stages selection, confirms)
        ↓ IGameplayDeckChooseController
            ↓ IGameplayDeckChooseModel (Observable<bool> IsReady)
            ↓ IGameplayDeckChooseNetworkBridge (nullable)
                ↓ GameplayDeckChooseNetworkView : NetworkBehaviour
                    [Rpc] Rpc_ConfirmDeckSelection(championId, cardIdsJoined, playerIndex)
                    → NetworkPlayerState.SetupDeck()   (server-only)
                    → [Networked] IsReady = true
                    Render() → PushState() → subsystem.OnAuthoritativeStateReceived()
```
