# Unity Editor Wiring Plan — Phase B & C

## Overview
This document provides **step-by-step, detailed instructions** for wiring up all Unity Editor assets after code implementation. Each section is self-contained and foolproof to avoid human errors.

---

## PHASE B: Global Subsystems (ProjectContext)

### Setup Location
- Scene: **Bootstrap** scene
- GameObject: **ProjectContext** (Zenject ProjectContext)
- Script: **CoreInstaller.cs** (already attached)

### Step-by-Step Wiring

#### 1. Verify CoreInstaller is Attached
1. Open **Bootstrap** scene
2. Find **ProjectContext** GameObject in hierarchy
3. Confirm **CoreInstaller** script is attached in Inspector
4. ✅ If CoreInstaller shows in Components, proceed to Step 2

#### 2. Assign UIMapping ScriptableObject
1. In **CoreInstaller** Inspector, locate the public field **UIMapping** (empty slot)
2. In Project folder, navigate to `Assets/_Game/SOs/`
3. Find **UIMappingSO** ScriptableObject (or similar mapping file)
4. Drag **UIMappingSO** into the **UIMapping** slot
5. ✅ Verify the slot shows the asset name and is no longer empty

#### 3. Verify All Subsystem Bindings
- No additional wiring needed for HttpService, AuthSession, AudioManager
- All bindings are code-wired via `BindInterfacesAndSelfTo<>()` in CoreInstaller
- ✅ No action required if step 2 is complete

---

## PHASE C: Account Scene Setup

### Scene Setup
- Open **Account** scene from `Assets/_Game/Scenes/Account/`

### Setup Location
- GameObject: **SceneContext** (Zenject SceneContext)
- Script: **AccountInstaller.cs** (attach if missing)

#### 1. Attach AccountInstaller to SceneContext
1. Select **SceneContext** GameObject in Account scene hierarchy
2. In Inspector, check if **AccountInstaller** script is already attached
   - If YES → proceed to Step 2
   - If NO → click **Add Component** → search for **AccountInstaller** → attach it
3. ✅ Inspector should show AccountInstaller component

#### 2. Verify No Public Slots on AccountInstaller
- AccountInstaller should have **no public serializable fields**
- All bindings are code-wired: `BindInterfacesAndSelfTo<>()`
- ✅ No UI elements to assign

---

## PHASE C: Account Screen Prefabs

### Screen: AccountLogin
**Location:** `Assets/_Game/Features/Account/UI/Prefabs/Screen_Account_Login.prefab`

#### 1. Locate and Select Prefab
1. In Project, open `Assets/_Game/Features/Account/UI/Prefabs/`
2. Find **Screen_Account_Login** prefab
3. Select it (or drag it into scene to edit inline)

#### 2. Root GameObject Setup
1. Select the **root GameObject** of the prefab (should be named "Screen_Account_Login" or "LoginScreen")
2. In Inspector, verify **AccountLoginPanel** script is attached
   - If NO → Add Component → search **AccountLoginPanel** → attach

#### 3. Find UIPanel Settings
1. On the same root GameObject, locate **UIPanel** script (base class of AccountLoginPanel)
2. In Inspector, set:
   - **_identifier** = `ACCOUNT_LOGIN` (enum)
   - **_layer** = `SCREEN` (enum)
   - **_isModal** = `false` (checkbox)
3. ✅ These settings configure the panel's registration in UIManager

#### 4. Assign Input Fields and Buttons
In the AccountLoginPanel component, assign these fields:

| Field Name | Target | How to Find |
|-----------|--------|------------|
| **_emailInput** | TMP_InputField for Email | Select child GameObject named "EmailInput" or similar, ensure it has TextMeshProUGUI InputField component |
| **_passwordInput** | TMP_InputField for Password | Select child GameObject named "PasswordInput", ensure it has TextMeshProUGUI InputField component |
| **_loginButton** | Login Button | Select child Button GameObject, typically labeled "LoginButton" or "Submit" |
| **_registerButton** | Register Button | Select child Button GameObject, typically labeled "RegisterButton" or "GoToRegister" |
| **_errorText** | TextMeshProUGUI for Error Display | Select child TextMeshProUGUI GameObject (e.g., "ErrorText"). **If it doesn't exist, create a new child TextMeshProUGUI and name it "ErrorText"** |

**Wiring Steps:**
1. On AccountLoginPanel component, click the small circle next to **_emailInput** field
2. In the Object Picker window, find and select the Email InputField GameObject
3. Repeat for _passwordInput, _loginButton, _registerButton, _errorText
4. ✅ All 5 fields should show assigned objects (not empty slots)

#### 5. Verify Interactivity
1. Select the **_loginButton** (from step above)
2. In Inspector, ensure the Button component has **onClick** listener
   - If empty, expand the **On Click ()** section
   - Click + to add listener
   - Drag the **root AccountLoginPanel** GameObject into the object field
   - From the dropdown, select **AccountLoginPanel** → **OnLogin()**
   - ✅ Listener should show "AccountLoginPanel.OnLogin()"

---

### Screen: AccountRegister
**Location:** `Assets/_Game/Features/Account/UI/Prefabs/Screen_Account_Register.prefab`

#### 1-3. Root Setup (Same as AccountLogin)
1. Select root GameObject
2. Attach **AccountRegisterPanel** script (if missing)
3. Set UIPanel settings:
   - **_identifier** = `ACCOUNT_REGISTER`
   - **_layer** = `SCREEN`
   - **_isModal** = `false`

#### 4. Assign Input Fields and Buttons
In the AccountRegisterPanel component, assign:

| Field Name | Target |
|-----------|--------|
| **_emailInput** | Email InputField |
| **_passwordInput** | Password InputField |
| **_confirmPasswordInput** | Confirm Password InputField |
| **_submitButton** | Register/Submit Button |
| **_backButton** | Back/Cancel Button |
| **_errorText** | Error display TextMeshProUGUI (create if missing) |

**Steps:** Same as AccountLogin — use small circle to pick objects

#### 5. Verify Button Listeners
- **_submitButton** → clicks → calls **AccountRegisterPanel.OnSubmit()**
- **_backButton** → clicks → calls **AccountRegisterPanel.OnBack()**

---

## Verification Checklist

### Bootstrap Scene (ProjectContext)
- [ ] ProjectContext GameObject exists
- [ ] CoreInstaller script attached
- [ ] UIMapping field assigned to UIMappingSO
- [ ] No errors in Console

### Account Scene (SceneContext)
- [ ] SceneContext GameObject exists
- [ ] AccountInstaller script attached
- [ ] No errors in Console

### Screen_Account_Login Prefab
- [ ] Root GameObject has AccountLoginPanel script
- [ ] UIPanel _identifier = ACCOUNT_LOGIN
- [ ] UIPanel _layer = SCREEN
- [ ] 5 fields assigned (_emailInput, _passwordInput, _loginButton, _registerButton, _errorText)
- [ ] _errorText GameObject exists (create if missing)
- [ ] No empty field slots showing red errors

### Screen_Account_Register Prefab
- [ ] Root GameObject has AccountRegisterPanel script
- [ ] UIPanel _identifier = ACCOUNT_REGISTER
- [ ] UIPanel _layer = SCREEN
- [ ] 6 fields assigned (_emailInput, _passwordInput, _confirmPasswordInput, _submitButton, _backButton, _errorText)
- [ ] _errorText GameObject exists (create if missing)
- [ ] No empty field slots showing red errors

---

## Common Issues & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| **"Missing script" error on prefab** | AccountLoginPanel/AccountRegisterPanel script not found or in wrong assembly | Ensure script is in **AccountFeatures** assembly. Check namespace is correct (no namespace prefix needed for public classes) |
| **UIPanel fields show red missing references** | Assigned objects were deleted | Re-assign the fields by selecting the correct child GameObjects |
| **_errorText field always empty** | ErrorText GameObject doesn't exist in prefab | Create new child TextMeshProUGUI, name it "ErrorText", drag into slot |
| **Login/Register button click does nothing** | onClick listener not wired, or wrong method selected | Select button → expand On Click() → ensure AccountLoginPanel.OnLogin() is listed |
| **Null reference when running** | Field assignment shows assigned but object is null at runtime | Check that assigned object has correct component (e.g., TMP_InputField, not TextMeshProUGUI for input fields) |
| **UIManager not finding panel** | UIPanel _identifier not set or mismatched enum value | Check Constants.cs enum UIIdentifier — use exact enum value name |

---

## Next Steps

After completing this wiring:
1. Open **Bootstrap** scene
2. Play the game
3. Verify AccountLogin and AccountRegister screens appear
4. Click buttons and confirm input fields register changes
5. Check Console for any runtime errors
6. If errors occur, refer to Common Issues table above

---

**Date Generated:** 2026-05-04
**Status:** Ready for Editor Wiring
