## 1. Authoritative Combat Core

- [x] 1.1 Add a dedicated server-authoritative combat coordinator and per-peer combat state model under `Assets/Scripts/Network/NetworkHost/`.
- [x] 1.2 Register `ShootInput` handling through the server host/runtime composition path and validate sender-scoped shooting payloads before accepting them.
- [x] 1.3 Resolve accepted shots against managed peers, mutate authoritative HP/death state, and emit reliable `CombatEvent` results including explicit `ShootRejected` responses.

## 2. Runtime And State Integration

- [x] 2.1 Reuse or extend the server-owned authoritative player state model so combat resolution updates the same per-peer state consumed by `PlayerState` broadcast.
- [x] 2.2 Expose the minimal runtime/host surface needed for host processes and tests to inspect authoritative combat state and drive any required combat update hooks.
- [x] 2.3 Preserve per-peer isolation for shoot tick validation, target lookup, and removal/cleanup when sessions disconnect or the runtime stops.

## 3. Regression Coverage And Tracking

- [x] 3.1 Add edit-mode regression tests for accepted versus rejected `ShootInput` handling across multiple peers.
- [x] 3.2 Add edit-mode regression tests for authoritative damage/death resolution, reliable `CombatEvent` broadcast, and HP propagation into later `PlayerState` snapshots.
- [x] 3.3 Update `TODO.md` and related change tracking/docs to reflect the completed server-authoritative shooting/combat resolution work.
