## ADDED Requirements

### Requirement: Client-owned authoritative player presentation can consume authoritative combat-result deltas
The client-owned authoritative player presentation model SHALL accept authoritative combat-result updates in addition to full `PlayerState` snapshots. Applying an authoritative `CombatEvent` for a player MUST be able to adjust the client-owned HP, death state, or related combat presentation truth for that player until a newer authoritative `PlayerState` snapshot refreshes the full state.

#### Scenario: Authoritative combat event updates owned player presentation state
- **WHEN** the client applies a `CombatEvent` that targets or otherwise affects a known player
- **THEN** that player's owned authoritative presentation state updates to reflect the authoritative combat result
- **THEN** a later accepted `PlayerState` snapshot remains allowed to refresh the full authoritative state for that player
