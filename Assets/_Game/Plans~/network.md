Given your deadline, here's the pragmatic path.

---

### The Network Runner Subsystem

This is your entry point. Make it a plain Zenject-managed subsystem:

```csharp
public interface INetworkRunnerSubsystem : ISubsystem
{
    Task StartMatchmaking(MatchmakingConfig config);
    void Shutdown();
}

public class NetworkRunnerSubsystem : INetworkRunnerSubsystem
{
    private NetworkRunner _runner;

    public async Task StartMatchmaking(MatchmakingConfig config)
    {
        _runner = new GameObject("NetworkRunner").AddComponent<NetworkRunner>();
        var result = await _runner.StartGame(new StartGameArgs {
            GameMode = GameMode.Shared,
            SessionName = config.SessionName,
            // etc
        });
    }
}
```

Bind it in your SceneContext. Your `MatchmakingPanel` injects `INetworkRunnerSubsystem` and calls `StartMatchmaking`. Done.

---

### Gameplay Subsystems — Given 6 Hours

**Don't try to apply the full networked subsystem pattern to everything.** You won't finish.

Instead, apply it **only at the seams** that matter:

| Feature | What to do |
|---|---|
| Matchmaking | Full subsystem — it's just an API call wrapper |
| Game phase management (Main/Combat/Draw) | One `IGamePhaseSubsystem` that fires events on phase change |
| Hand/card management | Local model + subsystem, no network yet |
| Network state sync | Put `[Networked]` props directly on `NetworkBehaviour`s for now — skip the bridge pattern |

The bridge/seam pattern is the right architecture but it takes time to wire correctly. For gameplay `NetworkBehaviour`s under deadline, it's acceptable to have them call controllers directly via `ProjectContext.Instance.Container.Resolve<>()` in `Spawned()` — you already know it's a seam you can clean up later, and your report documents it as a known tradeoff.

---

### Priority Order

1. `INetworkRunnerSubsystem` → get players into a room
2. `IGamePhaseSubsystem` → drives the turn loop, fires events panels listen to
3. Wire `CombatPanel` / `MainPhasePanel` to those events
4. Handle card play and unit deployment as local controller calls for now

Ship it, then clean the network seam after the deadline.