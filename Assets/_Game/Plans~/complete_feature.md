# Complete Feature List - Primora Chronicle

Based on the Graduation Project Report (Báo cáo DACN_HK251).

## 1. Account & Authentication
- **[UC-A01] Account Registration**: Create new account with username, password. Server-side validation and storage.
- **[UC-A02] Account Login**: Authenticate with server, load player data (assets, XP, decks), and transition to Lobby.
- **[UC-L02] Account Logout**: Terminate session, save settings, return to login screen.

## 2. Lobby & Navigation
- **[UC-L01] Map-based Navigation**: Interactive map locations (Combat, Profile, Deck, Shop, Settings).
- **[UC-C01] Loading Screen**: Visual feedback during scene transitions or data loading.
- **[UC-C02] System Settings**: Adjust Audio (Music, SFX) and Graphics.

## 3. Deck Building
- **[UC-D01] Deck Creation/Editing**: 
    - 20 cards per deck.
    - Single Faction restriction (Hollow, Verdant, Ashen).
    - Must include 1 Champion.
- **[UC-D02] Champion Synergy**: Auto-load specific "Champion Cards" when a Champion is selected.
- **[UC-D03] Card Detail View**: Inspect HP, Speed, Death Anchor, Mana Cost, Move/Attack Patterns, and Skills.
- **[UC-D04] Card Lore & Art**: Full-screen art view and background story.
- **[UC-D05] Deck Persistence**: Save decks to server, limit number of decks per account.

## 4. Matchmaking
- **[UC-M01] Match Settings**: Select Game Mode (PvP, AI, Offline) and choose Deck.
- **[UC-M02] Matchmaking Process**: Queue on server, level-based matching, timer, and expansion of search range.
- **[UC-M03] Match Confirmation**: Accept/Reject prompt before entering the match.

## 5. Profile & Social
- **[UC-P01] Profile View**: Display username, Level, XP, and Currency (Gold).
- **[UC-P02] Match History**: List recent matches with results, opponents, and rewards.
- **[UC-P03] Match Replay**: Re-simulate matches from recorded actions with playback controls (Pause, Fast Forward).

## 6. Shop & Economy
- **[UC-S01] Card Shop**: Purchase individual cards or card packs using in-game currency. Level-gated items.

## 7. Gameplay Mechanics
- **Grid System**: Hexagonal grid (Tile-based).
- **Faction System**: 
    - **Hollow**: Undead, sacrifice/revive mechanics, Death Anchor focus.
    - **Verdant**: Nature, healing/sustainability, growth.
    - **Ashen**: Fire, explosive damage, resource-for-power trade-offs.
- **Card Types**:
    - **Units**: Champion and Troops.
    - **Spells**: Skill (add skill), Action (immediate effect), Equipment (stat boost/passive).
- **Fusion Mechanic**: Combine Unit + Spell (up to 4 spells per unit) during Main Phase.

## 8. Match Phases (Turn Loop)
- **Start Phase**:
    - **Ban Phase**: Ban opponent's Champion choice.
    - **Pick Phase**: Secretly pick Champion.
    - **Deck Select**: Choose deck after seeing opponent's Champion.
    - **Initialization**: 4 starting cards, 2 Mana (increases to 8), HP = Champ HP.
- **Main Phase**:
    - Deploy Units to **Deploy Area**.
    - Fuse Units with Spells.
    - Use Action Spells.
- **Combat Phase**:
    - **Cycles**: Units act in order of Speed.
    - **Actions**: Move, Attack, Use Skill (Cooldown-based).
    - **Death Anchor**: Damage dealt to player HP when their unit dies.
    - Ends when 1 unit (or none) remains.
- **Draw Phase**: 
    - Draw 1 card (Hand limit: 7).
    - Discard overflow to Discard Pile.
    - Reshuffle Discard Pile if Deck is empty.
- **Win Phase**:
    - Check Win/Loss conditions (HP <= 0 or Surrender).
    - **[UC-G08] Rewards**: Calculate XP and Gold, update server.
