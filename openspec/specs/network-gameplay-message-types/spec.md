## ADDED Requirements

### Requirement: Gameplay message types are defined independently
The shared networking contract SHALL define `MoveInput`, `ShootInput`, `CombatEvent`, and `PlayerState` as independently addressable business message types rather than overloading one broad gameplay input payload.

#### Scenario: Shared code references split gameplay messages
- **WHEN** shared networking code or tests need to reference movement input, shooting input, authoritative state, or combat results
- **THEN** each concern is represented by its own business message type
- **THEN** code does not need to reinterpret one broad `PlayerInput` payload to determine message intent

### Requirement: Protobuf schema remains the canonical source for generated gameplay messages
The repository SHALL keep the source protobuf schema that defines gameplay network messages under version control, and generated C# message types SHALL be regenerated from that schema when gameplay message definitions change.

#### Scenario: Gameplay message schema changes regenerate shared C# types
- **WHEN** a contributor adds or changes `MoveInput`, `ShootInput`, `CombatEvent`, or `PlayerState` fields in the source protobuf schema
- **THEN** the shared generated `Message.cs` output is regenerated from that schema
- **THEN** the checked-in generated code matches the schema contract used by client and server hosts