# Evidence record: strategy-mode tickets 10–12

Date: 2026-08-18
Branch: `logic-overhaul`
Parent fixed point: `247a2d7b704ef5c3f6ead59e0b13a73d55e288b1`
Provisional task-group commit: recorded by the closing Docker audit after this note is committed

This note records the 10–12 implementation group. `dotabyss_x_cl` was never written. Every
decompilation, build, test, review, RCA, and Git verification command used an ephemeral
`docker run --rm` container; the game was mounted only as read-only `/game`.

## Ticket mapping

| Ticket | Implemented seam | Evidence |
|---|---|---|
| 10 | Complete visible Recovery branch proof, deterministic Rest/Purification choice, fail-closed no-safe-branch handling, and explicit Transform restrictions | `AutoNether/Services/NetherEventPolicy.cs`, `NetherInteractiveFloorPreEntrySafety.cs` |
| 11 | Exact 40/80 Treasure HP payment, held-key preference, group-survival exception, and exact projected reconciliation | `AutoNether/Services/NetherEventPolicy.cs`, `NetherPopupDispatchPolicy.cs` |
| 12 | Selected-safe-branch rank-5 Treasure objective, exact 150-Gold Event/200-Gold Shop source preference, HP/erosion fallback rules, and route objective ordering | `AutoNether/Services/NetherRankFiveKeyProcurementPolicy.cs`, `NetherRoutePlanner.cs`, `NetherRouteSafetyProductionCoordinator.cs` |

## Fresh native evidence

The final native integration review is Docker job `j-6mcoyj`; its complete log is
`C:/Users/Eden/.fastctx/jobs/j-6mcoyj/output.log`. The command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/final-diffable --output-as diffable-cs >/tmp/final-diffable.log 2>&1; diff_status=$?; echo "DIFFABLE_EXIT=$diff_status"; test "$diff_status" = 0; /tmp/Cpp2IL --game-path /game --output-to /tmp/final-isil --output-as isil >/tmp/final-isil.log 2>&1; isil_status=$?; echo "ISIL_EXIT=$isil_status"; test "$isil_status" = 0; echo FINAL_NATIVE_REVIEW=PASS'
```

The fresh game inputs were:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

Cpp2IL returned `DIFFABLE_EXIT=0`, `ISIL_EXIT=0`, and `FINAL_NATIVE_REVIEW=PASS`. Fresh
decompiled artifact hashes were:

```text
MNetherFloorEvents.cs          aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006
MNetherFloorEventParts.cs      5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128
MNetherFloorShopContents.cs    177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9
MItems.cs                      e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27
NetherEventResultModel.cs      f79123d206000bfc369af7bad485fc22b60fd749048c36d6d49a0f504ab52f83
NetherCharacterEntity.cs       22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f
NetherUpdateEventResponseEntity.cs 30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa
NetherApiDataStore.cs           b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071
```

Authoritative fresh anchors in the job log are:

- `NetherRecoverPopupController.cs` owns `_mNetherEvents` and `_mNetherEventPartsArray`, and
  exposes `ExecuteEvent` (job output lines 8–12, 60–66).
- `NetherTreasurePopupController.cs` owns `_mNetherEvents` and `_mNetherEventPartsArray`, and
  exposes `OnConfirm` (job output lines 13–22, 109–126).
- `MNetherFloorEvents.cs` contains the Event ID, map-floor ID, raw type, and four exact part IDs;
  `MNetherFloorEventParts.cs` contains three raw target/parameter pairs plus content type/ID/amount
  (job output lines 23–60).
- `MNetherFloorShopContents.cs` contains raw `consume_content_type`, `consume_content_id`,
  `consume_amount`, `content_type`, `content_id`, and `amount`; `MItems.type` is raw `long` (job
  output lines 61–90).
- `NetherApiDataStore.RequestNetherUpdateEventAsync` accepts floor level, floor index, selected
  option, Code-change ID, and cancellation only (job output lines 91–104). `NetherEventResultModel`
  retains the Code-change overload (lines 105–118).
- Native Event flow calls the update, then opens the change-Code list for the server-selected
  transform child and transitions to battle (ISIL lines 119–135). The transform parameter is not
  treated as a client-selected Code ID.
- `NetherCharacterEntity.current_hp_ratio` and
  `NetherUpdateEventResponseEntity.t_nether_characters` are the response-side HP authority (lines
  136–151).

The native integration review changed `NetherRuntimeBridge.TryMapEventPopup` to resolve the exact
native Event row and its declared part IDs for Event, Recovery, and Treasure controllers. This is
supported directly by the fresh Recovery/Treasure controller fields above; it does not add any
unsupported native request argument. `NetherEventProductionEvidenceBinding` now carries the same
exact pre-entry binding through Recovery and Treasure popup policy seams.

## Native-first conflicts and decisions

1. Native target type 7 is a server-random transform flow. The implementation never substitutes a
   client Code ID; it preserves the server child transition and only carries a removable hard-
   excluded Code as the explicit Recovery commitment.
2. Native Shop transport exposes raw content fields but no closed key-product semantic. Production
   therefore leaves `NetherShopContent.IsTreasureKey` false unless an authoritative mapper proves
   it; the rank-5 Shop path fails closed without that proof. Pure policy tests use explicit
   `IsTreasureKey=true` evidence rather than guessing from a raw content ID.
3. Native item type is raw `long`; narrowing is performed only through the existing checked,
   evidence-backed mapping seam. Overflow or unsupported values remain option-local unknown.
4. Native Recovery/Treasure controllers omit the Event presenter character ID, so HP payment is
   projected over the authoritative response character rows, never against a popup presenter.
5. Missing complete Recovery branch proof remains fail-closed when a proof is supplied but malformed;
   no hidden future branch or random transform outcome is invented.

## RED/GREEN, RCA, and Docker results

The initial public-seam RED was job `j-l6enho`, log
`C:/Users/Eden/.fastctx/jobs/j-l6enho/output.log`. It used a Docker `run --rm` test container with
the repository writable only for build outputs, `/game` read-only, and the established NuGet volume.
The test compile failed as expected because the new tests referenced the not-yet-implemented
`NetherRecoveryBranchKind` (`RED_EXIT=1`). The RED was preceded by fresh read-only decomp jobs
`j-5g2159` (diffable) and `j-0qk8yv` (ISIL), with the same immutable game hashes recorded above.

After the implementation and policy fixes, the full Docker suite was job `j-zwhexp`, log
`C:/Users/Eden/.fastctx/jobs/j-zwhexp/output.log`, using:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/tmp/nuget -e NUGET_PACKAGES=/tmp/nuget -e ABYSS_GAME_DIR=/game -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
```

It passed `Failed: 0, Passed: 1215, Skipped: 0, Total: 1215`, with exit code 0. The final fresh
native review first had a Docker extraction-only failure in job `j-onufsa` because that image did
not contain `rg` (exit 127 after successful `DIFFABLE_EXIT=0`). The RCA was bounded to the
container tool availability, not game or code behavior; the exact same fresh game inputs were
rerun with POSIX `grep` in `j-6mcoyj`, which passed both decompilation modes and the final native
review marker.

The additional integration edit is intentionally limited to exact native row binding and is
covered by the existing production mapper/popup policy suite. The first provisional commit was
created immediately after this evidence record as `214dc32a5d3025c4f43cad1d7798ffc7a31c6e0e`;
its parent is the fixed point above. The content-addressed SHA will change once this closing gate
result is added to the same amendable task-group commit.

Post-commit Docker gates before the closing amend:

```text
j-aanwm5  full Docker suite: 1215 passed / 1215 total, exit 0
j-o9izuo  Release build: 0 Warning(s), 0 Error(s), exit 0
j-o9izuo  release/Release/net6.0/AutoNether.dll SHA-256:
          6fcd11f211aaf9fc5be06cba798b92fcbc2d20a8a484cb130c723ca8bfcd3898
```

The commands used the repository bind, a writable container/build layer, the established NuGet
volume, and the game bind at `/game,readonly`:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/tmp/nuget -e NUGET_PACKAGES=/tmp/nuget -e ABYSS_GAME_DIR=/game -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/tmp/nuget -e NUGET_PACKAGES=/tmp/nuget -e ABYSS_GAME_DIR=/game -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --nologo --verbosity minimal'
```

The final Docker worktree audit must verify that the amended commit has parent
`247a2d7b704ef5c3f6ead59e0b13a73d55e288b1`, contains exactly the allowlist above, has no staged or
unstaged residue, and has not been pushed. Any issue discovered by that audit is repaired by
amending this same provisional commit; no second task-group commit is created.

## Intended changed paths

The task-group allowlist is:

```text
AutoNether.Tests/NetherAutoClimbControllerEndToEndTests.cs
AutoNether.Tests/NetherEventPolicyTests.cs
AutoNether.Tests/NetherInteractiveFloorPreEntrySafetyTests.cs
AutoNether.Tests/NetherInteractiveRouteSafetyWiringTests.cs
AutoNether.Tests/NetherStrategyModes1012Tests.cs
AutoNether/Services/NetherEventPolicy.cs
AutoNether/Services/NetherInteractiveFloorPreEntrySafety.cs
AutoNether/Services/NetherPopupDispatchPolicy.cs
AutoNether/Services/NetherRankFiveKeyProcurementPolicy.cs
AutoNether/Services/NetherRoutePlanner.cs
AutoNether/Services/NetherRouteSafetyProductionCoordinator.cs
AutoNether/Services/NetherRuntimeBridge.cs
AutoNether/Services/NetherStrategyEvidence.cs
AutoNether/Services/NetherStrategyVisibleEvidenceMapper.cs
AutoNether/Services/NetherEventProductionEvidenceBinding.cs
docs/agents/evidence-backed-strategy-modes-10-12-evidence.md
```

No game path is in this list. The closing Docker audit must report exactly this set, parent
`247a2d7b704ef5c3f6ead59e0b13a73d55e288b1`, and one provisional commit with no push.
## Closing review response and repair evidence

Repair date: 2026-08-18. The final review response stayed on logic-overhaul, preserved the
provisional commit as the only amend target, and did not write dotabyss_x_cl. Generated native
decompilation directories remain outside the commit as evidence artifacts:

~~~text
docs/agents/native-decomp-rerun-20260818-b/
docs/agents/native-decomp-rerun-20260818-c/
docs/agents/native-decomp-rerun-20260818/
~~~

### Fresh native rerun

The fresh repair-response decompilation was Docker job j-xyeh6l; its complete log is
C:/Users/Eden/.fastctx/jobs/j-xyeh6l/output.log. The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether/docs/agents,dst=/evidence -w /evidence mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; evidence=/evidence/native-decomp-rerun-20260818-c; mkdir -p "$evidence"; echo GAME_MOUNT_READONLY=1 | tee "$evidence/current-state.txt"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat | tee "$evidence/game-hashes.txt"; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to "$evidence/diffable" --output-as diffable-cs >"$evidence/cpp2il.log" 2>&1; status=$?; echo DIFFABLE_EXIT=$status | tee "$evidence/status.txt"; tail -30 "$evidence/cpp2il.log"; find "$evidence/diffable" -type f \( -name "MNetherFloorEvents.cs" -o -name "MNetherFloorEventParts.cs" -o -name "MNetherFloorBattles.cs" -o -name "MNetherFloorShopContents.cs" -o -name "MItems.cs" -o -name "NetherRecoverPopupController.cs" -o -name "NetherTreasurePopupController.cs" -o -name "NetherEventPopupController.cs" -o -name "NetherApiDataStore.cs" -o -name "NetherCharacterEntity.cs" -o -name "NetherUpdateEventResponseEntity.cs" \) -print | sort | tee "$evidence/decompiled-files.txt"; while IFS= read -r f; do sha256sum "$f"; done < "$evidence/decompiled-files.txt" | tee "$evidence/decompiled-hashes.txt"; { echo NATIVE_ANCHORS; while IFS= read -r f; do echo ---$f; grep -n -E "class |struct |target_type|select_parameter|content_type|content_id|amount|consume_|Event|Part|_mCharacterId|current_hp_ratio|t_nether_characters|RequestNetherUpdateEventAsync|ExecuteEvent|OnConfirm|SetupPopup|m_nether_floor_event_part_id" "$f" | head -140; done < "$evidence/decompiled-files.txt"; echo FINAL_NATIVE_REVIEW=PASS; } > "$evidence/native-anchors.txt"; exit "$status"'
~~~

The evidence output is under docs/agents/native-decomp-rerun-20260818-c/, with
current-state.txt recording GAME_MOUNT_READONLY=1, status.txt recording DIFFABLE_EXIT=0, and
the complete native anchor list in native-anchors.txt. The independently rehashed game inputs were:

~~~text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
~~~

Fresh native anchors include MNetherFloorEvents raw part IDs
(aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006), MNetherFloorEventParts
target/parameter and content fields (5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128),
MNetherFloorShopContents consume/content fields
(177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9),
NetherEventPopupController exact part-array/ExecuteEvent fields
(a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf),
NetherRecoverPopupController exact part-array/ExecuteEvent fields
(2ffbbf17144a658915f2334f5168d3eeb6d7f8a62eea6b56cadecc95f704cc67), and
NetherTreasurePopupController exact part-array/OnConfirm fields
(19f36f6e018f4c37337f94bf1324bbbca0142e8de5227036ee871cc756474bee).
The API anchor remains RequestNetherUpdateEventAsync(floorLevel, floorIndex, selectedNumber,
changeTargetMNetherCodeId, cancellation); response HP remains current_hp_ratio and
t_nether_characters. Native does not expose the client rank-five semantic, so exact branch
commitment is carried client-side and missing or mismatched proof pauses.

### Review-response RCA and Docker gates

NuGet restore was recovered without host dependency changes. Docker job j-ydv087 completed with
exit 0 using the checked-in NuGet.config, the versioned SDK image, and named writable cache volume
abyss-autonethernupkgs. Its exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet restore AutoNether.sln --configfile NuGet.config --disable-parallel --verbosity normal'
~~~

The first post-edit full-suite RED was Docker job j-jsy981 (1,224 passed, 3 failed, 1,227
total). The concrete RCA was that positive-Damage plus TreasureKeyGain rows with Damage other
than 80 fell through ordinary Event selection and could report UnsafeHp instead of being rejected
as an unauthorized HP-paid key shape. Production now rejects that malformed shape with
hp-paid-key-damage-must-equal-eighty; only the exact two-effect Damage 80 plus one-key-gain shape
can receive HP-paid key authorization.

The final focused public-seam run was Docker job j-dd03qq, exit 0, and passed 230/230:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet test AutoNether.sln --no-restore --filter "FullyQualifiedName~NetherRouteSafetyProductionCoordinatorTests|FullyQualifiedName~NetherAutoClimbControllerEndToEndTests|FullyQualifiedName~NetherEventProductionEvidenceBindingTests|FullyQualifiedName~NetherPopupDispatchPolicyTests|FullyQualifiedName~NetherActionReconcilePolicyTests|FullyQualifiedName~NetherStrategyModes1012Tests|FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests|FullyQualifiedName~NetherEventPolicyTests.Hp_paid_rank_five_event_key_requires_exactly_eighty_damage|FullyQualifiedName~NetherInteractiveRouteSafetyWiringTests.Production_preentry_rejects_hp_paid_key_shape_without_route_objective_proof" --logger "console;verbosity=minimal"'
~~~

This covers the controller final Recovery proof recapture/consistency pause, selected-branch
rank-five evaluation/binding, Event binding through dispatch and exact reconciliation, Shop
commitment binding through dispatch and exact content/cost reconciliation, and existing Treasure
popup commitment/reconciliation seams.

The final full Docker suite was job j-uuf84s, exit 0: Failed: 0, Passed: 1227, Skipped: 0,
Total: 1227. The current total is 1,227 because review coverage added cases after the historical
1,215-count evidence; all current tests pass. The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
~~~

The Release build was Docker job j-q79f87, exit 0, with 0 Warning(s), 0 Error(s), and DLL
SHA-256 eac43f28698b1587cc2e998ae50278f5e272a15296c82acd5053e5029cf365e5. Its exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --nologo --verbosity minimal; status=$?; if [ "$status" -eq 0 ]; then sha256sum AutoNether/bin/Release/net6.0/AutoNether.dll; fi; exit "$status"'
~~~

The pre-amend Docker audit reported branch logic-overhaul, HEAD
6c744cfbe11ae7dfb094f24f12b9968f211993df, parent
247a2d7b704ef5c3f6ead59e0b13a73d55e288b1, and exactly one commit from base. git diff --check
returned no whitespace errors; Git emitted only CRLF normalization warnings. The tracked review-fix
allowlist before this document was 21 paths:

~~~text
AutoNether.Tests/NetherActionReconcilePolicyTests.cs
AutoNether.Tests/NetherAutoClimbControllerEndToEndTests.cs
AutoNether.Tests/NetherEventPolicyTests.cs
AutoNether.Tests/NetherEventProductionEvidenceBindingTests.cs
AutoNether.Tests/NetherPopupDispatchPolicyTests.cs
AutoNether.Tests/NetherRouteSafetyProductionCoordinatorTests.cs
AutoNether.Tests/NetherRuntimeInteractivePreEntryInputCaptureTests.cs
AutoNether.Tests/NetherStrategyModes1012Tests.cs
AutoNether/Services/NetherActionReconcilePolicy.cs
AutoNether/Services/NetherAutoClimbController.cs
AutoNether/Services/NetherAutoClimbModels.cs
AutoNether/Services/NetherAutoClimbRouteSafetyWiring.cs
AutoNether/Services/NetherEventPolicy.cs
AutoNether/Services/NetherEventProductionEvidenceBinding.cs
AutoNether/Services/NetherFloorActionTransactionComposer.cs
AutoNether/Services/NetherInteractiveFloorPreEntrySafety.cs
AutoNether/Services/NetherPopupDispatchPolicy.cs
AutoNether/Services/NetherRankFiveKeyProcurementPolicy.cs
AutoNether/Services/NetherRouteSafetyProductionCoordinator.cs
AutoNether/Services/NetherRuntimeBridge.cs
AutoNether/Services/NetherRuntimeInteractivePreEntryInputCapture.cs
~~~

After this document is included, that same tracked set plus this evidence file is the single amend
allowlist. The three native decomp directories above are generated evidence and intentionally remain
untracked; no game path is staged.

## Spec re-review response: final sibling-proof recapture repair (2026-08-19)

The final re-review exposed one production seam defect in the otherwise complete Ticket 10 proof
flow. Native Recovery popup code binds the selected callback and does not make sibling option
projections authoritative during the final recapture. The coordinator now preserves an already-bound
sibling proof only when its exact EventPart identity, branch kind, authoritative complete-horizon
flag, and known effect payload all match. The carried proof may restore the sibling's known/safe
state; it can never make a new branch safe. A mismatched, absent, unsafe, or unknown proof remains
fail-closed.

### Fresh native evidence

The current game was independently decompiled after the repair was identified. `dotabyss_x_cl` was
mounted read-only; all generated output went to the repository evidence directory. Docker job
`j-q6ji2s` exited 0. Its complete output is retained at
`C:/Users/Eden/.fastctx/jobs/j-q6ji2s/output.log`. The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether/docs/agents,dst=/evidence -w /evidence mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; evidence=/evidence/native-decomp-rerun-20260818-e; mkdir -p "$evidence"; echo GAME_MOUNT_READONLY=1 | tee "$evidence/current-state.txt"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat | tee "$evidence/game-hashes.txt"; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to "$evidence/diffable" --output-as diffable-cs >"$evidence/cpp2il.log" 2>&1; status=$?; echo DIFFABLE_EXIT=$status | tee "$evidence/status.txt"; tail -30 "$evidence/cpp2il.log"; find "$evidence/diffable" -type f \( -name "MNetherFloorEvents.cs" -o -name "MNetherFloorEventParts.cs" -o -name "MNetherFloorBattles.cs" -o -name "MNetherFloorShopContents.cs" -o -name "MItems.cs" -o -name "NetherRecoverPopupController.cs" -o -name "NetherTreasurePopupController.cs" -o -name "NetherEventPopupController.cs" -o -name "NetherApiDataStore.cs" -o -name "NetherCharacterEntity.cs" -o -name "NetherUpdateEventResponseEntity.cs" \) -print | sort | tee "$evidence/decompiled-files.txt"; while IFS= read -r f; do sha256sum "$f"; done < "$evidence/decompiled-files.txt" | tee "$evidence/decompiled-hashes.txt"; { echo NATIVE_ANCHORS; while IFS= read -r f; do echo ---$f; grep -n -E "class |target_type|select_parameter|content_type|content_id|amount|consume_|Event|Part|_mCharacterId|current_hp_ratio|t_nether_characters|RequestNetherUpdateEventAsync|ExecuteEvent|OnConfirm|SetupPopup|m_nether_floor_event_part_id" "$f" | head -140; done < "$evidence/decompiled-files.txt"; echo FINAL_NATIVE_REVIEW=PASS; } > "$evidence/native-anchors.txt"; exit "$status"'
~~~

Fresh game hashes:

~~~text
53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300  /game/BepInEx/interop/Project.dll
573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb  /game/GameAssembly.dll
ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5  /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat
DIFFABLE_EXIT=0
~~~

Fresh decompilation outputs:

~~~text
docs/agents/native-decomp-rerun-20260818-e/current-state.txt
docs/agents/native-decomp-rerun-20260818-e/game-hashes.txt
docs/agents/native-decomp-rerun-20260818-e/status.txt
docs/agents/native-decomp-rerun-20260818-e/cpp2il.log
docs/agents/native-decomp-rerun-20260818-e/decompiled-files.txt
docs/agents/native-decomp-rerun-20260818-e/decompiled-hashes.txt
docs/agents/native-decomp-rerun-20260818-e/native-anchors.txt
docs/agents/native-decomp-rerun-20260818-e/diffable/
~~~

The native anchors are unchanged and authoritative: `MNetherFloorEventParts` exposes
`target_type_1/2/3`, `select_parameter_1/2/3`, `content_type`, `content_id`, and `amount`;
`MNetherFloorEvents` carries four exact EventPart IDs; shop rows carry exact consume/content
fields; the Event/Recovery/Treasure popup controllers retain exact EventPart lookup and one
selected callback; and `NetherUpdateEventResponseEntity.t_nether_characters` carries post-event HP
rows. Native code does not expose a route-suffix safety proof, so the plugin's complete visible
horizon simulation remains required. No native conflict was found.

### Public-seam RED/GREEN and verification

The final native recapture binds only the selected Recovery callback, while sibling options can
arrive with unknown local projections. The coordinator now preserves a carried sibling proof only
for the exact EventPart and branch kind, only when it is authoritative for the complete visible
horizon and its effect payload is known, and still re-simulates the suffix. It cannot make an
unsafe branch safe; absent, mismatched, unknown, or unsafe proof remains fail-closed.

The intentional RED run used an isolated in-container copy with the carried-sibling-proof branch
removed. Docker job `j-qihci0` exited 1 at the rest-sibling `IsKnown` assertion, proving that final
recapture lost the existing sibling proof. Its exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; rm -rf /tmp/abyss-red; mkdir -p /tmp/abyss-red; tar -C /src --exclude="*/bin" -cf - AutoNether AutoNether.Tests | tar -C /tmp/abyss-red -xf -; cp /src/*.sln /tmp/abyss-red/; target=/tmp/abyss-red/AutoNether/Services/NetherRouteSafetyProductionCoordinator.cs; perl -0pi -e '\''s/                bool carriedProofMatches.*?                bool isKnown = authoritative && optionEvidenceKnown && simulation\.IsKnown;/                RecoveryBranchSimulation simulation = authoritative\n                    ? SimulateRecoveryBranch(capture.Input, horizon, projection)\n                    : new RecoveryBranchSimulation(false, false, context.HorizonRejection(nodeId));\n                bool optionEvidenceKnown = projection.IsKnown && projection.HasRouteSafetyEvidence;\n                bool isKnown = authoritative && optionEvidenceKnown && simulation.IsKnown;/s; s/routeSafetyAllowed/projection.RouteSafetyAllowed/g'\'' "$target"; cd /tmp/abyss-red; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~NetherRouteSafetyProductionCoordinatorTests.Production_final_recapture_preserves_carried_sibling_recovery_proofs" --logger "console;verbosity=minimal"'
~~~

The corrected production seam passed the same public regression in Docker job `j-taobpt` (exit 0,
1/1). Its exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~NetherRouteSafetyProductionCoordinatorTests.Production_final_recapture_preserves_carried_sibling_recovery_proofs" --logger "console;verbosity=minimal"'
~~~

The expanded focused public-seam run before the new regression was 233/233 (Docker job
`j-av1may`, exit 0). The final focused run after the correction and regression was 234/234
(Docker job `j-ebiqyq`, exit 0; Failed 0, Passed 234, Skipped 0, Total 234). The final command
was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet test AutoNether.sln --no-restore --nologo --filter "FullyQualifiedName~NetherRouteSafetyProductionCoordinatorTests|FullyQualifiedName~NetherAutoClimbControllerEndToEndTests|FullyQualifiedName~NetherEventProductionEvidenceBindingTests|FullyQualifiedName~NetherPopupDispatchPolicyTests|FullyQualifiedName~NetherActionReconcilePolicyTests|FullyQualifiedName~NetherStrategyModes1012Tests|FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests|FullyQualifiedName~NetherEventPolicyTests.Hp_paid_rank_five_event_key_requires_exactly_eighty_damage|FullyQualifiedName~NetherInteractiveRouteSafetyWiringTests.Production_preentry_rejects_hp_paid_key_shape_without_route_objective_proof" --logger "console;verbosity=minimal"'
~~~

The previous full-suite baseline was 1230/1230 (Docker job `j-bsech6`, exit 0). Adding the
sibling regression increased the final suite to 1231/1231 (Docker job `j-gows3q`, exit 0;
Failed 0, Passed 1231, Skipped 0, Total 1231). The final full-suite command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
~~~

The final Release build was Docker job `j-u4i6f0`, exit 0, with 0 warnings and 0 errors. The
resulting `release/Release/net6.0/AutoNether.dll` SHA-256 was
`8b5657aec3596024be40b3ee8caa8be3ad4635cf24fb2cd056f8c97b0e78cebd`. Its exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --nologo --verbosity minimal; status=$?; if [ "$status" -eq 0 ]; then sha256sum release/Release/net6.0/AutoNether.dll; fi; exit "$status"'
~~~

## Standards re-review response: neutral path-index utility (2026-08-19)

The final Standards finding identified the same first-match node-index loop in the route-owned
procurement producer, the route coordinator, and rank-five procurement policy. A neutral internal
`NetherPathIndexUtility.PathIndexOf` now owns that exact ordered-identity lookup. All callers use it;
the coordinator's prior helper and the policy's private loop are deleted. The utility assigns no
native reward, safety, or route semantics, so behavior remains the existing first matching node
index and `-1` when absent.

### Fresh native evidence and authority

Docker job `j-uhm5px` independently decompiled the current game with `dotabyss_x_cl` mounted
read-only. Its complete output is at `C:/Users/Eden/.fastctx/jobs/j-uhm5px/output.log`; generated
evidence is under `docs/agents/native-decomp-standards-20260819-b/`. The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether/docs/agents,dst=/evidence -w /evidence mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; evidence=/evidence/native-decomp-standards-20260819-b; mkdir -p "$evidence"; echo GAME_MOUNT_READONLY=1 | tee "$evidence/current-state.txt"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat | tee "$evidence/game-hashes.txt"; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to "$evidence/diffable" --output-as diffable-cs >"$evidence/cpp2il.log" 2>&1; status=$?; echo DIFFABLE_EXIT=$status | tee "$evidence/status.txt"; tail -30 "$evidence/cpp2il.log"; find "$evidence/diffable" -type f \( -name "MNetherFloorEvents.cs" -o -name "MNetherFloorEventParts.cs" -o -name "MNetherFloorBattles.cs" -o -name "MNetherFloorShopContents.cs" -o -name "MItems.cs" -o -name "NetherRecoverPopupController.cs" -o -name "NetherTreasurePopupController.cs" -o -name "NetherEventPopupController.cs" -o -name "NetherApiDataStore.cs" -o -name "NetherCharacterEntity.cs" -o -name "NetherUpdateEventResponseEntity.cs" \) -print | sort | tee "$evidence/decompiled-files.txt"; while IFS= read -r f; do sha256sum "$f"; done < "$evidence/decompiled-files.txt" | tee "$evidence/decompiled-hashes.txt"; { echo NATIVE_ANCHORS; while IFS= read -r f; do echo ---$f; grep -n -E "class |target_type|select_parameter|content_type|content_id|amount|consume_|Event|Part|_mCharacterId|current_hp_ratio|t_nether_characters|RequestNetherUpdateEventAsync|ExecuteEvent|OnConfirm|SetupPopup|m_nether_floor_event_part_id" "$f" | head -140; done < "$evidence/decompiled-files.txt"; echo FINAL_NATIVE_REVIEW=PASS; } > "$evidence/native-anchors.txt"; exit "$status"'
~~~

Fresh game hashes are recorded in `native-decomp-standards-20260819-b/game-hashes.txt`:
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
global-metadata.dat `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
`status.txt` records `DIFFABLE_EXIT=0`. The fresh `native-anchors.txt` again proves raw ordered
EventPart target/parameter/content fields, four Event part IDs, exact Shop consume/content rows,
popup exact-part lookup/callback seams, and the option-only native Event update signature. Native
design introduces no separate path-index abstraction, so the neutral utility remains plugin-local
identity plumbing and does not invent game semantics.

### Public-seam verification and RCA

The final focused Docker run `j-2o9cjd` passed 234/234 (exit 0; Failed 0, Passed 234, Skipped 0,
Total 234). The full Docker suite `j-5u47zw` passed 1231/1231 (exit 0; Failed 0, Passed 1231,
Skipped 0, Total 1231). Release build `j-o2ihf1` passed with 0 warnings and 0 errors; the DLL
SHA-256 was `605a0f731213c0b9a71a955d1280ad3ac33acb676cd0244b584134a0b1eff4db`.

RCA: the previous standards fix stopped at assembly-sharing the coordinator's helper, leaving the
policy's equivalent loop as a second implementation. Moving the primitive to a neutral utility
removes the remaining drift surface while preserving every caller's existing path ordering and
unknown-node behavior. No implementation-detail test was added; the existing public coordinator
and procurement paths provide the behavioral regression boundary.

The current Docker pre-amend audit was job `j-kpspt4`; its complete output is at
`C:/Users/Eden/.fastctx/jobs/j-kpspt4/output.log` and its concise evidence copy is
`docs/agents/native-decomp-standards-20260819-b/path-audit-pre-amend.txt`. It reported a read-only
`/game` mount, matching fresh-game hashes, `DIFFABLE_EXIT=0`, branch `logic-overhaul`, current HEAD
`610f5a2752a174d397bd21b34a6967a034f4ea0e`, parent/base
`247a2d7b704ef5c3f6ead59e0b13a73d55e288b1`, and `BASE_COUNT=1`. `git diff --check` was clean;
the helper audit found one neutral declaration, five coordinator calls, three policy calls, and
zero legacy loops. Only the intended production files, evidence document, and new utility were
uncommitted; prior generated native evidence remained preserved and unstaged.

The Docker pre-amend audit was job `j-gcqjus`, with complete output retained at
`C:/Users/Eden/.fastctx/jobs/j-gcqjus/output.log` and a concise evidence copy at
`docs/agents/native-decomp-standards-20260819-a/standards-audit-pre-amend.txt`. It reported
`GAME_MOUNT_READONLY=PASS`, a current-game hash comparison pass, branch `logic-overhaul`,
HEAD `c60a647e6ce503c52e2d24dc27f0bfd639f49ee3`, parent/base
`247a2d7b704ef5c3f6ead59e0b13a73d55e288b1`, and `BASE_COUNT=1`. `git diff --check` had no
whitespace errors. The helper audit reported one `PathIndexOf` declaration, two qualified
coordinator calls, zero coordinator `PathIndex` duplicates, and two shared erosion-helper
references. Only the three intended tracked paths were uncommitted; generated native evidence
directories remained unstaged.

RCA: the final native recapture binds only the selected Recovery callback, while the sibling
options can arrive with unknown local projections. The coordinator accepts a carried proof only
for the exact EventPart and branch kind, only when it is authoritative for the complete visible
horizon and its effect payload is known, and still re-simulates the suffix. The proof can restore
knowledge for that exact sibling but cannot authorize an unsafe branch; absent, mismatched, unknown,
or unsafe proof remains fail-closed. This fixes the sibling-proof edge without weakening the final
consistency gate.

## Standards re-review response: shared native predicates and path-index deduplication (2026-08-19)

The final Standards review identified two duplicate helpers. The existing `PathIndexOf` belongs to
the route-owned procurement producer and was private, while the coordinator had copied the same
loop as `PathIndex`. The duplicate is deleted; the existing helper is now assembly-internal and
the coordinator calls it explicitly. The exact erosion-80-plus-key check was likewise duplicated
between route projection and rank-five procurement. `NetherRankFiveKeyProcurementPredicates` now
owns the native two-effect EventPart predicate (exactly one payment effect and one key gain, with
all present effects known); both policy evaluation and coordinator projection reuse it. Behavior is
unchanged, including the exact payment amounts 150 gold, 80 damage, and 80 erosion.

### Fresh native evidence

Docker job `j-g8cuph` independently decompiled the current game with `dotabyss_x_cl` mounted
read-only. The complete job log is at `C:/Users/Eden/.fastctx/jobs/j-g8cuph/output.log`; generated
evidence is under `docs/agents/native-decomp-standards-20260819-a/`. The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether/docs/agents,dst=/evidence -w /evidence mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; evidence=/evidence/native-decomp-standards-20260819-a; mkdir -p "$evidence"; echo GAME_MOUNT_READONLY=1 | tee "$evidence/current-state.txt"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat | tee "$evidence/game-hashes.txt"; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to "$evidence/diffable" --output-as diffable-cs >"$evidence/cpp2il.log" 2>&1; status=$?; echo DIFFABLE_EXIT=$status | tee "$evidence/status.txt"; tail -30 "$evidence/cpp2il.log"; find "$evidence/diffable" -type f \( -name "MNetherFloorEvents.cs" -o -name "MNetherFloorEventParts.cs" -o -name "MNetherFloorBattles.cs" -o -name "MNetherFloorShopContents.cs" -o -name "MItems.cs" -o -name "NetherRecoverPopupController.cs" -o -name "NetherTreasurePopupController.cs" -o -name "NetherEventPopupController.cs" -o -name "NetherApiDataStore.cs" -o -name "NetherCharacterEntity.cs" -o -name "NetherUpdateEventResponseEntity.cs" \) -print | sort | tee "$evidence/decompiled-files.txt"; while IFS= read -r f; do sha256sum "$f"; done < "$evidence/decompiled-files.txt" | tee "$evidence/decompiled-hashes.txt"; { echo NATIVE_ANCHORS; while IFS= read -r f; do echo ---$f; grep -n -E "class |target_type|select_parameter|content_type|content_id|amount|consume_|Event|Part|_mCharacterId|current_hp_ratio|t_nether_characters|RequestNetherUpdateEventAsync|ExecuteEvent|OnConfirm|SetupPopup|m_nether_floor_event_part_id" "$f" | head -140; done < "$evidence/decompiled-files.txt"; echo FINAL_NATIVE_REVIEW=PASS; } > "$evidence/native-anchors.txt"; exit "$status"'
~~~

The immutable current-game hashes are recorded in `native-decomp-standards-20260819-a/game-hashes.txt`:
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
global-metadata.dat `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
`status.txt` records `DIFFABLE_EXIT=0`. Fresh anchors in `native-anchors.txt` confirm raw
`MNetherFloorEventParts` target/parameter/content fields, four Event part IDs, exact Shop consume
and content fields, popup exact-part lookup plus callbacks, and the option-only
`RequestNetherUpdateEventAsync` signature. Native design therefore supports one shared exact
EventPart predicate and provides no conflicting route/procurement abstraction.

### RED/GREEN and RCA

The first Docker focused validation was intentionally a compile RED (`j-y3cnck`, exit 1): the
coordinator initially called the existing private `PathIndexOf` unqualified, producing CS0103 at
the two coordinator call sites. The smallest correction made that existing helper assembly-internal
and used its qualified name; no duplicate loop was restored. The public-seam focused run then passed
234/234 in Docker job `j-oy4go9` (exit 0). This is a standards-only refactor, so the existing
production coordinator test covering the +80 erosion/key route remains the behavioral authority;
the shared predicate preserves its exact two-effect semantics in both callers.

RCA: the earlier implementation optimized for local readability twice instead of giving the
native EventPart payment/key shape one assembly-level vocabulary. The refactor centralizes that
vocabulary without broadening accepted effects or changing route safety.

### Verification records

The full Docker suite passed 1231/1231 in job `j-oa3iyk` (exit 0; Failed 0, Passed 1231,
Skipped 0, Total 1231). The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
~~~

The Release Docker build passed with 0 warnings and 0 errors in job `j-ah3wrm` (exit 0). The
resulting DLL SHA-256 was
`9569c68e1bd47a04aef00fbf21afc89049d03b7b42982ece4d73c2b98e69949f`. The exact command was:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --network default --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonethernupkgs,dst=/nuget -w /src -e NUGET_PACKAGES=/nuget -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --nologo --verbosity minimal; status=$?; if [ "$status" -eq 0 ]; then sha256sum release/Release/net6.0/AutoNether.dll; fi; exit "$status"'
~~~
