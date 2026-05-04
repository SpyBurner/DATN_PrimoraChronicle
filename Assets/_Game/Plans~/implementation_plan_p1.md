# Unity Client Architecture — Full Implementation Plan

---

## 1. POST-MERGE BUG REPORT (Lobby + Account Flow)

> [!WARNING]
> **Do NOT fix these yet.** Report only. Suggested fixes inline.

### BUG-1: `IAccountLoginModel` / `IAccountRegisterModel` bypass MVVM

**Problem:** Model properties (`Email`, `Password`) use plain `string` auto-properties, not `Observable<string>`. Controller sets them directly via `_model.Email = email`. No `OnChanged` event fires → Views cannot observe model state → architecture violation.

**Cause:** Models were written as POCOs, not as observable state containers.

**Fix:** Replace `string` properties with `Observable<string>`. Controller sets via `.Value`. Subsystem wires `OnChanged` → events. Views listen to events for error messages, validation state, etc. Form *input* fields still call subsystem methods (no loop — input writes go through controller, only *result* state like error messages flow back via events).

---

### BUG-2: Navigation methods violate subsystem boundary

**Problem:** `AccountLoginController.NavigateToRegister()` calls `_uiManager.CloseView()` and `_uiManager.ShowScreen<>()` directly. Same in `AccountRegisterController.NavigateToLogin()` and `LobbyMainController` (all navigation methods). Controllers should only modify *their own* model. UI navigation is a UIManager concern.

**Cause:** Controller took on navigation responsibility that belongs to the View or a dedicated navigation subsystem.

**Fix:** Move navigation calls to the Panel (View) itself. Panel calls `_uiManager.CloseView(this)` then `_uiManager.ShowScreen<TargetPanel>()`. The `ShowScreen<T>` method already queries `UIMappingSO` to resolve the correct prefab for that type.

---

### BUG-3: `SceneLoaderController` implements `IInitializable, IDisposable` redundantly

**Problem:** `SceneLoaderController` declares `: ISceneLoaderController, IInitializable, IDisposable` but `ISceneLoaderController` already extends `IInitializable`. The `IDisposable` is not on the interface → not discoverable by Zenject unless `BindInterfacesAndSelfTo` catches it from the concrete class.

**Cause:** Interface doesn't declare `IDisposable`.

**Fix:** Add `IDisposable` to `ISceneLoaderController`. Remove redundant declarations from concrete class. Same pattern issue exists on most controllers — they declare `IInitializable` on the interface but not `IDisposable`. Standardize: all controller interfaces extend both.

---

### BUG-4: UIManager API Semantics (`ShowScreen` vs `ShowView` for Overlays)

**Problem/Clarification:** `LobbyMainController.NavigateToProfile()` calls `ShowScreen<ProfilePanel>()`, even though Profile is an overlay. 

**Cause:** The name `ShowScreen` implies a full-screen replacement, but under the hood, `UIManagerController.ShowView()` already inspects the prefab's `UIPanel.Layer` and `UIRoot.GetLayerParent()` puts it in the correct layer (e.g., OVERLAY).

**Fix:** Normalize the API. Since the `UIPanel`'s attributes define its layer, UIManager only needs simple `Show<T>()` and `Hide(panel)` methods. UIRoot completely handles layer placement. We will ensure the View directly calls `_uiManager.ShowScreen<ProfilePanel>()` (or rename to `Show<T>()`) and trust the prefab's `UILayer` setting.

---

### BUG-5: `Bootstrapper.cs` has `using UnityEditor` — build-breaking

**Problem:** Line 2: `using UnityEditor;` in a runtime script. Will fail on any non-editor build.

**Cause:** Leftover from development.

**Fix:** Remove the `using UnityEditor;` line. Wrap in `#if UNITY_EDITOR` if actually needed.

---

### BUG-6: `ISceneLoaderController` extends `IInitializable` but not `IController`

**Problem:** `ISceneLoaderController : IInitializable` — doesn't extend `IController`. All other controllers extend `IController`. Inconsistency.

**Fix:** Change to `ISceneLoaderController : IController, IInitializable`.

---

### BUG-7: UIPanel registers in `Start()`, but `OnEnable()` runs first

**Problem:** `UIPanel.Start()` calls `RegisterPanel(this)`. But `OnEnable()` fires before `Start()` on first activation. If any OnEnable logic depends on the panel being registered, it will fail silently.

**Cause:** Unity lifecycle ordering.

**Fix:** Move `RegisterPanel` to `Awake()` or beginning of `OnEnable()` with a guard flag.

---

### BUG-8: Race Condition — `SceneLoaderController.OnSceneLoaded` auto-shows default screen

**Problem:** `OnSceneLoaded` uses a fragile `Task.Yield()` delay to wait for UIRoot to initialize before calling `ShowDefaultScreenForScene`.

**Cause:** SceneLoader is guessing when UIRoot is ready.

**Fix:** Reverse the responsibility. `SceneLoader` should only load the scene. When `UIRoot` finishes its `Awake()` and registers itself, `UIRoot` (or `UIManager`) should trigger the loading of the default screen for its scene.

---

## 2. ARCHITECTURE RULES (Codified from your requirements + report doc)

| Rule | Description |
|------|-------------|
| R1 | View = MonoBehaviour. Only handles player input + observes model events via subsystem. |
| R2 | View calls subsystem methods for actions. Never modifies model directly. |
| R3 | Subsystem exposes: controller methods (public) + model events (UnityAction). |
| R4 | Only the owning controller may modify its model. |
| R5 | Model properties use `Observable<T>`. Changes fire events. |
| R6 | Networked models (gameplay) use Photon Fusion `[Networked]` properties. Single source of truth. |
| R7 | Zenject `ProjectContext` → global subsystems. `SceneContext` → scene subsystems. `GameObjectContext` → scoped/transient. |
| R8 | Each subsystem = 7 files: IModel, IController, ISubsystem, Model, Controller, Subsystem, + View(s). |
| R9 | Views do NOT listen to their own input-driven model changes (prevents loops). Views DO listen to result/error/status changes. |
| R10 | Maximize code-wired references. Editor wiring only for: SerializeField on prefab components (buttons, text, etc.) and SO asset slots on installers. |

---

## 3. GLOBAL SUBSYSTEMS (ProjectContext — lives entire app lifetime)

Bound in `CoreInstaller` on the `ProjectContext` prefab.

| Subsystem | Model State | Controller Methods | Notes |
|-----------|------------|-------------------|-------|
| **UIManager** | Panels dict, PanelsByLayer, PopupStack | Register/Unregister, Show/Close/Fade | ✅ EXISTS. Will normalize to just show/hide. |
| **SceneLoader** | IsLoading, CurrentLoad, SceneToken | LoadScene, ReloadScene | ✅ EXISTS. Needs IController fix. |
| **AudioManager** | MasterVol, MusicVol, SFXVol (Observable) | SetMasterVol, SetMusicVol, SetSFXVol, PlaySFX, PlayMusic | 🆕 Global audio state. |
| **AuthSession** | CurrentUserId, AuthToken, IsLoggedIn (Observable) | StoreSession, ClearSession | 🆕 Persists auth across scenes. |
| **HttpService** | RequestQueue, IsRequesting (Observable) | Post(url, payload), Get(url) | 🆕 Abstracts all HTTP calling for the app. |

> [!IMPORTANT]
> `AuthSession` and `HttpService` are critical. `HttpService` handles the underlying UnityWebRequest/HttpClient logic. Other subsystems (Login, LobbyMain, Deck, etc.) call `HttpService.Post/Get` instead of handling the request library directly. `AuthSession` stores the token and automatically injects it into `HttpService` headers.

---

*Continued in Part 2...*
