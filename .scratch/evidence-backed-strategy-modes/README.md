# Evidence-Backed Strategy Modes — Local Implementation Tracker

This feature is tracked locally only. Do not create, edit, label, close, or otherwise touch any remote issue or repository state.

Source specification: [Evidence-Backed Equipment and Research Strategy Modes](../../docs/specs/evidence-backed-strategy-modes.md)

Each ticket is a context-sized vertical slice under `issues/`. A ticket becomes part of the implementation frontier only when every listed blocker is complete. Status is tracked inside each ticket.

## Current frontier

- `01`–`03` — complete; two-axis review passed (Spec PASS; Standards PASS, independent 39/39)
- `04`–`06` — complete; two-axis review passed (Spec PASS; Standards PASS; staged full suite 1084/1084)
- `07`–`09` — complete as one implementation group; current repair RED 3/3, GREEN 3/3, focused 123/123, expanded 189/189, full 1208/1208, Release build 0 warnings/0 errors; procurement snapshot invalidation, permitted-source filtering, selected-horizon identity, and native-first fail-closed behavior are recorded in [`docs/agents/evidence-backed-strategy-modes-07-09-evidence.md`](../../docs/agents/evidence-backed-strategy-modes-07-09-evidence.md). Pre-amend `HEAD` is `b837c5ce1822b3b05990ff34df62ad75a974877e`; post-amend exact identity will be in `refs/notes/logic-overhaul-evidence`.
- `10`–`12` — complete; Recovery, Treasure, and rank-5 procurement acceptance suites and Docker evidence are recorded in [`docs/agents/evidence-backed-strategy-modes-10-12-evidence.md`](../../docs/agents/evidence-backed-strategy-modes-10-12-evidence.md).
- `13`–`15` — complete; Shop, visible-branch, and controller-commitment acceptance suites and Docker evidence are recorded in [`docs/agents/evidence-backed-strategy-modes-13-15-evidence.md`](../../docs/agents/evidence-backed-strategy-modes-13-15-evidence.md).
- `16`–`17` — implementation complete; the current semantic traceability repair is locally green and awaits final-review re-review. The fresh repair-focused Docker GREEN is 5/5, the valid in-repo full suite is 1325/1325, and the Release build is 0 warnings/0 errors. The current single-reviewer FAIL is superseded only by this local repair evidence; re-review is still pending. Earlier dual-reviewer PASS claims are superseded, and no remote issue or label state was touched. Evidence is in [`docs/agents/evidence-backed-strategy-modes-16-17-evidence.md`](../../docs/agents/evidence-backed-strategy-modes-16-17-evidence.md).

## Dependency order

| Ticket | Title | Blocked by |
|---|---|---|
| 01 | Explicit strategy modes and Boss-aligned run boundaries | None |
| 02 | Expand authoritative strategy evidence contracts | 01 |
| 03 | Project route-horizon safety and erosion recoverability | 02 |
| 04 | Enforce Code-family integrity and hard eligibility | 02, 03 |
| 05 | Simulate native buff timelines and parameter value | 02 |
| 06 | Value crest, charge, stack, erosion, and Force Chain mechanics | 02, 03, 04, 05 |
| 07 | Select and replace Equipment-mode Code offers | 01, 03, 04, 05, 06 |
| 08 | Complete Research-mode progression and settlement | 01, 03, 04, 05, 06 |
| 09 | Resolve Event options into exact commitments | 01, 02, 03 |
| 10 | Choose Recovery actions from branch safety | 01, 03, 04, 09 |
| 11 | Execute Treasure payment without deadlock | 02, 03 |
| 12 | Procure rank-5 Treasure keys safely | 03, 09, 11 |
| 13 | Execute committed Shop budgets and late-Shop value | 02, 12 |
| 14 | Compare complete visible branches by semantic vector | 03, 07, 08, 09, 10, 11, 12, 13 |
| 15 | Integrate strategy commitments with controller ownership | 07, 08, 14 |
| 16 | Make strategy decisions auditable and update-tolerant | 15 |
| 17 | Prove the complete strategy in production build | 16 |

## Completion rule

The feature is complete only when all 17 tickets are completed, the full regression suite and production build pass, and the verified DLL artifact is identified from that build.
