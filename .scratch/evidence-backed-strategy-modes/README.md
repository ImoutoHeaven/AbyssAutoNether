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
- `16`–`17` — implementation complete; the current semantic traceability repair and current-world production fixes are locally green and await final-review re-review. The fresh repair-focused Docker GREEN is 5/5, the valid in-repo full suite is 1328/1328, and the Release build is 0 warnings/0 errors. The current single-reviewer FAIL is superseded only by this local repair evidence; re-review is still pending. Earlier dual-reviewer PASS claims are superseded, and no remote issue or label state was touched. Evidence is in [`docs/agents/evidence-backed-strategy-modes-16-17-evidence.md`](../../docs/agents/evidence-backed-strategy-modes-16-17-evidence.md).

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

## Current final US-100 production behavior — 2026-08-20

The current final US-100 production behavior is recorded against the checked-out
`HEAD`, which the semantic audit derives with `git rev-parse HEAD`. The
repair-focused Docker GREEN is
5/5, the full suite is 1328/1328, and the production Release build is 0
warnings/0 errors. Final native evidence is
`final-sol-current-world-native-20260820-b`; final Release evidence is
`final-sol-current-world-release-20260820-j`, with DLL SHA-256
`412a66cfe3e70a2225b2b34940b78f7da585e3fa26d5e8bf05ff0aa7946e8d71`.
The production regression proves that complete authoritative Recovery branch
evidence can select the only safe eligible transform after Rest and
Purification are both unsafe, while retaining the transform policy and
hard-exclusion gates.
The current source traceability map contains 125 rows and exactly 156 method
links, including the US-100 production links; 154 is superseded historical
evidence, not the current requirement.

## Final adversarial declaration-gate/native re-review — 2026-08-20

The final re-review uses fresh read-only native evidence
`task10-us100-final-native-20260820-k`, with Project.dll SHA-256
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll SHA-256
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
global-metadata.dat SHA-256
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
Cpp2IL diffable and ISIL both exited 0. The semantic extractor rejects the
adversarial invocation as both a declaration and an extractable method while
preserving 125 rows and 156 method links. The final Release evidence is
`task10-us100-final-release-20260820-o`, with DLL SHA-256
`898109b593e3d9319a04d461be14e983572df407e724d89989611e17374a7001`.

## Current-world reconciliation — 2026-08-20

The canonical accessible game tree is `C:/Users/Eden/PixelAbyssX/dotabyss_x_cl`.
Historical native hashes above are retained only as historical evidence and are
not acceptance criteria for this current game version. The current read-only
native evidence is `final-sol-current-world-native-20260820-b` (Docker job
`j-10nd0n`) with Project.dll SHA-256
`033a5d1e92df1f90d15b4f33312fb935327fd2baa87811b7860b227d6c1c75f4`,
GameAssembly.dll SHA-256
`f2ad94781c161fe93040463b884c328599a40c78079aecacbe17a9b78edfc767`, and
global-metadata.dat SHA-256
`d7dffa623675ac493a0a4c7cfe8dc729bc37846b455a5284af94a901c1e25c27`.
Cpp2IL acquisition, diffable, and ISIL all exited 0; the reported Cpp2IL
version is `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
and Unity is `6000.3.8f1`.

Current-world gates are tracked as targeted four-finding GREEN
`final-sol-current-world-green-20260820-g` (foreground Docker run, 6/6),
mixed-duplicate GREEN `final-sol-current-world-mixed-duplicate-green-20260820-h`
(foreground Docker run, 3/3), focused/full
`final-sol-current-world-focused-full-20260820-i` (foreground Docker run, 5/5
and 1328/1328), Release
`final-sol-current-world-release-20260820-j` (0 warnings/0 errors; DLL SHA-256
`412a66cfe3e70a2225b2b34940b78f7da585e3fa26d5e8bf05ff0aa7946e8d71`),
semantic/anchor/tracker `final-sol-current-world-semantic-20260820-k`, same-run
read-only preservation/path/remote/isolation `task10-us100-current-preservation-20260820-x`
(`j-p4dfku`), and final bounded audit
`final-sol-current-world-audit-20260820-l`.

The Release identity is reproducible across both clean Docker source contexts:
the Git worktree and a Git archive without `.git` each build the exact DLL
SHA-256
`412a66cfe3e70a2225b2b34940b78f7da585e3fa26d5e8bf05ff0aa7946e8d71`, with 0
warnings and 0 errors. Release no longer injects the source revision into the
informational version, so the verified artifact identity does not depend on the
presence of Git metadata.

The current four-finding regression set covers RecoveryTransform semantic-tier
preservation in both audit paths, the native `b__9_2` Continue callback contract,
one audit per invalid Code candidate, and fixed-point incorporation of generated
route-owned procurement commitments before route selection, plus mixed duplicate
Code candidates producing one ambiguous audit per presented candidate. These
tests are included in the 1328-test full-suite result.
