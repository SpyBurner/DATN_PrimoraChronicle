# Architecture Compliance Fix Plan

Rules reference: [implementation_plan_p1.md](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Plans~/implementation_plan_p1.md) §2 (R1–R10)

---

## Category A — IModel Missing Setters (R4 Violation)

IModel interface only exposes Observable getters. Setter methods exist only on concrete `internal class`, invisible through the interface. Controllers inject `IModel` → **cannot call setters at all across assembly boundaries**.

> [!CAUTION]
> This is the most critical systemic issue. Every subsystem where Model and Controller are in different assemblies will fail at runtime.

### A1. `ILobbyMainModel` — missing setters

| Problem | Model has `internal void SetUsername/SetLevel/SetGold/SetAvatarUrl` — not on interface |
|---------|---|
| Cause | Setters added to concrete class only, interface left read-only |
| Fix | Add `void SetUsername(string)`, `SetLevel(int)`, `SetGold(int)`, `SetAvatarUrl(string)` to `ILobbyMainModel`. Change `internal` → `public` on `LobbyMainModel` setters |

**File:** [ILobbyMainModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/LobbyMain/ILobbyMainModel.cs)
**File:** [LobbyMainModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/LobbyMain/LobbyMainModel.cs)

---

### A2. `IBattleModel` — missing setters

| Problem | Model has `internal void Set{OpponentName,OpponentLevel,PlayerHP,OpponentHP,PlayerMaxHP,OpponentMaxHP,IsReady}` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add all 7 setters to `IBattleModel`. Change `internal` → `public` on `BattleModel` |

**File:** [IBattleModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Battle/IBattleModel.cs)
**File:** [BattleModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Battle/BattleModel.cs)

---

### A3. `IShopModel` — missing setters

| Problem | `internal void SetItems(...)`, `internal void SetUserGold(...)` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add `void SetItems(List<ShopItemData>)`, `void SetUserGold(int)` to `IShopModel`. Change `internal` → `public` |

**File:** [IShopModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Shop/IShopModel.cs)
**File:** [ShopModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Shop/ShopModel.cs)

---

### A4. `ISettingModel` — missing setters

| Problem | `internal void SetMasterVolume/SetMusicVolume/SetSFXVolume` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add 3 setters to `ISettingModel`. Change `internal` → `public` |

**File:** [ISettingModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Setting/ISettingModel.cs)
**File:** [SettingModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Setting/SettingModel.cs)

---

### A5. `IMatchHistoryModel` — missing setter

| Problem | `internal void SetMatchHistory(...)` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add `void SetMatchHistory(List<MatchHistoryData>)` to `IMatchHistoryModel`. Change `internal` → `public` |

**File:** [IMatchHistoryModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/MatchHistory/IMatchHistoryModel.cs)
**File:** [MatchHistoryModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/MatchHistory/MatchHistoryModel.cs)

---

### A6. `IMatchMakingModel` — missing setters

| Problem | `internal void SetIsSearching/SetStatus/SetQueuePosition` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add 3 setters to `IMatchMakingModel`. Change `internal` → `public` |

**File:** [IMatchMakingModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/MatchMaking/IMatchMakingModel.cs)
**File:** [MatchMakingModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/MatchMaking/MatchMakingModel.cs)

---

### A7. `IDeckBuildModel` — missing setters

| Problem | `internal void SetDeckCards/AddCard/RemoveCard` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add `void SetDeckCards(List<string>)`, `void AddCard(string)`, `void RemoveCard(string)` to `IDeckBuildModel`. Change `internal` → `public` |

**File:** [IDeckBuildModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/DeckBuild/IDeckBuildModel.cs)
**File:** [DeckBuildModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/DeckBuild/DeckBuildModel.cs)

---

### A8. `ICardDetailModel` — missing setters

| Problem | `internal void SetCardName/SetCardDescription/SetCardCost/SetCardPower/SetCardImageUrl` — not on interface |
|---------|---|
| Cause | Same as A1 |
| Fix | Add 5 setters to `ICardDetailModel`. Change `internal` → `public` |

**File:** [ICardDetailModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/CardDetail/ICardDetailModel.cs)
**File:** [CardDetailModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/CardDetail/CardDetailModel.cs)

---

### A9. Core models — `IAudioManagerModel`, `IAuthSessionModel` — missing setters

| Problem | `internal void Set*` methods on `AudioManagerModel` and `AuthSessionModel` — not on interfaces |
|---------|---|
| Cause | Same pattern |
| Fix | Add setters to both IModel interfaces. Change `internal` → `public` |

**Files in:** `Core/Scripts/UI/SubSystem/AudioManager/` and `Core/Scripts/UI/SubSystem/AuthSession/`

---

## Category B — Bogus Using Directives (Build-Breaking)

### B1. `IAccountLoginModel.cs` — `using Codice.CM.Common;`

| Problem | Editor-only Plastic SCM namespace in runtime code |
|---------|---|
| Cause | IDE auto-import artifact |
| Fix | Remove the `using Codice.CM.Common;` line |

**File:** [IAccountLoginModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Account/Scripts/Login/IAccountLoginModel.cs)

---

### B2. `IAccountRegisterModel.cs` — `using Codice.CM.Common;`

| Problem | Same as B1 |
| Fix | Remove the line |

**File:** [IAccountRegisterModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Account/Scripts/Register/IAccountRegisterModel.cs)

---

### B3. `IProfileModel.cs` — `using log4net.Core;`

| Problem | log4net is an editor/test dependency, not available in runtime builds |
|---------|---|
| Cause | IDE auto-import artifact |
| Fix | Remove the `using log4net.Core;` line |

**File:** [IProfileModel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/IProfileModel.cs)

---

## Category C — Redundant `IInitializable, IDisposable` on Concrete Classes

### C1. All Subsystem concrete classes declare `IInitializable, IDisposable` redundantly

| Problem | `ISubsystem` already extends `IInitializable, IDisposable`. Concrete classes re-declare both |
|---------|---|
| Cause | Defensive coding / copy-paste |
| Fix | Remove `, IInitializable, IDisposable` from all concrete subsystem class declarations |

**Affected (all):**
- `AccountLoginSubsystem`, `AccountRegisterSubsystem`
- `LobbyMainSubsystem`, `ProfileSubsystem`, `BattleSubsystem`
- `DeckSubsystem`, `DeckEditSubsystem`, `DeckBuildSubsystem`
- `ShopSubsystem`, `SettingSubsystem`, `MatchHistorySubsystem`
- `MatchMakingSubsystem`, `CardDetailSubsystem`
- `SceneLoaderSubsystem`, `HttpServiceSubsystem`
- `AudioManagerSubsystem`, `AuthSessionSubsystem`
- `GameStateSubsystem`

> [!NOTE]
> Functionally harmless — Zenject resolves inherited interfaces. But violates DRY and masks intent. Low priority.

---

## Category D — Navigation in Subsystem (R2 Violation)

### D1. `ProfilePanel` calls `_profile.NavigateToMatchHistory()`

| Problem | Navigation routed through subsystem. R2: View handles pure UI routing directly via UIManager |
|---------|---|
| Cause | Navigation method placed on subsystem instead of View |
| Fix | Replace `_profile.NavigateToMatchHistory()` with `_uiManager.ShowScreen<MatchHistoryPanel>()`. Inject `IUIManagerSubsystem` into `ProfilePanel`. Remove `NavigateToMatchHistory()` from `IProfileSubsystem` and `ProfileSubsystem` |

**File:** [ProfilePanel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/ProfilePanel.cs)
**File:** [IProfileSubsystem.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/IProfileSubsystem.cs)
**File:** [ProfileSubsystem.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/ProfileSubsystem.cs)

---

### D2. `BattlePanel` calls `_battle.StartMatchmaking()`

| Problem | `IBattleSubsystem` does not declare `StartMatchmaking()`. `BattlePanel` calls a method that doesn't exist on the injected interface → compile error |
|---------|---|
| Cause | Method defined on `IMatchMakingSubsystem`, not `IBattleSubsystem`. Panel injects wrong subsystem |
| Fix | Inject `IMatchMakingSubsystem` into `BattlePanel`, call `_matchMaking.StartMatchmaking()`. Or if BattlePanel should navigate to MatchMaking overlay, use `_uiManager.ShowScreen<MatchMakingPanel>()` instead |

**File:** [BattlePanel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Battle/BattlePanel.cs)

---

## Category E — Sync-Over-Async / Manual Controller Init (Deadlock Risk)

### E1. `ProfileController.Initialize()` — `.Result` deadlock

| Problem | `_httpService.Get<>(...).Result` blocks the main thread synchronously |
|---------|---|
| Cause | `Initialize()` is `void` (from `IInitializable`), cannot be `async` |
| Fix | Change to `async void Initialize()` with `await`, or fire-and-forget via helper |

**File:** [ProfileController.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/ProfileController.cs#L18)

---

### E2. `ProfileSubsystem.Initialize()` — calls `_controller.Initialize().ConfigureAwait(false)`

| Problem | `IController.Initialize()` returns `void`, not `Task`. This line shouldn't compile unless `ProfileController.Initialize()` was changed to return Task (which would break `IInitializable`) |
|---------|---|
| Cause | Mismatch between interface contract (`void`) and actual usage (Task) |
| Fix | Use same pattern as `LobbyMainController` — `async void Initialize()`. Remove `.ConfigureAwait(false)` from subsystem. Let Zenject call `Initialize()` naturally |

**File:** [ProfileSubsystem.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/ProfileSubsystem.cs#L39)

---

### E3. `LobbyMainSubsystem.Initialize()` — manually calls `_controller.Initialize()`

| Problem | Subsystem manually invokes `_controller.Initialize()`. If Zenject also binds controller as `IInitializable`, Initialize fires twice |
|---------|---|
| Cause | Subsystem assumed controller init won't be auto-triggered |
| Fix | Remove manual `_controller.Initialize()` call. Zenject's `BindInterfacesAndSelfTo` handles it. If execution order matters, use Zenject's `ExecutionOrder` |

**File:** [LobbyMainSubsystem.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/LobbyMain/LobbyMainSubsystem.cs#L31)

---

## Category F — Cross-Subsystem Coupling (Architecture Smell)

### F1. `ShopController` calls `_lobbyMain?.Initialize()`

| Problem | ShopController injects `ILobbyMainSubsystem` and calls `Initialize()` to refresh gold display after purchase. Re-initializes entire LobbyMain subsystem |
|---------|---|
| Cause | No shared gold source. Hack to propagate gold change |
| Fix | Either: (a) emit event from ShopController after purchase → LobbyMain observes it, or (b) share gold through `AuthSessionModel` global state. Remove `_lobbyMain` injection from `ShopController` |

**File:** [ShopController.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Shop/ShopController.cs#L48)

---

## Category G — ProfilePanel Missing Event Listeners

### G1. `ProfilePanel` doesn't listen to any model events

| Problem | Profile has 6 observable fields (Username, Level, Xp, XpToNextLevel, Gold, AvatarUrl) and the subsystem exposes 6 events. ProfilePanel subscribes to none of them — displays nothing |
|---------|---|
| Cause | Skeleton view never expanded |
| Fix | Subscribe to all 6 `IProfileSubsystem` events in `OnEnable/OnDisable`. Add `[SerializeField]` fields for TextMeshProUGUI elements |

**File:** [ProfilePanel.cs](file:///d:/UnityProjects/DATN_PrimoraChronicle/Assets/_Game/Features/Lobby/Scripts/Profile/ProfilePanel.cs)

---

## Execution Order

| Priority | Items | Risk |
|----------|-------|------|
| **P0 — Critical** | A1–A9 (IModel setters), B1–B3 (bad usings) | Compile errors / runtime failures |
| **P1 — High** | E1–E3 (async deadlock), D1–D2 (wrong method calls), F1 (hacky init) | Deadlocks, missing methods |
| **P2 — Medium** | G1 (ProfilePanel empty) | Non-functional UI |
| **P3 — Low** | C1 (redundant interfaces) | Code hygiene |

---

## Verification

1. Open Unity → Console → 0 compile errors
2. Enter Play → Account scene → Login → Lobby → click Profile/Battle/Settings → no NullRef
3. `grep -r "internal void Set" Features/ Core/` → 0 results
4. `grep -r "using Codice\|using log4net" Features/` → 0 results
