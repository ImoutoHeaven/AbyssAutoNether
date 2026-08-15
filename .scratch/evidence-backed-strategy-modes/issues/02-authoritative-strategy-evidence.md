# 02 — Expand authoritative strategy evidence contracts

**What to build:** Give every strategy decision one immutable, owner-bound evidence package containing the live party, Code, research, native mechanic, and visible-map facts it needs, while preserving existing behavior during migration.

**Blocked by:** 01 — Explicit strategy modes and Boss-aligned run boundaries.

**Status:** complete

- [x] The evidence package is bound to the current generation, controller owner, authoritative server snapshot, and matching entered subscene.
- [x] The active party profile captures position, element, ManaType, HP, level, limit break, native parameter inputs, and character, equipment, and general ability effects.
- [x] Owned Code state captures positive-amount distinct Codes, capacity, rerolls, decoded effects, category-skill rows, and opposing-family counts.
- [x] Research evidence captures each family wallet, projected normal-settlement inputs, and current technology research rates.
- [x] Native mechanic evidence captures current buff strategies and all available trigger, target, duration, grouping, cap, and parameter relationships needed later.
- [x] Visible interactive evidence captures exact Event, battle, item, Treasure, Shop inventory, resource, and Boss rows without speculative hidden content.
- [x] Displayed power and displayed target coverage remain diagnostic fields only and cannot satisfy missing combat evidence.
- [x] Unknown mapping preserves an exact reason and invalidates only the smallest dependent evidence component.
- [x] Existing strategy callers can migrate incrementally without breaking the stable controller transaction model.
- [x] Mapper and production-wiring characterization tests prove fresh owner binding, immutability, and fail-closed unknowns.
- [x] Mandatory two-axis re-review passes after the latest corrections.

**Evidence:** Fresh `Project.dll` (`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`) evidence covers the FloorSelection owner/`OnEntered`, party model, Code/category rows, point wallets/rates, result-only research projection, Event payloads, and live Shop inventory. Native mechanics copy exact typed trigger base controls (`_probabilityType`, `_probabilityPerMille`, `_levelBasedProbability`, `_executeCountLimit`, `_situationCost`), subtype parameters, target, duration, grouping, cap, threshold, filter, reference, Mana/Energy, charge, stack, and linked-buff relationships; unsupported native subtypes and a missing owned `MNetherCodes` row remain typed row/component-local unknowns with exact errors. Visible evidence preserves all four exact Event-part effect slots and resolves `MNetherFloorBattles`/`MNetherFloorTreasures` by `m_nether_map_floor_id`, with distinct battle/stage/content IDs and exact `MItems` rewards. `NetherNativeMechanicProductionCapture` and `NetherStrategyVisibleEvidenceAssembler` now localize the native/lifecycle assembly outside the bridge. Behavior-sensitive tests cover all-known triggers, literal nested values, exact errors, deliberately different relation IDs, immutability, and the full owner/generation/snapshot/`OnEntered` gate. The expanded Ticket 01–03 Docker focus passed 276/276, the full Docker suite passed 945/945, and the production Release build completed with 0 warnings/0 errors. Mandatory two-axis re-review closed: Spec PASS; Standards PASS (independent review 39/39).
