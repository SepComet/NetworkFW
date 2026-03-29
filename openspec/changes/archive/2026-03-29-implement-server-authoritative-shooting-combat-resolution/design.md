## Context

The repository now has a concrete server runtime entry point and a shared `ServerNetworkHost` that already owns per-peer authoritative movement state and fixed-cadence `PlayerState` broadcast. Clients can send `ShootInput` and apply inbound `CombatEvent`, but no shared server path currently accepts `ShootInput` or mutates authoritative HP from combat. The networking layer also has an explicit split between high-frequency sync traffic and reliable gameplay events, so combat resolution needs to fit the existing message contracts instead of introducing another transport abstraction.

A second constraint is code placement: shared networking code under `Assets/Scripts/Network/` must remain Unity-free. That rules out designs that depend on scene physics or Unity colliders inside the shared server path. The MVP therefore needs a deterministic server combat loop that can be exercised by edit-mode tests and owned entirely by shared host/runtime code.

## Goals / Non-Goals

**Goals:**
- Add a shared server combat coordinator that registers `ShootInput` on `ServerNetworkHost` and validates each request against the sending peer.
- Keep combat ownership server-side: authoritative HP, damage, death, and shoot rejection are resolved in the server host path and never inferred from client presentation.
- Reuse existing message contracts by emitting `CombatEvent` on the reliable lane and folding resulting HP into later `PlayerState` snapshots.
- Preserve per-peer isolation so one sender's shoot tick history or invalid payload cannot corrupt another managed session's combat state.
- Keep the MVP implementation deterministic and easy to regression test from edit-mode tests.

**Non-Goals:**
- Full projectile simulation, lag compensation, or physics-based hit scanning.
- Unity-scene integration, VFX timing, or client cosmetic preplay logic.
- Advanced anti-cheat policy beyond sender identity validation, stale filtering, and basic target eligibility checks.
- Broader gameplay systems such as respawn, inventory, or weapon-specific balance rules.

## Decisions

### Decision: Use a dedicated server-authoritative combat coordinator beside movement authority
The server already centralizes movement authority inside `ServerAuthoritativeMovementCoordinator`. Combat should follow the same pattern with a focused coordinator that is constructed by `ServerNetworkHost`, owns per-peer combat bookkeeping, and exposes only the minimal inspection/update hooks needed by runtime code and tests.

This keeps combat logic out of `MessageManager` and avoids overloading `MultiSessionManager` with gameplay concerns. An alternative was to fold shooting directly into `ServerNetworkHost`, but that would make host orchestration responsible for validation rules, state mutation, and message emission simultaneously.

### Decision: Define the MVP hit model around sender-scoped validation plus explicit target lookup
The shared server path will treat `ShootInput` as a request to attack a specific managed peer identified by `targetId`. A request is accepted only when the sender maps to a managed authoritative player state, the `playerId` matches that peer, the tick is newer than the sender's last accepted shoot tick, the aim vector is finite and non-zero, and the target resolves to another managed peer that is still alive.

This deliberately favors a narrow target-based authority model over scene-geometry hit checks. The alternative was to leave hit validation abstract or physics-driven, but that would either make the spec untestable or force Unity-only dependencies into shared networking code.

### Decision: Emit one reliable `CombatEvent` per authoritative combat outcome and keep rejection explicit
When a valid shot is accepted, the server will apply deterministic combat resolution immediately and emit authoritative `CombatEvent` messages through the existing reliable-lane contract. Accepted shots may produce `Hit`, `DamageApplied`, and `Death` events as needed; invalid shots produce `ShootRejected` instead of being dropped silently.

Keeping rejection explicit improves observability and aligns with the current client-side `CombatEvent` handling path. An alternative was to let invalid fire requests disappear without a response, but that would make client/server divergence harder to diagnose.

### Decision: Keep authoritative HP in the shared player state model rather than a separate combat-only snapshot
The movement authority work already introduced server-owned per-peer `PlayerState` snapshots. Combat resolution should update the same server-owned state so later `PlayerState` broadcasts naturally include the current authoritative HP alongside position, rotation, velocity, and tick.

The alternative was a separate combat-state store plus ad hoc synchronization into `PlayerState`, but that creates two competing sources of truth for the same player's HP.

## Risks / Trade-offs

- [Risk] The target-id-based MVP hit model is less realistic than spatial hit detection. → Mitigation: document it as an MVP constraint and keep the coordinator boundary narrow so later geometry-aware validation can replace only the acceptance rule.
- [Risk] Emitting multiple `CombatEvent` messages for one accepted shot increases event volume on the reliable lane. → Mitigation: keep the event set minimal and reserve it for state-changing outcomes (`Hit`, `DamageApplied`, `Death`, `ShootRejected`).
- [Risk] HP updates now come from both movement snapshots and combat resolution paths. → Mitigation: make combat mutate the same authoritative player-state object that movement broadcast already reads.
- [Risk] Reliable ordered shooting requests do not need stale filtering as aggressively as sync traffic, but duplicate or out-of-order resends could still replay damage. → Mitigation: keep a per-peer last accepted shoot tick and reject non-increasing ticks for that sender.
