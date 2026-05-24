# T6 — Hosting Strategy (Supabase + Render + Headless GDS)

> The user's planned stack: **Supabase** (DB), **Render** (BE web service),
> and a separate path for the **headless dedicated game server** because
> Supabase doesn't run binaries and Render's standard web services don't
> handle long-lived UDP game sessions. This doc lays out the candidates,
> the trade-offs, and a recommendation.

---

## 0. What we're hosting

| Component | Process shape | Network shape | Hosting needs |
|---|---|---|---|
| Postgres | DB | TCP 5432 | Managed Postgres |
| GDS (Streamlit + FastAPI) | Python container | HTTP 8001 / 8501 | Container web service |
| BE / Orchestrator (FastAPI) | Python container | HTTP 8000 | Container web service, outbound HTTPS |
| **Headless Game Server (Unity batchmode)** | Native Linux binary, spawned per match | UDP (Photon Fusion) **and** HTTP receive on 8080 | On-demand spawn, public IP/UDP, ~1–3 GB RAM, 60 s–10 min lifetime |

Everything except the headless GDS slots into "normal SaaS hosting". The
headless GDS is the actual problem.

---

## 1. Supabase

Use it for **Postgres only**. Replace the `db` service in
`docker-compose.yml` with `DATABASE_URL = <supabase pooled connection string>`.

| Pros | Cons |
|---|---|
| Free tier covers thesis demo traffic | No backup story past 7 days |
| Built-in auth (could replace mock JWT later — not in scope) | Adds external dependency for local dev (mitigate with docker postgres for local) |
| Realtime channels if we ever need lobby chat | Cold start latency on the free tier |

**Migration steps (when the time comes):**
1. Create Supabase project, copy pooled URL (port 6543).
2. Run `alembic upgrade head` (or whatever migration tool — confirm; currently uses `Base.metadata.create_all`).
3. Set `DATABASE_URL` env on Render.
4. Drop the `db` service from docker-compose. Keep it commented for local dev.

---

## 2. Render — works for BE + GDS, not for headless

Render's "Web Service" deploys a container, gives a public HTTPS endpoint, and
auto-restarts on crash. Perfect for the FastAPI BE and the GDS.

| Service | Render plan | Notes |
|---|---|---|
| BE (TestBE) | Web Service, free or Starter | Set env vars: `DATABASE_URL`, `GDS_URL`, `GDS_APP_SECRET`, `SERVER_SECRET`, `HEADLESS_PROVIDER`, etc. |
| GDS | Web Service, free | Public endpoint required so the BE can pull cards. Lock behind `X-App-Secret` header (already implemented) |

Render's **limitations** for our use:
- No UDP, only HTTPS — eliminates Render Web Service for the game server.
- Background workers exist but run continuously, not one-process-per-match.
- The free tier sleeps services after 15 min idle — fine for GDS, OK for BE
  during demo (warm with a `/health` ping).

So Render hosts the orchestrator and database access. Headless lives
elsewhere — see §3.

---

## 3. Headless server — the four real options

### Option A — Photon Cloud relay only, no dedicated process

Photon Fusion 2 supports a relay-only mode where ANY connected peer can be
the host. We could keep the current `GameMode.Host` model and skip the
dedicated server entirely.

| ✅ | ❌ |
|---|---|
| Zero infra cost | One peer is authoritative → cheats are possible (the committee's Q2 concern) |
| Already works today | Defeats the purpose of T4/T5 |
| Free for thesis-scale traffic | Loss of one player = loss of host → migration headaches |

**Verdict:** acceptable as a fallback for the demo if §B–§D all fall through,
but the thesis pitch ("we have a server-authoritative architecture") is
weaker.

### Option B — Fly.io Machines API (on-demand)

Fly.io's "Machines" can be created via REST API in ~2 s, run for a defined
time, and auto-stop. Each match = one new machine.

| ✅ | ❌ |
|---|---|
| Pay only for compute used (matches finish quickly) | Adds Fly.io as a third vendor |
| Native UDP, native IPv6, public IP per machine | Cold-start ~2 s — clients see "Connecting" for that window (retry loop in T5 §2.3 covers this) |
| Linux x86_64 — Unity Server build target supported | Need to publish the headless build as a Docker image and push to Fly's registry |
| One global region option for thesis demo | Free tier limited; thesis scale fits, prod doesn't |

Workflow:
```
BE.spawn_headless(session_name, player1, player2):
    POST https://api.machines.dev/v1/apps/primora-gds/machines
      body: { config: { image: "registry.fly.io/primora-gds:latest",
                        env: { SESSION_NAME, PLAYER_1, PLAYER_2, SERVER_SECRET },
                        auto_destroy: true,
                        guest: { cpu_kind: "shared", cpus: 1, memory_mb: 1024 } } }
    → returns machine_id + public IPv4
    → BE polls health endpoint on the new machine for ~10 s
    → BE returns session_name to clients
```

**Verdict (recommended).** Best fit: matches our event-driven shape, doesn't
need always-on infra, and the cost model degrades gracefully past thesis.

### Option C — Self-hosted VM (Hetzner / DigitalOcean) + spawn supervisor

Rent one small VM. Run a "spawner" daemon that listens for spawn requests
from the BE and forks a fresh `unity-server.x86_64` process per match.

| ✅ | ❌ |
|---|---|
| Cheapest predictable bill ($4–6/mo) | One VM caps concurrent matches (each match ~1 GB RAM, ~50% CPU) |
| Full control; trivial to attach debugger / `tcpdump` | We have to write and operate the spawner daemon |
| Same Linux binary builds as Option B | One VM = one machine = a single point of failure |

A bare spawner is ~80 lines of Python — listen HTTP, fork, track PIDs, return
public port. We'd need to expose UDP ports per-match (assign a port range,
4000–4500, hand one out per spawn).

**Verdict:** strong runner-up. Pick this if Fly.io introduces friction. Also
the best option for **local development** of T4/T5 — run the spawner on
localhost during dev so we don't burn cloud minutes.

### Option D — Kubernetes job per match (GKE / AKS / DOKS)

Each match = one short-lived Kubernetes Job.

**Verdict:** rejected. Overkill for thesis scope. Mention it in the report's
"future work" section if asked.

---

## 4. Recommended end-state architecture

```
                          ┌──────────────────────────────┐
                          │ Supabase Postgres            │
                          └──────────────▲───────────────┘
                                         │ pooled TCP
                                         │
   ┌────────┐     HTTPS     ┌────────────┴────────────┐
   │ Client │ ─────────────►│ Render: BE (FastAPI)    │
   │ (Unity)│  /matchmaking │                         │
   └───┬────┘ ◄──────────── │  • queue + ready check  │
       │                     │  • spawn API caller     │
       │  Photon UDP         └──────┬──────────────────┘
       │  (session_name)            │ HTTPS POST machine.create
       │                            ▼
       │                    ┌──────────────┐
       │                    │ Fly.io       │
       │                    │ Machines API │
       │                    └──────┬───────┘
       │                           │ spawn
       │                           ▼
       │                    ┌────────────────────────┐
       └──────UDP───────────► Headless Unity server  │
                            │ (one machine per match)│
                            └─────────┬──────────────┘
                                      │ HTTPS POST match.result
                                      │
                            ┌─────────▼──────────┐
                            │ Render: BE         │
                            └────────────────────┘
                            ┌────────────────────┐
                            │ Render: GDS        │ pulled at server boot
                            └────────────────────┘
```

Total monthly cost at thesis scale (~20 matches/day): well under $10. Most
services sit on free tiers.

---

## 5. Build pipeline (CI)

To deploy headless to Fly.io we need a **server-target Unity build** in a
Docker image. The simplest pipeline:

| Step | Tool |
|---|---|
| 1. Build Linux Server target | Unity Cloud Build, or GitHub Actions with `game-ci/unity-builder` action (free, ~10 min build) |
| 2. Wrap binary in Docker image | `FROM debian:bookworm-slim` + copy server build + `ENTRYPOINT ["./primora.x86_64", "-batchmode", "-nographics", "-headless"]` |
| 3. Push image to Fly registry | `flyctl deploy --no-deploy` (image only) |
| 4. BE updates `IMAGE_TAG` env var when calling Machines API | Latest image used on next match |

Note: Unity's Server build target is a separate build target (set in Build
Settings). Configure it as a CI matrix alongside the Standalone target.

---

## 6. Local-dev story (must stay simple)

For day-to-day work, we don't want to deploy to Fly. Two modes:

1. **In-editor playtest** — unchanged. `GameMode.Host` + a second client in
   another editor instance.
2. **Local headless test** — build the server target once to
   `Builds/Server/primora.x86_64`, then either:
   - Run it directly: `./primora.x86_64 -batchmode -nographics -headless -session devtest`
   - Or run the docker-compose stack with an added `gds-headless` service
     that runs the binary as PID 1, exposing UDP 27015.

Add a simple shell script `tools/dev/run-headless.sh` that boots BE +
postgres + GDS + a single headless instance for end-to-end smoke.

---

## 7. Concrete env vars (single source of truth)

These are referenced by BE/Unity code today and will continue to be:

| Var | Used by | Set on |
|---|---|---|
| `DATABASE_URL` | BE | Render |
| `GDS_URL` | BE | Render |
| `GDS_APP_SECRET` | BE | Render + GDS |
| `HEADLESS_PROVIDER` | BE | Render — value `fly` or `localhost` |
| `FLY_API_TOKEN` | BE | Render (secret) |
| `FLY_APP_NAME` | BE | Render |
| `SERVER_SECRET` | BE + Unity headless | Render + Fly env on machine create |
| `PRIMORA_LOG_DIR` | Unity client + headless | Fly env per machine (mounts ephemeral volume) |
| `SESSION_NAME`, `PLAYER_1_ID`, `PLAYER_2_ID`, `REGION_CODE` | Unity headless | Fly machine env at spawn |

---

## 8. What this means for the thesis report

In the architecture chapter, write the diagram from §4 once. In the
"infrastructure" appendix, explain:

1. Stateless services on Render (free tier suffices for demo load).
2. Stateful storage on Supabase Postgres.
3. **On-demand UDP servers** on Fly.io Machines — this is the answer to
   committee Q1 ("why two servers?"). The orchestrator (BE) lives long;
   game servers live per-match. Not redundancy, not over-engineering — it's
   the only way to do server-authoritative networking without burning a VM
   permanently.

If Q2 (host-cheats) comes up: "the dedicated headless server has
StateAuthority over every NetworkObject. Clients have InputAuthority only.
This is what justified the GameMode.Server bootstrap path in T4."

---

## 9. Decision points still owed to the user

1. Fly.io or self-hosted VM? Default to **Fly.io** unless billing concerns
   push us to VM.
2. Mock auth or real Supabase Auth? Default to keeping **mock auth** for
   thesis; mention Supabase Auth as future work.
3. Is the BE allowed to phone Photon Cloud to register match results (e.g.,
   webhooks) or is HTTP-only enough? **HTTP-only is sufficient** — Photon
   doesn't need to know about our match history.
