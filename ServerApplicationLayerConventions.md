# Server Application Layer Conventions

## Purpose

This document describes the contracts that the server application layer must follow when built on top of the shared networking layer under `Assets/Scripts/Network/`.

The goal is to keep the server-side gameplay/application code aligned with the existing dual-lane transport, session lifecycle, tick filtering, and metrics model.

## Scope Boundary

- Shared networking concerns belong in `Assets/Scripts/Network/`.
- Server application concerns should sit above the shared network layer and consume it through `ServerNetworkHost`, `ServerRuntimeEntryPoint`, `ServerRuntimeHandle`, `MessageManager`, and the authoritative coordinators.
- Do not move Unity-only logic into shared network code.
- Do not make the shared network layer depend on scene objects, MonoBehaviours, or Unity presentation state.

## Startup Contract

- Start the dedicated server through `ServerRuntimeEntryPoint.StartAsync(...)` or `NetworkIntegrationFactory.CreateServerHost(...)`.
- Configure distinct reliable and sync ports when dual-lane behavior is required.
- Do not bind reliable and sync traffic to the same port when the intention is to validate mixed sync behavior. `NetworkIntegrationFactory` treats identical ports as invalid.
- Validate server-side tuning through `ServerRuntimeConfiguration`, `ServerAuthoritativeMovementConfiguration`, and `ServerAuthoritativeCombatConfiguration` instead of ad hoc constants spread across gameplay code.

## Session Lifecycle Contract

The server application layer is responsible for driving session state transitions at the correct time.

- Call `NotifyLoginStarted(remoteEndPoint)` when login processing begins.
- Call `NotifyLoginSucceeded(remoteEndPoint, playerId)` after the login has been accepted and the peer identity is known.
- Call `NotifyLoginFailed(remoteEndPoint, reason)` when login is rejected.
- Call `NotifyHeartbeatReceived(remoteEndPoint, serverTick)` when a heartbeat request is accepted and answered.
- Call `NotifyInboundActivity(remoteEndPoint)` only for accepted peer traffic that should refresh liveness.
- Call `RemoveSession(remoteEndPoint, reason)` when the player disconnects, logs out, times out permanently, or is forcefully removed.

Important implications:

- `NotifyLoginSucceeded(remoteEndPoint, playerId)` is not just bookkeeping. It also bootstraps the peer's authoritative movement state through `ServerNetworkHost`.
- If the application layer forgets to send login success into the host, a freshly logged-in idle player may never receive initial authoritative state and may fail later gameplay assumptions.
- Removing a session through the host also clears authoritative movement state, combat state, and remembered player identity for that peer.

## Message Routing Contract

The application layer must preserve the shared lane mapping.

- `MoveInput` uses `DeliveryPolicy.HighFrequencySync`.
- `PlayerState` uses `DeliveryPolicy.HighFrequencySync`.
- `ShootInput` uses `DeliveryPolicy.ReliableOrdered`.
- `CombatEvent` uses `DeliveryPolicy.ReliableOrdered`.
- Any new high-frequency "latest wins" snapshot-like message must be explicitly mapped to the sync lane.
- Any gameplay event that must not be dropped must remain on the reliable ordered lane.

Do not collapse movement and combat intent back into one broad input message if they require different delivery behavior.

## Envelope And Handler Contract

- All inbound and outbound gameplay messages must go through `MessageManager`.
- Do not bypass `MessageManager` by writing transport-specific send logic in application code.
- Register handlers on `MessageManager` using `MessageType`.
- Assume the shared layer always wraps payloads in `Envelope`.

Implications for server application code:

- If you add a new gameplay message, you need both a protobuf definition and a handler registration path.
- Broadcasts should use `MessageManager.BroadcastMessage(...)`.
- Directed replies should use `MessageManager.SendMessage(..., target)`.

## Tick And Sequence Contract

The shared sync filter currently applies stale-packet rejection to:

- `MoveInput`, keyed by sender + playerId + tick.
- `PlayerState`, keyed by playerId + tick.

Server application rules:

- Treat `tick` as required for all gameplay-relevant messages.
- Keep `MoveInput.Tick` monotonic per player.
- Keep `PlayerState.Tick` monotonic per player.
- Do not expect stale `MoveInput` packets to reach gameplay handlers.
- Do not add stale filtering assumptions for reliable gameplay events unless the shared layer is explicitly extended for that purpose.

## Authoritative Ownership Contract

The current shared/server runtime assumes the server application layer owns gameplay truth.

Server authoritative data includes:

- final position
- final rotation used for authoritative state
- HP
- accepted or rejected shooting
- hit resolution
- death state

Application-layer rules:

- Clients may submit intent only; they must not finalize gameplay outcomes.
- The server should apply `MoveInput` and `ShootInput` as requests, not as trusted outcomes.
- Authoritative snapshots sent back to clients must be derived from server state, not echoed from client state.

## Movement Contract

The movement-side server application layer should follow these rules:

- Ensure every logged-in player has authoritative movement state, even before the first non-zero `MoveInput`.
- Accept zero-vector `MoveInput` as valid current intent.
- Keep authoritative movement progression on the server, not on the client.
- Broadcast authoritative `PlayerState` on a fixed interval using server-owned timing.
- Include `position`, `rotation`, `hp`, and `velocity` when building `PlayerState`.

Do not assume a player who has not moved yet can be omitted from authoritative broadcast state.

## Combat Contract

The combat-side server application layer should follow these rules:

- Treat `ShootInput` as a reliable gameplay request.
- Validate shooter identity against the sending peer.
- Reject shots from dead players or from stale shoot ticks.
- Support both explicit-target and aim-based resolution if `targetId` is optional in the gameplay contract.
- Apply damage and death server-side before broadcasting combat results.
- Emit `CombatEvent` as the only authoritative combat result stream consumed by clients.

Current expected `CombatEvent` usage:

- `Hit` for resolved contact.
- `DamageApplied` for HP loss.
- `Death` for kill resolution.
- `ShootRejected` for invalid fire requests.

## Broadcast Contract

- Use `BroadcastMessage(PlayerState, MessageType.PlayerState)` for authoritative state snapshots.
- Use `BroadcastMessage(CombatEvent, MessageType.CombatEvent)` for authoritative combat outcomes.
- Do not use per-client divergent truth for shared gameplay state unless the design explicitly requires private information.

If a message represents common world truth, broadcast one authoritative result instead of recomputing or customizing it client by client.

## Metrics Contract

The transport layer already supports metrics and diagnostics capture. The server application layer should preserve that signal instead of bypassing it.

- Use transports that implement the existing metrics sink path when running diagnostics or bad-network tests.
- Keep session transitions routed through `ServerNetworkHost` so transport/application snapshots remain coherent.
- Record login success, heartbeat activity, disconnects, and authoritative tick progress through the host/runtime APIs rather than out-of-band state machines.
- When testing with tools like Clumsy, compare sync-lane degradation and reliable-lane survivability using the generated transport reports instead of subjective observation alone.

Practical expectation:

- sync lane may degrade in freshness under packet loss or jitter
- reliable lane may pay retransmission and latency cost
- gameplay semantics on the reliable lane should still remain correct and ordered

## Dispatcher Contract

- Client code may use deferred dispatchers such as `MainThreadNetworkDispatcher`.
- Server code defaults to immediate dispatch through `ServerNetworkHost`.
- If the server application layer injects a custom dispatcher, it must preserve deterministic handling expectations and must not accidentally require Unity main-thread semantics.

Do not introduce a Unity main-thread dependency into the dedicated server path.

## Identity Contract

- The authoritative identity for a peer is the combination of endpoint and accepted player id.
- Login handling must establish that mapping before gameplay is allowed to proceed.
- Any gameplay message whose `playerId` does not match the accepted identity for the sender should be treated as invalid.

This is especially important for:

- `MoveInput`
- `ShootInput`
- future player-owned gameplay messages

## Extending The Message Set

When adding a new gameplay message, the server application layer should answer all of the following before implementation:

1. Is this a sync message or a reliable gameplay event?
2. Does it require monotonic tick semantics?
3. Does stale filtering apply?
4. Is the message peer-owned input, server-owned state, or server-owned event?
5. Is the message broadcast world truth or a directed reply?
6. What metrics should confirm correct behavior under packet loss, latency, and jitter?

If these answers are unclear, the message design is not ready.

## Test Contract

Any server application-layer change that touches gameplay networking should add or update edit-mode regression coverage.

Minimum expectations:

- one test for lane routing if delivery behavior changes
- one test for authoritative server acceptance/rejection behavior
- one test for client-visible end-to-end outcome when the gameplay flow changes
- one test for a realistic edge case if the change fixes a regression

Examples of edge cases worth preserving:

- idle logged-in player can still receive `PlayerState`
- idle logged-in player can still shoot
- stale `MoveInput` is ignored
- `ShootInput` without `targetId` still resolves correctly when aim-based targeting is intended

## Anti-Patterns To Avoid

- Bypassing `MessageManager` and writing directly to `ITransport`.
- Letting the client authoritatively decide damage, death, or final position.
- Treating login/session state as a parallel system disconnected from `ServerNetworkHost`.
- Emitting authoritative gameplay state before the server has established peer identity.
- Depending on Unity scene state inside shared network code.
- Adding new gameplay messages without deciding their lane and tick behavior.
- Passing bad or missing player identity into `NotifyLoginSucceeded`.
- Forgetting to remove authoritative state when the session is removed.

## Checklist For New Server Application Features

- define protobuf message fields and ownership clearly
- choose reliable lane or sync lane explicitly
- decide tick semantics explicitly
- register handlers through `MessageManager`
- validate sender identity against accepted session identity
- update authoritative server state only on the server
- emit `PlayerState` or `CombatEvent` style outputs as needed
- route login/heartbeat/disconnect transitions through `ServerNetworkHost`
- verify metrics remain readable under Clumsy or equivalent bad-network simulation
- add regression tests

## Current Reference Types

- `Assets/Scripts/Network/NetworkApplication/MessageManager.cs`
- `Assets/Scripts/Network/NetworkApplication/DefaultMessageDeliveryPolicyResolver.cs`
- `Assets/Scripts/Network/NetworkApplication/SyncSequenceTracker.cs`
- `Assets/Scripts/Network/NetworkApplication/NetworkIntegrationFactory.cs`
- `Assets/Scripts/Network/NetworkHost/ServerNetworkHost.cs`
- `Assets/Scripts/Network/NetworkHost/ServerRuntimeEntryPoint.cs`
- `Assets/Scripts/Network/NetworkHost/ServerRuntimeConfiguration.cs`
- `Assets/Scripts/Network/NetworkHost/ServerAuthoritativeMovementCoordinator.cs`
- `Assets/Scripts/Network/NetworkHost/ServerAuthoritativeCombatCoordinator.cs`
