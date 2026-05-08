# Primora Chronicle — Complete Unity Editor Hand-Wiring Plan

This document provides a consolidated, step-by-step checklist for manually wiring up the Unity Editor assets, Zenject contexts, and UI Panels to match the re-architected MVVM system (P1 Compliance).

---

## 1. Global Context (ProjectContext)
**Location:** `Assets/_Game/Scenes/Bootstrap` scene.

### 1.1 ProjectContext Setup
1. Find **ProjectContext** GameObject in the hierarchy.
2. Ensure **CoreInstaller** script is attached.
3. **ServerConfig Wiring**:
    - Navigate to `Assets/_Game/Core/Resources/Config/`.
    - Find (or create: Right-click -> Create -> Primora -> Server Config) `ServerConfig.asset`.
    - Set `ApiBaseUrl` to `http://localhost:8000` (or your testing BE).
4. **UIMapping Wiring**:
    - In **CoreInstaller** Inspector, find the **UIMapping** slot.
    - Drag `Assets/_Game/SOs/UIMappingSO.asset` into the slot.
    - Ensure `UIMappingSO` contains the following panel prefabs in its list:
        - `Screen_Account_Login`
        - `Screen_Account_Register`
        - `Screen_Lobby_Main`
        - `Screen_Lobby_DeckBuild` (formerly DeckEdit)
        - `Popup_Confirmation`
        - `Popup_TextInput`

---

## 2. Scene Contexts

### 2.1 Account Scene
**Location:** `Assets/_Game/Scenes/Account` scene.
1. Select **SceneContext** GameObject.
2. Ensure **AccountInstaller** is attached to the **Installers** list.

### 2.2 Lobby Scene
**Location:** `Assets/_Game/Scenes/Lobby` scene.
1. Select **SceneContext** GameObject.
2. Ensure **LobbyInstaller** is attached to the **Installers** list.

---

## 3. Subsystem UI Wiring (Lobby Scene)

### 3.1 Deck Panel (Deck Choosing)
**Location:** Usually a child of the Lobby Main canvas in the hierarchy.
1. Find the GameObject with **DeckPanel.cs** attached.
2. **Field Assignments**:
    - **_deckSlot**: Assign the 8 child GameObjects (slots) where deck buttons will spawn.
    - **_deckButtonPrefab**: Drag `Assets/_Game/Features/Lobby/UI/Prefabs/DeckButton.prefab` here.
3. **DeckButton Prefab Setup**:
    - Open `DeckButton.prefab`.
    - Ensure **DeckButton.cs** is attached.
    - Assign `_deckNameText` (TextMeshPro) and `_button` (Unity UI Button).

### 3.2 Deck Build Panel (Deck Editing)
**Location:** `Assets/_Game/Features/Lobby/UI/Screen/Screen_Lobby_DeckBuild.prefab`
1. Open the prefab.
2. **Component Fix**: Ensure the root has **DeckBuildPanel.cs** (NOT `DeckEditPanel`).
3. **Field Assignments**:
    - **_cardDisplayPrefab**: Drag `Assets/_Game/Features/Lobby/Scripts/Deck/DeckBuildCard/CardDisplay.prefab` here.
    - **_championCardContainer**: GameObject child (Horizontal/Vertical Layout Group).
    - **_deckContainer**: GameObject child (Grid Layout Group).
    - **_cardContainer**: GameObject child (Grid Layout Group for inventory).
    - **_championPortrait**: Image component.
    - **_saveButton**: Unity UI Button.
4. **CardDisplay Prefab Setup**:
    - Open `CardDisplay.prefab`.
    - Ensure **CardDisplay.cs** is attached.
    - Assign `_cardIllustration` (Image) and `_cardFrame` (Image).

---

## 4. Screen & Popup Prefab Configurations
For every prefab below, ensure the **UIPanel** base settings are correct in the Inspector:

| Prefab Name | Script Attached | Identifier (Enum) | Layer (Enum) |
|-------------|-----------------|-------------------|--------------|
| `Screen_Account_Login` | `AccountLoginPanel` | `ACCOUNT_LOGIN` | `SCREEN` |
| `Screen_Account_Register` | `AccountRegisterPanel` | `ACCOUNT_REGISTER` | `SCREEN` |
| `Screen_Lobby_Main` | `LobbyMainPanel` | `LOBBY_MAIN` | `SCREEN` |
| `Screen_Lobby_DeckBuild` | `DeckBuildPanel` | `DECK_BUILD` | `SCREEN` |
| `Popup_Confirmation` | `ConfirmationPopup` | `CONFIRMATION` | `POPUP` |
| `Popup_TextInput` | `TextInputPopup` | `TEXT_INPUT` | `POPUP` |
| `System_Loading` | `LoadingScreenPanel` | `LOADING_SCREEN` | `SYSTEM` |

---

## 5. System Layer & Loading Screen Setup
The Loading Screen is handled globally by the `UIManager` and `SceneLoader`.

### 5.1 Loading Screen Prefab
1. Locate (or create) the Loading Screen prefab: `Assets/_Game/Features/System/UI/System/System_Loading_Black.prefab`.
2. **Component Fix**: Attach **LoadingScreenPanel.cs** (inherits from `UIPanel`).
3. **UIPanel Settings**:
    - **_identifier**: `LOADING_SCREEN` (Value 101).
    - **_layer**: `SYSTEM`.
4. **Visuals**: Ensure it has a black background (Image) or animation that covers the screen.

### 5.2 UIRoot Wiring (Crucial)
Each scene (or the global `Bootstrap` scene) must have a **UIRoot** that understands the `SYSTEM` layer.
1. Select the **UIRoot** GameObject in the scene.
2. In the **Layer Parents** list, ensure there is an entry for:
    - **Layer**: `SYSTEM`
    - **Parent**: A Transform that is at the bottom of the hierarchy (rendered on top of everything else).
    - *Tip:* This should usually be a dedicated Canvas or the last child of a global Canvas.

### 5.3 UIMappingSO Registration
1. Open `Assets/_Game/SOs/UIMappingSO.asset`.
2. Add the `System_Loading` prefab to the **UI Mappings** list with the `LOADING_SCREEN` identifier.

## 6. Verification Checklist
- [ ] **Zenject Validation**: Click `Edit -> Zenject -> Validate Current Scene` in both Account and Lobby scenes. Resolve any injection errors.
- [ ] **Button Listeners**: 
    - Verify `OnLogin`, `OnRegister`, `OnSave`, etc., are NOT manually wired in the `onClick` event in the Inspector (P1 rule: bind via code in `OnEnable`).
- [ ] **Deck ID Resolution**: In the `DeckPanel` (Lobby), clicking a `DeckButton` must trigger `_deckBuild.LoadDeck(deck.id)` via code.
- [ ] **Placeholder**: In `DeckBuildPanel`, verify there is a child GameObject or logic to handle the "Empty Deck" state.

---
**Date Compiled:** 2026-05-07
**Status:** Architecture V2 Compliant (P1 Rules)
