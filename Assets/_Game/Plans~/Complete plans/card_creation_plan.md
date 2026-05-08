# Card Creation Plan: Data to ScriptableObjects

This plan outlines the automated process for creating `CardSO` assets from the Excel/CSV card data.

## 1. Prerequisites
*   **Data Source**: `Assets/_Game/TestBE~/card_ideas.csv`.
*   **Data Format**: UTF-16 CSV with headers (Faction, Name, Mana Cost, Description, Rarity?, Type, HP, Speed, Death Anchor, Normal Attack DMG, Normal Attack Range, Skill name, On Death Effect).

## 2. Infrastructure Setup
1.  **CardSO Refinement**: Ensure `CardSO`, `TroopCardSO`, `ChampionCardSO`, and `SpellCardSO` have fields matching the CSV data (Death Anchor, Speed, HP, Damage, Attack Range, Move Range).
2.  **Folder Structure**: Create subfolders under `Assets/_Game/Data/Cards/` for each Faction (`Hollow`, `Verdant`, `Ashen`).

## 3. Automation Script (`CardDataImporter.cs`)
Create an Editor script that:
*   Parses the CSV/JSON data.
*   Iterates through each entry.
*   Instantiates the correct ScriptableObject type based on the `Type` field.
*   Maps fields:
    *   `Name` -> `CardName` and sanitized `StringID`.
    *   `Faction` -> `CardNation` enum.
    *   `Mana Cost` -> `Cost`.
    *   `HP`, `Speed`, `Damage` -> Unit fields.
*   Saves assets to the faction-specific folder.

## 4. Backend Synchronization
Once assets are generated:
1.  Export the list of `StringID`s.
2.  Update `Assets/_Game/TestBE~/app/seed.py` with these IDs to ensure the mock backend recognizes the new cards.
3.  Restart the backend container to re-seed the database.

## 5. Visual Asset Binding
*   Manually assign `CardIllustration` sprites once the art team provides them.
*   Assign 3D Prefabs to the `UnitCardSO` (future expansion).

---
*Status: Ready for execution once the project structure is stable.*
