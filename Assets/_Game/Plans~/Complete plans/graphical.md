# Graphical Atmosphere Plan: Gloomy Magical Fantasy

Step-by-step guide to achieving a dark, premium, and magical aesthetic for Primora Chronicle.

## 1. Scene Lighting & Environment
*   **Ambient Lighting**: Set Environment Lighting to `Gradient`. 
    *   Sky: Dark Navy (#050510)
    *   Equator: Deep Purple (#100515)
    *   Ground: Black (#000000)
*   **Fog**: Enable `Exponential Squared` fog.
    *   Color: Dark Grey/Purple (#0A0A0F)
    *   Density: High enough to obscure the grid edges (0.015 - 0.02).
*   **Main Light**: Low intensity (0.5), cool color temperature (8000K). Cast soft shadows.
*   **Rim Lighting**: Add a custom shader feature or global property to give all Units a subtle magical rim light (Teal/Cyan) so they remain visible in the dark environment.

## 2. Post-Processing Volume (URP)
Setup a global `Volume` with the following overrides:
1. **Bloom**: High threshold (1.0), high intensity (1.5), Soft Knee (0.5). Used for magical glowing elements (runes, spell effects).
2. **Vignette**: Intensity (0.45), Smoothness (0.3). Create a focused "dream-like" view.
3. **Color Grading (Tonemapping: ACES)**:
    *   **Lift**: Slight Blue tint.
    *   **Gain**: Slight Purple/Teal tint.
    *   **Contrast**: Increase slightly to make magic glows pop against the gloom.
4. **Motion Blur**: Low intensity to smooth out unit movements and attacks.
5. **Film Grain**: Thin, low-intensity grain to give a cinematic "dark fantasy" texture.

## 3. Magical Visual Effects (VFX)
*   **The Grid**: Hex tiles should have a "faint pulse" animation. 
    *   `Idle`: 10% opacity cyan glow.
    *   `Hover`: 50% opacity purple glow with rising particles.
*   **Units**: 
    *   Hollow: Black smoke/mist particles trailing from joints.
    *   Ashen: Ember sparks and heat distortion.
    *   Verdant: Glowing teal leaves/vines.
*   **Spells**: High-energy "Impact" particles. Use Additive shaders for maximum brightness.

## 4. Implementation Steps (Chronological)
1.  **[Step 1]**: Create `GameplayGraphicsVolume` in the Gameplay scene.
2.  **[Step 2]**: Import/Create a custom `Skybox` with a dark nebulous texture.
3.  **[Step 3]**: Implement the `AtmosphereManager` script to transition lighting between Lobby (softer, magical) and Gameplay (darker, gloomier).
4.  **[Step 4]**: Create a base `MagicalUnit` shader in Shader Graph that supports Rim Lighting and Dissolve (for death animations).
5.  **[Step 5]**: Setup `Volumetric Lighting` for the Main Light to create "God Rays" through the fog.

---
*Target Aesthetic: Diablo 4 meets Hearthstone (Shadowreaper Anduin expansion style).*
