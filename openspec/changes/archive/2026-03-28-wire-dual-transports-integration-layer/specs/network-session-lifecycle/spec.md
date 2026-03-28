## ADDED Requirements

### Requirement: Sessions retain dual transport wiring
Session lifecycle components SHALL initialize session-scoped networking services with both the primary reliable transport and the optional sync transport supplied by the integration layer.

#### Scenario: Client single-session initialization with dual transports
- **WHEN** the client integration path creates a single session and a sync transport is configured
- **THEN** the session-scoped services SHALL retain both transport references for subsequent message routing

#### Scenario: Server multi-session initialization with fallback transport
- **WHEN** the server integration path creates session-scoped services without a dedicated sync transport
- **THEN** each session SHALL continue to initialize successfully and SHALL use the primary reliable transport as the fallback lane
