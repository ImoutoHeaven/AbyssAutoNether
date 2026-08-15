# 03 — Project route-horizon safety and erosion recoverability

**What to build:** Decide whether a visible branch is safe through its next terminal Boss using authoritative per-character HP and per-combat erosion projections, including the approved Risk operating band.

**Blocked by:** 02 — Expand authoritative strategy evidence contracts.

**Status:** complete

- [x] Safety is projected across the complete authoritative visible branch through the next terminal map Boss.
- [x] Every confirmed combat uses its exact pre-entry HP evidence, base erosion settlement, active Code modifiers, and projected start erosion.
- [x] Ordinary route costs require every living character to remain alive; narrow Treasure and HP-paid-key exceptions are represented separately rather than weakening the general gate.
- [x] Lethal erosion remains a hard rejection regardless of encounter value.
- [x] Dedicated Risk Research prefers 50–70 erosion and never raises erosion merely to increase a Risk payoff.
- [x] A transient value above 70 is accepted only when the visible branch proves no unsafe battle and certain return to 70 or below.
- [x] Erosion at or above 70 without a Confirmed Recovery Route produces a user pause before mutation.
- [x] Necessary combat on a proven recovery branch remains legal.
- [x] Missing future floors or random outcomes never count as recovery evidence.
- [x] The output separates eligibility from later reward ordering and includes peak erosion, minimum active-character HP, and exact rejection evidence.
- [x] Tests cover 50, 70, transient above 70, no recovery, necessary combat, lethal erosion, and unknown-horizon cases.
- [x] Mandatory two-axis re-review passes after the latest corrections.

**Evidence:** Fresh `Project.dll` (`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`) evidence anchors erosion to `User.NetherData.ErosionPoint`, active HP to exact `NetherPartyCharacterModel.MCharacterId`/`HpRatio`/`IsAlive`, and post-combat HP exclusively to `NetherClearBattleResponseEntity.t_nether_characters`. A later combat is never certified from the preceding stale HP snapshot; it remains ineligible until the authoritative clear response causes a replan. After a permitted partial-party Treasure payment, dead roster rows are validated but excluded from the fresh current-living HP minimum. The evidence preserves an immutable CharacterId-sorted living row set, and the coordinator requires exact per-character identity and HP equality against the authoritative snapshot. Native `System.Single HpRatio` is reconstructed by one checked, midpoint-away-from-zero permille quantizer shared by runtime HP, snapshot, and strategy-party capture, preventing `0.299f` from diverging as 298 versus 299. Public extractor-to-coordinator tests prove matching evidence can replan while equal minima from different identities, genuinely different HP, all-dead state, and missing `IsAlive` remain fail-closed. Exact `NetherTreasurePopupController` event/part ownership, `NetherTreasurePanelType`, `NetherFloorEventType.Damage`, `ContentType.NetherKey`, and `MNetherFloorEventParts` fields restrict partial-party-survival exceptions to a matching typed prevalidated objective: reachable rank-5/no-better-key-source for HP-paid Event Key, and reachable rank-5 or only-terminal-route for Treasure HP payment. Production supplies no proof by default; exact option shape alone fails closed, ordinary deterministic Event damage requires every living character to remain above zero, and full-party death always rejects. The expanded Ticket 01–03 Docker focus passed 276/276 and full Docker suite passed 945/945. The production Release build passed with 0 warnings/0 errors and produced `release/review-fix-01-03/Release/net6.0/AutoNether.dll` (1,218,048 bytes, SHA-256 `df8eef2183ff7c874a9dfb331a3a4c6ea91131a5779b328c39230a9f7cdcb509`). Mandatory two-axis re-review closed: Spec PASS; Standards PASS (independent review 39/39).
