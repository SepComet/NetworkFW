## 1. CombatEvent Receive Path

- [x] 1.1 Register a client-side `CombatEvent` handler in `Assets/Scripts/NetworkManager.cs` and route parsed authoritative combat events through the existing `NetworkManager -> MasterManager -> Player` gameplay boundary.
- [x] 1.2 Extend the player-management path so authoritative combat events are delivered to the relevant local and/or remote player instance safely, including missing-player guard behavior for MVP diagnostics.

## 2. Player-Owned Combat Application

- [x] 2.1 Add a player-owned client combat-result application path that can consume authoritative `CombatEvent` results and update HP, death state, hit feedback, or equivalent presentation truth without bypassing existing authoritative player ownership.
- [x] 2.2 Keep local fire FX cosmetic by ensuring `ShootRejected`, hit, damage, and death handling come from authoritative `CombatEvent` application rather than speculative local fire presentation.
- [x] 2.3 Expose lightweight MVP UI or debug visibility for recent authoritative combat results, including rejected shots and player damage/death changes where practical.

## 3. Verification

- [x] 3.1 Add or extend edit-mode tests for client `CombatEvent` receive routing and authoritative player-application behavior.
- [x] 3.2 Add or extend regression tests for `ShootRejected` visibility and authoritative HP/death or hit-feedback updates that this change introduces.
- [x] 3.3 Run the relevant validation flow and confirm the client-side `CombatEvent` handling path works in editor play/testing.
