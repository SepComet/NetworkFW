## 1. Server Bootstrap Contract

- [x] 1.1 Add a server bootstrap configuration type that captures reliable port, optional sync port, and supported dependency overrides for shared server startup.
- [x] 1.2 Add a concrete server runtime entry type that validates bootstrap configuration and creates the underlying `ServerNetworkHost` through shared integration code.
- [x] 1.3 Ensure the new entry contract preserves the existing client single-session path and does not introduce `UnityEngine` dependencies into shared networking code.

## 2. Runtime Lifecycle Ownership

- [x] 2.1 Implement startup sequencing so the server runtime entry point starts reliable and optional sync transports before reporting success.
- [x] 2.2 Implement failure rollback and idempotent shutdown so partially started resources are stopped and managed sessions are cleared through existing host cleanup behavior.
- [x] 2.3 Expose the minimal runtime handle needed for callers to access `ServerNetworkHost`, drain messages, evaluate lifecycle, and stop the runtime without reconstructing components.

## 3. Regression Coverage And Integration Guidance

- [x] 3.1 Add or update edit-mode tests covering reliable-only startup, dual-transport startup, startup failure rollback, and repeated shutdown behavior.
- [x] 3.2 Add or update tests confirming the runtime entry point preserves multi-session lifecycle visibility and cleanup semantics through `ServerNetworkHost`.
- [x] 3.3 Update TODO or related integration-facing documentation to point future server wiring at the new runtime entry point.
