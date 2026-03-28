## Context

The shared networking layer already separates reliable messaging from sync-heavy traffic conceptually, but the runtime integration path still tends to compose a single transport dependency and leaves lane selection to host-specific setup. This makes the dual-transport design incomplete: shared services can express sync intent, yet composition can still collapse both lanes into one implicit path. The repository also requires preserving the existing client single-session flow and avoiding Unity-specific dependencies in shared networking code.

## Goals / Non-Goals

**Goals:**
- Allow integration-layer composition to pass both reliable and sync transports into shared networking services.
- Keep the single-transport path valid by falling back to the reliable transport when no dedicated sync transport is configured.
- Centralize lane selection inside shared networking/session code so client and server hosts follow the same wiring rules.
- Add regression coverage for both client single-session and server multi-session composition paths.

**Non-Goals:**
- Redesign transport implementations or message schemas.
- Introduce Unity-only adapters into `Assets/Scripts/Network/`.
- Change message delivery policy beyond what is required to connect existing sync traffic to the proper transport.

## Decisions

### Decision: Treat sync transport as an optional secondary dependency
The integration layer will accept a primary reliable transport and an optional sync transport. Shared services will retain a deterministic fallback to the primary transport when the secondary dependency is absent.

This preserves existing callers and lets the change land without forcing all hosts to upgrade in one step.

Alternative considered: require dual transports everywhere. Rejected because it would break the current single-session client setup and create unnecessary migration pressure.

### Decision: Keep transport ownership at session/runtime composition boundaries
Session managers, message managers, or equivalent composition roots should receive both transport references during construction so downstream routing logic can stay host-agnostic.

This keeps transport selection in shared code and prevents Unity or host bootstrapping layers from re-implementing routing rules.

Alternative considered: inject a host-side selector callback. Rejected because it spreads transport policy across hosts and makes regression coverage weaker.

### Decision: Encode fallback behavior in requirements and tests
The change will specify that sync-routed traffic uses the sync transport when available and otherwise uses the primary reliable transport. Tests should cover both client single-session and server multi-session variants.

Alternative considered: rely on implementation comments only. Rejected because the fallback contract is easy to regress during future refactors.

## Risks / Trade-offs

- [Risk] Integration constructors may grow more complex with optional transport parameters. → Mitigation: keep the extra dependency scoped to composition roots and default the sync transport explicitly.
- [Risk] Hosts may accidentally provide mismatched transport instances across session types. → Mitigation: express wiring requirements in specs and add regression tests for composition paths.
- [Risk] Existing tests may only cover single-session client behavior. → Mitigation: add server multi-session coverage as part of the task list.

## Migration Plan

1. Extend composition APIs to accept the optional sync transport without breaking existing call sites.
2. Update shared routing/session initialization to store and use both dependencies.
3. Add or adjust edit-mode tests for fallback and dedicated sync-lane behavior.
4. Rollback, if needed, by removing the optional sync transport wiring while retaining the original single-transport constructor path.

## Open Questions

- Which concrete integration types currently own transport composition for client and server entry points?
- Are there any message categories besides sync traffic that should explicitly target the secondary transport at composition time?
