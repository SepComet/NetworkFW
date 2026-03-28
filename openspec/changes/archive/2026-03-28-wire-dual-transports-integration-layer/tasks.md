## 1. Extend Integration Composition

- [x] 1.1 Identify the shared integration/composition entry points that currently construct networking services with a single transport.
- [x] 1.2 Update the relevant constructors or factory methods to accept an optional sync transport alongside the primary reliable transport.
- [x] 1.3 Preserve backward-compatible call paths so existing single-transport composition still builds and defaults correctly.

## 2. Wire Dual Transports Through Session Services

- [x] 2.1 Update session-scoped networking services to retain both transport references provided by the integration layer.
- [x] 2.2 Route sync-designated traffic to the sync transport when present and fall back to the reliable transport otherwise.
- [x] 2.3 Ensure the shared wiring keeps host-specific transport policy out of Unity-only or integration call sites.

## 3. Verify Regression Coverage

- [x] 3.1 Add or update edit-mode tests for client single-session composition with both reliable and sync transports.
- [x] 3.2 Add or update edit-mode tests for server multi-session composition when no dedicated sync transport is provided.
- [x] 3.3 Run `dotnet build Network.EditMode.Tests.csproj -v minimal` and `dotnet test Network.EditMode.Tests.csproj --no-build -v minimal` after implementation.
