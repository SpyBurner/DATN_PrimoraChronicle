# Dedicated Server Orchestration & Matchmaking Design

This document outlines the high-level architectural design and data flow for a custom matchmaking system integrating with headless dedicated game servers.

## 1. System Components

The architecture consists of four primary components:

*   **Game Client:** The local game application running on the player's device. It handles the user interface, renders the game, captures inputs, and communicates via HTTP for matchmaking and UDP (via network framework) for gameplay.
*   **Backend Orchestrator (Matchmaker):** A central web server handling HTTP requests. It manages the matchmaking queue, evaluates player compatibility, and controls the provisioning of dedicated server containers.
*   **Player Database:** The persistent storage system that holds player profiles, including their current level, ranking, and matchmaking history.
*   **Dedicated Game Server (Headless):** A standalone, physics-authoritative instance of the game running without rendering or UI. It simulates the match, enforces rules, and manages its own lifecycle.

---

## 2. Matchmaking & Provisioning Flow

The process of getting players from the main menu into an active game session follows a strict, four-phase sequence.

### Phase 1: Queuing & Match Selection
1.  **Request:** The Game Client configures their desired game mode and sends a matchmaking request to the Backend Orchestrator.
2.  **Data Retrieval:** The Orchestrator queries the Player Database to retrieve the player's level and ranking metrics.
3.  **Queue Entry:** The player is placed into an active matchmaking pool, ordered by their level.
4.  **Dynamic Evaluation:** The Orchestrator continuously evaluates the pool. It uses a "maximum level difference" threshold to find opponents. If no match is found, this threshold incrementally expands over time (e.g., every 5 seconds) for that specific player.

### Phase 2: The Ready Check
1.  **Match Found:** When two players fall within each other's acceptable level thresholds, the Orchestrator locks them together in a pending state and sends them opponent details.
2.  **Client Prompt:** Both Game Clients display a match-found prompt, requiring the players to explicitly Accept or Reject the match.
3.  **Rejection Handling:** If either player rejects the match (or times out), the Orchestrator sends a failure response to both. 
    *   The rejecting player is removed from the queue.
    *   The accepting player is placed back into the queue. *Crucially, their expanded level threshold is preserved so they do not have to wait from scratch.*

### Phase 3: Server Provisioning
1.  **Confirmation:** If both players explicitly Accept the match, the Orchestrator proceeds to provisioning.
2.  **Session Generation:** The Orchestrator generates a unique, secure Session ID for the match.
3.  **Server Spin-Up:** The Orchestrator commands the host infrastructure to launch a new Dedicated Game Server instance, passing the Session ID and expected player count as startup parameters.

### Phase 4: Game Connection
1.  **Server Initialization:** The Dedicated Server boots up, initializes its networking in server mode using the provided Session ID, and awaits connections.
2.  **Client Handoff:** Once the Orchestrator confirms the server is launching, it sends the Session ID back to both waiting Game Clients.
3.  **Gameplay Begins:** The Game Clients connect directly to the Dedicated Server session using the provided ID. The backend's job for this match is now complete.

---

## 3. Server Lifecycle & Teardown

To prevent resource leaks and ensure accurate match reporting, the Dedicated Server is responsible for terminating itself based on specific triggers, rather than waiting for the Orchestrator to kill it.

### Managed Shutdown Scenarios
The Dedicated Server monitors its own state and triggers a self-shutdown (application quit) under the following conditions:

*   **The No-Show Timeout:** If the server boots up but the expected number of players fails to connect within a strict grace period, the server terminates itself.
*   **Mid-Game Disconnect:** If a player disconnects unexpectedly during the match, the server applies a brief reconnection grace period. If the player fails to return, the server calculates the result (e.g., forfeit), reports the outcome to the Backend Orchestrator, and terminates.
*   **Clean Finish:** When a match concludes normally, the server reports the final results to the Backend Orchestrator, instructs the Game Clients to transition to the post-match screen, and gracefully terminates.

### Failsafe Teardown
While the Dedicated Server manages its own exit, the Backend Orchestrator maintains a passive failsafe. If a server instance remains active beyond a hard time limit (exceeding the maximum possible length of a standard game), the Orchestrator will force-kill the process to prevent frozen instances from consuming infrastructure resources.