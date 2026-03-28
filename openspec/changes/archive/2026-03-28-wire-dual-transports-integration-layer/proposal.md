## Why

The networking stack already distinguishes between reliable gameplay messaging and high-frequency sync traffic, but the integration layer still assumes a single transport path in its runtime wiring. Step 9 is needed now to expose the dual-transport design at composition time so hosts can route each lane consistently without breaking the existing single-session client path.

## What Changes

- Wire the integration layer so session/runtime composition can accept both a primary reliable transport and a sync transport.
- Preserve backward-compatible behavior when only the primary transport is provided by continuing to use the reliable path for all traffic that lacks a dedicated sync lane.
- Update message/session integration contracts so transport selection is resolved in shared networking code rather than by host-specific call sites.
- Add regression coverage for client single-session and server multi-session integration paths that depend on dual-transport wiring.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `shared-network-foundation`: Extend the shared composition contract so network managers and related services can be constructed with dual transports while preserving the existing single-transport fallback.
- `network-session-lifecycle`: Update runtime/session wiring requirements so sessions initialize and retain both reliable and sync transport dependencies where available.
- `network-sync-strategy`: Require integration-layer routing to connect sync traffic to the dedicated sync transport instead of relying on host-side manual wiring.

## Impact

- Affected code under `Assets/Scripts/Network/` for integration/composition, session initialization, and transport-aware message dispatch.
- Edit-mode regression tests under `Assets/Tests/EditMode/Network/`.
- No Unity-specific dependency changes; Unity adapters should remain outside shared networking code.
