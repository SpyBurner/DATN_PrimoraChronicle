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

- [ ] **DI Installer**: bind IGameplayDeckChooseModel, IGameplayDeckChooseController,
      IGameplayDeckChooseSubsystem as AsSingle in the Gameplay SceneContext installer
- [ ] **Prefab wiring**: open `PhaseInteractionPanel_DeckChoose.prefab`, add
      `GameplayDeckChoosePanel` component, wire `_deckListContainer`, `_deckButtonPrefab`,
      `_confirmButton` SerializeFields
- [ ] **NetworkView prefab**: create a NetworkObject prefab for `GameplayDeckChooseNetworkView`,
      add GameObjectContext + MonoInstaller (empty), register in NetworkViewRegistry
- [ ] **Spawn trigger**: in NetworkSpawnCoordinator (or NetworkGameplayManager.StartMatch),
      spawn one GameplayDeckChooseNetworkView per player at StartPhase start
- [ ] **AutoConfirmDecks** integration: replace hardcoded defaults in
      `NetworkGameplayManager.AutoConfirmDecks()` with a call to
      `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` via the bridge
- [ ] **Assembly reference**: verify `GameplayFeatures.asmdef` references `LobbyFeatures.asmdef`
      (needed for DeckButton, DeckSummaryData, IDeckSubsystem)
- [ ] **Phase-aware panel show/hide**: show `GameplayDeckChoosePanel` when
      `CurrentPhase == StartPhase`; hide when `IsReady == true`

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
