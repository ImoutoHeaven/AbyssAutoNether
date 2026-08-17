# 09 — Resolve Event options into exact commitments

**What to build:** Turn each visible Event into one exact, safe, mode-aware option commitment whose later popup action is the same choice that justified the route.

**Blocked by:** 01 — Explicit strategy modes and Boss-aligned run boundaries; 02 — Expand authoritative strategy evidence contracts; 03 — Project route-horizon safety and erosion recoverability.

**Status:** complete — implementation/review repair closure 2026-08-18; native-first battle-tier boundary is recorded in [`docs/agents/evidence-backed-strategy-modes-07-09-evidence.md`](../../../docs/agents/evidence-backed-strategy-modes-07-09-evidence.md)

- [x] Every displayed option is resolved through exact Event, part, effect, content, item, and optional-battle rows.
- [x] Eligibility checks binding, resource sufficiency, route safety, and committed budget before reward semantics.
- [x] Ordinary Event damage is projected against every living party character and requires every one to remain alive.
- [x] The popup presenter identifier is never treated as the sole HP target.
- [x] Ordinary erosion and Gold costs require complete recoverability and preservation of exact committed budgets.
- [x] Event Boss, MiniBoss, and Normal Battle inherit their nonterminal semantic combat tiers and exact safety projection.
- [x] A direct Code Offer retains its semantic opportunity tier at capacity without assuming acquisition.
- [x] Research-incomplete and Equipment/completed-Research ordinary reward priorities match the approved semantic order.
- [x] Unknown future content, effect, item, or battle data rejects only its option; other exact choices remain available.
- [x] The selected commitment records exact Event, part, option, effects, reward or battle, projected state, and deterministic option tie break.
- [x] A later popup mismatch pauses before payment rather than silently changing choices.
- [x] Tests cover compound effects, all-character HP, optional combat, direct Offer, unknown-option locality, and stale commitment rejection.

## Closure evidence and native boundary

The runtime mapper binds the native Event row, declared part IDs, exact `MItems` rows, and exact
`MNetherFloorBattles` rows; malformed or duplicate item/battle rows now preserve their original
native ID and make only the dependent option unknown, even when a valid sibling has the same ID.
The production binding carries mode-aware Research and Equipment route/resource/semantic evidence
and the option-keyed immutable commitment. Its identity includes EventId, EventPartId, floor/node,
option, effects, reward-or-battle, projected state/resources, and the deterministic tie fields. The
native popup presenter `_mCharacterId` is retained only as correlation data, and the native update
request continues to use floor/option/Code arguments. Fresh native `MNetherFloorBattles.cs:4-15`
proves only a raw integer battle type, not Boss/MiniBoss/Normal semantics; raw types 1–8 therefore
remain unknown while exact battle/stage/content identity is retained. Typed policy tests cover
known Boss/MiniBoss/Normal tiers supplied separately, while current native uncertainty remains
fail-closed by design. Stale commitment protection passes before payment and is also enforced
during transaction reconcile. Final focused coverage passed 61/61 (`j-wwler8`), expanded focused
323/323 (`j-gmybcz`), and the full Docker suite passed 1151/1151 (`j-czkkaa`); exact fresh native
evidence is in the durable note linked above.

### Final gate addendum (2026-08-18)

The downstream projected-state repair added public RED cases where Event identity and effects match
but projected erosion, HP delta, Gold, or Treasure Keys diverge. RED job `j-d2vg2g` recorded
`5 failed / 46 passed / 51 total`; focused GREEN `j-fqejbx` passed `68/68`, expanded focused
`j-r7q5jc` passed `380/380`, and full suite `j-08wpm7` passed `1157/1157`. Composer and
reconcile now enforce the same immutable commitment before native payment/update, while preserving
the native floor/index/selected-option/Code-change request seam. The test-project linked production
source was removed; the binder boundary assertion and product-isolation job `j-9xopoe` prove
tests exercise the production assembly. Fresh native hashes/artifact hashes and exact anchors are in
the final-gate section of the durable evidence note.

### Partial-death downstream repair addendum (2026-08-18)

The public-seam RED loop reproduced three failures in `j-n92867` (`3 failed / 128 passed / 131
total`): authorized partial death was rejected by reconcile, the composed parent lost its exact
commitment, and the production controller paused. The minimized calibration RED was `j-dj6ldi`
(`1 failed / 0 passed / 1 total`) with `hp-projection-drift:1`. After the minimal fixes, clean
focused GREEN `j-xn1k1b` passed `137/137`, expanded focused `j-7r5idl` passed `313/313`, and the
full Docker suite `j-b0tcg9` passed `1166/1166`.

Authorized Treasure partial death now carries its exact proof through dispatch, composition,
projection calibration, and reconcile: `[character 1: 0, inactive]` plus `[character 2: 300,
active]` is Applied only with the exact commitment; full-party death, unauthorized true-to-false
transitions, and projected-state/resource mismatches remain fail-closed. Ordinary Event damage
continues to require all previously active characters to remain active.

Fresh final native evidence is job `j-d221gi` (`CPP2IL_EXIT=0`, `DIFFABLE_EXIT=0`) with game hashes:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

The decompiled artifact hashes are `NetherCharacterEntity.cs`
`22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`,
`NetherUpdateEventResponseEntity.cs`
`30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`,
`NetherApiDataStore.cs` `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`,
`NetherEventResultModel.cs` `f79123d206000bfc369af7bad485fc22b60fd749048c36d6d49a0f504ab52f83`, and
`NetherEventPopupController.cs`
`a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf`. Anchors in
`j-d221gi/output.log` are `:11-17` (character ID and `current_hp_ratio`, with no native
`IsAlive`), `:28-32` (update response character/floor/Code rows), `:411-415` (native update
request and its floor/index/selected-option/Code parameters), `:417-423` (EventResult update
state machine and same request seam), and `:397-403` (popup presenter `_mCharacterId`, Event,
and Event-Part fields). Native design therefore keeps Event/Part IDs as client correlation and
does not override the spec; no hidden battle/death semantics were invented.

Release build `j-v08z84` passed with `0 Warning(s), 0 Error(s)`; product-isolation
`j-fpmbq6` proved the test project uses the `AutoNether` ProjectReference and has no linked
production binder source. The game directory remained read-only for every Docker invocation.

### Post-repair verification addendum (2026-08-18)

This was a verification-only continuation after the authorized partial-death repair; no new
production code change was required. Fresh native decomp job `j-hvd4j4` used
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly` and returned
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. The exact game hashes were:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

Fresh artifact hashes are `NetherCharacterEntity.cs`
`22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`,
`NetherUpdateEventResponseEntity.cs`
`30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`,
`NetherApiDataStore.cs` `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`,
`NetherEventResultModel.cs` `f79123d206000bfc369af7bad485fc22b60fd749048c36d6d49a0f504ab52f83`, and
`NetherEventPopupController.cs`
`a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf`.
Authoritative anchors are `j-hvd4j4/output.log:15-18` (character ID and
`current_hp_ratio`, with no native `IsAlive`), `:31-33` (response character/floor/Code rows),
`:399-404` (popup presenter/Event/Event-Part fields), and `:411-424` (native update method and
EventResult update flow). Native still accepts only floor/index/selected option/Code-change ID;
Event/Part IDs remain client commitment correlation and no hidden death/battle semantics were
invented.

The partial-death public-seam RED remains `j-n92867` (`3 failed / 128 passed / 131 total`) and
minimized calibration RED `j-dj6ldi` (`1 failed / 0 passed / 1 total`); the current fresh GREEN
revalidation passed focused `j-rh669t` (`137/137`), expanded `j-ehq5wo` (`313/313`), and full
`j-f5znai` (`1166/1166`). Release `j-mets3c` succeeded with `0 Warning(s), 0 Error(s)`, product
isolation `j-x71jbk` returned `PRODUCT_ISOLATION_PASS=1`, and Docker diff checking is recorded in
the durable evidence addendum. These results use the exact commitment through composer, controller,
projection calibration, and reconcile, including partial death with an active survivor, while full
party/unauthorized death and projected-state mismatch remain rejected.
### Budget and malformed-target repair addendum (2026-08-18)

Production procurement minima are now carried from exact route/pre-entry option evidence into
option projections, Event binding, immutable commitments, and the pre-payment resource gate.
Public RED j-wbk5zy reproduced an empty commitment for the branch-local Gold minimum; GREEN
j-m4o95q passed the Gold population and unsafe-spend cases, and j-jzex6q passed the compound
Gold+Key minimum case. The malformed target RED in j-pvy9dv selected option 1 when
target_type_1=0/select_parameter_1=999; GREEN j-m4o95q leaves that dependent option unknown
while selecting the exact sibling. Fresh native MNetherFloorEventParts.cs evidence is
j-bbli4l/output.log:19-35, artifact hash
5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128; it proves the raw fields
without inventing semantics. Final focused/expanded/full Docker suites passed 115/115, 293/293,
and 1173/1173; Release was 0 warnings/0 errors; product isolation passed; /game was read-only.
### Final post-amend verification (2026-08-18)

The route-owned procurement map now has a durable producer separate from the one-shot pending
handoff, so repeated route-safety capture does not erase a proven budget. The historical validation
records above are superseded by the closing validation addendum below. The historical pre-amend
task-group commit (not final) was explicitly `1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, with parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`; no unrecorded command is needed to identify it.

### Current closing repair addendum (2026-08-18)

The route-owned production seam now binds the captured Gold/Key procurement map before
`ApplyProcurementBudgets`; positive minima survive the immutable commitment handoff and are
rechecked before native payment/reconcile. Fresh RED is `j-y33pp2` (6 failed / 119 passed /
125 total) plus decoder base-commit RED `j-5l8cza` (2 failed / 0 passed / 2 total); focused
GREEN `j-voopjz` passed 196/196. Fresh native RO decomp `j-53m4lb` returned
`CPP2IL_EXIT=0`/`DIFFABLE_EXIT=0`; `MNetherFloorEventParts.cs` hash
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` lines 13-29 proves only
raw target/parameter and content fields, while `NetherApiDataStore.cs` hash
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` lines 287-288 proves the
floor/index/option/Code-only native request. Type 7 nonzero parameters and content 160 nonzero
IDs therefore remain option-local unknown/fail-closed, and no Event/Part IDs are fabricated into
the native request.

### Closing P1 repair addendum (2026-08-18)

The real production route-owned seam now stores exact committed Gold/Key minima in
`NetherRouteOwnedEventProcurementProducer`. `NetherRuntimeBridge.TryCaptureRouteSafety` reads the
durable producer; successful pre-entry capture refreshes it only from exact option projections;
pending handoff clearing no longer erases the route-owned proof. The public production E2E removes
`RouteSafetyOverride`, captures the same positive Gold/Key map twice, then proves unsafe spending is
rejected before Event semantics.

Ordinary non-partial-death Event updates now require exact projected HP for every character living at
the pre-event snapshot and reject ordinary lethal projections. The authorized Treasure/HP-paid
partial-death path still requires exact permitted death/survivor state and at least one survivor;
full-party and unauthorized death remain rejected. RED was 3 failed / 50 passed / 53 total in the
fresh pre-fix Docker run; final focused GREEN `j-n05h3m` is 53/53, expanded `j-ulroy1` is 313/313,
and full `j-6h5nv7` is 1183/1183. Release `j-qhiwpe` is 0 warnings/0 errors and isolation
`j-anvi44` is `PRODUCT_ISOLATION_PASS=1`.

Fresh native RO decomp jobs `j-qcpyn6` and `j-8924sc` both returned `CPP2IL_EXIT=0` and
`DIFFABLE_EXIT=0`. `MNetherFloorEventParts.cs` hash
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`, output lines 17-33, proves
only raw Event target/parameter/content fields; `NetherCharacterEntity.cs` hash
`22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`, output lines 10-14, proves
the native per-character `current_hp_ratio`; `NetherUpdateEventResponseEntity.cs` hash
`30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`, output lines 24-28, proves
the response character/floor/Code rows; and `NetherApiDataStore.cs` hash
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`, output lines 95-96,
proves the native floor/index/selected-number/Code request seam. The immutable game hashes are
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. Native-first review found
no conflict and no hidden budget, battle tier, or target semantic was inferred.

### Documentation integrity addendum (2026-08-18)

The historical pre-repair closing Docker gate set was `j-l9m0j6` (focused 111/111), `j-529vvn`
(expanded 230/230), `j-nyu1bj` (full 1186/1186), `j-3ulak2` (Release 0 warnings/0 errors),
and `j-ju3ij7` (`EVIDENCE_AUDIT_PASS=1`, `PRODUCT_ISOLATION_PASS=1`, `DIFF_CHECK_EXIT=0`). Its recorded current
historical `HEAD` and `HEAD^` were `1e1e7a0d6f0215910e9b7d1254c7771d217326ea` and
`5f3de38572d5526e73e8576ffe505669c1c8dbc3` at `j-ju3ij7/output.log:5-6`. Every gate used
`docker run --rm` with `--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`.

### Current canonical-restore closing record (2026-08-18)

Clean restore RED `j-urwq7m/output.log:1-6` reproduced NU1101 for
`BepInEx.Unity.IL2CPP` and `BepInEx.PluginInfoProps`; clean restore GREEN
`j-g4xeyq/output.log:1-9` used repository `NuGet.config` without explicit source flags and
passed 1184/1184. Fresh RO native Cpp2IL `j-53m4lb/output.log:1-150` returned
`CPP2IL_EXIT=0`/`DIFFABLE_EXIT=0`; its exact immutable game/decomp hashes and authoritative raw
Event/part/battle/update anchors are in the durable evidence note. The historical pre-amend pin (not final) was
`1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`.

### Closing native ItemType overflow repair (2026-08-18; pre-amend)

This production Event repair starts from `ffa3ef96ba7862456e668195fdea6207b69543a5`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. Fresh native RO jobs `j-bghfub` (pre-fix) and
`j-5l2ncz` (post-fix) returned `CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0` with `/game` read-only.
`MItems.cs=e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27` source line 11
proves raw `long type`; `MNetherFloorEventParts.cs=5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`
source lines 13-29 prove the raw Event fields. Game hashes are Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and metadata
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.

Public RED `j-lzlkcg` reproduced the positive `ItemType=2147483648` overflow at
`FindReward` (`1/1` failed); GREEN `j-ofgd9w` passed `1/1`. The warning-free focused/expanded/
full gates were `j-8dph58` `207/207`, `j-ie7n3h` `477/477`, and `j-1y9bfs` `1209/1209`.
Release `j-gwj93w` passed 0 warnings/0 errors, release audit, product isolation, and
`DIFF_CHECK_EXIT=0`. The shared `TryMapItemType` seam now makes visible, pre-entry, runtime,
and commitment binding option-local fail closed before any narrowing cast; no native conflict
or item-type semantic inference was introduced. Post-amend identity is authoritative only in
the final Docker audit and `refs/notes/logic-overhaul-evidence`.

### Procurement invalidation and source-domain repair (2026-08-18; pre-amend)

Pre-amend `HEAD=b837c5ce1822b3b05990ff34df62ad75a974877e`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The production bridge now compares the previous
authoritative snapshot before writing the incoming fingerprint and clears pending/committed/
route-owned procurement before pre-entry capture; the 150-cost Shop and rarity-five non-equipment
cases therefore remain unknown/without commitment. The producer accepts only known positive-quantity
Gold rows at 200/300/500 and rank-five equipment bags with `ItemType=91`.

RED `j-xyz67j/output.log:1-40` was 3 failed / 0 passed / 3 total; GREEN
`j-3l24iu/output.log:1-9` was 3/3. Focused `j-4gqmbu` was 123/123, expanded `j-u32ye3` was
189/189, full `j-eg7dic` was 1208/1208, and Release `j-hrbptk` was 0 warnings/0 errors. Fresh
RO native `j-p18rn7/output.log:1-28` (`CPP2IL_EXIT=0`, `DIFFABLE_EXIT=0`) has game hashes
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`; source anchors are raw
Event fields 13/15/25/27, raw battle fields 7-15, and native Event update lines 287-288.
`j-phv0bh/output.log:62` freshly confirms the native shop currency field is raw
`consume_content_type`. Native evidence does not add hidden budget or threshold semantics, so the
repository/spec domain remains exact and fail-closed. Final post-amend HEAD/parent/tree, product
isolation, diff check, and the note ref are recorded only after the last amend.

### Same-branch route identity and post-event invalidation repair (2026-08-18; pre-amend)

`NetherRoutePlan` now carries the exact selected horizon path and snapshot-bound branch identity;
`FromInteractivePreEntry` and visible Shop/Treasure procurement generation reject safe but
alternate graph branches. `NetherRuntimeBridge.BeginRouteReplan` clears the prior route proof,
and authoritative floor-scene confirmation plus snapshot mismatch retire it before later Event
semantics or payment. RED failed the two new coordinator tests (2/2); GREEN `j-0t56g0` passed
4/4, including the real controller alternate-branch E2E and bridge replan/invalidation seam.
The current pre-amend tree is `d38ed66c7bd247129266d01b53e0daa85f3a90a1`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`, tree
`4aa969ea9f6becde6afa9001411ad27b6e8cff19`.

Fresh native RO Cpp2IL `j-ard4dj/output.log:1-96` returned `CPP2IL_EXIT=0` and
`DIFFABLE_EXIT=0`. Its immutable game hashes are Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. Artifact hashes are
`MNetherFloorEventParts=5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`MNetherFloorEvents=aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`,
`MNetherFloorBattles=7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`, and
`NetherApiDataStore=b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`.
Anchors are raw Event target/parameter/content lines 4-29, raw battle fields 4-15, Event/Part
identity lines 4-25, and native update parameters 284-288. Native proves no Event/Part request
arguments or hidden budget field; no unsupported native mechanic was inferred.

Expanded `j-e6tp0m` passed 120/120, full `j-7lbau3` passed 1205/1205, clean Release restore/test
`j-54geyq` passed 1205/1205, Release build `j-bt997x` passed 0 warnings/0 errors, isolation
`j-xzpkyj` passed, and diff `j-xzpkyj` passed. All used `docker run --rm` with `/game` read-only.
Because this ticket is part of the amended tree, the final SHA cannot be embedded in this file
without changing it; after the last amend, exact final HEAD/parent/tree and gate IDs will be stored
in `refs/notes/logic-overhaul-evidence`.

### Current final-repair audit (2026-08-18; supersedes prior pins)

Prior SHA/count paragraphs above are historical pre-amend records and are not the final task-group
identity. The current implementation tree before the in-place amend is
`beb8824604298da985965bad332b24ac9d7845c7`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The final commit's own SHA cannot be embedded in
this ticket without changing the content-addressed commit; the post-amend Docker audit prints
the actual final `HEAD`, `HEAD^`, tree, and status and is authoritative.

Fresh native RO Cpp2IL is `j-cg7xis/output.log:1-161` (`GAME_MOUNT_READONLY=1`,
`CPP2IL_EXIT=0`, `DIFFABLE_EXIT=0`). It records immutable game hashes, `MNetherFloorEventParts`
raw target/parameter/content anchors lines 13-29, raw battle fields lines 7-15, Event/Part IDs
lines 7-25, and the floor/index/selected-number/Code update seam lines 287-288. It also records
MItems raw rarity and native rarity enums closed at 0..5; no hidden procurement or battle semantic
mapping was inferred. Public REDs were `j-x4qiyb` (3 failed / 1 passed / 4 rarity tests) and
`j-xlvwrk` (2 failed / 0 passed / 2 route-owned producer tests); GREEN was `j-b6bdes` (128/128),
expanded `j-g23akv` (337/337), full `j-n7g4ih` (1201/1201), Release `j-dbw8t4` (0 warnings/0
errors), and isolation/diff `j-1lixdk` (PASS/0). Every command used `docker run --rm` with
`C:/Users/Eden/PixelAbyssX/dotabyss_x_cl` mounted read-only at `/game`.

### P1 procurement-threshold priority repair (2026-08-18)

The exact production Event commitment now carries its committed Gold/key minimum into
threshold-aware priority. When the current balance is below the minimum and that option's
projected reward reaches it, the option outranks direct Code Offer and ordinary Gold semantics;
unrelated options remain candidate-local. RED `j-kmf1l2` reproduced Gold and key selecting the
Code Offer (2 failures), while GREEN `j-1ro4y6` selected the threshold option in both cases
(2/2). Fresh native RO Cpp2IL `j-86iu89` returned `CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`;
its exact hashes/anchors are recorded in the durable note. Current full gate records are
`j-l9m0j6` 111/111, `j-529vvn` 230/230, `j-nyu1bj` 1186/1186, `j-3ulak2` Release 0/0,
and `j-ju3ij7` read-only evidence audit. Historical pre-amend pin (not final):
`1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`.

### Final-gate route-owned procurement repair (2026-08-18)

The final procurement repair is now wired through the real production route seam: exact valid
option-projection budgets are carried by `NetherProductionRouteSafetyPlan`, published by
`NetherAutoClimbController.PlanRoute`, promoted by `NetherRuntimeBridge`, and retained by
`NetherRouteOwnedEventProcurementProducer` across repeated empty captures. Popup commitments are
promoted only after exact immutable Event binding. The former manual ScriptedRuntimeBridge seed
E2E was removed; the public real-bridge E2E captures positive Gold/Key minima twice and proves
unsafe spending is rejected before semantics. RED `j-vii6om` was 1 failed / 0 passed / 1 total;
GREEN `j-dyqez9` was 1/1, expanded repair `j-am5nzl` was 109/109, and full `j-vtk92z` was
1184/1184. Release `j-xw2sb3` reported 0 warnings/0 errors, isolation `j-6r4fg3` passed, and
the Docker diff check is recorded in the durable note.

Fresh native RO Cpp2IL `j-9lqbic` returned `CPP2IL_EXIT=0`/`DIFFABLE_EXIT=0`; game hashes are
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. Artifact hashes and exact
anchors are `j-9lqbic/output.log:32-86`: raw Event part target/parameter/content fields, raw
battle type/stage/drop fields, Event/part identity, and floor/index/option/Code-only native
update request. Native-first evidence therefore supports repository-owned exact commitments but
does not invent a hidden native procurement field. The historical evidence pin (not final) was
`1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`.
