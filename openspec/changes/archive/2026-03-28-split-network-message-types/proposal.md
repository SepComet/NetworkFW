## Why

The MVP protocol now needs different delivery semantics for movement, shooting, and authoritative combat results, but the shared message contract still models all player intent as one broad `PlayerInput` type. That coupling blocks the next routing and stale-filtering steps in `TODO.md`, so the protocol must be split now before the sync lane work can be implemented safely.

## What Changes

- Split the gameplay input contract into dedicated `MoveInput`, `ShootInput`, and `CombatEvent` message types instead of overloading `PlayerInput` for both movement and shooting.
- Update the shared message-type enum and protobuf schema so each new gameplay message can be referenced, serialized, and regenerated independently in shared networking code.
- Preserve `PlayerState` as the authoritative state update while redefining sync-policy expectations around `MoveInput` versus reliable ordered expectations around `ShootInput` and `CombatEvent`.
- Retire the MVP assumption that one `PlayerInput` message can satisfy both latest-wins movement traffic and reliable shooting/combat-result traffic.

## Capabilities

### New Capabilities
- `network-gameplay-message-types`: Shared protocol definitions for distinct movement input, shooting input, authoritative player state, and combat event messages used by the MVP gameplay loop.

### Modified Capabilities
- `network-sync-strategy`: Delivery-policy and stale-filter requirements change from broad `PlayerInput` handling to message-specific behavior for `MoveInput`, `ShootInput`, `PlayerState`, and `CombatEvent`.
- `kcp-transport`: Reliable transport requirements now explicitly keep `ShootInput` and `CombatEvent` on the ordered KCP lane while allowing `MoveInput` and `PlayerState` to use the sync lane.
- `shared-network-foundation`: The shared envelope and message-type contract changes so hosts can route and reference split gameplay message types without introducing Unity-specific protocol forks.

## Impact

- Affected code: `Assets/Scripts/Network/Defines/MessageType.cs`, the source `message.proto` used to generate `Assets/Scripts/Network/Defines/Message.cs`, message-routing policy resolvers, sync sequence tracking, and client prediction buffering.
- Affected behavior: movement input becomes an explicitly high-frequency latest-wins message, while shooting requests and authoritative combat results become independently routable reliable messages.
- Affected tests: edit mode networking tests need coverage for split message-type routing, stale filtering that only applies to movement/state traffic, and regression protection against `PlayerInput`-style overloading returning later.