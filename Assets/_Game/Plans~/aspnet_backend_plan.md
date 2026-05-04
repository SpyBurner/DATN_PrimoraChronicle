# ASP.NET Core Backend Implementation Plan

This document outlines the blueprint for constructing the official Primora Chronicle Backend using **ASP.NET Core Web API**, **Entity Framework Core (EFCore)**, and **PostgreSQL**.

## 1. Technology Stack
* **Framework:** ASP.NET Core 8.0 (or latest).
* **ORM:** Entity Framework Core (EFCore) with Npgsql.
* **Database:** `postgres:18-alpine` running via Docker Compose (Port `5433:5432`).
* **API Documentation:** Swagger/OpenAPI (set as default route `/`).

## 2. Database Entities & EFCore Mapping
The backend must define the following C# entity classes. All entities inherit from a `BaseEntity` containing `Id` (of type `Guid`, configured to auto-generate on add), `CreatedDateTime`, `UpdatedDateTime`, and `IsDeleted`.

### 2.1 Entities
* **User:** `Username`, `PasswordHash`, `XpTotal`, `Gold`.
* **Card:** `StringId` (string, serves as the human-readable card identifier from Unity SOs, while `Id` remains an auto-generated GUID).
* **CardCopy:** Foreign keys to `UserId` and `CardId`.
* **Deck:** `Name`, `Description`, `UserId`.
* **Match:** `EndDateTime`, `ActionLogId`.
* **MatchParticipant:** `IsWinner`, `GoldReceived`, `XpReceived`, `DeckId`, `UserId`, `ChampionCardId`, `MatchId`.
* **ActionLog:** `FileBucketUrl` (URL pointing to the externally stored JSON payload in a bucket).
* **SystemConfig:** `DailyDealDiscountRate`, `ChampionCardBasePrice`, `CommonCardBasePrice`, `LevelXpGrowthRate`, `StartingLevelXp`, `AfkPenaltyAmount`.

### 2.2 Entity Framework Core (DbContext) Configuration
Use Fluent API in `ApplicationDbContext` to configure relationships and key generation:
* **Key Generation:** Explicitly configure `HasKey(e => e.Id)` and `ValueGeneratedOnAdd()` for all entities inherited from `BaseEntity`.
* `User` 1:N `CardCopy`
* `User` 1:N `Deck`
* `Deck` N:M `CardCopy` (Implicit or explicit join table `DeckConsistsOfCardCopy`).
* `Match` 1:N `MatchParticipant`
* `User` 1:N `MatchParticipant`
* `Match` 1:1 `ActionLog`
* `Card` N:M `Card` (Join table `ChampionHasCard`).

## 3. API Endpoints (Controllers)
Implement the following RESTful controllers returning JSON. Use DTOs (Data Transfer Objects) for requests and responses to avoid exposing EFCore entities directly.

### 3.1 AuthController
* `POST /api/auth/register`: Create a new User. Ensure username is unique.
* `POST /api/auth/login`: Validate credentials and return a mock/JWT token.

### 3.2 UserController
* `GET /api/users/me`: Return `XpTotal` and `Gold` for the authenticated user.

### 3.3 CollectionController
* `GET /api/collection/card-copies`: Return list of `CardCopy` DTOs owned by the user.

### 3.4 DeckController
* `GET /api/decks`: Return user's decks, including a list of associated `CardCopyId`s.
* `POST /api/decks`: Create a new deck. **Constraint:** Validate that all requested `CardCopyId`s belong to the user.
* `PUT /api/decks/{id}`: Update deck name/description and replace the associated cards.
* `DELETE /api/decks/{id}`: Delete a deck (do not delete the card copies).

### 3.5 MatchController
* `GET /api/matches`: Return the user's match history.
* `POST /api/matches/result`: Heavy operation. 
  * Accepts Match Result DTO (winner, loser, decks, action log data).
  * Creates an `ActionLog` and delegates the storage of the JSON log data to a dedicated Bucket File Storage service (e.g., AWS S3, MinIO, or a custom blob storage abstraction). The BE Agent must design, orchestrate, and implement a storage strategy (e.g., via an `IStorageService` interface) to handle uploading these files and generating the accessible `FileBucketUrl`.
  * Creates the `Match` and `MatchParticipant` records.
  * Updates `Gold` and `XpTotal` for both Users based on `SystemConfig` logic.

### 3.6 ConfigController
* `GET /api/config`: Return the `SystemConfig` entity.

## 4. Seeding & Initialization
Implement an EFCore Data Seeder (e.g., inside `Program.cs` or an `IHostedService`):
1. **Apply Migrations:** Auto-apply `context.Database.Migrate()`.
2. **Seed SystemConfig:** Insert default values.
3. **Seed Cards:** Insert basic Card IDs (e.g., "Lich", "Reject", "card-1" to "card-47").
4. **Seed Users & Economy:** Create 2 test users. Assign 50 random `CardCopy`s to each.
5. **Seed Decks:** Create 2 decks per user and assign 20 of their card copies to each deck.
6. **Seed Matches:** Generate 5 mock matches between the 2 users to populate history.

## 5. Docker & Deployment Configuration
Update `docker-compose.yml` to orchestrate the ASP.NET Core container and PostgreSQL.
* **Database Service:** `postgres:18-alpine` running on port `5433` (`command: -p 5433`), healthcheck enabled.
* **API Service:** Build the .NET application via a multi-stage Dockerfile, expose on port `8080` (or `8000`), depend on the DB healthcheck, and pass the connection string via Environment Variables.
