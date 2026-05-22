Here is the concise, agent-ready design document. You can copy and paste this directly into your IDE or AI prompt to set the context before asking it to write any scripts.

---

# System Design: Headless Orchestration & Benchmarking

## 1. Objective

Design an automated, UI-independent testing harness to validate the MVVM architecture's decoupling. The system will execute and benchmark core gameplay and meta-game subsystems directly via their internal APIs, completely bypassing the View (UI) layer.

## 2. Core Components

* **Headless Bootstrapper:** A lightweight entry point that initializes the Service Locator/Dependency Injection and instantiates necessary ViewModels and Subsystems without waiting for Scene UI events.
* **Simulation Orchestrator:** The master controller. It programmatically simulates user flows (e.g., triggering deck saves, shop purchases, and grid generation) in rapid succession.
* **Telemetry Logger:** A dedicated logging service that records execution times and system states, outputting exclusively in a structured CSV string format to the Unity Console.

## 3. Execution Phases

1. **Phase 1: System Initialization:** Bind all required data models, subsystems, and ViewModels. Start absolute telemetry timer.
2. **Phase 2: Meta-Game Stress Test:** Execute programmatic loops for Shop transactions and Deck Builder payload serialization/deserialization.
3. **Phase 3: Core Logic Test:** Execute spatial/gameplay logic (e.g., Hex Grid generation iterations, AI pathfinding checks).
4. **Phase 4: Teardown & Export:** Conclude the simulation, finalize the stopwatch, and halt execution.

## 4. Strict Agent Directives & Constraints

* **Zero View Dependency:** The implementation must NOT reference UnityEngine.UI, UnityEngine.UIElements, TMPro, or any visual Canvas components.
* **Null-Safe Bypassing:** Any pre-existing event hooks intended for the UI must be handled via safe null-checks (the "Duct Tape" pattern) rather than architectural rewrites.
* **High-Precision Timing:** Benchmarking must use System.Diagnostics.Stopwatch for absolute millisecond tracking. Do NOT use Time.time or Unity frame-dependent timers.
* **Strict CSV Logging Schema:** All outputs from the Telemetry Logger must adhere to this exact string format for post-processing:
Timestamp_MS, LogCode, Subsystem, Elapsed_Time_MS, Message

---