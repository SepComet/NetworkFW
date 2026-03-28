## 1. Restore And Split The Shared Schema

- [x] 1.1 Restore or recreate the checked-in `message.proto` source file that generates `Assets/Scripts/Network/Defines/Message.cs`.
- [x] 1.2 Add `MoveInput`, `ShootInput`, and `CombatEvent` protobuf message definitions and stop modeling both movement and shooting through one broad `PlayerInput` schema.
- [x] 1.3 Update `Assets/Scripts/Network/Defines/MessageType.cs` so split gameplay messages have distinct enum values aligned with the shared envelope contract.
- [x] 1.4 Regenerate `Assets/Scripts/Network/Defines/Message.cs` from the updated protobuf schema and verify the generated types are checked in.

## 2. Realign Shared Runtime Message Semantics

- [x] 2.1 Update delivery-policy resolution so `MoveInput` and `PlayerState` map to the sync lane while `ShootInput` and `CombatEvent` stay reliable ordered.
- [x] 2.2 Update sync sequence tracking so stale-drop logic applies to `MoveInput` and `PlayerState` but not to `ShootInput` or `CombatEvent`.
- [x] 2.3 Narrow `ClientPredictionBuffer` and related callers to record and replay `MoveInput` only.
- [x] 2.4 Replace remaining shared-network references to broad `PlayerInput` intent with the new split gameplay message types.

## 3. Verify The Split Contract

- [x] 3.1 Extend edit mode networking tests to cover split message routing and stale filtering behavior for `MoveInput`, `ShootInput`, and `CombatEvent`.
- [x] 3.2 Build `Network.EditMode.Tests.csproj` and run `dotnet test Network.EditMode.Tests.csproj --no-build -v minimal`.
- [x] 3.3 Update `TODO.md` or related implementation notes to reflect completion of the split-message-types step if behavior changed during implementation.