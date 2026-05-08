# Implementation Plan — Part 2: Scene Subsystems & Views

---

## 4. ACCOUNT SCENE SUBSYSTEMS (SceneContext on Account scene)

Bound in `AccountInstaller` attached to Account scene's `SceneContext`.

---

### 4.1 AccountLogin Subsystem ✅ EXISTS — NEEDS FIXES

**Model (`AccountLoginModel`):**
```
Observable<string> Email
Observable<string> Password
Observable<string> ErrorMessage      ← 🆕 API error feedback
Observable<bool>   IsSubmitting      ← 🆕 loading state
```

**Controller (`AccountLoginController`):**
```
void SetEmail(string)
void SetPassword(string)
Task Login()                         → calls API using HttpService, stores token in AuthSession, on success: SceneLoader.LoadScene("Lobby"), on fail: sets ErrorMessage
void NavigateToRegister()            → REMOVE from controller. Move to View.
```

**Subsystem events:**
```
event UnityAction<string> ErrorMessageChanged
event UnityAction<bool>   IsSubmittingChanged
```

**View = `AccountLoginPanel` (on prefab `Screen_Account_Login`)**

| Responsibility | Detail |
|----------------|--------|
| Input capture | `_emailInput.onValueChanged` → `_subsystem.SetEmail(val)` |
| Input capture | `_passwordInput.onValueChanged` → `_subsystem.SetPassword(val)` |
| Button action | `_loginButton.onClick` → `_subsystem.Login()` |
| Button action | `_registerButton.onClick` → `_uiManager.CloseView(this); _uiManager.ShowScreen<AccountRegisterPanel>()` (navigation in View, not controller) |
| Event listen | `_subsystem.ErrorMessageChanged` → display error label |
| Event listen | `_subsystem.IsSubmittingChanged` → toggle button interactable / show spinner |

**Editor wiring (prefab `Screen_Account_Login`):**
1. Root GameObject → `AccountLoginPanel` script attached
2. `_emailInput` → drag TMP_InputField for email
3. `_passwordInput` → drag TMP_InputField for password
4. `_loginButton` → drag LOGIN Button
5. `_registerButton` → drag REGISTER Button
6. 🆕 `_errorText` → add TMP_Text child, drag reference
7. Set `_identifier` = `ACCOUNT_LOGIN`, `_layer` = `SCREEN`, `_isModal` = false

---

### 4.2 AccountRegister Subsystem ✅ EXISTS — NEEDS FIXES

**Model (`AccountRegisterModel`):**
```
Observable<string> Email
Observable<string> Password
Observable<string> ConfirmPassword
Observable<string> ErrorMessage      ← 🆕
Observable<bool>   IsSubmitting      ← 🆕
```

**Controller:**
```
void SetEmail(string)
void SetPassword(string)
void SetConfirmPassword(string)
Task Register()                      → validate match, call API using HttpService, on success: SceneLoader.LoadScene("Lobby"), on fail: set ErrorMessage
```

**View = `AccountRegisterPanel` (on prefab `Screen_Account_Register`)**

Same pattern as Login. Navigation back to login handled in View:
- `_backButton.onClick` → `_uiManager.CloseView(this); _uiManager.ShowScreen<AccountLoginPanel>()`

**Editor wiring:** Same pattern. Add `_errorText` field. Wire 3 input fields + 2 buttons.

---

## 5. LOBBY SCENE SUBSYSTEMS (SceneContext on Lobby scene)

Bound in `LobbyInstaller` attached to Lobby scene's `SceneContext`.

---

### 5.1 LobbyMain Subsystem ✅ EXISTS — NEEDS FIXES

**Model (`LobbyMainModel`):**
```
Observable<string> Username          ← 🆕 display
Observable<int>    Level             ← 🆕 display
Observable<int>    Gold              ← 🆕 display
Observable<string> AvatarUrl         ← 🆕 display
```
Populated on scene load by controller calling API via HttpService.

**Controller:**
```
void Initialize()                    → fetch user profile from API, populate model
void NavigateToProfile()             → REMOVE. View handles ShowScreen directly.
void NavigateToBattle()              → REMOVE. View handles.
... (all Navigate* removed from controller)
void Logout()                        → clear AuthSession, LoadScene("Account")
```

> [!IMPORTANT]
> Navigation buttons on LobbyMain are pure UI routing. They don't involve business logic. The View should call `_uiManager.ShowScreen<T>()` directly. Only `Logout()` remains on controller because it has business logic (clear session).

**Subsystem events:**
```
event UnityAction<string> UsernameChanged
event UnityAction<int>    LevelChanged
event UnityAction<int>    GoldChanged
```

**View = `LobbyMainPanel` (on prefab `Screen_Lobby_Main`)**

| Responsibility | Detail |
|----------------|--------|
| Button | `_profileButton` → `_uiManager.ShowScreen<ProfilePanel>()` |
| Button | `_battleButton` → `_uiManager.ShowScreen<BattlePanel>()` |
| Button | `_deckButton` → `_uiManager.ShowScreen<DeckPanel>()` |
| Button | `_shopButton` → `_uiManager.ShowScreen<ShopPanel>()` |
| Button | `_settingsButton` → `_uiManager.ShowScreen<SettingPanel>()` |
| Button | `_logoutButton` → `_lobbyMain.Logout()` |
| Event | `UsernameChanged` → update username text |
| Event | `LevelChanged` → update level text |
| Event | `GoldChanged` → update gold text |

**Editor wiring:** 6 buttons + 3 display texts + avatar image. All `[SerializeField]`.

---

### 5.2 Profile Subsystem ✅ EXISTS — NEEDS EXPANSION

**Model:**
```
Observable<string> Username
Observable<int>    Level
Observable<int>    Xp
Observable<int>    XpToNextLevel
Observable<int>    Gold
Observable<string> AvatarUrl
```

**Controller:**
```
void Initialize()                    → fetch from API or share with LobbyMain model
Task ChangeAvatar(string url)        → call API
```

**View = `ProfilePanel` (on `Overlay_Lobby_Profile`)**

| Responsibility | Detail |
|----------------|--------|
| Display | Listen to all model events → update labels |
| Button | `_matchHistoryButton` → `_uiManager.ShowScreen<MatchHistoryPanel>()` |
| Button | `_changeAvatarButton` → `_profile.ChangeAvatar(...)` |
| Button | `_closeButton` (inherited) → close overlay |

---

### 5.3 BattleSetup Subsystem (rename from "Battle") ✅ EXISTS — NEEDS EXPANSION

**Model:**
```
Observable<bool>   IsOffline
Observable<int>    BotCount
Observable<int>    PlayerCount
Observable<string> ErrorMessage
```

**Controller:**
```
void SetOffline(bool)
void SetBotCount(int)
void SetPlayerCount(int)
Task StartMatchmaking()              → validate settings, transition to MatchMaking UI or start offline match
```

**View = `BattlePanel` (on `Overlay_Lobby_Battle`)**

| Responsibility | Detail |
|----------------|--------|
| Toggle | offline toggle → `_battle.SetOffline(val)` |
| Dropdown | player count → `_battle.SetPlayerCount(val)` |
| Button | add bot → `_battle.SetBotCount(val)` |
| Button | MATCH → `_battle.StartMatchmaking()` |
| Event | `ErrorMessageChanged` → show error |

---

### 5.4 MatchMaking Subsystem 🆕

**Model:**
```
Observable<float>   ElapsedTime
Observable<bool>    IsMatched
Observable<List<MatchMakingPlayerInfo>> Players
Observable<string>  ErrorMessage
```

**Controller:**
```
void Initialize()                    → start searching via Photon or API
void AcceptMatch()                   → confirm, load Gameplay scene
void CancelSearch()                  → abort, return to BattleSetup
void Dispose()                       → cleanup
```

**View = `MatchMakingPanel` (on `Overlay_Lobby_MatchMaking`)**

| Responsibility | Detail |
|----------------|--------|
| Event | `ElapsedTimeChanged` → update timer text |
| Event | `IsMatchedChanged` → enable Accept button, show player info |
| Event | `PlayersChanged` → populate player slots |
| Button | Accept → `_matchMaking.AcceptMatch()` |
| Button | Cancel → `_matchMaking.CancelSearch()` |

**UIIdentifier:** `LOBBY_MATCH_MAKING` (306) — already defined.

---

### 5.5 Deck Subsystem ✅ EXISTS — NEEDS EXPANSION

Split into **DeckList** (overlay) and **DeckBuild** (screen).

**DeckList Model:**
```
Observable<List<DeckInfo>> Decks
Observable<string>         ErrorMessage
```

**DeckList Controller:**
```
void Initialize()                    → fetch decks from API
Task CreateDeck(string name)         → API call
Task DeleteDeck(string deckId)       → API call
void SelectDeck(string deckId)       → navigate to DeckBuild screen with selected deck
```

**DeckList View = `DeckPanel` (on `Overlay_Lobby_Decks`)**

| Responsibility | Detail |
|----------------|--------|
| Event | `DecksChanged` → populate deck list with DeckButton components |
| Button per item | edit → `_deck.SelectDeck(id)` |
| Button | create new → show Form Popup → `_deck.CreateDeck(name)` |

**DeckBuild** — separate subsystem (GameObjectContext on the DeckBuild screen prefab):

**DeckBuild Model:**
```
Observable<DeckInfo>           CurrentDeck
Observable<List<CardCopyInfo>> DeckCards
Observable<List<CardCopyInfo>> AvailableCards
Observable<CardSO>             SelectedChampion
```

**DeckBuild Controller:**
```
void Initialize(string deckId)
void AddCard(string cardCopyId)
void RemoveCard(string cardCopyId)
Task SaveDeck()
```

**DeckBuild View = `DeckBuildPanel` (on `Screen_Lobby_DeckBuild`)**

---

### 5.6 Shop Subsystem ✅ EXISTS — NEEDS EXPANSION

**Model:**
```
Observable<List<ShopItem>>  DailyDeals
Observable<List<ShopItem>>  AllCards
Observable<ShopItem>        FeaturedChampion
Observable<int>             PlayerGold
Observable<string>          ErrorMessage
Observable<string>          SuccessMessage
```

**Controller:**
```
void Initialize()                    → fetch shop data from API
Task PurchaseItem(string itemId)     → API call, update gold
```

**View = `ShopPanel` (on `Screen_Lobby_Shop`)**

---

### 5.7 Setting Subsystem ✅ EXISTS — NEEDS EXPANSION

**Model:**
```
Observable<float> MasterVolume
Observable<float> MusicVolume
Observable<float> SFXVolume
```

**Controller:**
```
void SetMasterVolume(float)          → update model + AudioManager
void SetMusicVolume(float)
void SetSFXVolume(float)
void ApplySettings()                 → persist to PlayerPrefs
```

> [!NOTE]
> Setting reads initial values from `PlayerPrefs` in Initialize(). On Apply, writes back. Also forwards to global AudioManager subsystem for immediate effect.

**View = `SettingPanel` (on `Overlay_Lobby_Settings`)**

| Responsibility | Detail |
|----------------|--------|
| Slider | master → `_setting.SetMasterVolume(val)` |
| Slider | music → `_setting.SetMusicVolume(val)` |
| Slider | sfx → `_setting.SetSFXVolume(val)` |
| Event | `MasterVolumeChanged` → update slider position (for init) |

---

### 5.8 MatchHistory Subsystem ✅ EXISTS — NEEDS EXPANSION

**Model:**
```
Observable<List<MatchHistoryEntry>> Matches
Observable<bool>                    IsLoading
```

**Controller:**
```
void Initialize()                    → fetch from API
Task ReplayMatch(string matchId)     → load replay
```

**View = `MatchHistoryPanel` (on `Overlay_Lobby_MatchHistory`)**

---

### 5.9 CardDetail Subsystem 🆕 (GameObjectContext — scoped to overlay)

**Model:**
```
Observable<CardSO>   Card
Observable<int>      SelectedSkillIndex
Observable<PatternSO> CurrentPattern
Observable<PatternSO> CurrentEffectPattern
```

**Controller:**
```
void SetCard(CardSO card)
void SelectSkill(int index)
```

**View = `CardDetailPanel` (on `Overlay_Lobby_CardDetails`)**

---

## 6. PREFAB → VIEW → SUBSYSTEM MAPPING (Complete)

| Prefab | View Class | Subsystem(s) Injected | UILayer | UIIdentifier |
|--------|-----------|----------------------|---------|-------------|
| `Screen_Account_Login` | `AccountLoginPanel` | IAccountLoginSubsystem, IUIManagerSubsystem | SCREEN | ACCOUNT_LOGIN (201) |
| `Screen_Account_Register` | `AccountRegisterPanel` | IAccountRegisterSubsystem, IUIManagerSubsystem | SCREEN | ACCOUNT_REGISTER (202) |
| `Screen_Lobby_Main` | `LobbyMainPanel` | ILobbyMainSubsystem, IUIManagerSubsystem | SCREEN | LOBBY_MAIN (301) |
| `Overlay_Lobby_Battle` | `BattlePanel` | IBattleSubsystem | OVERLAY | LOBBY_PLAY (302) |
| `Overlay_Lobby_Profile` | `ProfilePanel` | IProfileSubsystem, IUIManagerSubsystem | OVERLAY | LOBBY_PROFILE (303) |
| `Overlay_Lobby_Decks` | `DeckPanel` | IDeckSubsystem | OVERLAY | LOBBY_DECK_BUILD (304) |
| `Overlay_Lobby_MatchHistory` | `MatchHistoryPanel` | IMatchHistorySubsystem | OVERLAY | LOBBY_MATCH_HISTORY (305) |
| `Overlay_Lobby_MatchMaking` | `MatchMakingPanel` 🆕 | IMatchMakingSubsystem 🆕 | OVERLAY | LOBBY_MATCH_MAKING (306) |
| `Screen_Lobby_Shop` | `ShopPanel` | IShopSubsystem | SCREEN | LOBBY_SHOP (307) |
| `Overlay_Lobby_Settings` | `SettingPanel` | ISettingSubsystem | OVERLAY | LOBBY_SETTING (310) |
| `Overlay_Lobby_CardDetails` | `CardDetailPanel` 🆕 | ICardDetailSubsystem 🆕 | OVERLAY | (add new ID) |
| `Screen_Lobby_DeckBuild` | `DeckBuildPanel` 🆕 | IDeckBuildSubsystem 🆕 | SCREEN | DECK_EDITOR (3041) |
| `Popup_Lobby_DeckItemContext` | `DeckItemContextPopup` 🆕 | IDeckSubsystem | POPUP | (add new ID) |
| `Popup_Lobby_ShopItemContext` | `ShopItemContextPopup` 🆕 | IShopSubsystem | POPUP | (add new ID) |
| `Popup_System_Confirmation` | `ConfirmationPopup` 🆕 | (self-contained callback) | POPUP | (add new ID) |
| `Popup_System_SingleTextInput` | `TextInputPopup` 🆕 | (self-contained callback) | POPUP | (add new ID) |

---

*Continued in Part 3...*
