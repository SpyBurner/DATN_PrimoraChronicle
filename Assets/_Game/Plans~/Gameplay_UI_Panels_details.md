Gameplay UI Panel contents for manual wiring - script reference

# Drawer panel anchors
- Script: D:\UnityProjects\DATN_PrimoraChronicle\Assets\_Game\Features\Gameplay\Scripts\UI\PanelDrawer.cs
- Panels:
    - HandPanelAnchor.prefab
    - SkillPanelAnchor.prefab
    - TurnOrderPanelAnchor.prefab
- Description: These prefabs are Drawer wrappers for the corresponding UI panel below. The UI Panel will be a child of this gameobject. The UI Panel will be moved between the Open and Closed position.
- Detail:
    - A Toggle: The toggle controlling the movement of the wrapped UI Panel to move to "Open" or "Closed" position.
    - Open position children: a child gameobject placed at the "Open position", where the UI Panel should be when the toggle is on.
    - Close position: not an exclusive gameobject. The local position of 0 relative to the anchor is considered the "Closed" position.

# Overall layout
Layout_Fullscreen_Gameplay.prefab
- Script on root: GameplayHUDController (MonoBehaviour)
- Detail:
    - PhaseNamePanel/PhaseNameValueText: TMP — current phase name ("START PHASE", "MAIN PHASE", etc.)
    - PhaseNamePanel/MatchTimeValueText: TMP — match elapsed time
    - Profile_Enemy1: GameplayPlayerProfileUI — active, top-left opponent slot
    - Profile_Enemy2: GameplayPlayerProfileUI — inactive by default, reserved for third player
    - Profile_Player: GameplayPlayerProfileUI — active, local player slot

# Profile widget
Profile_Gameplay.prefab
- Script on root: GameplayPlayerProfileUI (MonoBehaviour)
- Detail:
    - FramedContainer/ReadyToggle: Toggle — player confirmation/ready state
    - FramedContainer/Panel: Image — profile picture (PFP)
    - FramedContainer/Panel (1)/Panel (2)/NameValueText: TMP — player display name
    - FramedContainer/Panel (1)/Panel (4)/HPValueText: TMP — current HP value

# Start phase
PhaseInteractionPanel_DeckChoose.prefab
- Description: First panel shown on match join; player picks a deck.
- Detail:
    - Panel/Panel/TimeValueText: TMP — countdown timer value
    - Panel/Button_Cancel: Button — skip / auto-select
    - Panel/Button_Confirm: Button — confirm deck selection
    - Panel (1)/DeckButton: DeckButton (LobbyFeatures) — holds selected deck name + ID; queries /api/decks on enable

Overlay_Gameplay_Decks.prefab
- Description: Grid of 8 deck option slots.
- Detail:
    - DeckSlot [×8]: The parent object to populate DeckButton objects in.
    - DeckButton: dynamically populated, from the API call to get deck name and deck id.
    - On click: send the deck name and id back to PhaseInteractionPanel_DeckChoose.prefab to display, disable this panel
    - Exit button: Disable this panel without making any change.

# Draw phase
PhaseInteractionPanel_DrawCard.prefab
- Detail:
    - Panel/Button_Confirm: Button — confirm draw
    - Panel (1)/CardSlot [×6]: Button + CardDisplay — drawable card slots (CardSlot, CardSlot (1)–(5))

# Fusion phase
PhaseInteractionPanel_Fusion.prefab
- Detail:
    - Panel/Panel/TimeValueText: TMP — countdown timer
    - Panel/Button_Cancel: Button — cancel fusion
    - Panel/Button_Confirm: Button — confirm fusion
    - Panel (1)/Panel/UnitSlot: Button + CardDisplay — unit being fused
    - Panel (1)/Panel (1)/Panel (2)/NormalAttackSlot: Button + CardDisplay — normal attack card input
    - Panel (1)/Panel (1)/Panel (2)/MovementSlot: Button + CardDisplay — movement card input
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot1: Button + CardDisplay — fusion result slot 1
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot2: Button + CardDisplay — fusion result slot 2
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot3: Button + CardDisplay — fusion result slot 3
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot4: Button + CardDisplay — fusion result slot 4

# Hand phase
PhaseInteractionPanel_Hand.prefab
- Detail:
    - Toggle_Sidebar: Toggle — drawer open/close (wired by PanelDrawer)
    - Panel (content area)/CardSlot [×N]: Button + CardDisplay — player hand card slots

# Combat phase
PhaseInteractionPanel_Skill.prefab
- Detail:
    - Toggle_Sidebar: Toggle — drawer open/close (wired by PanelDrawer)
    - Panel (content area)/CardSlot_Empty [×N]: Button + CardDisplay × 2 — unit skill slots

PhaseInteractionPanel_TurnOrder.prefab
- Detail:
    - Toggle_Sidebar: Toggle — drawer open/close (wired by PanelDrawer)
    - Panel/ScrollView_Horizontal/Viewport/Content: RectTransform — spawn container for turn-order card items
    - Content/CardSlot_Empty [×5]: Button + CardDisplay × 2 — pre-placed unit slots (scripts populate at runtime)

# Match result
PhaseInteractionPanel_MatchResult.prefab
- Detail:
    - Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0Crown: Image — winner crown (shown if winner)
    - Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0PFP: Image — player 0 portrait
    - Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0Name: TMP — player 0 display name
    - Player1/Player1Crown, Player1PFP, Player1Name: same pattern for player 1
    - Player2/Player2Crown, Player2PFP, Player2Name: same pattern for player 2
    - Panel/FramedContainer_Stone/Panel/GoldValueText: TMP — gold earned
    - Panel/FramedContainer_Stone/Panel/XPValueText: TMP — XP earned
    - Panel/FramedContainer_Stone/Panel/TimeValueText: TMP — match duration
    - Panel/Button_Confirm: Button — dismiss and return to lobby
