# API Implementation Plan

---

## 1. System Database ERD

Based on the system analysis documentation (`6. PhanTichThietKeHeThong.tex`), the database schema revolves around Users, their Card collection (CardCopy), Decks, Match records, and System configurations.

```mermaid
erDiagram
    User {
        guid ID PK
        string username
        string passwordHash
        int xpTotal
        int gold
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    Card {
        guid ID PK
        string StringID
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    CardCopy {
        guid ID PK
        guid userID FK
        guid cardID FK
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    Deck {
        guid ID PK
        string name
        string description
        guid userID FK
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    DeckConsistsOfCardCopy {
        guid deckID FK
        guid cardCopyID FK
    }
    Match {
        guid ID PK
        datetime endDateTime
        guid actionLogID FK
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    MatchParticipant {
        guid ID PK
        bool isWinner
        int goldReceived
        int xpReceived
        guid deckID FK
        guid userID FK
        guid championCardID FK
        guid matchID FK
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    ActionLog {
        guid ID PK
        string fileBucketURL
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }
    ChampionHasCard {
        guid championCardID FK
        guid cardID FK
    }
    SystemConfig {
        guid ID PK
        float dailyDealDiscountRate
        int championCardBasePrice
        int commonCardBasePrice
        float levelXpGrowthRate
        int startingLevelXp
        int afkPenaltyAmount
        datetime createdDateTime
        datetime updatedDateTime
        bool isDeleted
    }

    User ||--o{ CardCopy : "owns"
    Card ||--o{ CardCopy : "of"
    User ||--o{ Deck : "owns"
    Deck ||--o{ DeckConsistsOfCardCopy : "consists of"
    CardCopy ||--o{ DeckConsistsOfCardCopy : "included in"
    User ||--o{ MatchParticipant : "plays as"
    Match ||--o{ MatchParticipant : "has"
    Deck ||--o{ MatchParticipant : "uses"
    CardCopy ||--o{ MatchParticipant : "chooses champion"
    Match ||--o| ActionLog : "records"
    Card ||--o{ ChampionHasCard : "grants (Champion)"
    Card ||--o{ ChampionHasCard : "is granted by"
```

> [!NOTE]
> `Card` table is intentionally simplified on the backend. All heavy gameplay attributes (HP, Damage, Patterns) are stored purely on the Unity Client via `ScriptableObject`. The backend only uses `Card` `StringID`s for economy and deck validation. `ID` is an auto-generated GUID for database referential integrity.

---

## 2. API Endpoints Specification

APIs will follow standard RESTful design, communicating via JSON. Authentication is required for all endpoints except `/auth`.

### 2.1 Authentication & User
Handles user registration, login, and fetching basic profile stats (Gold, XP).

* `POST /api/auth/register`
  * **Desc:** Creates a new user with minimum starting resources.
  * **Body:** `{"username": "player1", "password": "securepassword"}`
  * **Returns:** `{"token": "JWT_TOKEN", "user": {...}}`
* `POST /api/auth/login`
  * **Desc:** Authenticates a user.
  * **Body:** `{"username": "player1", "password": "securepassword"}`
  * **Returns:** `{"token": "JWT_TOKEN", "user": {...}}`
* `GET /api/users/me`
  * **Desc:** Gets current authenticated user's details (XP, Gold).
  * **Returns:** `{"ID": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "username": "player1", "xpTotal": 500, "gold": 1200}`

### 2.2 Collections (Cards)
Queries the player's card collection.

* `GET /api/collection/card-copies`
  * **Desc:** Gets all card copies owned by the user.
  * **Returns:** `[{"ID": "123e4567-e89b-12d3-a456-426614174000", "cardID": "987e6543-e21b-12d3-a456-426614174111", "StringID": "Lich", "createdDateTime": "..."}, ...]`

### 2.3 Decks
CRUD operations for building and managing decks.

* `GET /api/decks`
  * **Desc:** Gets all decks created by the user, including the array of `cardCopyID`s inside them.
  * **Returns:** `[{"ID": "d1234567-e89b-12d3-a456-426614174000", "name": "Aggro Undead", "description": "...", "championCardID": "987e6543-e21b-12d3-a456-426614174111", "cardCopyIDs": ["123e4567-e89b-12d3-a456-426614174000", "..."]}]`
* `POST /api/decks`
  * **Desc:** Creates a new deck.
  * **Body:** `{"name": "New Deck", "description": "...", "championCardID": "987e6543-e21b-12d3-a456-426614174111", "cardCopyIDs": ["123e4567-e89b-12d3-a456-426614174000", "..."]}`
* `PUT /api/decks/{id}`
  * **Desc:** Updates an existing deck (name, cards). Validates that the user owns the `cardCopyIDs`.
* `DELETE /api/decks/{id}`
  * **Desc:** Deletes a deck (does NOT delete the cards).

### 2.4 Matches & History
Operations handling match results and replay logs.

* `GET /api/matches`
  * **Desc:** Retrieves the match history for the current user.
  * **Returns:** `[{"matchID": "m1234567-e89b-12d3-a456-426614174000", "endDateTime": "...", "isWinner": true, "goldReceived": 50, "xpReceived": 100, "actionLogURL": "/static/logs/m-1.json"}]`
* `POST /api/matches/result` (Heavy Operation)
  * **Desc:** Submits the result of a match from the dedicated game server or authority client. This endpoint calculates XP/Gold gains based on `SystemConfig`, updates the `User` table, and creates `Match` and `MatchParticipant` records. **CRITICAL:** Creates an `ActionLog` and delegates the storage of the JSON log data to a dedicated Bucket File Storage service (e.g., AWS S3, MinIO, or a custom blob storage abstraction). The ASP.NET BE Agent must design, orchestrate, and implement a storage strategy (e.g., via an `IStorageService` interface) to handle uploading these files and generating the accessible `FileBucketUrl`. (Note: The Python TestBE simply mocks this by writing to a local static `logs/` directory).
  * **Body:** `{"winnerUserID": "3fa85f64...", "loserUserID": "3fa85f65...", "winnerDeckID": "d1234567...", "loserDeckID": "d1234568...", "actionLogData": {...}}`

### 2.5 System Config
* `GET /api/config`
  * **Desc:** Returns the global system configuration for client calculations.
  * **Returns:** `{"dailyDealDiscountRate": 0.2, "levelXpGrowthRate": 1.5, ...}`

---

## 3. Python Test Backend Architecture (Part 2 Plan)

If approved, Part 2 will construct a Python backend inside `Assets/TestBE` using:
1. **FastAPI**: For high-performance, easy-to-read REST endpoints.
2. **PostgreSQL**: For the relational database, running via Docker.
3. **SQLAlchemy ORM**: To map Python classes to the Postgres ERD above.
4. **Docker Compose**: To orchestrate the FastAPI app and Postgres DB with one click.
5. **Mock Data Seeding**: On startup, a script will populate the DB so every user has at least 2 decks, 50 card copies, and 10 match histories. External bucket files will be mocked via static file hosting returning `.json` log files.

### Action Plan for Part 2:
- Write `docker-compose.yml`, `requirements.txt`, and `.env`.
- Write `models.py` matching the ERD.
- Write `main.py` with FastAPI endpoints.
- Write `seed.py` for DB mockup data.
- Write `.bat` files for `run-compose.bat` and `stop-compose.bat`.
