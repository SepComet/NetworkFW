## ADDED Requirements

### Requirement: Multi-session server lifecycle can be owned by a runtime entry point
The multi-session lifecycle model SHALL support being created, owned, and torn down through a concrete server runtime entry point. Starting the entry point MUST provide access to the `MultiSessionManager` through the hosted `ServerNetworkHost`, and stopping the entry point MUST clear managed sessions without requiring callers to remove peers individually.

#### Scenario: Entry point startup exposes multi-session lifecycle ownership
- **WHEN** a server runtime entry point starts successfully
- **THEN** the hosted `ServerNetworkHost` exposes its `MultiSessionManager` for per-peer lifecycle observation
- **THEN** callers do not need to manually construct or inject a separate multi-session coordinator

#### Scenario: Entry point shutdown clears all managed peers
- **WHEN** the server runtime entry point is stopped while one or more managed sessions exist
- **THEN** the hosted `ServerNetworkHost` removes all managed sessions as part of shutdown
- **THEN** no unrelated external cleanup step is required to reset multi-session lifecycle state
