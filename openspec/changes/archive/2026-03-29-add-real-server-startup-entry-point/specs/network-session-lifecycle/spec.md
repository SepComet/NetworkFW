## ADDED Requirements

### Requirement: Server startup and shutdown preserve explicit session lifecycle sequencing
When sessions are hosted through the server runtime entry point, the shared networking layer SHALL preserve explicit startup and shutdown sequencing boundaries for session lifecycle services. The runtime MUST NOT report server startup success until transport startup has completed, and shutdown MUST transition hosted sessions through the existing disconnect/cleanup path instead of abandoning lifecycle state.

#### Scenario: Startup success is reported only after transport startup completes
- **WHEN** a host starts the server runtime entry point
- **THEN** the runtime does not report success before required transport startup finishes
- **THEN** any sessions created after startup begin from the existing server-side lifecycle model

#### Scenario: Shutdown routes through existing lifecycle cleanup
- **WHEN** the caller stops a running server runtime entry point
- **THEN** hosted sessions are disconnected or removed through the existing session lifecycle cleanup path
- **THEN** subsequent lifecycle inspection reflects a stopped runtime rather than stale active sessions
