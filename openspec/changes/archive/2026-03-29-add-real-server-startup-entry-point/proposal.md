## Why

The current networking foundation supports transports, sessions, and multi-session lifecycle building blocks, but it still lacks a concrete server startup path that can be used to boot a real host outside of tests or ad hoc wiring. This blocks end-to-end integration of the server role and leaves TODO step 6 unresolved.

## What Changes

- Add a real server startup and integration entry point that assembles the shared networking pieces needed to host a server session lifecycle.
- Define the contract for server bootstrap configuration, startup sequencing, and shutdown ownership so server hosting can be invoked consistently from a runtime entry point.
- Clarify how the server entry point integrates with existing shared/session lifecycle components without changing the client single-session path.
- Add regression coverage for server startup integration behavior and failure handling around startup/shutdown orchestration.

## Capabilities

### New Capabilities
- `server-runtime-entry-point`: Covers creating and running a concrete server bootstrap/integration entry point for the host process.

### Modified Capabilities
- `multi-session-lifecycle`: Extend requirements so a real server startup path can create, own, and tear down the multi-session manager through a defined integration entry point.
- `network-session-lifecycle`: Extend requirements for startup/shutdown sequencing expectations when sessions are initialized by the server runtime entry point.

## Impact

Affected areas include shared networking bootstrap code under `Assets/Scripts/Network/`, any host-specific adapter needed to invoke server startup, edit-mode regression tests for lifecycle integration, and the TODO-driven server integration workflow documented by this repository.
