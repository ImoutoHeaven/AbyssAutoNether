# Tickets 16–17 evidence ledger

This ledger covers the current implementation cycle on `logic-overhaul`.
It is local repository evidence only; no remote issue or label state is
modified. The fixed point for the cycle is
`0982cbc89bd70848694b45754dad47c8780fb13b`.

## Fresh native evidence — task16-17-fresh-20260819-a

Collected 2026-08-19 in Docker before the first RED. The game directory was
mounted read-only exactly as required and all Cpp2IL output stayed in the
container under `/tmp`.

Command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; printf "%s\n" "NATIVE_EVIDENCE_ID=task16-17-fresh-20260819-a"; printf "%s\n" "GAME_MOUNT_READONLY=1"; mount | grep " /game " || true; test -r /game/GameAssembly.dll && printf "%s\n" "GAME_READ_OK=1"; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; rm -rf /tmp/task16-17-diffable /tmp/task16-17-isil; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-diffable --output-as diffable-cs > /tmp/task16-17-diffable.log 2>&1; DIFFABLE_EXIT=$?; printf "%s\n" "CPP2IL_DIFFABLE_EXIT=$DIFFABLE_EXIT"; grep -m1 "Version" /tmp/task16-17-diffable.log || true; grep -m1 "Determined.*unity version" /tmp/task16-17-diffable.log || true; if test "$DIFFABLE_EXIT" -eq 0; then for f in /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs; do if test -f "$f"; then sha256sum "$f"; fi; done; grep -n -E "RequestNetherUpdateEventAsync|current_hp_ratio|target_type_[123]|select_parameter_[123]|content_type|content_id|amount|consume_content|code_drop_ratio|battle_stage|public int type|m_nether_floor_event_part_id_[1-4]|InitializeView|ExecuteEvent|OnConfirm|SetupPopupEvent" /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs | head -n 180; fi; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-isil --output-as isil > /tmp/task16-17-isil.log 2>&1; ISIL_EXIT=$?; printf "%s\n" "CPP2IL_ISIL_EXIT=$ISIL_EXIT"; grep -m1 "Version" /tmp/task16-17-isil.log || true; grep -E "Processed assemblies|Done\\. Total execution time|Finished outputting" /tmp/task16-17-isil.log | tail -n 5 || true; if test "$DIFFABLE_EXIT" -eq 0 && test "$ISIL_EXIT" -eq 0; then printf "%s\n" "NATIVE_EVIDENCE_EXIT=0"; else printf "%s\n" "NATIVE_EVIDENCE_EXIT=1"; fi'
```

Immutable input hashes:

| input | SHA-256 |
| --- | --- |
| `/game/BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `/game/GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `/game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

Container markers: `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`. Cpp2IL was
`2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224` and
reported Unity `6000.3.8f1`.

Diffable artifact hashes used as immutable anchors:

| artifact | SHA-256 |
| --- | --- |
| `Api/NetherApiDataStore.cs` | `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` |
| `Api/NetherCharacterEntity.cs` | `22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f` |
| `Api/NetherUpdateEventResponseEntity.cs` | `30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa` |
| `Master/NoaMessagePack/MItems.cs` | `e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27` |
| `Master/NoaMessagePack/MNetherFloorBattles.cs` | `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` |
| `Master/NoaMessagePack/MNetherFloorEventParts.cs` | `5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` |
| `Master/NoaMessagePack/MNetherFloorEvents.cs` | `aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006` |
| `Master/NoaMessagePack/MNetherFloorShopContents.cs` | `177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9` |
| `Nether/NetherEventPopup/NetherEventPopupController.cs` | `a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf` |
| `Nether/NetherRecoverPopup/NetherRecoverPopupController.cs` | `2ffbbf17144a658915f2334f5168d3eeb6d7f8a62eea6b56cadecc95f704cc67` |
| `Nether/NetherTreasurePopup/NetherTreasurePopupController.cs` | `19f36f6e018f4c37337f94bf1324bbbca0142e8de5227036ee871cc756474bee` |

Relevant native anchors: `NetherApiDataStore.RequestNetherUpdateEventAsync`
has `(floorLevel, floorIndex, selectedNumber, changeTargetMNetherCodeId,
CancellationToken)`; `NetherCharacterEntity.current_hp_ratio` is a native
field; `MNetherFloorBattles` exposes `type`,
`m_nether_battle_stage_id`, and `code_drop_ratio`; event parts expose
`target_type_1..3`, `select_parameter_1..3`, `content_type`, `content_id`,
and `amount`; events expose `type` and part IDs 1–4; shop contents expose
consume and reward type/id/amount fields. Event/recovery/treasure popup
controllers expose the native `InitializeView`, `ExecuteEvent`,
`SetupPopupEvent`, and treasure `OnConfirm` control-flow anchors.

These anchors support the existing native-first deviations: route semantics
must remain typed and fail closed when native event/battle proof is absent;
the API call is the transaction boundary; raw display power is not a
decision input; and unknown data is rejected locally rather than guessed.

## RED — task16-17-red-20260819-a

Fresh native evidence `task16-17-fresh-20260819-a` was collected before this
RED. The focused Docker command used a read-only repository copy, the exact
read-only `/game` mount, and an ephemeral output path:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; mkdir -p /tmp/repo; tar -C /src --exclude=./.git --exclude=./docs/agents/native-decomp-* -cf - . | tar -C /tmp/repo -xf -; dotnet test /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj --filter FullyQualifiedName~NetherStrategyModes1617Tests --configuration Debug -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/task16-17-red-out/ --logger "console;verbosity=minimal"; status=$?; printf "%s\n" "RED_TEST_EXIT=$status"; if test "$status" -ne 0; then printf "%s\n" "RED_EXPECTED=1"; else printf "%s\n" "RED_EXPECTED=0"; fi; exit 0'
```

Result markers: `RED_TEST_EXIT=1`, `RED_EXPECTED=1`. Restore completed and
the production project compiled to `/tmp/task16-17-red-out/Debug/net6.0/`;
the test project then failed on the intentionally absent 16/17 public seam
(`EvidenceVersion`, `StrategyMode`, typed route/code audit fields, and
`Decision`/`Transition` audit kinds). No repository or game path was written.

## Fresh native evidence — task16-17-fresh-20260819-b

Collected after RED and immediately before GREEN. The exact command was the
fresh-native command in the first section with the immutable evidence ID and
container output paths changed from `task16-17-fresh-20260819-a`,
`/tmp/task16-17-diffable`, `/tmp/task16-17-isil` to
`task16-17-fresh-20260819-b`, `/tmp/task16-17-diffable-b`, and
`/tmp/task16-17-isil-b` respectively; the command still ran both
`--output-as diffable-cs` and `--output-as isil`, the hash loop, and the same
anchor grep. This is the exact native command invocation prefix:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; printf "%s\n" "NATIVE_EVIDENCE_ID=task16-17-fresh-20260819-b"; printf "%s\n" "GAME_MOUNT_READONLY=1"; mount | grep " /game " || true; test -r /game/GameAssembly.dll && printf "%s\n" "GAME_READ_OK=1"; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; ...; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-diffable-b --output-as diffable-cs; ...; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-isil-b --output-as isil; ...'
```

The command exited with `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`. The three immutable game
hashes and all eleven selected diffable artifact hashes were identical to the
first evidence table; Cpp2IL again reported
`2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224` and Unity
`6000.3.8f1`. The API, HP, battle, event, shop, and popup anchors were all
present at the same exact lines.

## GREEN — task16-17-green-20260819-a

Fresh native evidence `task16-17-fresh-20260819-b` preceded this focused
test. Exact Docker command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; mkdir -p /tmp/repo; tar -C /src --exclude=./.git --exclude=./docs/agents/native-decomp-* -cf - . | tar -C /tmp/repo -xf -; dotnet test /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj --filter FullyQualifiedName~NetherStrategyModes1617Tests --configuration Debug -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/task16-17-green-out/ --logger "console;verbosity=minimal"; status=$?; printf "%s\n" "GREEN_TEST_EXIT=$status"; if test "$status" -eq 0; then printf "%s\n" "GREEN_EXPECTED=0"; else printf "%s\n" "GREEN_EXPECTED=1"; fi; exit 0'
```

Result markers: `GREEN_TEST_EXIT=0`, `GREEN_EXPECTED=0`; all 5 focused tests
passed. Production and test outputs stayed under
`/tmp/task16-17-green-out/`.

## Fresh native evidence — task16-17-fresh-20260819-c/d/e

These three independent reruns were collected before the corresponding
expanded-test retry, characterization additions, and focused GREEN cycles. Each
used the same literal Docker Cpp2IL command as `task16-17-fresh-20260819-a`,
with only the evidence ID and the two container output directory names changed
to `c`, `d`, and `e`. All three ran both `diffable-cs` and `isil` under `/tmp`
and exited with `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`.

The immutable Project.dll, GameAssembly.dll, metadata hashes, all eleven
diffable artifact hashes, Cpp2IL version, Unity version, and the API/HP/
battle/event/shop/popup anchors were byte-for-byte identical to the tables and
anchors above. This rules out a moving native input between the cycles.

## Fresh native evidence — task16-17-fresh-20260819-f

Collected before the additional typed-version, route-selection, and duplicate-
candidate characterization GREEN. Exact command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; printf "%s\n" "NATIVE_EVIDENCE_ID=task16-17-fresh-20260819-f"; printf "%s\n" "GAME_MOUNT_READONLY=1"; mount | grep " /game " || true; test -r /game/GameAssembly.dll && printf "%s\n" "GAME_READ_OK=1"; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; rm -rf /tmp/task16-17-diffable-f /tmp/task16-17-isil-f; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-diffable-f --output-as diffable-cs > /tmp/task16-17-diffable-f.log 2>&1; DIFFABLE_EXIT=$?; printf "%s\n" "CPP2IL_DIFFABLE_EXIT=$DIFFABLE_EXIT"; grep -m1 "Version" /tmp/task16-17-diffable-f.log || true; grep -m1 "Determined.*unity version" /tmp/task16-17-diffable-f.log || true; if test "$DIFFABLE_EXIT" -eq 0; then for file in /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs; do sha256sum "$file"; done; grep -nE "RequestNetherUpdateEventAsync|current_hp_ratio|class MNetherFloorBattles|m_nether_battle_stage_id|code_drop_ratio|target_type_1|select_parameter_1|content_type|content_id|amount|class MNetherFloorEvents|event_part_id_1|InitializeView|ExecuteEvent|SetupPopupEvent|OnConfirm" /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-diffable-f/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs; fi; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-isil-f --output-as isil > /tmp/task16-17-isil-f.log 2>&1; ISIL_EXIT=$?; printf "%s\n" "CPP2IL_ISIL_EXIT=$ISIL_EXIT"; printf "%s\n" "NATIVE_EVIDENCE_EXIT=$(( DIFFABLE_EXIT == 0 && ISIL_EXIT == 0 ? 0 : 1 ))"; test "$DIFFABLE_EXIT" -eq 0 && test "$ISIL_EXIT" -eq 0'
```

Markers were `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`. The game hashes were
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` in the
same order as the immutable input table. All selected artifact hashes and
anchors matched the first table exactly.

## Focused characterization GREEN — task16-17-green-20260819-b

Fresh native evidence `task16-17-fresh-20260819-f` preceded this run. The
read-only Docker test command copied source into the container and placed all
outputs under `/tmp/repo`:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; mkdir -p /tmp/repo; tar -C /src --exclude=./.git --exclude=./docs/agents/native-decomp-* -cf - . | tar -C /tmp/repo -xf -; dotnet test /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj --filter FullyQualifiedName~NetherStrategyModes1617Tests --configuration Debug -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-focused-f-out/ --logger "console;verbosity=minimal"; status=$?; printf "%s\n" "FOCUSED_F_TEST_EXIT=$status"; printf "%s\n" "FOCUSED_F_EXPECTED=0"; exit 0'
```

Result: `FOCUSED_F_TEST_EXIT=0`, `FOCUSED_F_EXPECTED=0`; 8/8 focused tests
passed.

## RCA — first expanded-test failure and repair

Fresh native evidence `task16-17-fresh-20260819-c` preceded the first expanded
run, and `task16-17-fresh-20260819-d` preceded the retry. The first Docker
repro used `-p:BaseOutputPath=/tmp/task16-17-expanded-out/` outside the copied
repository. It produced `EXPANDED_TEST_EXIT=1`: 1301 total, 1276 passed, 25
failed. Twenty-four failures were existing repository-root discovery tests;
one was the intentionally changed Code decision audit contract still
expecting `audit=interactive`.

Falsifiable hypotheses and results:

1. The game or mount had changed: falsified by both fresh native runs' matching
   three game hashes, Cpp2IL artifact hashes, and read-only markers.
2. The source copy omitted the solution/root: falsified by the copied solution
   and production assembly being present; the failure was the output directory
   ancestor relationship.
3. The test runner could not find the repository because `AppContext.BaseDirectory`
   was outside the copied repository: confirmed by the 24 root-discovery
   failures and falsified by moving `BaseOutputPath` below `/tmp/repo`.
4. The new audit family broke decision behavior: falsified as a production
   behavior regression; one test assertion alone still described the old
   contract, and was updated to `audit=decision` for the Code decision/candidate
   records.

Repair: keep the test output beneath `/tmp/repo` and update the affected
characterization expectation. The retry used the same exact Docker mounts and
`-p:BaseOutputPath=/tmp/repo/.task16-17-expanded-out/`; it returned
`EXPANDED_RETRY_EXIT=0`, `EXPANDED_RETRY_EXPECTED=0`, with 1301/1301 passed,
0 failed, and 0 skipped.

## Cycle status

- RED: complete (`task16-17-red-20260819-a` and
  `task16-17-context-red-20260819-a`), with fresh native evidence
  `task16-17-fresh-20260819-a` and `task16-17-fresh-20260819-h`.
- GREEN: focused seam complete (`task16-17-green-20260819-a`, 5/5) and
  characterization expansion complete (`task16-17-green-20260819-b`, 8/8),
  including the audit-context seam (`task16-17-context-green-20260819-a`, 9/9).
- RCA: complete for the expanded-run harness/contract failure above; the
  corrected retry and context-audit repair are green. The context RED was an
  expected missing-seam failure, not a product/runtime failure.
- Review: pending dual-reviewer convergence. Build/audit: complete after fresh
  native evidence `task16-17-fresh-20260819-j`; see the final clean Docker gates
  below.

The previous `docs/agents/native-decomp-*` directories are pre-existing user
evidence and remain untracked and untouched.

## Fresh native evidence — task16-17-fresh-20260819-g

Collected after the candidate-audit refinement and before the final focused
GREEN. The exact command was the same full Cpp2IL diffable+ISIL command shown
for `task16-17-fresh-20260819-f`, with literal substitutions only for the
evidence ID and `/tmp/task16-17-diffable-g`/`/tmp/task16-17-isil-g` output
directories. It returned `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`.

The immutable game hashes were again Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
Cpp2IL was `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
and Unity was `6000.3.8f1`. All eleven artifact hashes and all native anchors
matched the first table exactly.

## Focused characterization GREEN — task16-17-green-20260819-c

Fresh native evidence `task16-17-fresh-20260819-g` preceded this run. It used
the same read-only source/game Docker mounts and an ephemeral
`/tmp/repo/.task16-17-focused-g-out/` output directory. Result markers were
`FOCUSED_G_TEST_EXIT=0` and `FOCUSED_G_EXPECTED=0`; all 8/8
`NetherStrategyModes1617Tests` passed.

## Fresh native evidence — task16-17-fresh-20260819-h

Collected immediately before the audit-context characterization RED. The exact
command was the full Cpp2IL diffable+ISIL command shown for
`task16-17-fresh-20260819-f`, with the immutable evidence ID changed to
`task16-17-fresh-20260819-h` and the container output directories changed to
`/tmp/task16-17-diffable-h` and `/tmp/task16-17-isil-h`. It used the exact
read-only `/game` mount and wrote all decompilation output under `/tmp`.

Markers were `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`. Cpp2IL again reported
`2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224` and Unity
`6000.3.8f1`. Project.dll, GameAssembly.dll, metadata, all eleven selected
artifact hashes, and the API/HP/battle/event/shop/popup anchors matched the
immutable table above byte-for-byte.

## RED — task16-17-context-red-20260819-a

Fresh native evidence `task16-17-fresh-20260819-h` preceded this RED. Exact
Docker command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; mkdir -p /tmp/repo; tar -C /src --exclude=./.git --exclude=./docs/agents/native-decomp-* -cf - . | tar -C /tmp/repo -xf -; dotnet test /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj --filter FullyQualifiedName~NetherStrategyModes1617Tests --configuration Debug -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-context-red-out/ --logger "console;verbosity=minimal"; status=$?; printf "%s\n" "CONTEXT_RED_TEST_EXIT=$status"; if test "$status" -ne 0; then printf "%s\n" "CONTEXT_RED_EXPECTED=1"; else printf "%s\n" "CONTEXT_RED_EXPECTED=0"; fi; exit 0'
```

Result: `CONTEXT_RED_TEST_EXIT=1`, `CONTEXT_RED_EXPECTED=1`. The intentional
failure was the missing `NetherStrategyAuditFormatting` characterization seam
referenced by the new behavior test; no game or repository path was written.

## Fresh native evidence — task16-17-fresh-20260819-i

Collected after the context RED and immediately before its GREEN. The exact
command was the full Cpp2IL diffable+ISIL command shown for
`task16-17-fresh-20260819-f`, with ID `task16-17-fresh-20260819-i` and output
directories `/tmp/task16-17-diffable-i` and `/tmp/task16-17-isil-i`. It returned
the same read-only markers, immutable three game hashes, eleven artifact
hashes, Cpp2IL/Unity versions, and native anchors as `h`.

## GREEN — task16-17-context-green-20260819-a

Fresh native evidence `task16-17-fresh-20260819-i` preceded this GREEN. The
same read-only Docker test shape as the context RED used
`/tmp/repo/.task16-17-context-green-out/`; it returned
`CONTEXT_GREEN_TEST_EXIT=0`, `CONTEXT_GREEN_EXPECTED=0`, with 9/9 focused
characterization tests passed. The implementation now emits bounded mode,
primary/secondary/active target, target-state, typed unknown, owner generation,
entered-subscene generation, and snapshot-fingerprint fields on decision/route
audit records, plus the complete typed route semantic vector.

## Fresh native evidence — task16-17-fresh-20260819-j

Collected after the context-audit GREEN and immediately before the final
expanded/full verification. The exact command was the same full Cpp2IL
diffable+ISIL command with ID `task16-17-fresh-20260819-j` and output
directories `/tmp/task16-17-diffable-j` and `/tmp/task16-17-isil-j`. It returned
`GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`, `GAME_WRITE_CHECK=readonly`,
`CPP2IL_DIFFABLE_EXIT=0`, `CPP2IL_ISIL_EXIT=0`, and
`NATIVE_EVIDENCE_EXIT=0`; all immutable hashes, artifact hashes, versions, and
anchors matched the first native table.

## Prior clean Docker gates — task16-17-final-audit-pre-context

The final audit used the exact read-only repository and game mounts, verified
the fixed-point diff with `git diff --check 0982cbc89bd70848694b45754dad47c8780fb13b --`,
and allowed only the task-group paths plus pre-existing
`docs/agents/native-decomp-*` directories. Markers were
`AUDIT_GAME_MOUNT_READONLY=1`, `AUDIT_GAME_READ_OK=1`,
`AUDIT_GAME_WRITE_CHECK=readonly`, `DIFF_CHECK_EXIT=0`,
`WORKTREE_PATH_AUDIT=1`, `FINAL_FULL_TEST_EXIT=0`,
`FINAL_RELEASE_RESTORE_EXIT=0`, `FINAL_RELEASE_BUILD_EXIT=0`,
`GAME_HASH_UNCHANGED=1`, and `RELEASE_AUDIT_EXIT=0`.

Exact final audit command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; printf "%s\n" "AUDIT_GAME_MOUNT_READONLY=1"; mount | grep " /game " || true; test -r /game/GameAssembly.dll && printf "%s\n" "AUDIT_GAME_READ_OK=1"; if test -w /game; then printf "%s\n" "AUDIT_GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "AUDIT_GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat > /tmp/game-before.sha256; git -C /src diff --check 0982cbc89bd70848694b45754dad47c8780fb13b --; diff_status=$?; printf "%s\n" "DIFF_CHECK_EXIT=$diff_status"; allowed=1; git -C /src diff --name-only 0982cbc89bd70848694b45754dad47c8780fb13b -- | while IFS= read -r path; do case "$path" in AutoNether.Tests/NetherAutoClimbControllerEndToEndTests.cs|AutoNether.Tests/NetherCodePolicyTests.cs|AutoNether.Tests/NetherDetailedAuditLoggerTests.cs|AutoNether.Tests/NetherStrategyModes1617Tests.cs|AutoNether/Services/NetherAutoClimbController.cs|AutoNether/Services/NetherCodePolicy.cs|AutoNether/Services/NetherDetailedAuditLogger.cs|AutoNether/Services/NetherRouteEncounterVectorPolicy.cs|AutoNether/Services/NetherRoutePlanner.cs|AutoNether/Services/NetherRuntimeBridge.cs|AutoNether/Services/NetherStrategyDecisionAudit.cs|AutoNether/Services/NetherStrategyEvidence.cs|docs/agents/evidence-backed-strategy-modes-16-17-evidence.md) ;; *) printf "%s\n" "UNEXPECTED_TRACKED_PATH=$path"; exit 1 ;; esac; done; if test "${PIPESTATUS[1]}" -ne 0; then allowed=0; fi; git -C /src status --porcelain=v1 | while IFS= read -r line; do path="${line:3}"; case "$path" in docs/agents/native-decomp-*|AutoNether.Tests/NetherAutoClimbControllerEndToEndTests.cs|AutoNether.Tests/NetherCodePolicyTests.cs|AutoNether.Tests/NetherDetailedAuditLoggerTests.cs|AutoNether.Tests/NetherStrategyModes1617Tests.cs|AutoNether/Services/NetherAutoClimbController.cs|AutoNether/Services/NetherCodePolicy.cs|AutoNether/Services/NetherDetailedAuditLogger.cs|AutoNether/Services/NetherRouteEncounterVectorPolicy.cs|AutoNether/Services/NetherRoutePlanner.cs|AutoNether/Services/NetherRuntimeBridge.cs|AutoNether/Services/NetherStrategyDecisionAudit.cs|AutoNether/Services/NetherStrategyEvidence.cs|docs/agents/evidence-backed-strategy-modes-16-17-evidence.md) ;; *) printf "%s\n" "UNEXPECTED_STATUS_PATH=$path"; exit 1 ;; esac; done; if test "${PIPESTATUS[1]}" -ne 0; then allowed=0; fi; printf "%s\n" "WORKTREE_PATH_AUDIT=$allowed"; mkdir -p /tmp/repo; tar -C /src --exclude=./.git --exclude=./docs/agents/native-decomp-* -cf - . | tar -C /tmp/repo -xf -; dotnet test /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj --configuration Debug -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-final-audit-test-out/ --logger "console;verbosity=minimal"; test_status=$?; printf "%s\n" "FINAL_FULL_TEST_EXIT=$test_status"; dotnet restore /tmp/repo/AutoNether/AutoNether.csproj -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-final-audit-release-out/; restore_status=$?; printf "%s\n" "FINAL_RELEASE_RESTORE_EXIT=$restore_status"; if test "$restore_status" -eq 0; then dotnet build /tmp/repo/AutoNether/AutoNether.csproj --configuration Release -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-final-audit-release-out/ -p:ContinuousIntegrationBuild=true --no-restore; build_status=$?; else build_status=1; fi; printf "%s\n" "FINAL_RELEASE_BUILD_EXIT=$build_status"; if test "$build_status" -eq 0; then dll=/tmp/repo/.task16-17-final-audit-release-out/Release/net6.0/AutoNether.dll; printf "%s\n" "FINAL_DLL_PATH=$dll"; stat -c "FINAL_DLL_SIZE=%s" "$dll"; stat -c "FINAL_DLL_TIMESTAMP_UTC=%y" "$dll"; printf "%s\n" "FINAL_DLL_SHA256=$(sha256sum "$dll" | cut -d" " -f1)"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat > /tmp/game-after.sha256; cmp -s /tmp/game-before.sha256 /tmp/game-after.sha256; game_status=$?; printf "%s\n" "GAME_HASH_UNCHANGED=$([ "$game_status" -eq 0 ] && printf 1 || printf 0)"; printf "%s\n" "RELEASE_AUDIT_EXIT=$(( diff_status == 0 && allowed == 1 && test_status == 0 && restore_status == 0 && build_status == 0 && game_status == 0 ? 0 : 1 ))"; exit 0'
```

The clean full test passed 1304/1304 with 0 failures and 0 skips. The clean
Release build passed with 0 warnings and 0 errors. The independently verified
container DLL was:

| field | value |
| --- | --- |
| path | `/tmp/repo/.task16-17-final-audit-release-out/Release/net6.0/AutoNether.dll` |
| size | `1,801,728` bytes |
| timestamp UTC | `2026-08-19 12:40:46.863544528 +0000` |
| SHA-256 | `f800f1e4567198973b4337880ada53933f69aacc4048eee33a1e0d90078965c7` |

The earlier release build independently produced the same size and hash. The
first release audit command had a shell-quoting error while formatting its
hash; it did not compile or mutate anything. It was corrected by replacing the
awk formatter with `cut`, then the clean Release build and final audit above
passed. This transport RCA is retained here so the failed command is not
mistaken for a product failure.

## Final clean Docker gates — task16-17-final-audit-post-context

After the audit-context seam was implemented, fresh native evidence
`task16-17-fresh-20260819-j` preceded the final expanded/full verification. The
final Docker audit used the exact read-only repository and game mounts, copied
the source into an ephemeral container directory while excluding `bin`/`obj`
and native evidence directories, and verified the fixed-point diff and allowed
worktree paths. Markers were
`AUDIT_GAME_MOUNT_READONLY=1`, `AUDIT_GAME_READ_OK=1`,
`AUDIT_GAME_WRITE_CHECK=readonly`, `DIFF_CHECK_EXIT=0`,
`WORKTREE_PATH_AUDIT=1`, `FINAL_RELEASE_RESTORE_EXIT=0`,
`FINAL_FULL_TEST_EXIT=0`, `FINAL_RELEASE_BUILD_EXIT=0`,
`PRODUCT_SOURCE_ISOLATION=1`, `SOURCE_WORKTREE_UNCHANGED=1`,
`GAME_HASH_UNCHANGED=1`, `RELEASE_AUDIT_EXIT=0`, and
`FINAL_AUDIT_EXIT=0`.

Exact command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -o pipefail; printf "%s\n" "AUDIT_GAME_MOUNT_READONLY=1"; mount | grep " /game " || true; test -r /game/GameAssembly.dll && printf "%s\n" "AUDIT_GAME_READ_OK=1"; if test -w /game; then printf "%s\n" "AUDIT_GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "AUDIT_GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat > /tmp/game-before.sha256; git -C /src diff --check 0982cbc89bd70848694b45754dad47c8780fb13b --; diff_status=$?; printf "%s\n" "DIFF_CHECK_EXIT=$diff_status"; allowed=1; git -C /src diff --name-only 0982cbc89bd70848694b45754dad47c8780fb13b -- > /tmp/changed-paths; while IFS= read -r path; do case "$path" in .scratch/evidence-backed-strategy-modes/README.md|.scratch/evidence-backed-strategy-modes/issues/16-audit-and-update-tolerance.md|.scratch/evidence-backed-strategy-modes/issues/17-production-acceptance.md|CONTEXT.md|AutoNether.Tests/NetherAutoClimbControllerEndToEndTests.cs|AutoNether.Tests/NetherCodePolicyTests.cs|AutoNether.Tests/NetherDetailedAuditLoggerTests.cs|AutoNether.Tests/NetherStrategyModes1617Tests.cs|AutoNether/Services/NetherAutoClimbController.cs|AutoNether/Services/NetherCodePolicy.cs|AutoNether/Services/NetherDetailedAuditLogger.cs|AutoNether/Services/NetherRouteEncounterVectorPolicy.cs|AutoNether/Services/NetherRoutePlanner.cs|AutoNether/Services/NetherRuntimeBridge.cs|AutoNether/Services/NetherStrategyDecisionAudit.cs|AutoNether/Services/NetherStrategyEvidence.cs|docs/agents/evidence-backed-strategy-modes-16-17-evidence.md) ;; *) printf "%s\n" "UNEXPECTED_TRACKED_PATH=$path"; allowed=0 ;; esac; done < /tmp/changed-paths; git -C /src status --porcelain=v1 > /tmp/status-before; while IFS= read -r line; do path="${line:3}"; case "$path" in .scratch/evidence-backed-strategy-modes/README.md|.scratch/evidence-backed-strategy-modes/issues/16-audit-and-update-tolerance.md|.scratch/evidence-backed-strategy-modes/issues/17-production-acceptance.md|CONTEXT.md|AutoNether.Tests/NetherAutoClimbControllerEndToEndTests.cs|AutoNether.Tests/NetherCodePolicyTests.cs|AutoNether.Tests/NetherDetailedAuditLoggerTests.cs|AutoNether.Tests/NetherStrategyModes1617Tests.cs|AutoNether/Services/NetherAutoClimbController.cs|AutoNether/Services/NetherCodePolicy.cs|AutoNether/Services/NetherDetailedAuditLogger.cs|AutoNether/Services/NetherRouteEncounterVectorPolicy.cs|AutoNether/Services/NetherRoutePlanner.cs|AutoNether/Services/NetherRuntimeBridge.cs|AutoNether/Services/NetherStrategyDecisionAudit.cs|AutoNether/Services/NetherStrategyEvidence.cs|docs/agents/evidence-backed-strategy-modes-16-17-evidence.md|docs/agents/native-decomp-*) ;; *) printf "%s\n" "UNEXPECTED_WORKTREE_PATH=$path"; allowed=0 ;; esac; done < /tmp/status-before; if test "$allowed" -eq 1; then printf "%s\n" "WORKTREE_PATH_AUDIT=1"; else printf "%s\n" "WORKTREE_PATH_AUDIT=0"; fi; mkdir -p /tmp/repo; tar -C /src --exclude=./.git --exclude=./docs/agents/native-decomp-* --exclude="*/bin" --exclude="*/obj" -cf - . | tar -C /tmp/repo -xf -; dotnet restore /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-final-audit-out/ --nologo -v:minimal; restore_status=$?; printf "%s\n" "FINAL_RELEASE_RESTORE_EXIT=$restore_status"; dotnet test /tmp/repo/AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Debug -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-final-audit-out/ --logger "console;verbosity=minimal"; test_status=$?; printf "%s\n" "FINAL_FULL_TEST_EXIT=$test_status"; dotnet build /tmp/repo/AutoNether/AutoNether.csproj --no-restore --configuration Release -p:ABYSS_GAME_DIR=/game -p:BaseOutputPath=/tmp/repo/.task16-17-final-audit-release-out/ --nologo -v:minimal; build_status=$?; printf "%s\n" "FINAL_RELEASE_BUILD_EXIT=$build_status"; dll=/tmp/repo/.task16-17-final-audit-release-out/Release/net6.0/AutoNether.dll; dll_ok=0; if test -f "$dll"; then dll_size=$(stat -c "%s" "$dll"); dll_timestamp=$(date -u -r "$dll" "+%Y-%m-%d %H:%M:%S.%N %z"); dll_sha=$(sha256sum "$dll" | cut -d " " -f1); printf "%s\n" "FINAL_DLL_PATH=$dll" "FINAL_DLL_SIZE=$dll_size" "FINAL_DLL_TIMESTAMP=$dll_timestamp" "FINAL_DLL_SHA256=$dll_sha"; if test "$dll_size" = "1805312" && test "$dll_sha" = "663e893a119e4baf61646cdc47abba24df6e00ddda1d3714f05ec8aeb42c0902"; then dll_ok=1; fi; fi; if test -e /src/.task16-17-final-audit-out || test -e /src/.task16-17-final-audit-release-out; then printf "%s\n" "PRODUCT_SOURCE_ISOLATION=0"; else printf "%s\n" "PRODUCT_SOURCE_ISOLATION=1"; fi; git -C /src status --porcelain=v1 > /tmp/status-after; if cmp -s /tmp/status-before /tmp/status-after; then printf "%s\n" "SOURCE_WORKTREE_UNCHANGED=1"; else printf "%s\n" "SOURCE_WORKTREE_UNCHANGED=0"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat > /tmp/game-after.sha256; if cmp -s /tmp/game-before.sha256 /tmp/game-after.sha256; then printf "%s\n" "GAME_HASH_UNCHANGED=1"; else printf "%s\n" "GAME_HASH_UNCHANGED=0"; fi; release_audit=0; if test "$dll_ok" -eq 1 && test "$restore_status" -eq 0 && test "$build_status" -eq 0 && test "$test_status" -eq 0 && test "$diff_status" -eq 0 && test "$allowed" -eq 1 && test -e /tmp/repo/.task16-17-final-audit-out/Debug/net8.0/AutoNether.Tests.dll; then release_audit=1; fi; printf "%s\n" "RELEASE_AUDIT_EXIT=$((1-release_audit))"; if test "$release_audit" -eq 1 && test -s /tmp/game-after.sha256 && cmp -s /tmp/status-before /tmp/status-after; then printf "%s\n" "FINAL_AUDIT_EXIT=0"; else printf "%s\n" "FINAL_AUDIT_EXIT=1"; fi; exit 0'
```

The clean final test passed 1305/1305 with 0 failures and 0 skips. The clean
Release build passed with 0 warnings and 0 errors. The independently verified
container DLL was:

| field | value |
| --- | --- |
| path | `/tmp/repo/.task16-17-final-audit-release-out/Release/net6.0/AutoNether.dll` |
| size | `1,805,312` bytes |
| timestamp UTC | `2026-08-19 13:02:11.168618208 +0000` |
| SHA-256 | `663e893a119e4baf61646cdc47abba24df6e00ddda1d3714f05ec8aeb42c0902` |

The final audit also proved `PRODUCT_SOURCE_ISOLATION=1`,
`SOURCE_WORKTREE_UNCHANGED=1`, and `GAME_HASH_UNCHANGED=1`. The only warning
output was Git's existing CRLF normalization advisory; `DIFF_CHECK_EXIT=0`.

## Task-group completion

Ticket 16 and ticket 17 are implementation-complete at the fixed point
`0982cbc89bd70848694b45754dad47c8780fb13b` plus the uncommitted task-group
changes. The controller transaction model remains the single execution owner;
unknown evidence is candidate/option/branch-local and only no legal choice or
ambiguous transaction identity pauses. No commit, push, remote Issue, or label
operation was performed. The next checkpoint is dual reviewer convergence.

## Spec-axis repair cycle — task16-17-spec-fix-20260819

The reviewers converged on a Spec-axis FAIL. The repair kept the existing
controller transaction boundary and public audit seams, then added complete
candidate/option records and typed route-context unknowns. No native conflict
was proven, so no ticket or CONTEXT semantic deviation was required.

Fresh native evidence was rerun before the repair RED and before the focused,
expanded, full-test, and Release/build audit gates. The final immutable run was
`task16-17-spec-fix-fresh-20260819-e` (Docker job `j-bqop5m`) with the exact
read-only game mount:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc '... /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-spec-fix-diffable-e --output-as diffable-cs ... /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-spec-fix-isil-e --output-as isil ...'
```

Markers were `NATIVE_EVIDENCE_ID=task16-17-spec-fix-fresh-20260819-e`,
`GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`, `GAME_WRITE_CHECK=readonly`,
`CPP2IL_DIFFABLE_EXIT=0`, `CPP2IL_ISIL_EXIT=0`, and
`NATIVE_EVIDENCE_EXIT=0`. Cpp2IL was
`2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224` and
Unity was `6000.3.8f1`. The immutable game inputs were unchanged:

| input | SHA-256 |
| --- | --- |
| `/game/BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `/game/GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `/game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

The eleven diffable artifacts were byte-identical to the first table in this
ledger. Their hashes were `Api/NetherApiDataStore.cs`
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`,
`Api/NetherCharacterEntity.cs`
`22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`,
`Api/NetherUpdateEventResponseEntity.cs`
`30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`,
`Master/NoaMessagePack/MItems.cs`
`e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27`,
`Master/NoaMessagePack/MNetherFloorBattles.cs`
`7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`,
`Master/NoaMessagePack/MNetherFloorEventParts.cs`
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`Master/NoaMessagePack/MNetherFloorEvents.cs`
`aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`,
`Master/NoaMessagePack/MNetherFloorShopContents.cs`
`177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9`,
`Nether/NetherEventPopup/NetherEventPopupController.cs`
`a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf`,
`Nether/NetherRecoverPopup/NetherRecoverPopupController.cs`
`2ffbbf17144a658915f2334f5168d3eeb6d7f8a62eea6b56cadecc95f704cc67`, and
`Nether/NetherTreasurePopup/NetherTreasurePopupController.cs`
`19f36f6e018f4c37337f94bf1324bbbca0142e8de5227036ee871cc756474bee`.
Anchors remained `RequestNetherUpdateEventAsync`,
`NetherCharacterEntity.current_hp_ratio`, raw battle `type`/
`m_nether_battle_stage_id`, Event-part `target_type_1`, and popup
`ExecuteEvent`/`InitializeView`/Treasure `OnConfirm`.

### Repair RED/GREEN/RCA

The intentional compile RED (`task16-17-spec-fix-red-c`, Docker, RO game and
source) failed on the absent public seam fields/types: complete route-candidate
audit facts and rationale, typed route-context maps, and per-option audit
contracts. Earlier restore-only REDs `red-a`/`red-b` were transport/configuration
reproductions; `red-c` reached the intended compile failure with
`ABYSS_GAME_DIR=/game` and explicit package sources.

Focused GREEN first exposed option-audit placement (`green-a`) and the missing
interactive `OptionNumber` (`green-b`); those were fixed without changing the
transaction model. The final focused GREEN (`task16-17-spec-fix-green-20260819-d`,
Docker job `j-tib0mz`, fresh native `-d`) passed `8/8`.

The first expanded run (`j-n2gkxr`, fresh native `-e`) reached `217/218`; its
single failure was the existing static-provider registration test when run in
that order. The RCA rerun (`j-erl4pt`, fresh native `-e`) passed that test `1/1`,
and the expanded rerun (`j-28s95r`, fresh native `-e`) passed `218/218`. A clean
full Docker invocation (`j-52urf1`, fresh native `-e`) passed `1313/1313`, with
0 failures and 0 skips. The order-sensitive failure was therefore not a
product regression; no source workaround was added.

The repair records every participating route candidate, including excluded
safe/unsafe alternatives, without controller-side route `Take(8)` truncation;
every Event/Recovery/Treasure/Shop option now has a typed audit; and route
unknowns preserve party, master-data, inventory, transaction, recovery, and
route-safety distinctions. Deterministic characterization tests cover these
contracts and fail-closed local rejection.

### Final Docker gates

The Docker Release gate (`j-702akp`, fresh native `-e`) restored successfully
and built with `--configuration Release --no-restore --nologo -warnaserror`:
`RELEASE_BUILD_EXIT=0`, 0 warnings, 0 errors,
`RELEASE_ISOLATION_EXIT=0`, `GAME_HASH_UNCHANGED=1`, and
`RELEASE_AUDIT_EXIT=0`. The verified container artifact was:

| field | value |
| --- | --- |
| path | `/tmp/repo/release/Release/net6.0/AutoNether.dll` |
| size | `1,853,440` bytes |
| timestamp UTC | `2026-08-19 14:21:55.199886984 +0000` |
| SHA-256 | `09811f9b16b2223afbbccf64727bdaa5755cf7e3f747a093376c59708897595b` |

The final Docker isolation/diff gate (`j-p5kqq9`, fresh native `-e`) reported
`GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`, `GAME_WRITE_CHECK=readonly`,
unchanged game hashes, `DIFF_CHECK_EXIT=0`, `GAME_PATH_DIFF_EXIT=0`, and
`FINAL_DIFF_AUDIT_EXIT=0`. Only Git's existing CRLF normalization advisories
appeared. No commit, push, remote Issue, or label operation was performed;
dual-reviewer re-review is the remaining human checkpoint.

## Spec-axis re-review repair cycle 2 — task16-17-spec-fix2-20260819

The second Spec-axis blockers were fixed in the shared worktree without
changing the controller transaction model or touching remote issues. The
production audit path now emits complete candidate, predecessor-branch,
pre-entry-floor, event, option, Code, and unknown route-bound records; Route,
Decision, and Interactive detailed-audit records are not subject to the
diagnostic entry/field caps. Unknown route-frontier nodes are locally rejected
with typed audit data before known legal siblings are evaluated. Safety-context
horizon/graph finalization preserves originating party, master-data,
inventory, transaction, recovery, or route source codes and stores horizon
rejection separately. Configuration, trigger, and buff-strategy unknowns have
distinct public reason codes. Recovery and Treasure now have deterministic
per-option typed-audit characterization coverage.

Fresh Docker native runs `task16-17-spec-fix2-fresh-20260819-a` through `-g`
(jobs `j-ld4zqm`, `j-za113p`, `j-374gng`, `j-ucp1y5`, `j-c89nm7`, `j-xz0u4o`,
and `j-cdwdow`) used
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_xcl,dst=/game,readonly`.
All reported `GAME_MOUNT_READONLY=1`, `GAME_READ_OK=1`,
`GAME_WRITE_CHECK=readonly`, `CPP2IL_DIFFABLE_EXIT=0`,
`CPP2IL_ISIL_EXIT=0`, and `NATIVE_EVIDENCE_EXIT=0`. Required game hashes
matched on every run: Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
Cpp2IL was `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
for Unity `6000.3.8f1`. The generated anchors were
`RequestNetherUpdateEventAsync` at line 288 with
`(floorLevel, floorIndex, selectedNumber, changeTargetMNetherCodeId,
CancellationToken)`, `current_hp_ratio` at line 10,
`m_nether_battle_stage_id` and `target_type_1` at line 13, plus the Event,
Recovery, and Treasure popup initialize/execute/confirm seams. No native
semantic deviation was required.

The intentional RED Docker gate `j-tmahw1` failed because the three required
typed enum values were absent (`RED_TEST_EXIT=1`). After implementation the
focused GREEN set passed 29/29 in `j-784l3g` and `j-mzpqxy`; the first full
run `j-r604ej` passed 1316/1319 and exposed only the stale diagnostic-cap
assertion and the planner second-loop Unknown/Default admission. Minimal RCA
`j-os1b3f` passed those three corrected cases 3/3. Final focused `j-91ndau`
passed 29/29 and clean full `j-9bi7n9` passed 1319/1319 with zero failures and
zero skips. Native hashes were unchanged, ruling out native drift and
transaction-boundary regressions.

Release Docker job `j-mhkdi3` ran after fresh native `-g`, with source and
game mounts read-only: restore and build exited zero with zero warnings and
zero errors, and `GAME_HASH_UNCHANGED=1`. The verified artifact was
`/tmp/repo/.task16-17-release-final/Release/net6.0/AutoNether.dll`,
1,853,952 bytes, timestamp `2026-08-19 15:11:50.337349710 +0000`, SHA-256
`163c20e463e6688ab4f9e23cd5616ae4f1fa0c4c1b3d8eedf7659f8f696ff44b`.

Persistent dual reviewers converged PASS on both Standards and Spec axes;
remaining Standards P2 smells are non-blocking. No commit or push had been
performed before the authorized task-group commit, no remote Issue/label state
was touched, and pre-existing `docs/agents/native-decomp-*` directories remain
untracked and excluded.
