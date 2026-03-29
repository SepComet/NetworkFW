## Why

The networking MVP still stops at authoritative movement. Clients can already send `ShootInput` and receive `CombatEvent`, but the shared server path does not yet validate shooting, resolve combat outcomes, or drive authoritative HP changes back into the replicated state model. Completing this closes the main gameplay-authority gap in the MVP and prevents combat truth from drifting back to client-side presentation code.

## What Changes

- Add a dedicated shared server-authoritative combat capability that accepts `ShootInput`, validates sender-scoped fire requests, and resolves hit, damage, death, and rejection outcomes on the server.
- Broadcast authoritative `CombatEvent` messages on the reliable lane and keep rejection results explicit instead of silently dropping invalid fire requests.
- Extend server-owned player state so authoritative HP changes produced by combat resolution are reflected in subsequent `PlayerState` snapshots.
- Preserve per-peer isolation for shoot validation and combat bookkeeping in the multi-session server host/runtime path.

## Capabilities

### New Capabilities
- `server-authoritative-combat`: Defines how the shared server path validates `ShootInput`, resolves authoritative combat outcomes, and emits `CombatEvent` results.

### Modified Capabilities
- `server-authoritative-movement`: Expand authoritative `PlayerState` broadcast requirements so combat-driven HP changes are reflected in later snapshots.
- `multi-session-lifecycle`: Clarify that sender-scoped authoritative input validation and combat bookkeeping remain isolated per managed peer.

## Impact

Affected areas include `Assets/Scripts/Network/NetworkHost/`, shared message-routing/runtime composition in `Assets/Scripts/Network/NetworkApplication/`, edit-mode regression tests under `Assets/Tests/EditMode/Network/`, and `TODO.md` / OpenSpec change tracking. No transport contract changes are expected; the work builds on the existing `ShootInput`, `CombatEvent`, and `PlayerState` message types.
