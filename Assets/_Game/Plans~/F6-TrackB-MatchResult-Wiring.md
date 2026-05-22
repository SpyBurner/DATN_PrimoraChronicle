# F6 Track B — Match Result Panel Wiring Instructions

## Prefab: `PhaseInteractionPanel_MatchResult.prefab`

**Location:** `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_MatchResult.prefab`

### 1. Attach Script

Add `MatchResultPanel` (MonoBehaviour) to the **root** GameObject of the prefab.

**Script location:** `Assets/_Game/Features/Gameplay/Scripts/UI/MatchResultPanel.cs`

---

### 2. Serialize Field Assignments

| Field | Target Path in Hierarchy | Component |
|---|---|---|
| `_player0Crown` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0Crown` | Image |
| `_player0PFP` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0PFP` | Image |
| `_player0Name` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0Name` | TMP_Text |
| `_player1Crown` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player1/Player1Crown` | Image |
| `_player1PFP` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player1/Player1PFP` | Image |
| `_player1Name` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player1/Player1Name` | TMP_Text |
| `_player2Crown` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player2/Player2Crown` | Image |
| `_player2PFP` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player2/Player2PFP` | Image |
| `_player2Name` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player2/Player2Name` | TMP_Text |
| `_goldValueText` | `Panel/FramedContainer_Stone/Panel/GoldValueText` | TMP_Text |
| `_xpValueText` | `Panel/FramedContainer_Stone/Panel/XPValueText` | TMP_Text |
| `_timeValueText` | `Panel/FramedContainer_Stone/Panel/TimeValueText` | TMP_Text |
| `_confirmButton` | `Panel/Button_Confirm` | Button |
| `_player0Slot` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0` | GameObject |
| `_player1Slot` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player1` | GameObject |
| `_player2Slot` | `Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player2` | GameObject |

---

### 3. Initial State

- **Player2 slot** (`_player2Slot`): Set **inactive** in the prefab (reserved for 3-player, out of scope for v1).
- **Crown images** (`Player0Crown`, `Player1Crown`, `Player2Crown`): Leave **enabled** in prefab — the script disables them at runtime and only enables the winner's crown.
- The panel itself should start **inactive** — it becomes active when `GameplayPhase.GameOver` is received via `IGameStateSubsystem.PhaseChanged`.

---

### 4. Zenject Injection

The script uses `[Inject]` attributes. Ensure the prefab is a child of (or instantiated within) a scene that has a `SceneContext` referencing `GameplayInstaller`. No additional installer bindings are needed for this MonoBehaviour — Zenject auto-injects scene MonoBehaviours.

---

### 5. Behavior Summary

| Trigger | Action |
|---|---|
| `IGameStateSubsystem.PhaseChanged → GameOver` | Panel activates itself |
| `IMatchResultSubsystem.MatchEnded` event | Populates player slots, crown, rewards, duration |
| `Button_Confirm` click | Calls `IMatchResultSubsystem.ReturnToLobby()` (loads Lobby scene) |

- **Player0** = local player (name from `IProfileSubsystem.Username`)
- **Player1** = opponent (displays "Opponent" — opponent profile data not yet available in v1)
- **Crown** = shown only on the winner's slot; hidden on tie
- **Time** = formatted as `MM:SS`
