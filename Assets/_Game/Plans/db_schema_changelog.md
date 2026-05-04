# Database Schema Changelog

This document records major changes and architectural decisions regarding the database schema design across the Unity Client, API Backend, and Database implementation.

---

## [2026-05-04] - Primary Key Standardization & Card ID Separation
**Context:** The initial ERD (Entity Relationship Diagram) used generic `string` representations for primary keys (`ID`), with the `Card` entity directly mapping its string `ID` to the Unity `ScriptableObject` identifiers (e.g., "Lich", "card-1").

**Change:**
1. **Enforced GUID Primary Keys:** All entities (including `User`, `Card`, `CardCopy`, `Deck`, `Match`, `MatchParticipant`, `ActionLog`, `SystemConfig`) are now strictly required to use auto-generated `Guid` types for their Primary Keys (`ID`).
2. **Introduced `Card.StringID`:** The `Card` entity was modified to decouple the database primary key from the Unity logic.
   - The primary key `ID` is now an auto-generated `Guid` (for strict database referential integrity).
   - A new column `StringID` (type `string`) was added to the `Card` entity. This column explicitly stores the human-readable identifier from Unity `ScriptableObject` instances (e.g., "Lich", "card-1").
3. **Synchronized Documentation:** Both `api_plan.md` and `aspnet_backend_plan.md` were updated to reflect these strict type requirements, ensuring that the EFCore implementations and API specifications correctly implement `Guid` foreign keys and primary keys.

**Reasoning:**
- **Referential Integrity:** GUIDs ensure universal uniqueness, preventing accidental collisions during distributed entity creation.
- **Consistency:** Requiring `Guid` for all primary keys standardizes the EFCore implementation (using `ValueGeneratedOnAdd`).
- **Separation of Concerns:** Detaching the Unity asset string from the DB Primary Key allows Unity assets to be renamed or modified without cascading catastrophic primary key update failures across the entire relational database (e.g., inside `MatchParticipant` or `CardCopy` tables).
