# PrimoraChronicle Unity Client — Execution Task List

## BATCH 1 — Phase A: Foundation Fixes
- [x] A1: Remove `using UnityEditor` from Bootstrapper.cs
- [x] A2: Fix ISceneLoaderController → add `: IController`
- [x] A3: Add `IDisposable` to all controller interfaces; remove redundant declarations from concretes
- [x] A4: Move `RegisterPanel` from `Start()` to `Awake()` with guard in UIPanel.cs
- [x] A5: IController — add `IInitializable, IDisposable` base markers
- **Commit:** `fix: Phase A foundation bug fixes (BUG-3,5,6,7)`

---

## BATCH 2 — Phase B: Global Subsystems (HttpService, AuthSession, AudioManager)
- [ ] B1: Create HttpService subsystem (7 files)
- [ ] B2: Create AuthSession subsystem (7 files)
- [ ] B3: Create AudioManager subsystem (7 files)
- [ ] B4: Update CoreInstaller to bind all three
- [ ] B5: Add new UIIdentifiers for popups to Constants.cs
- **Commit:** `feat: global subsystems — HttpService, AuthSession, AudioManager`

---

## BATCH 3 — Phase C: Account Scene Refactor
- [ ] C1: Refactor AccountLoginModel → Observable fields + ErrorMessage + IsSubmitting
- [ ] C2: Refactor IAccountLoginModel interface
- [ ] C3: Refactor AccountLoginController → remove navigation, add HttpService + AuthSession calls
- [ ] C4: Refactor IAccountLoginController interface
- [ ] C5: Refactor AccountLoginSubsystem → wire error/submitting events
- [ ] C6: Refactor IAccountLoginSubsystem interface
- [ ] C7: Refactor AccountLoginPanel → listen to events, move navigation to View
- [ ] C8: Repeat C1-C7 for AccountRegister
- **Commit:** `feat: Account scene — full Observable model + API integration`

---

## BATCH 4 — Phase D1: Lobby Core (LobbyMain + Profile + BattleSetup)
- [ ] D1: Refactor LobbyMainModel → Observable user info fields
- [ ] D2: Refactor ILobbyMainModel
- [ ] D3: Refactor LobbyMainController → remove Navigate* (keep Logout), add Initialize()
- [ ] D4: Refactor ILobbyMainController
- [ ] D5: Refactor LobbyMainSubsystem → wire events
- [ ] D6: Refactor ILobbyMainSubsystem
- [ ] D7: Refactor LobbyMainPanel → direct UIManager calls, listen to model events
- [ ] D8: Expand ProfileSubsystem (model + controller + view) — full Observable
- [ ] D9: Expand BattleSubsystem → BattleSetupModel full fields + controller
- **Commit:** `feat: Lobby core — LobbyMain, Profile, BattleSetup expanded`

---

## BATCH 5 — Phase D2: Lobby New Subsystems (MatchMaking, DeckBuild, CardDetail, Popups)
- [x] D10: Create MatchMakingSubsystem (7 files)
- [x] D11: Create DeckBuildSubsystem (7 files + DeckBuildInstaller)
- [x] D12: Create CardDetailSubsystem (7 files + CardDetailInstaller)
- [x] D13: Expand DeckSubsystem (DeckList full model/controller)
- [x] D14: Expand ShopSubsystem (full model/controller)
- [x] D15: Expand SettingSubsystem (Observable volumes + PlayerPrefs)
- [x] D16: Expand MatchHistorySubsystem (full model/controller)
- [x] D17: Create popup views: ConfirmationPopup, TextInputPopup, DeckItemContextPopup, ShopItemContextPopup
- [x] D18: Update LobbyInstaller with MatchMaking binding
- **Commit:** `feat: Lobby new subsystems — MatchMaking, DeckBuild, CardDetail, Popups`

---

## BATCH 6 — Phase E: Gameplay Scene Subsystems
- [ ] E1: Create GameplayInstaller MonoInstaller
- [ ] E2: Create GameStateSubsystem (7 files, NetworkBehaviour model)
- [ ] E3: Create HandSubsystem (7 files)
- [ ] E4: Create FusePhaseSubsystem (7 files)
- [ ] E5: Create BoardSubsystem (7 files, NetworkBehaviour model)
- [ ] E6: Create CombatSubsystem (7 files)
- [ ] E7: Create DrawPhaseSubsystem (7 files)
- [ ] E8: Create MatchResultSubsystem (7 files)
- **Commit:** `feat: Gameplay scene subsystems skeleton`

---

## EDITOR WIRING PLAN (separate doc — no code required)
- [ ] Generate step-by-step Unity Editor wiring guide for all prefabs & SOs

---

## Excluded
- feat_deck_edit_adaptation_plan.md → handled by another member. DeckEdit binding already exists in LobbyInstaller — do not remove.
