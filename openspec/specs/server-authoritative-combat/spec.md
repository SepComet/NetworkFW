# server-authoritative-combat Specification

## Purpose
Define the shared server-side combat authority contract that accepts `ShootInput`, validates sender-scoped fire requests, resolves authoritative hit and damage outcomes, and emits reliable `CombatEvent` results.

## Requirements
### Requirement: Server registers and validates `ShootInput` per peer
The shared server networking path SHALL register `ShootInput` handling through the server host/runtime composition and SHALL validate each inbound shooting request against the sending managed peer before applying any authoritative combat result. Validation MUST reject malformed numeric input, missing or mismatched player identity, non-increasing shoot ticks for that sender, zero-direction fire requests, and targets that do not resolve to a living managed peer.

#### Scenario: Valid `ShootInput` is accepted for the sending peer
- **WHEN** a managed peer sends a well-formed `ShootInput` whose `playerId` maps to that sender, whose tick is newer than the sender's last accepted shoot tick, and whose `targetId` resolves to another living managed peer
- **THEN** the server accepts the request for that sender only
- **THEN** the sender's last accepted shoot tick is updated without changing other peers' combat bookkeeping

#### Scenario: Invalid `ShootInput` is rejected without mutating authoritative combat state
- **WHEN** a managed peer sends a `ShootInput` with malformed direction data, a stale tick, a mismatched `playerId`, or a target that is missing, self-targeted, or already dead
- **THEN** the server rejects that request
- **THEN** no authoritative damage or death state is applied to any peer from that rejected request

### Requirement: Server resolves authoritative combat outcomes
The shared server networking path SHALL own final combat resolution for accepted shots, including hit acceptance, damage application, authoritative HP mutation, and death determination. Combat resolution MUST update the authoritative server-owned state of both the attacker and target as needed without delegating gameplay truth to client-side prediction or presentation code.

#### Scenario: Accepted shot applies damage to the authoritative target state
- **WHEN** the server accepts a `ShootInput` that targets a living managed peer
- **THEN** it resolves the shot as an authoritative hit against that target
- **THEN** it reduces the target's authoritative HP according to the configured damage rule before later state snapshots are broadcast

#### Scenario: Lethal damage marks the authoritative target as dead
- **WHEN** an accepted shot reduces a target's authoritative HP to zero or below
- **THEN** the server clamps the target's authoritative HP to zero
- **THEN** subsequent combat and state broadcast treat that target as dead until a later server-owned lifecycle change resets it

### Requirement: Server emits reliable authoritative `CombatEvent` results
The shared server networking path SHALL emit authoritative combat outcomes through `CombatEvent` messages using the existing reliable-lane delivery contract. Accepted shots MUST produce reliable events for hit and damage application, and lethal results MUST also produce a death event. Rejected shots MUST produce a reliable `ShootRejected` event that identifies the attacker and rejected target context.

#### Scenario: Accepted shot produces authoritative hit and damage events
- **WHEN** the server resolves an accepted shot that damages a living target
- **THEN** it broadcasts `CombatEvent` messages on the reliable lane for the authoritative combat result
- **THEN** the emitted events identify the attacker, target, damage, and authoritative tick for client-side application

#### Scenario: Rejected shot produces an authoritative rejection event
- **WHEN** the server rejects a `ShootInput` during validation
- **THEN** it broadcasts a reliable `CombatEvent` with event type `ShootRejected`
- **THEN** clients can observe that rejection without inferring local authoritative damage or hit success
