# Person B — Backend, Lobby UI, Economy & Infrastructure

> **Scope:** ASP.NET Core backend, all Lobby-scene UI features (Shop, Profile, Match History, Deck Management, Matchmaking, Settings), network infrastructure (Photon Fusion session lifecycle), backend deployment, Admin Tool, and Card Detail / UI polish.
> **Codebase root:** `Assets/_Game/Features/Lobby/`, `Assets/_Game/Features/Account/`, `Assets/_Game/Features/System/`, `Assets/_Game/TestBE~/`, and external ASP.NET project.

---

## Current Status (as of 2026-05-08)

### ✅ Already Implemented
| Area | What Exists | Notes |
|---|---|---|
| **Account** | `AccountLoginSubsystem`, `AccountRegisterSubsystem` + full UI prefabs | Login/Register screens functional |
| **Lobby Navigation** | `LobbyMainSubsystem` + `LobbyMainPanel` + prefab | Hub with Battle/Deck/Shop/Profile/Settings buttons |
| **Settings** | `SettingSubsystem` + `SettingPanel` + prefab | Volume sliders wired to `ISettingSubsystem` |
| **Deck List** | `DeckSubsystem` + `DeckPanel` + prefab | Deck listing overlay functional |
| **Deck Build** | `DeckBuildSubsystem` (full Controller/Model/Panel) | Substantial logic — validation, drag-drop, save |
| **Shop** | `ShopSubsystem` (Controller/Model/Panel) | Champion highlight, Daily Deals, All Cards sections |
| **Profile** | `ProfileSubsystem` + `ProfilePanel` + prefab | Player info display |
| **Match History** | `MatchHistorySubsystem` + prefab | History list view |
| **Matchmaking** | `MatchMakingSubsystem` + `MatchMakingPanel` + prefab | Timer, status, Accept/Reject |
| **Popups** | Confirmation, DeckAction, Form, Detail popups | All wired in System feature |
| **Test Backend (Python)** | FastAPI + PostgreSQL + Docker Compose | Seed data, basic endpoints working |
| **Build Scenes** | Bootstrap → Account → Lobby → Gameplay | 4-scene flow in build settings |
| **Core Infrastructure** | MVVM base classes, UIManager, Observable, DI (Zenject), SceneMappingSO, UIMappingSO | Architecture solid |

### ❌ Not Yet Implemented (from Report §6.3, §8)
- **ASP.NET Core production backend** (report §6.3.2)
- **Card Detail UI** (`CardDetailPanel.cs` — placeholder only)
- **Match History → Replay button** integration (US-P03)
- **Shop purchase logic** (actual buy flow with backend)
- **Post-match reward persistence** (backend `POST /api/matches/result`)
- **Admin Tool** for Live Ops (report §8.2)
- **SBA architecture** upgrade for backend (report §8.2 — future)
- **Photon Fusion session lifecycle** management (room creation, join, disconnect handling)
- **Audio integration** across all Lobby UI (from `complete_feature.md` §7)

---

## Execution Plan

### Phase B1: ASP.NET Core Backend (Week 1–3)

> Ref: `Plans~/aspnet_backend_plan.md`, Report §6.3.2

#### B1.1 — Project Scaffolding
- [ ] **Create ASP.NET Core Web API** project (separate repo or folder under project root)
- [ ] **Setup Docker Compose**: 
  - `postgres:18-alpine` on port 5433
  - ASP.NET container on port 8080
  - Health checks + dependency ordering
- [ ] **Configure EFCore** with Npgsql + `ApplicationDbContext`
- [ ] **Create `BaseEntity`** with `Id (Guid)`, `CreatedDateTime`, `UpdatedDateTime`, `IsDeleted`

#### B1.2 — Entity Framework Entities & Migrations
- [ ] **Implement Entities** (matching Report §6.1 ERD exactly):
  - `User` (Username, PasswordHash, XpTotal, Gold)
  - `Card` (StringId — maps to Unity SO string IDs)
  - `CardCopy` (UserId FK, CardId FK)
  - `Deck` (Name, Description, UserId FK)
  - `DeckConsistsOfCardCopy` (join table)
  - `Match` (EndDateTime, ActionLogId FK)
  - `MatchParticipant` (IsWinner, GoldReceived, XpReceived, DeckId, UserId, ChampionCardId, MatchId)
  - `ActionLog` (FileBucketUrl)
  - `ChampionHasCard` (ChampionCardId, CardId — self-referencing Card)
  - `SystemConfig` (all config fields from Report §6.1)
- [ ] **Configure Fluent API** relationships in DbContext
- [ ] **Generate initial migration** and test against PostgreSQL

#### B1.3 — REST API Endpoints
- [ ] **AuthController**:
  - `POST /api/auth/register` — create user, hash password, return JWT
  - `POST /api/auth/login` — validate credentials, return JWT
- [ ] **UserController**:
  - `GET /api/users/me` — return authenticated user profile (XP, Gold, Level computed from `SystemConfig.LevelXpGrowthRate`)
- [ ] **CollectionController**:
  - `GET /api/collection/card-copies` — return user's CardCopies with StringIDs
- [ ] **DeckController**:
  - `GET /api/decks` — user's decks with card lists
  - `POST /api/decks` — create deck (validate ownership of CardCopyIds)
  - `PUT /api/decks/{id}` — update deck
  - `DELETE /api/decks/{id}` — soft delete
- [ ] **MatchController**:
  - `GET /api/matches` — match history with participant details
  - `POST /api/matches/result` — **heavy**: create Match, MatchParticipant, ActionLog, upload log to storage, update User Gold/XP
- [ ] **ConfigController**:
  - `GET /api/config` — return SystemConfig
- [ ] **ShopController** (new, not in TestBE):
  - `GET /api/shop/daily-deals` — return discounted cards for today
  - `POST /api/shop/purchase` — buy a card (validate Gold, create CardCopy)

#### B1.4 — Storage Service for Action Logs
- [ ] **Create `IStorageService` interface** with `UploadAsync(filename, data) → URL`
- [ ] **Implement `LocalFileStorageService`** for development (save to `/static/logs/`)
- [ ] **Future**: Swap to S3/MinIO implementation for production

#### B1.5 — Data Seeding
- [ ] **Seed `SystemConfig`** with default values (from report):
  - `dailyDealDiscountRate: 0.2`, `championCardBasePrice: 500`, `commonCardBasePrice: 100`
  - `levelXpGrowthRate: 1.5`, `startingLevelXp: 100`, `afkPenaltyAmount: 30`
- [ ] **Seed Cards**: insert all Card StringIDs (must sync with Person A's generated SOs)
- [ ] **Seed Users**: 2 test users with 50 CardCopies each
- [ ] **Seed Decks**: 2 decks per user with 20 cards each
- [ ] **Seed Matches**: 5 mock match records with fake ActionLog URLs
- [ ] **Swagger/OpenAPI** configured as default route for development

---

### Phase B2: Unity Client ↔ Backend Integration (Week 3–4)

#### B2.1 — Switch Unity from TestBE to ASP.NET Backend
- [ ] **Update API base URL config** in Unity client to point to ASP.NET backend
- [ ] **Update JWT handling**: ensure token is stored and sent in `Authorization: Bearer` header
- [ ] **Test all existing features** against new backend:
  - Login / Register
  - Deck CRUD (list, create, edit, delete)
  - Shop (fetch deals, purchase cards)
  - Profile (fetch user data)
  - Match History (fetch)

#### B2.2 — Shop Feature Completion
> Ref: Report §7.1 (Shop UI), US-S01

- [ ] **Wire `ShopController.cs`** (Unity side) to real backend endpoints:
  - `GET /api/shop/daily-deals` → populate Daily Deals section
  - `POST /api/shop/purchase` → actual purchase flow with Gold deduction
  - `GET /api/collection/card-copies` → identify already-owned cards
- [ ] **Champion purchase flow**: buy Champion → auto-grant champion cards (via `ChampionHasCard`)
- [ ] **Gold balance UI refresh** after purchase (update LobbyMainPanel header)
- [ ] **Error handling**: insufficient Gold popup, already-owned card message

#### B2.3 — Profile & Match History Enhancement
> Ref: Report §7.1 (Profile, Match History UIs)

- [ ] **Profile panel**: show Level (computed: `floor(xpTotal / growthRate)` per `SystemConfig`)
- [ ] **Profile panel**: "Change Avatar" button (stub/future)
- [ ] **Match History panel**:
  - Color-coded entries (green=win, red=loss)
  - Show opponent names, XP/Gold earned, match duration
  - **Replay button** per entry (download ActionLog JSON from `FileBucketUrl`)

---

### Phase B3: Card Detail & Missing Lobby UI (Week 4–5)

> Ref: Report §7.1 (Card Detail UI), `complete_feature.md` §9

#### B3.1 — Card Detail Panel
- [ ] **Create `CardDetailPanel.cs`** (currently missing — only placeholder in `complete_feature.md`):
  - Left column: card illustration + description/lore
  - Center column: action buttons (Normal Attack, Skill 1, 2, 3)
  - Right column: hex grid pattern visualizer
    - Apply Pattern: show attack range on mini hex grid
    - Effect Pattern: show AoE on mini hex grid
- [ ] **Create prefab** at `Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_CardDetail.prefab`
- [ ] **Wire to DeckBuild**: clicking a card in DeckBuild opens CardDetail overlay
- [ ] **Wire to Shop**: clicking a card in Shop opens CardDetail overlay

#### B3.2 — Deck Management Polish
- [ ] **DeckAction popup**: wire Edit and Delete to real backend calls
- [ ] **Form popup**: wire "Create Deck" name input to `POST /api/decks`
- [ ] **Confirmation popup**: wire delete confirmation to `DELETE /api/decks/{id}`
- [ ] **Deck validation feedback**: show error if deck doesn't have exactly 20 cards or is missing a Champion

---

### Phase B4: Photon Fusion Session Management (Week 5–6)

> Ref: Report §6.3 (Architecture Stage 4), §5.7 (Matchmaking)

#### B4.1 — Network Session Lifecycle
- [ ] **Room creation**: after matchmaking Accept → create Fusion room
- [ ] **Player join**: handle player connection, assign player indices
- [ ] **Disconnect handling**: detect player disconnect, apply AFK penalty from `SystemConfig.AfkPenaltyAmount`
- [ ] **Session cleanup**: when match ends, properly close Fusion session
- [ ] **Scene transition**: Lobby → Gameplay scene load with Fusion runner active

#### B4.2 — Offline/AI Mode
- [ ] **Offline toggle** in BattlePanel: skip Fusion room creation
- [ ] **Local-only `GameStateSubsystem`** without Networked properties
- [ ] **AI player slots**: configure via BattlePanel "Add Bot" button

---

### Phase B5: Audio Integration (Week 6–7)

> Ref: `complete_feature.md` §7

- [ ] **Wire `IAudioManagerSubsystem`** to all UI button events:
  - Login screen: gloomy theme (Music)
  - Lobby: magical ambiance (Music), click SFX on map buttons
  - Matchmaking: drum beat when match found (SFX)
  - Deck Build: card slide/click sounds (SFX)
  - Shop: coin clink on purchase (SFX)
- [ ] **Create `AtmosphereSubsystem`**: dynamic music transitions between scenes
- [ ] **Settings wiring**: ensure `SettingPanel` sliders control `IAudioManagerSubsystem` volumes (Master/Music/SFX) — partially done, verify and fix

---

### Phase B6: Admin Tool (Week 7–8)

> Ref: Report §8.2 (Future work — Admin Tool for Live Ops)

- [ ] **Create Admin API endpoints** (backend):
  - `GET/PUT /api/admin/config` — read/update SystemConfig in real-time
  - `GET /api/admin/users` — list users with stats
  - `POST /api/admin/cards` — add new Card entries
  - `GET /api/admin/matches` — search/filter match records
- [ ] **Simple Admin Dashboard** (web page or Swagger-based):
  - View/edit SystemConfig parameters (prices, XP rates, penalties)
  - View user statistics
  - View match analytics
- [ ] **Auth guard**: Admin endpoints require admin role JWT

---

### Phase B7: Testing & Load Testing (Week 8–9)

> Ref: Report §8.2 (Kiểm thử chịu tải)

- [ ] **Unit tests** for backend Services (xUnit):
  - Auth registration/login
  - Deck CRUD validation
  - Match result processing
  - Gold/XP calculation
- [ ] **Integration tests**: end-to-end API flow with test database
- [ ] **Unity Edit Mode tests** (existing test assembly `AccountFeatures`):
  - Mock backend calls for Lobby subsystems
  - Validate Deck validation logic
- [ ] **Basic load test**: simulate concurrent API requests to identify bottlenecks

---

## Dependencies on Person A
- **Card SO StringIDs**: Person A generates CardSO assets → provides list of StringIDs for backend seeding
- **Match result data format**: Person A defines the ActionLog JSON schema for Command logging
- **Champion card lists**: Person A defines which cards each Champion grants (for `ChampionHasCard` seeding)
- **Gameplay scene readiness**: Person B needs Gameplay scene functional to test full matchmaking → gameplay → results flow

## Sync Points (Both Persons)

| Week | Sync Topic | Action |
|---|---|---|
| Week 2 | **Card ID List** | A exports StringIDs → B seeds backend |
| Week 3 | **Backend API Ready** | B provides live API → A integrates for Start Phase deck fetch |
| Week 5 | **Match Result Flow** | A defines CommandLog JSON schema → B implements `POST /api/matches/result` |
| Week 6 | **Full Loop Test** | Both test: Login → Lobby → Matchmaking → Gameplay → Result → History |
| Week 8 | **Polish & Bug Bash** | Joint debugging, UI polish, load testing |

## Deliverables Checklist
- [ ] ASP.NET Core backend running via Docker with all endpoints
- [ ] All Lobby UI features fully functional against real backend
- [ ] Card Detail panel with pattern visualization
- [ ] Shop with real purchase flow
- [ ] Match History with replay button integration
- [ ] Audio wired to all Lobby interactions
- [ ] Admin Tool with SystemConfig management
- [ ] Unit + integration test suite for backend
