# Complete Feature List - Primora Chronicle

Comprehensive inventory of all features, subsystems, and assets derived from the Graduation Project Report (Báo cáo DACN_HK251).

## 1. Account & Authentication
*   **[UC-A01] Account Registration**:
    *   **Subsystem**: `AccountRegisterSubsystem`
    *   **UI**: `AccountRegisterPanel` attached to `Assets/_Game/Features/Account/UI/Screen/Screen_Account_Register.prefab` (Username, Password, Confirm Password).
    *   **Logic**: REST API call to `/api/users/register`.
*   **[UC-A02] Account Login**:
    *   **Subsystem**: `AccountLoginSubsystem`
    *   **UI**: `AccountLoginPanel` attached to `Assets/_Game/Features/Account/UI/Screen/Screen_Account_Login.prefab` (Initial screen).x 
    *   **Audio**: Gloomy, mysterious main menu theme (Music). Metallic "Click" for login (SFX).
*   **[UC-L02] Account Logout**:
    *   **Logic**: Local session clearance, return to `Bootstrap` scene.

## 2. Lobby & Navigation
*   **[UC-L01] Navigation Hub**:
    *   **Subsystem**: `LobbyMainSubsystem`
    *   **UI**: `LobbyMainPanel` attached to `Assets/_Game/Features/Lobby/UI/Screen/Screen_Lobby_Main.prefab` with interactive buttons for:
        *   **Battle**: `BattlePanel` (Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_Battle.prefab)
        *   **Decks**: `DeckPanel` (Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_Decks.prefab)
        *   **Shop**: `ShopPanel` (Assets/_Game/Features/Lobby/UI/Screen/Screen_Lobby_Shop.prefab)
        *   **Profile**: `ProfilePanel` (Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_Profile.prefab)
        *   **Settings**: `SettingPanel` (Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_Settings.prefab)
    *   **Audio**: Background magical ambiance (Music). Map click sounds (SFX).

## 3. Deck Building & Collection
*   **[UC-D01] Deck Editor**:
    *   **Subsystem**: `DeckBuildSubsystem`
    *   **UI**: `DeckBuildPanel` attached to `Assets/_Game/Features/Lobby/UI/Screen/Screen_Lobby_DeckBuild.prefab`.
    *   **Logic**: Validation for 20 cards + 1 Champion, single Faction.
*   **[UC-D03/D04] Card Details**:
    *   **Subsystem**: `CardDetailSubsystem`
    *   **UI**: `CardDetailPanel` (Placeholder) attached to `Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_CardDetail.prefab` (Placeholder).
    *   **Audio**: Subtle magical hum when inspecting cards (SFX). Page turning sound for Lore (SFX).

## 4. Matchmaking
*   **[UC-M01/M02] Search**:
    *   **Subsystem**: `MatchMakingSubsystem`
    *   **UI**: `MatchMakingPanel` attached to `Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_MatchMaking.prefab` (Timer and status).
    *   **Logic**: API call to `/api/matchmaking/start`.
*   **[UC-M03] Confirmation**:
    *   **UI**: `ConfirmationPopup` attached to `Assets/_Game/Features/System/UI/Popup/Popup_System_Confirmation.prefab` (Accept/Reject).
    *   **Audio**: Intense rhythmic drum beat when match is found (SFX).

## 5. Gameplay Mechanics (In-Match)
*   **Grid System**:
    *   **Subsystem**: `BoardSubsystem`
    *   **UI**: `BoardPanel` attached to `Assets/_Game/Features/Gameplay/UI/Screen/Screen_Gameplay_Board.prefab` (Placeholder).
    *   **Models**: 
        *   Hollow: Bone/Shadow structures.
        *   Verdant: Overgrown stone.
        *   Ashen: Scorched obsidian.
*   **Start Phase (Ban/Pick)**:
    *   **Subsystem**: `GameStateSubsystem` / `StartPhaseSubsystem`
    *   **UI**: `BanPickPanel` (Placeholder) attached to `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_ChooseAChampion.prefab`.
*   **Combat Phase**:
    *   **Subsystem**: `CombatSubsystem`
    *   **UI**: `CombatPanel` / `SkillPanel` (`Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_Skill.prefab`).
    *   **Logic**: Action-bar based on `Speed` via `TurnOrderPanel` (`Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_TurnOrder.prefab`).
    *   **Animations**: `Idle`, `Walk`, `Attack`, `Hurt`, `Death`, `SkillCast`.
    *   **Audio**: 
        *   **Hollow**: Whispers and bone cracks.
        *   **Ashen**: Fire roar and metal impact.
        *   **Verdant**: Root growth and wind swirls.

## 6. Post-Match & Rewards
*   **[UC-G08] Results**:
    *   **Subsystem**: `MatchResultSubsystem`
    *   **UI**: `MatchResultPanel` attached to `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_MatchResult.prefab`.
    *   **Audio**: Heroic/Tragic orchestral stinger (Music). Coin clinking for Gold gain (SFX).

## 7. Audio Subsystem Integration
*   **Standard**: Use `IAudioManagerSubsystem` via Zenject injection.
*   **Implementation**:
    *   `PlaySFX(clip)` for one-shots (UI clicks, impacts).
    *   `PlayMusic(clip, loop)` for ambiance/themes.
    *   Volume parameters mapped to `SettingPanel` sliders via `ISettingSubsystem`.

## 8. 3D Model & Animation Requirements
*   **Units**: Low-poly stylised models with baked PBR textures (Metallic/Roughness focus).
*   **Animations**: 
    *   `Champion`: Elaborate intro and ultimate skill animations.
    *   `Troop`: Efficient 4-5 clip sets.
    *   `Spells`: Particle systems (VFX) linked to `VFXSubsystem`.

## 9. Next Features & UI Todo (next_features)
*   **[UI] Create `CardDetailPanel.cs`**: Implement the view logic for card inspection.
*   **[UI] Create `BanPickPanel.cs`**: Implement the simultaneous secret selection UI.
*   **[UI] Create `BoardPanel` Prefab**: Create the gameplay board screen prefab at `Assets/_Game/Features/Gameplay/UI/Screen/Screen_Gameplay_Board.prefab`.
*   **[UI] Create `Overlay_Lobby_CardDetail` Prefab**: Create the card detail overlay prefab at `Assets/_Game/Features/Lobby/UI/Overlay/Overlay_Lobby_CardDetail.prefab`.
 