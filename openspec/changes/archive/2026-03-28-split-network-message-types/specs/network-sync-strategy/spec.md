## MODIFIED Requirements

### Requirement: Hosts assign delivery policies to synchronization message types
The shared networking core SHALL allow hosts to map business message types to delivery policies. `MoveInput` and `PlayerState` MUST be assignable to a high-frequency sync policy that is independent from the reliable ordered control policy used by login and lifecycle traffic, while `ShootInput` and `CombatEvent` MUST remain independently routable business messages that can stay on the reliable ordered lane.

#### Scenario: High-frequency movement and state messages use a dedicated policy
- **WHEN** the client or server sends `MoveInput` or `PlayerState`
- **THEN** the runtime resolves a high-frequency sync delivery policy for that message type
- **THEN** the message is sent through the sync lane configured for that policy instead of defaulting to reliable ordered delivery

#### Scenario: Shooting and combat events keep reliable ordered delivery
- **WHEN** the client or server sends `ShootInput` or `CombatEvent`
- **THEN** the runtime resolves the reliable ordered delivery policy for that message type
- **THEN** those messages continue to use the reliable transport path

#### Scenario: Control traffic keeps reliable delivery
- **WHEN** the runtime sends login, logout, heartbeat, or other session-management messages
- **THEN** the runtime resolves the reliable ordered control policy
- **THEN** those messages continue to use the reliable transport path

### Requirement: Sequenced sync receivers discard stale gameplay updates
The high-frequency sync strategy SHALL tag gameplay synchronization messages with monotonic sequencing information and MUST discard stale `MoveInput` or `PlayerState` updates that arrive older than the last accepted update for the same peer or entity stream. `ShootInput` and `CombatEvent` MUST NOT be discarded by the latest-wins stale filter.

#### Scenario: Older movement input is ignored
- **WHEN** the server receives a `MoveInput` update with a tick or sequence older than the latest accepted input for that player
- **THEN** the server drops that stale movement update
- **THEN** the newer accepted movement input remains authoritative for simulation

#### Scenario: Older player state does not rewind a client
- **WHEN** the client receives a `PlayerState` update with a tick or sequence older than the latest applied authoritative state for that player
- **THEN** the client ignores the stale state update
- **THEN** visible movement continues from the newer authoritative state without rewinding to older data

#### Scenario: Reliable gameplay events bypass stale-drop filtering
- **WHEN** the runtime receives a `ShootInput` or `CombatEvent` message
- **THEN** the latest-wins stale filter does not reject that message solely because of sync-sequence rules
- **THEN** reliable ordered handling remains responsible for preserving event delivery semantics

### Requirement: Authoritative correction prunes acknowledged prediction history
The client sync strategy SHALL reconcile local prediction against authoritative player-state updates by pruning acknowledged movement inputs at or before the authoritative tick and only reapplying newer pending `MoveInput` messages.

#### Scenario: Reconciliation removes already acknowledged movement inputs
- **WHEN** the client accepts an authoritative `PlayerState` update for tick `N`
- **THEN** locally buffered predicted `MoveInput` messages with tick less than or equal to `N` are removed from the replay buffer
- **THEN** only `MoveInput` messages newer than `N` remain eligible for re-simulation