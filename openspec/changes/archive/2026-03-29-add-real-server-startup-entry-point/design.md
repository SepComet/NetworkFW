## Context

The repository already has shared networking building blocks for client and server roles. `NetworkIntegrationFactory.CreateClientRuntime(...)` and `SharedNetworkRuntime` provide a concrete client bootstrap path, while `NetworkIntegrationFactory.CreateServerHost(...)` only returns a `ServerNetworkHost` object with lower-level lifecycle methods such as `StartAsync()`, `Stop()`, `UpdateLifecycle()`, and message draining. That leaves TODO step 6 unfinished because there is still no single integration entry point that represents "boot a real server runtime with configuration, startup, and ownership semantics".

Constraints for this design:
- Shared networking code under `Assets/Scripts/Network/` must remain free of `UnityEngine` dependencies.
- The existing client single-session path must remain unchanged.
- Server hosting must continue to rely on `MultiSessionManager`, `ServerNetworkHost`, and the current transport abstractions instead of introducing a second lifecycle model.
- The result should be testable from edit-mode tests without needing a Unity scene or manual wiring.

## Goals / Non-Goals

**Goals:**
- Introduce a concrete server runtime entry point that can be invoked by a host process with minimal wiring.
- Centralize server startup configuration validation, transport construction, startup sequencing, and shutdown ownership in one place.
- Preserve the current `ServerNetworkHost` as the session/lifecycle owner for connected peers.
- Define a stable integration surface that can later be used by Unity adapters, console hosts, or dedicated-server launchers.
- Add regression coverage for startup success, startup failure, and shutdown cleanup behavior.

**Non-Goals:**
- Redesign `ServerNetworkHost`, `MultiSessionManager`, or transport interfaces.
- Change client runtime behavior or the single-session entry path.
- Introduce authentication, gameplay loop, or headless process packaging concerns beyond the networking startup contract.
- Add Unity-specific MonoBehaviour startup code inside shared networking assemblies.

## Decisions

### Decision: Add a dedicated server runtime entry type above `CreateServerHost`
The change will define a higher-level entry type, centered on a server bootstrap configuration object plus a startup/integration facade, instead of asking callers to directly compose transports and manually coordinate `ServerNetworkHost` lifecycle calls.

Rationale:
- `CreateServerHost(...)` is still a factory for components, not an integration entry point with ownership semantics.
- A dedicated entry type makes startup failure handling, disposal/stop behavior, and future host adapters explicit.
- It mirrors the repository's existing preference for focused types and keeps `ServerNetworkHost` focused on per-peer session orchestration.

Alternatives considered:
- Expand `ServerNetworkHost` to absorb bootstrap concerns: rejected because it mixes runtime assembly and per-session lifecycle responsibilities.
- Leave only `NetworkIntegrationFactory.CreateServerHost(...)` and document manual wiring: rejected because TODO step 6 specifically calls for a real startup/integration entry point.

### Decision: Keep the entry point in shared networking code and expose host-specific integration separately
The entry point contract will live under `Assets/Scripts/Network/` so it can be reused by any host. Unity or other environment-specific launchers may wrap it elsewhere, but they will call into the shared entry contract rather than duplicating bootstrap logic.

Rationale:
- The startup contract is not inherently Unity-specific.
- Tests can validate the shared contract directly.
- This preserves the repository rule against introducing `UnityEngine` into shared networking code.

Alternatives considered:
- Put the entry point only in a Unity-facing adapter: rejected because it would make dedicated-server and CLI-style integration harder and would not strengthen the shared network contract.

### Decision: Model startup as configuration + runtime handle
The design will use an immutable or validation-friendly configuration object for server ports, optional sync port, dispatcher, reconnect policy, time provider, and transport factory overrides. Starting the entry point produces or owns a runtime handle that exposes the underlying `ServerNetworkHost`, startup state, and a single stop/dispose path.

Rationale:
- Separating configuration from runtime ownership makes validation and tests straightforward.
- A runtime handle can guard against double-start, partial-start, and stop-before-start errors.
- This is the minimum structure needed to support a "real server startup" contract without over-designing deployment concerns.

Alternatives considered:
- Static `StartServer(...)` helper returning only `ServerNetworkHost`: rejected because ownership and shutdown semantics remain implicit.
- A fully generic dependency injection container entry flow: rejected as unnecessary for the current repository scope.

### Decision: Make startup and shutdown sequencing explicit and failure-safe
The entry point will be responsible for invoking reliable transport startup first, then sync transport startup if configured, and ensuring partial failures stop any already-started resources. Shutdown will be idempotent and will cascade into `ServerNetworkHost.Stop()` so session cleanup remains owned by existing lifecycle code.

Rationale:
- Startup sequencing already exists in lower-level runtime types; the integration entry point must make it observable and enforceable.
- Failure-safe cleanup prevents leaked transports and stale multi-session state during host bootstrap failures.
- Idempotent stop behavior is important for integration with external host lifecycles.

Alternatives considered:
- Let callers implement their own try/finally around startup: rejected because it recreates the missing integration contract in every host.

## Risks / Trade-offs

- [Risk] A new runtime wrapper could duplicate behavior already present in `ServerNetworkHost` or `NetworkIntegrationFactory`. → Mitigation: keep the wrapper narrowly focused on config validation, ownership, and lifecycle sequencing while delegating per-peer behavior to existing types.
- [Risk] Naming the new entry type poorly could create confusion between factory, host, and runtime responsibilities. → Mitigation: pick a name that clearly indicates bootstrap/integration ownership rather than per-session management.
- [Risk] Delta specs may over-constrain future dedicated-server hosting scenarios. → Mitigation: define minimal contractual guarantees around startup, shutdown, and lifecycle ownership, while leaving environment-specific hosting adapters out of scope.
- [Risk] Tests may only cover happy-path startup. → Mitigation: require regression cases for startup failure rollback and repeated shutdown calls.

## Migration Plan

1. Add the new entry-point/configuration types and wire them to existing `NetworkIntegrationFactory` and `ServerNetworkHost` infrastructure.
2. Add or update edit-mode tests to cover startup sequencing, failure rollback, and shutdown cleanup semantics.
3. Update any TODO/documented integration references to use the new entry point as the recommended server bootstrap path.
4. Keep existing `CreateServerHost(...)` available unless implementation evidence shows it should be downgraded or internally delegated.

Rollback strategy: remove the new entry point and revert callers to direct `CreateServerHost(...)` usage. Because the change is additive, rollback should not require transport or protocol migration.

## Open Questions

- Should the runtime handle implement `IDisposable`/`IAsyncDisposable`, or is an explicit `Stop()`/`StopAsync()` contract sufficient for current usage?
- Should the shared entry point expose a long-running integration loop hook, or only startup/stop plus access to `ServerNetworkHost` for callers that own their own loops?
- Is there an existing external host adapter in this repository that should be updated immediately once the shared entry point exists, or does the initial change stop at shared bootstrap plus tests?
