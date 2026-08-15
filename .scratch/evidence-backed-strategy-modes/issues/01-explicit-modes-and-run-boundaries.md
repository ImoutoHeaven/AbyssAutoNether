# 01 — Explicit strategy modes and Boss-aligned run boundaries

**What to build:** Let the user explicitly run Equipment or Research strategy from the correct native starting point and finish only at a reward-preserving authoritative Boss boundary.

**Blocked by:** None — can start immediately.

**Status:** complete

- [x] New configuration defaults to Equipment and accepts Research explicitly; no automatic mode detection is introduced.
- [x] Research requires a primary Code Family and permits one optional secondary family.
- [x] Opposed primary/secondary pairs and unknown family values fail validation before native run mutation.
- [x] Equipment normalizes a requested positive non-Boss target upward to the next authoritative Boss.
- [x] Equipment resolves an out-of-range target to the deepest authoritative Boss and fails closed when no Boss can be resolved.
- [x] Research resolves its independent hard ceiling to the authoritative floor-70 Boss.
- [x] Research starts at floor zero; Equipment starts at the highest live unlocked checkpoint not above its target, or zero when none qualifies.
- [x] A Boss-aligned target produces normal settlement after Boss victory rather than a mid-segment pause.
- [x] Ordinary Retreat and proactive Lost Signal use are absent from every successful completion path.
- [x] Configuration, checkpoint, target-normalization, start-floor, and settlement behavior are covered through public policy and controller acceptance seams.
- [x] Mandatory two-axis re-review passes after the latest corrections.

**Evidence:** Fresh `Project.dll` (`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`) evidence anchors the `NotPlayed` path to `FloorSelection.SubViewController.<CreateNetherModelAsync>d__38`, exposes live unlock progress through `NetherPointData.RecoveryFloorLevel`, and proves the exact party-owned start mutation `Project.Party.Top.SubViewController.Method_Internal_Static_UniTask_Int32_Int32_Int32_CancellationToken_PDM_0(int useTicket, int startFloorLevel, int partyNo, CancellationToken ct)`. Behavior-sensitive public policy and production-controller tests prove that Research consumes floor zero while Equipment derives the highest eligible ten-floor checkpoint from live recovery progress, including Recovery=70 with non-decimal Boss rows, and that the exact production binding fails closed when unavailable. `NetherStartRunNativeBinding` keeps the semantic request separate from a typed native invocation; a distinct `(ticket=3, startFloor=70, party=7)` test proves the bridge preserves native positional order. The expanded Ticket 01–03 Docker focus passed 276/276, the full Docker suite passed 945/945, and the production Release build completed with 0 warnings/0 errors. Mandatory two-axis re-review closed: Spec PASS; Standards PASS (independent review 39/39).
