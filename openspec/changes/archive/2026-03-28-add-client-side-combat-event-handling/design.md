## Context

The client currently has the full MVP wire contract for `CombatEvent`, but the Unity receive path stops at `PlayerState`. `NetworkManager` registers login, join, logout, heartbeat, and `PlayerState` handlers, while `Player` and `PlayerUI` only expose authoritative snapshot data that came from `PlayerState`. As a result, local muzzle flash can exist as cosmetic presentation, but server-truth combat outcomes such as damage, death, or `ShootRejected` are not surfaced on the client at all.

This step sits after authoritative player-state ownership and remote interpolation are already in place. The change must reuse that ownership model instead of inventing a second source of truth for HP or death, and it must avoid pulling future server-combat implementation details into the client-side receive/apply design. Shared networking contracts under `Assets/Scripts/Network/` already define the message and lane policy, so this change should stay on the Unity client side.

## Goals / Non-Goals

**Goals:**
- Register and route authoritative `CombatEvent` messages through the existing client receive path.
- Define one explicit client-side application point for authoritative combat outcomes so hit, damage, death, and `ShootRejected` are not handled ad hoc across unrelated presentation scripts.
- Let the client update owned authoritative player presentation state from server-confirmed combat results while keeping local firing FX cosmetic.
- Expose lightweight combat-result visibility in existing UI or diagnostics so MVP playtests can observe accepted damage, death, and rejected shots.

**Non-Goals:**
- Implement server-authoritative combat resolution or decide how the server emits each event.
- Redesign shared protobuf schemas, message lanes, or stale-drop rules for reliable gameplay events.
- Add speculative client-side rollback, lag compensation, or remote combat prediction.
- Replace `PlayerState` as the longer-lived authoritative snapshot source for movement and periodic HP correction.

## Decisions

### Decision: Route client combat events through `NetworkManager -> MasterManager -> Player`
`NetworkManager` will register a `CombatEvent` handler alongside `PlayerState`, then forward parsed events into player-owned presentation logic through `MasterManager`. `MasterManager` remains the player lookup boundary, and `Player` becomes the point that decides how a given authoritative combat result affects its owned state and presentation.

Why this approach:
- It matches the receive path already used for authoritative `PlayerState`.
- It keeps endpoint lookup and Unity object ownership out of `NetworkManager`.
- It avoids spreading combat-result handling across UI and movement components directly.

Alternative considered:
- Let `NetworkManager` update UI or scene objects directly. Rejected because it would bypass the current player ownership model and make later combat presentation harder to reason about.

### Decision: Keep `PlayerState` as the base authoritative snapshot, but allow authoritative combat-result deltas to update client-owned presentation state between snapshots
The client already owns one authoritative snapshot per player. `CombatEvent` handling will extend that player-owned state with authoritative combat-result application, such as subtracting confirmed damage from the current authoritative HP view, marking death state, or recording last combat-result metadata for presentation. The next accepted `PlayerState` remains allowed to refresh the full snapshot and correct any drift.

Why this approach:
- `CombatEvent` is itself authoritative server output, so applying it on the client does not create speculative gameplay truth.
- It gives the client immediate combat-result feedback without waiting for the next sync-lane `PlayerState`.
- It preserves the existing rule that periodic `PlayerState` snapshots can refresh the full authoritative state.

Alternative considered:
- Wait for `PlayerState` only and use `CombatEvent` for cosmetic feedback. Rejected because TODO step 5 explicitly requires client combat-result handling that can update HP or death state from server truth.

### Decision: Separate local fire FX from authoritative combat-result presentation
The controlled player may continue to play local fire FX immediately, but `CombatEvent` handling will own authoritative damage, death, hit feedback, and rejection feedback. `ShootRejected` will be surfaced as a client-visible authoritative result without introducing rollback of predicted movement or speculative state rewrites.

Why this approach:
- It preserves the MVP rule that cosmetic preplay is allowed, but gameplay truth is not.
- It keeps this step focused on receive/apply behavior rather than prediction rollback mechanics.

Alternative considered:
- Roll back or cancel local fire presentation on `ShootRejected`. Rejected because the MVP only requires authoritative visibility of rejection, not a full local combat preplay rollback system.

### Decision: Use lightweight player UI or diagnostics for development-time combat visibility
Existing MVP diagnostics such as `PlayerUI` should expose enough combat-result state to make damage, death, and `ShootRejected` visible during playtests. This should stay lightweight: a last combat-event line, death indicator, or similar output is enough.

Why this approach:
- It keeps observability close to the player object already showing authoritative HP.
- It avoids introducing a larger diagnostics framework for one MVP step.

Alternative considered:
- Defer visibility until end-to-end server combat exists. Rejected because the TODO step explicitly calls for combat-result visibility during development.

## Risks / Trade-offs

- [Applying `CombatEvent.damage` on top of the latest snapshot can be refreshed again by a later `PlayerState`] → Mitigation: treat `CombatEvent` as an immediate authoritative delta for presentation and let the next accepted `PlayerState` remain the full-state correction path.
- [A combat event may reference an attacker or target player that is not spawned locally] → Mitigation: route events through `MasterManager` and ignore or log missing-player references instead of throwing.
- [UI can accidentally become the owner of combat truth] → Mitigation: keep combat-result application in `Player` or a player-owned helper, and let UI only read derived authoritative state/diagnostic values.
- [`ShootRejected` handling could grow into a rollback system] → Mitigation: scope this step to surfacing rejection and keeping local fire FX cosmetic rather than retroactively undoing unrelated local state.
