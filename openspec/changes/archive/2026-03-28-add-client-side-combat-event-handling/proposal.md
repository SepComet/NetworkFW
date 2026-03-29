## Why

The MVP now has client input flow, authoritative `PlayerState`, and remote interpolation in place, but the client still has no authoritative receive/apply path for `CombatEvent`. TODO step 5 is the next dependency before server-authoritative combat can feel complete, because clients need one explicit place to consume server-truth hit, damage, death, and shoot-rejected outcomes without turning local fire FX into gameplay authority.

## What Changes

- Add a dedicated client-side combat-event handling capability that defines how the Unity client receives, routes, and applies authoritative `CombatEvent` messages.
- Register a client `CombatEvent` receive path in the gameplay runtime so combat results are delivered to the relevant player or presentation component instead of being ignored.
- Apply authoritative hit, damage, death, and `ShootRejected` outcomes on the client while keeping local muzzle flash or fire FX cosmetic and separate from authoritative gameplay resolution.
- Expose lightweight combat-result diagnostics or UI so MVP development can observe combat events and rejected shots during playtests.

## Capabilities

### New Capabilities
- `client-combat-event-handling`: Defines how the client receives, routes, applies, and exposes authoritative `CombatEvent` results for player presentation and development diagnostics.

### Modified Capabilities
- `client-authoritative-player-state`: Client-side authoritative player presentation expands from `PlayerState`-only updates to also consume authoritative combat outcomes that can change HP, death state, or related presentation truth.

## Impact

- Affected code: `Assets/Scripts/NetworkManager.cs`, `Assets/Scripts/MasterManager.cs`, `Assets/Scripts/Player.cs`, `Assets/Scripts/UI/PlayerUI.cs`, and any new client-side combat presentation helper introduced for routing or diagnostics.
- Affected behavior: The client begins reacting to server-authored `CombatEvent` messages for damage, death, hit feedback, and shot rejection while local fire FX remains cosmetic.
- Testing: Edit-mode regression coverage will need client receive/apply tests for `CombatEvent`, including routing, player-state updates, stale assumptions, and `ShootRejected` visibility.
