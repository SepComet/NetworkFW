## Context

The shared networking stack already has delivery-policy routing, stale-sequence filtering, and client prediction code, but those behaviors still treat `PlayerInput` as one broad gameplay input message. The MVP in `TODO.md` now requires movement input, shooting input, and authoritative combat results to travel with different semantics: movement is latest-wins sync traffic, while shooting and combat results stay reliable and ordered.

This change is cross-cutting even though it starts from protocol definitions. `MessageType`, generated protobuf classes, delivery policy resolution, stale filtering, and prediction buffering all currently assume that one input message covers both movement and shooting. The generated code also points to `message.proto`, but the repository currently only contains the generated `Assets/Scripts/Network/Defines/Message.cs`, so the design must account for restoring or recreating the schema source as part of the protocol split.

## Goals / Non-Goals

**Goals:**
- Establish distinct shared message contracts for `MoveInput`, `ShootInput`, `CombatEvent`, and the existing authoritative `PlayerState` flow.
- Keep the envelope-based protocol stable while making gameplay message intent explicit in `MessageType` and generated protobuf types.
- Realign sync-policy expectations so only movement/state traffic participates in latest-wins stale filtering and prediction replay.
- Make the first TODO step implementation-ready by identifying the spec and code surfaces that must change together.

**Non-Goals:**
- Implement the later TODO steps that wire both transport instances, update every handler, or add the final message fields beyond what the protocol split requires.
- Replace KCP or redesign the reliable control-plane transport contract.
- Rework unrelated room, chat, login, or lifecycle messages.
- Commit to a protobuf generation toolchain beyond requiring a checked-in schema source and regenerated C# output.

## Decisions

### Split gameplay intent into separate business message types
The shared contract will introduce `MoveInput`, `ShootInput`, and `CombatEvent` as first-class business messages instead of continuing to overload `PlayerInput`. `PlayerState` remains the authoritative state message. This lets routing and stale filtering reason over message intent directly from `MessageType` rather than inspecting a mixed payload shape or carrying unused fields.

Alternative considered: keep `PlayerInput` and add optional fields plus message metadata for delivery policy.
Rejected because it preserves the ambiguous contract that caused the MVP mismatch and keeps routing/filtering logic coupled to one payload type.

### Keep the envelope contract stable while changing only gameplay payload identities
`Envelope.Type` and `Envelope.Payload` remain the shared wire wrapper across hosts. The change happens at the business-message layer: new `MessageType` enum values, new protobuf message definitions, and regenerated C# classes. This keeps client/server interoperability centered on the existing envelope parsing path and avoids a protocol fork between reliable and sync lanes.

Alternative considered: introduce separate envelope formats for sync and reliable traffic.
Rejected because delivery lane is a routing concern, not a serialization concern, and two envelope formats would complicate shared parsing for little benefit.

### Treat protobuf schema source restoration as part of the protocol contract
Because `Assets/Scripts/Network/Defines/Message.cs` was generated from `message.proto` and the source schema is not currently present in the repository, implementation must restore or recreate `message.proto` in source control before regenerating. The checked-in schema becomes the canonical definition for future protocol changes, instead of editing generated C# manually.

Alternative considered: hand-edit `Message.cs` to avoid introducing the missing schema file.
Rejected because generated protobuf output is not a maintainable source of truth and would make the next protocol iteration error-prone.

### Narrow latest-wins sequencing and prediction replay to movement
`MoveInput` and `PlayerState` remain the only messages that participate in stale-drop sequencing and client prediction replay. `ShootInput` and `CombatEvent` stay outside that path because they represent discrete reliable actions/results where silent stale dropping would hide gameplay events.

Alternative considered: let all gameplay messages share the same sequence filter for consistency.
Rejected because reliable shooting/combat messages need ordered delivery semantics, not latest-wins replacement semantics.

## Risks / Trade-offs

- [The repo lacks the checked-in `message.proto` source] -> Mitigation: make schema restoration an explicit implementation task and do not treat generated `Message.cs` as the source of truth.
- [Renaming or removing `PlayerInput` can ripple through existing handlers and tests] -> Mitigation: scope this change around protocol and contract surfaces first, then update routing/filtering/tests in follow-up tasks within the same change.
- [MessageType numeric compatibility could break mixed-version peers] -> Mitigation: preserve existing envelope behavior, document enum changes, and regenerate all shared message code together.
- [Specs may drift from the current TODO ordering if they include later implementation detail] -> Mitigation: keep requirements focused on the message split contract and only the directly dependent routing semantics.

## Migration Plan

1. Add the change artifacts that redefine gameplay message capabilities and modified routing requirements around split message types.
2. Restore or recreate `message.proto` as the canonical schema source, then add `MoveInput`, `ShootInput`, and `CombatEvent` definitions alongside the existing envelope and state messages.
3. Update `MessageType` and regenerate `Assets/Scripts/Network/Defines/Message.cs` from the schema so shared code can reference the new types.
4. Replace `PlayerInput` assumptions in delivery policy resolution, stale filtering, and prediction buffering with message-specific handling.
5. Add or update edit mode tests for routing and stale filtering once the implementation reaches those steps. If rollback is needed, revert the new gameplay message types and route all gameplay input back through the previous `PlayerInput` contract temporarily.

## Open Questions

- Which repository path should own the restored `message.proto` so regeneration stays obvious to future contributors?
- Should `PlayerInput` be removed immediately after callers migrate, or kept briefly as a compatibility shim during implementation?
- Does `CombatEvent` need its event-type enum in this first contract split, or should that remain part of the later message-field finalization step?