# client-combat-event-handling Specification

## Purpose
Define how the Unity client receives, routes, applies, and exposes authoritative `CombatEvent` results for player presentation and development diagnostics.

## Requirements
### Requirement: Client registers and routes authoritative combat events to player-owned presentation logic
The Unity client SHALL register a `CombatEvent` receive path in the gameplay runtime and route accepted authoritative combat results to the relevant player-owned presentation logic instead of ignoring them or applying them directly in transport-facing code.

#### Scenario: Incoming combat event is forwarded through the client gameplay runtime
- **WHEN** the client receives a `CombatEvent` message from the server
- **THEN** the gameplay receive path parses the message and routes it through the existing client player-management boundary
- **THEN** the relevant player-owned presentation logic receives the authoritative combat result for application or diagnostics

### Requirement: Client applies authoritative hit, damage, death, and rejection outcomes without making local fire FX authoritative
The Unity client SHALL apply authoritative `CombatEvent` outcomes for hit, damage, death, and `ShootRejected` as server-truth gameplay results. Any local muzzle flash, fire animation, or similar client fire FX MUST remain cosmetic and MUST NOT decide final damage, hit, death, or rejection outcomes before the authoritative event arrives.

#### Scenario: Damage or death event updates authoritative client combat presentation
- **WHEN** the client applies an authoritative `CombatEvent` whose `eventType` is `DamageApplied` or `Death`
- **THEN** the relevant player updates HP, death state, hit feedback, or comparable combat presentation from that authoritative event
- **THEN** the client does not wait for speculative local combat logic to decide that outcome

#### Scenario: Shoot rejection is surfaced without speculative rollback logic
- **WHEN** the controlled player receives an authoritative `CombatEvent` whose `eventType` is `ShootRejected`
- **THEN** the client surfaces that rejection through player presentation or diagnostics
- **THEN** the client does not need a speculative rollback system to make the rejection visible

### Requirement: MVP development diagnostics expose authoritative combat-result visibility
The Unity client SHALL expose lightweight UI or debug output that makes recent authoritative combat results observable during MVP development, including rejected shots and player death or damage outcomes where applicable.

#### Scenario: Development UI reflects latest authoritative combat result
- **WHEN** the client applies an authoritative combat result for a player
- **THEN** the current MVP UI or diagnostics update to show the most recent relevant combat-event information for that player
- **THEN** the displayed result is sourced from authoritative `CombatEvent` handling rather than speculative local combat logic
