## ADDED Requirements

### Requirement: Shared network composition accepts dual transports
The shared networking composition layer SHALL allow construction of network managers and related shared services with a primary reliable transport and an optional sync transport.

#### Scenario: Integration receives both transports
- **WHEN** a host composes the shared networking stack with both a reliable transport and a sync transport
- **THEN** the shared composition path SHALL retain both dependencies for downstream routing and session services

#### Scenario: Integration receives only one transport
- **WHEN** a host composes the shared networking stack with only the reliable transport
- **THEN** the shared composition path SHALL remain valid and SHALL treat the reliable transport as the fallback lane for traffic without a dedicated secondary transport
