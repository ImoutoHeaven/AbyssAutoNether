# Evidence-Backed Strategy Modes — Local Implementation Tracker

This feature is tracked locally only. Do not create, edit, label, close, or otherwise touch any remote issue or repository state.

Source specification: [Evidence-Backed Equipment and Research Strategy Modes](../../docs/specs/evidence-backed-strategy-modes.md)

Each ticket is a context-sized vertical slice under `issues/`. A ticket becomes part of the implementation frontier only when every listed blocker is complete. Status is tracked inside each ticket.

## Current frontier

- `01`–`03` — complete; two-axis review passed (Spec PASS; Standards PASS, independent 39/39)
- `04`–`06` — complete; two-axis review passed (Spec PASS; Standards PASS; staged full suite 1084/1084)
- `07` — paused/blocked by explicit user quota request; do not start until the user resumes it

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
