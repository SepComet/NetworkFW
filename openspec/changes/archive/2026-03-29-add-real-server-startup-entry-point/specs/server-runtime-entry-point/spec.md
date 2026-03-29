## ADDED Requirements

### Requirement: Server runtime entry point assembles a host from validated configuration
The shared networking layer SHALL provide a concrete server runtime entry point that accepts validated bootstrap configuration for reliable transport startup and optional sync transport startup. The entry point MUST construct the server host using the existing shared integration components instead of requiring callers to manually assemble transports and session lifecycle services.

#### Scenario: Start server runtime with reliable transport only
- **WHEN** a host starts the server runtime entry point with a valid reliable port and no sync port
- **THEN** the entry point creates a `ServerNetworkHost` using the shared networking integration path
- **THEN** the reliable transport is started and the runtime exposes the started host to the caller

#### Scenario: Start server runtime with reliable and sync transports
- **WHEN** a host starts the server runtime entry point with both reliable and sync ports configured
- **THEN** the entry point creates the server host with both transport lanes configured
- **THEN** the runtime starts both transports before reporting startup success

### Requirement: Server runtime entry point owns startup failure rollback and shutdown
The server runtime entry point SHALL own startup/shutdown sequencing for the created server host. If startup fails after any transport or host resource has begun initializing, the entry point MUST stop already-started resources before surfacing the failure. The shutdown path MUST be safe to call repeatedly and MUST leave the server host with no remaining managed sessions.

#### Scenario: Startup failure rolls back started resources
- **WHEN** reliable transport startup succeeds and a later startup step fails before the runtime reports success
- **THEN** the entry point stops the already-started transport resources
- **THEN** the failure is surfaced to the caller without leaving a partially running server runtime

#### Scenario: Repeated shutdown is safe
- **WHEN** the caller stops the server runtime entry point more than once
- **THEN** shutdown does not throw because the runtime was already stopped
- **THEN** the underlying server host remains in a fully stopped state with its managed sessions cleared

### Requirement: Server runtime entry point exposes the integration surface needed by host processes
The server runtime entry point SHALL expose the started `ServerNetworkHost` and the minimal runtime handle required for host processes to drive message draining, lifecycle evaluation, and eventual shutdown.

#### Scenario: Host process drives the shared server lifecycle through the runtime handle
- **WHEN** startup completes successfully
- **THEN** the caller can access the started `ServerNetworkHost` through the runtime handle
- **THEN** the caller can use that handle to invoke message draining, lifecycle updates, and shutdown without reconstructing networking components
