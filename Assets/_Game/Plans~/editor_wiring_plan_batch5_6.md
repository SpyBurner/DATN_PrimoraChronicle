# Unity Editor Wiring Plan — Batch 5 & 6

## BATCH 5 (Lobby UI & Configurations)

### 1. Server Configuration
* **Action:** Create `ServerConfig` asset.
* **Path:** `Assets/_Game/Core/Resources/Config/ServerConfig.asset`
* **Steps:**
  1. Right-click in the Project window under `Assets/_Game/Core/Resources/Config/` (create folders if missing).
  2. Select `Create -> Primora -> Server Config`.
  3. Name the asset `ServerConfig`.
  4. In the Inspector, set `ApiBaseUrl` to your target backend (e.g., `http://localhost:8000` or production URL).
  5. The `CoreInstaller` will automatically load this from the `Resources/Config/` path.

### 2. New Popup Prefabs
You need to create 4 new popups and assign them as `UIPanel` components.
* **ConfirmationPopup:**
  1. Create a UI Panel prefab named `Popup_Confirmation`.
  2. Attach `ConfirmationPopup.cs`.
  3. Assign `Title Text` (TMP_Text), `Message Text` (TMP_Text), `Confirm Button`, and `Cancel Button`.
* **TextInputPopup:**
  1. Create `Popup_TextInput`.
  2. Attach `TextInputPopup.cs`.
  3. Assign `Title Text` (TMP), `Input Field` (TMP_InputField), `Confirm Button`, `Cancel Button`.
* **DeckItemContextPopup:**
  1. Create `Popup_DeckItemContext`.
  2. Attach `DeckItemContextPopup.cs`.
  3. Assign `Deck Name Text` (TMP), `Edit Button`, `Select Button`, `Delete Button`, `Close Button`.
* **ShopItemContextPopup:**
  1. Create `Popup_ShopItemContext`.
  2. Attach `ShopItemContextPopup.cs`.
  3. Assign `Item Name Text` (TMP), `Item Price Text` (TMP), `Purchase Button`, `Close Button`.

### 3. Update UIMapping (Constants & CoreInstaller)
* **Action:** Add the new Popups to the UI Manager.
* **Steps:**
  1. Open `UIMappingSO` in the Editor.
  2. Add the 4 new Popups to the list.
  3. Ensure `Constants.cs` reflects their `UIIdentifier` enums (311, 312, 313, 314).

---

## BATCH 6 (Gameplay Scene & Photon Fusion)

### 1. Gameplay Scene Context
* **Action:** Create `Gameplay` scene and context.
* **Steps:**
  1. Create a new Scene `Gameplay`.
  2. Create an empty GameObject `SceneContext` and attach `SceneContext` (Zenject).
  3. Attach `GameplayInstaller` (to be completed in this batch) to a generic `Installers` GameObject and link it to `SceneContext`.

### 2. NetworkBehaviour Models
* **Action:** Wiring models as network prefabs or scene objects.
* **Steps:**
  1. For every Subsystem that implements `NetworkBehaviour` (like `GameStateModel`), it MUST be on a `NetworkObject`.
  2. Create an empty GameObject in the `Gameplay` scene named `GameStateModel` or bake it into a `GameplayNetworkManager` prefab.
  3. Attach `NetworkObject` and `GameStateModel` components.
  4. Ensure `GameplayInstaller` grabs this instance from the scene, or spawns/binds it upon runner initialization:
     * *Zenject Tip:* Use `Container.BindInterfacesAndSelfTo<GameStateModel>().FromComponentInHierarchy().AsSingle();`
     * Ensure the `NetworkObject` is registered with the `NetworkRunner`.