## ADDED Requirements

### Requirement: Integration wiring enforces sync lane selection
The networking stack SHALL route sync-designated traffic through the sync transport when the integration layer provides one, and SHALL fall back to the primary reliable transport when it does not.

#### Scenario: Dedicated sync transport available
- **WHEN** sync-designated traffic is sent from a session whose integration wiring includes a sync transport
- **THEN** the traffic SHALL be dispatched on the sync transport instead of the primary reliable transport

#### Scenario: Dedicated sync transport unavailable
- **WHEN** sync-designated traffic is sent from a session whose integration wiring does not include a sync transport
- **THEN** the traffic SHALL be dispatched on the primary reliable transport without failing session operation
