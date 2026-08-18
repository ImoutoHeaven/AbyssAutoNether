# Evidence record: strategy-mode tickets 13–15

Date: 2026-08-19
Branch: `logic-overhaul`
Parent fixed point: `51e30ee8f96f3923e5cb7b8437441cbdc8ff0df9`

This record covers the 13–15 candidate. The game directory was never made writable. Decompilation,
builds, tests, and verification used ephemeral `docker run --rm` containers; `/game` was always a
read-only bind mount.

## Fresh native evidence

The final bounded fresh native run was FastCtx job `j-u653bz`. Its game hashes were:

| Artifact | SHA-256 |
|---|---|
| `BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

The bounded invocation mounted only the game read-only and ran Cpp2IL `2022.1.0-pre-release.21`
to ephemeral `/tmp/t14-diffable` and `/tmp/t14-isil` outputs:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/t14-diffable --output-as diffable-cs; /tmp/Cpp2IL --game-path /game --output-to /tmp/t14-isil --output-as isil; echo CPP2IL_DIFFABLE_EXIT=0; echo CPP2IL_ISIL_EXIT=0'
```

Relevant fresh output anchors:

- `MNetherFloorShopContents` exposes raw shop identity, consume content type/id/amount, and
  content type/id/amount. `NetherShopItemAmountEntity` and `NetherShopHistoryEntity` carry exact
  content ID plus amount; the update request/response carry those arrays.
- `NetherShopPopupController` owns floor level/index/map-floor identity, gold/key balances and
  shop content models, and exposes the native async view/purchase/update flow. The implementation
  therefore requires exact content identity and amount and does not infer a Red shop bag.
- `MNetherFloorBattles` exposes `id`, map-floor ID, raw `type`, battle-stage ID, and
  `code_drop_ratio`; no fresh native local semantic Boss/MiniBoss/Normal mapping was proven.
- `MNetherMapFloors` exposes min/max order, min/max erosion, predecessor/successor, element, size,
  raw type, and usage count. Event master/part rows likewise expose raw IDs, target parameters,
  content type/id, and amount.

Native-first deviations:

1. Event battle `type` remains `NetherEventBattleTier.Unknown`; ticket 14's exact semantic tier is
   accepted only through a future authoritative typed provider. The route planner never guesses
   from raw `type` or `code_drop_ratio`.
2. The current native settlement response is the first proven authoritative research-point
   result. `ResearchIncomplete` remains nullable in production and is never manufactured from a
   displayed gauge, Code count, or technology rate.

## Implementation and RED–GREEN evidence

- Ticket 13 changed `NetherEventPolicy`, production commitment binding, and shop reconciliation;
  tests cover floor 90/91, gold 299/300/499/500, exact Gold type-91 bag identity, Red/unknown
  rejection, key-before-bag ordering, and malformed balance/amount reconciliation.
- Ticket 14 added `NetherRouteEncounterVectorPolicy`, visible-map route comparison, mode-dependent
  ordering, safety-first filtering, and fail-closed unresolved content handling.
- Ticket 15 added the public runtime-flow owner guard requiring a positive registered popup
  sequence. Existing native popup lifecycle, continuation, settlement, F12 drain, and re-entry
  tests remain covered.

Historical candidate report (not current repair validation):

- The following numbers were reported for the pre-review candidate and are retained only as
  historical context: Ticket 13 targeted green `132/132`; Ticket 14 route/wiring `75/75` after a
  `3/3` focused group; Ticket 15 bounded lifecycle/E2E `157/157`; build `0 Warning(s)` and
  `0 Error(s)`; and full test `1239/1239`. They are not evidence for the current A–E repair.
- The current repair adds new C/D/E production behavior and fixtures after that candidate, so no
  current compile/test count is inferred from these historical results.

All historical test/build containers mounted the source workspace at `/src`, the game at `/game` with
read-only bind mode,
and used the versioned `mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim` image. No game file,
decompilation artifact, or temporary native evidence directory is part of this candidate.

## Fresh native evidence for the review repair

The current repair-cycle rerun mounted only `/c/Users/Eden/PixelAbyssX/dotabyss_x_cl` as
`/game:readonly`; both decompilation outputs were written to disposable container paths under
`/tmp`, and the game directory was not changed. This is the complete bounded command used for the
current native evidence; each Cpp2IL output had a 105-second limit:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; printf "CONTAINER_GAME_MOUNT=/game:readonly\n"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/tickets13-15-fix-diffable --output-as diffable-cs; printf "CPP2IL_DIFFABLE_EXIT=0\n"; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/tickets13-15-fix-isil --output-as isil; printf "CPP2IL_ISIL_EXIT=0\n"; for f in /tmp/tickets13-15-fix-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/tickets13-15-fix-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/tickets13-15-fix-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/tickets13-15-fix-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/tickets13-15-fix-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs; do test -f "$f"; sha256sum "$f"; case "$f" in *MNetherFloorBattles.cs) sed -n "1,28p" "$f";; *MNetherFloorEvents.cs) sed -n "1,34p" "$f";; *MNetherFloorEventParts.cs) sed -n "1,38p" "$f";; *MNetherFloorShopContents.cs) sed -n "1,32p" "$f";; *NetherApiDataStore.cs) sed -n "284,291p" "$f";; esac; done'
```

The actual run returned `CONTAINER_GAME_MOUNT=/game:readonly`,
`CPP2IL_DIFFABLE_EXIT=0`, and `CPP2IL_ISIL_EXIT=0`. It printed the immutable game hashes listed
above and these fresh diffable output hashes/anchors:

| Native row | Fresh output SHA-256 | Native fact used by policy |
|---|---|---|
| `MNetherFloorBattles.cs` | `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` | `type` is raw `int`; only stage and drop-ratio fields are present, so no Boss/MiniBoss/Normal mapping is invented. |
| `MNetherFloorEvents.cs` | `aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006` | one Event row carries four part IDs. |
| `MNetherFloorEventParts.cs` | `5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` | each part carries raw target/parameter triples and content type/id/amount. |
| `MNetherFloorShopContents.cs` | `177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9` | shop identity, consume fields, content identity, and amount are separate raw fields. |
| `NetherApiDataStore.cs` | `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` | Event update takes floor level/index, selected option, and Code-change ID; Shop update takes exact content-amount entities. |

These facts require Event route classification to invoke the actual Event policy with the current
snapshot, active mode, resources, and route-owned commitments, then bind route value to its returned
`(EventId, EventPartId, OptionNumber)`; an unselected part contributes no route tier. An unknown
battle tier invalidates only that part. They require a nullable pre-settlement Research state to
pause only Research-mode mode-sensitive comparison rather than use the completed order; Equipment
mode uses its explicit order. They also require an unknown or malformed sibling in one materialized
Shop to invalidate that Shop's route value. The Gold Treasure/eligible-Shop tier is compared by
combined count before the direct Treasure tie break. No issue or CONTEXT assumption is promoted over
these native boundaries.

## Third-review repair RED–GREEN evidence

The current bounded RED ran after adding four public regressions: complete Event dependency rows,
typed production Battle binding, mapper-to-late-Shop rarity flow, and the one-Treasure/one-Shop
semantic tie. It used a read-only source mount, a read-only game mount, and copied only the two
projects plus solution into the disposable container workspace. The Docker wrapper exited 0 after
recording the expected failing test exit:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; rm -rf /tmp/tickets13-15-third-red-src; mkdir -p /tmp/tickets13-15-third-red-src; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/tickets13-15-third-red-src/; cd /tmp/tickets13-15-third-red-src; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo; timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal; set +e; timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherVisibleBranchRoutePlannerTests|FullyQualifiedName~NetherEventProductionEvidenceBindingTests" --logger "console;verbosity=minimal"; code=$?; set -e; printf "RED_TEST_EXIT=%s\n" "$code"; if [ "$code" -eq 0 ]; then printf "RED_EXPECTATION_UNMET=tests-passed-before-fix\n"; else printf "RED_EXPECTATION=behavioral-or-compile-failure-before-fix\n"; fi; exit 0'
```

RED output was a successful build with `0 Warning(s)` and `0 Error(s)`, followed by four expected
failures and 40 passes (`Failed: 4, Passed: 40, Total: 44`), `RED_TEST_EXIT=1`, and
`RED_EXPECTATION=behavioral-or-compile-failure-before-fix`. The failures were: Event selected node
3 instead of typed Boss node 2, binding returned
`event-battle-semantic-tier-unavailable-for-raw-type:9`, mapped Shop rarity was `NoEffect`, and
the one-to-one Gold Treasure/Shop tie selected Shop.

The minimal GREEN was then run with the same Docker image and mounts:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; rm -rf /tmp/tickets13-15-third-green-src; mkdir -p /tmp/tickets13-15-third-green-src; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/tickets13-15-third-green-src/; cd /tmp/tickets13-15-third-green-src; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo; timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal; timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherVisibleBranchRoutePlannerTests|FullyQualifiedName~NetherEventProductionEvidenceBindingTests" --logger "console;verbosity=minimal"; printf "GREEN_TARGETED_EXIT=0\n"'
```

GREEN Docker exit was 0; restore completed, build reported `0 Warning(s)` and `0 Error(s)`, and the
targeted test run reported `Failed: 0, Passed: 44, Skipped: 0, Total: 44` with
`GREEN_TARGETED_EXIT=0`. The disposable source/build workspace was
`/tmp/tickets13-15-third-green-src`; no output was written to the host workspace or game directory.
No full suite was rerun in this interruption-bounded repair cycle.

The implementation facts tied to the fresh native run are:

- `NetherRouteEncounterVectorPolicy` now passes complete visible rows into the Event policy while
  retaining `(EventId, EventPartId, OptionNumber)` selection binding; only the selected part can
  contribute a tier, and an untyped battle remains local Unknown.
- `NetherEventProductionEvidenceBinding.FindBattle` constructs known evidence only for a non-Unknown
  typed `EventBattleTier`; raw battle fields still produce fail-closed Unknown.
- `NetherStrategyVisibleEvidenceMapper` carries typed Shop rarity consistently in `Rank` and
  `ItemRarity`, while exact amount/content/currency and unknown-sibling gates remain unchanged.
- Route Treasure colour now consumes `NetherCanonicalRewardTierProvider`; raw native Gold/Red or a
  display rank cannot create canonical rank-five value. The explicit typed provider is carried by
  the visible item/master-row seam.

The native facts supporting these choices are the hashes and anchors in the preceding section:
`MNetherFloorEvents` has four raw part IDs, `MNetherFloorEventParts` has raw target/content tuples,
`MNetherFloorBattles.type` has no proven local semantic mapping, and Shop update identity/amount are
separate exact fields. Those facts justify locality and fail-closed behavior; no Boss/MiniBoss/Normal
mapping was invented.

## Provider-boundary repair evidence

The fresh native run above remains the native basis for this repair: `Project.dll` SHA-256
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, `GameAssembly.dll` SHA-256
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat` SHA-256 `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
That run proves only raw MItems type/rarity and raw MNetherFloorBattles type/stage/drop fields;
the current native bridge therefore has no built-in semantic provider. The new provider is an
explicit optional capture boundary: production startup can register a snapshot-scoped factory,
while the standalone Plugin passes null by default. Registered evidence is carried through the
actual RuntimeBridge singleton and pre-entry capture; missing, stale, duplicate, conflicting, or
invalid evidence remains Unknown. Raw capture fields never populate `CanonicalRewardTier` or
`EventBattleTier`.

The following RED is the complete command for the production-shaped pre-entry-capture to
assembler-to-mapper regression before provider propagation was implemented. `/src` and `/game`
were read-only; the source was copied to `/tmp/provider-red-src` before build/test. The wrapper
itself exits zero only after recording the expected test failure.

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; rm -rf /tmp/provider-red-src; mkdir -p /tmp/provider-red-src; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/provider-red-src/; cd /tmp/provider-red-src; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo; timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal; set +e; timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests.Production_preentry_typed_provider_reaches_mapper_through_assembler" --logger "console;verbosity=minimal"; code=$?; set -e; printf "RED_TEST_EXIT=%s\n" "$code"; test "$code" -ne 0; printf "RED_EXPECTATION=provider-not-yet-propagated\n"; exit 0'
```

The RED Docker exit was `0`; restore completed, build reported `0 Warning(s)` and `0 Error(s)`,
and the selected test reported `Failed: 1, Passed: 0, Total: 1` with
`event-battle-semantic-tier-unavailable-for-raw-type:1`, `RED_TEST_EXIT=1`, and
`RED_EXPECTATION=provider-not-yet-propagated`.

The minimal provider-propagation GREEN used this complete command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; rm -rf /tmp/provider-green-src; mkdir -p /tmp/provider-green-src; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/provider-green-src/; cd /tmp/provider-green-src; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo; timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal; timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests.Production_preentry_typed_provider_reaches_mapper_through_assembler" --logger "console;verbosity=minimal"; printf "GREEN_PROVIDER_EXIT=0\n"'
```

That GREEN Docker exit was `0`; build reported `0 Warning(s)` and `0 Error(s)`, and the selected
test reported `Failed: 0, Passed: 1, Skipped: 0, Total: 1` with `GREEN_PROVIDER_EXIT=0`.

The final bounded production-shaped targeted gate used this complete command after adding the raw
capture-backed canonical Treasure/Shop aggregate and tie fixtures, the mapper-to-route typed Battle
fixture, and duplicate/conflicting provider regressions:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; rm -rf /tmp/provider-targeted-src; mkdir -p /tmp/provider-targeted-src; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/provider-targeted-src/; cd /tmp/provider-targeted-src; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo; timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal; timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherVisibleBranchRoutePlannerTests|FullyQualifiedName~NetherStrategyVisibleEvidenceMapperTests|FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests" --logger "console;verbosity=minimal"; printf "TARGETED_PROVIDER_CAPTURE_ROUTE_EXIT=0\n"'
```

The final targeted Docker exit was `0`; restore completed, build reported `0 Warning(s)` and
`0 Error(s)`, and the filter reported `Failed: 0, Passed: 55, Skipped: 0, Total: 55` with
`TARGETED_PROVIDER_CAPTURE_ROUTE_EXIT=0`. The 55 tests include the two provider-backed
Gold-Treasure/eligible-Shop aggregate and tie cases, the provider-backed mapper-to-route Event
Boss case, the pre-entry capture-to-assembler provider case, and conflicting-provider plus raw
missing-provider fail-closed cases. The only intermediate failure was a test fixture KeyNotFound
for a missing NodeId; it was corrected by adding the one-shop normal node and was not a production
logic failure.

## Final P1-A/P1-B bounded validation

After the production registration and typed-Shop contract repair, the exact bounded gate was rerun
against read-only `/src` and `/game` mounts. The container copied source into `/tmp/p1ab-green-src-2`
and wrote all build/test intermediates there:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; rm -rf /tmp/p1ab-green-src-2; mkdir -p /tmp/p1ab-green-src-2; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/p1ab-green-src-2/; cd /tmp/p1ab-green-src-2; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo; timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal; timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests|FullyQualifiedName~NetherShopContentMapperTests|FullyQualifiedName~NetherEventPolicyTests|FullyQualifiedName~NetherEventProductionEvidenceBindingTests|FullyQualifiedName~NetherVisibleBranchRoutePlannerTests" --logger "console;verbosity=minimal"; printf "P1AB_TARGETED_GREEN_EXIT=0\n"'
```

The actual Docker exit was `0`. Restore succeeded; build reported `0 Warning(s)` and `0 Error(s)`;
the filter reported `Failed: 0, Passed: 107, Skipped: 0, Total: 107`, followed by
`P1AB_TARGETED_GREEN_EXIT=0`. The earlier `106/107` result was the real key/bag reconciliation
failure; the final gate includes the minimal action-specific key-or-bag matching fix.

The fresh native command was rerun separately with `/game:readonly` and disposable Cpp2IL outputs
`/tmp/tickets13-15-p1ab-diffable` and `/tmp/tickets13-15-p1ab-isil`:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; printf "CONTAINER_GAME_MOUNT=/game:readonly\n"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; rm -rf /tmp/tickets13-15-p1ab-diffable /tmp/tickets13-15-p1ab-isil; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/tickets13-15-p1ab-diffable --output-as diffable-cs; printf "CPP2IL_DIFFABLE_EXIT=0\n"; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/tickets13-15-p1ab-isil --output-as isil; printf "CPP2IL_ISIL_EXIT=0\n"; for f in /tmp/tickets13-15-p1ab-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/tickets13-15-p1ab-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/tickets13-15-p1ab-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/tickets13-15-p1ab-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/tickets13-15-p1ab-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs; do test -f "$f"; sha256sum "$f"; done'
```

The actual native Docker exit was `0`, with `CONTAINER_GAME_MOUNT=/game:readonly`,
`CPP2IL_DIFFABLE_EXIT=0`, and `CPP2IL_ISIL_EXIT=0`. The game hashes were unchanged:
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
metadata `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. Fresh output hashes
were `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` for battles,
`aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006` for events,
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` for event parts,
`177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9` for ShopContents, and
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` for NetherApiDataStore.

## Final bounded fresh decompile (2026-08-19)

The final candidate evidence run used the exact bounded command below. It mounted the game
read-only, kept all Cpp2IL output in the container's executable 4 GiB `/tmp` tmpfs, and used
an ephemeral `--rm` container:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/*_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl ca-certificates >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/final-fresh-diffable --output-as diffable-cs; echo CPP2IL_DIFFABLE_EXIT=0; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/final-fresh-isil --output-as isil; echo CPP2IL_ISIL_EXIT=0'
```

The actual Docker exit was `0`. The complete current log reported:

- Cpp2IL `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`.
- Unity `6000.3.8f1`, metadata version `39`, and successful PE/codereg/metareg detection.
- Diffable output completed at `/tmp/final-fresh-diffable`, followed by
  `CPP2IL_DIFFABLE_EXIT=0`.
- ISIL processed assemblies 1 through 168 and completed at `/tmp/final-fresh-isil`,
  followed by `CPP2IL_ISIL_EXIT=0`.
- The FastCtx Docker invocation returned in `36.536` seconds.

The current command emitted the three immutable game hashes (rather than output-file hashes):

```text
53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300  /game/BepInEx/interop/Project.dll
573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb  /game/GameAssembly.dll
ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5  /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat
```

Because the requested command stores output under an ephemeral container `/tmp` and does not
hash those output files, this section does not claim new output-file hashes. The native anchor
hashes recorded immediately above were produced from the same immutable game hashes and the
same Cpp2IL version: `MNetherFloorBattles.cs`
`7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`,
`MNetherFloorEvents.cs`
`aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`,
`MNetherFloorEventParts.cs`
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`MNetherFloorShopContents.cs`
`177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9`, and
`NetherApiDataStore.cs`
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`.
Those anchors continue to support the native boundary: raw battle `type`, item `rarity`, and
stage/drop fields do not prove Boss/MiniBoss/Normal or Gold/Red semantic tiers; only the
authoritative typed provider can enable those meanings, otherwise the implementation remains
Unknown/fail-closed.

## Managed Shop DTO production-path targeted validation (2026-08-19)

This bounded validation was run after the managed Shop DTO production-path fix. It did not rerun
Cpp2IL or the full suite; the native boundary and hashes remain the immutable fresh-decompile
anchors recorded above. The source and game mounts were read-only, and all build/test artifacts
were copied into the container filesystem:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget --workdir=/src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; rm -rf /tmp/shop-provider-src-4; mkdir -p /tmp/shop-provider-src-4; cp -a /src/AutoNether.sln /src/NuGet.config /src/AutoNether /src/AutoNether.Tests /tmp/shop-provider-src-4/; cd /tmp/shop-provider-src-4; export NUGET_PACKAGES=/nuget; timeout 120s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --configfile NuGet.config --nologo > /tmp/shop-provider-4-restore.log 2>&1; restore=$?; echo SHOP_PROVIDER4_RESTORE_EXIT=$restore; if [ "$restore" -eq 0 ]; then timeout 120s dotnet build AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Release --nologo --verbosity minimal > /tmp/shop-provider-4-build.log 2>&1; build=$?; echo SHOP_PROVIDER4_BUILD_EXIT=$build; else build=125; fi; if [ "$build" -eq 0 ]; then timeout 120s dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-build --configuration Release --nologo --filter "FullyQualifiedName~NetherRuntimePopupProductionTests|FullyQualifiedName~NetherShopContentMapperTests|FullyQualifiedName~NetherVisibleBranchRoutePlannerTests|FullyQualifiedName~NetherRuntimeInteractivePreEntryInputCaptureTests" --logger "console;verbosity=minimal" > /tmp/shop-provider-4-test.log 2>&1; test_exit=$?; else test_exit=125; fi; echo SHOP_PROVIDER4_TEST_EXIT=$test_exit; tail -n 20 /tmp/shop-provider-4-restore.log; tail -n 100 /tmp/shop-provider-4-build.log 2>/dev/null; tail -n 280 /tmp/shop-provider-4-test.log 2>/dev/null; exit 0'
```

The actual container markers were `SHOP_PROVIDER4_RESTORE_EXIT=0`,
`SHOP_PROVIDER4_BUILD_EXIT=0`, and `SHOP_PROVIDER4_TEST_EXIT=0`. The build reported `0 Warning(s)`
and `0 Error(s)`. The affected filter reported `Failed: 0, Passed: 60, Skipped: 0, Total: 60`.
The preceding bounded RED/GREEN sequence recorded the production-specific failures before the
minimal fixes: the initial provider-conflict test was RED until Shop-key evidence participated in
provider equivalence; the ID-less Shop regression was RED until visible mapping preserved
`MasterRowId=0` and carried the exact Shop content ID separately. The final 60-test run covered
the managed RuntimeBridge capture, snapshot provider, popup binding, assembler, visible mapper,
route vector/planner, Shop policy, 300/499/500 key→bag reconciliation, raw-only fail-closed, and
unknown-sibling fail-closed paths.

## Fresh native decompile after managed Shop DTO GREEN (2026-08-19)

No production logic was changed for this rerun. The game directory was mounted read-only, Cpp2IL
and both output trees lived only in the container `/tmp`, and the container was ephemeral:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/*_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl ca-certificates >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/final-shop-fresh-diffable --output-as diffable-cs; echo CPP2IL_DIFFABLE_EXIT=0; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/final-shop-fresh-isil --output-as isil; echo CPP2IL_ISIL_EXIT=0'
```

The actual Docker command completed with exit `0` in `36.503` seconds. The immutable game hashes
emitted by that command were:

```text
53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300  /game/BepInEx/interop/Project.dll
573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb  /game/GameAssembly.dll
ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5  /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat
```

Cpp2IL reported version `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`,
Unity `6000.3.8f1`, metadata version `39`, and codereg/metareg
`0x186519B60`/`0x187604C80`. It mapped `249642` method definitions. Diffable output completed at
`/tmp/final-shop-fresh-diffable` with `CPP2IL_DIFFABLE_EXIT=0` and total execution time
`9006.9952ms`. ISIL output completed at `/tmp/final-shop-fresh-isil` after processing assemblies
`1` through `168`, with `CPP2IL_ISIL_EXIT=0` and total execution time `15182.7135ms`.

This fresh run revalidated the same native anchors used by the managed DTO boundary: the raw
`MNetherFloorShopContents` identity/content/amount fields and raw `MItems` type/rarity fields are
available, while Gold/Red, rank-five, and Shop-key meanings still require the snapshot-scoped
authoritative provider; missing, stale, conflicting, or duplicate provider evidence remains
Unknown/fail-closed.

## Fresh native decompile after A-D production safety repair (2026-08-19)

This is the bounded fresh decompile for candidate `0911bc166f8006319259cd0a8f1eade6b6d0f8ba`.
The command used the game directory read-only, avoided host path conversion, used a container-only
`/tmp`, and used a wildcard metadata path so the packaged Unicode data-directory name was not
hard-coded:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/*_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl ca-certificates >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/round10-final-diffable --output-as diffable-cs; echo CPP2IL_DIFFABLE_EXIT=0; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/round10-final-isil --output-as isil; echo CPP2IL_ISIL_EXIT=0'
```

The Docker command returned exit `0`. Its immutable input hashes and successful markers were:

```text
53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300  /game/BepInEx/interop/Project.dll
573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb  /game/GameAssembly.dll
ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5  /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat
CPP2IL_DIFFABLE_EXIT=0
CPP2IL_ISIL_EXIT=0
```

Cpp2IL reported `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`, Unity
`6000.3.8f1`, metadata version `39`, codereg/metareg `0x186519B60`/`0x187604C80`, and
`249642` mapped method definitions. Diffable output completed at `/tmp/round10-final-diffable`
in `8617.3752ms`; ISIL output completed at `/tmp/round10-final-isil` after assemblies `1` through
`168` in `15281.2854ms`. No native runtime or GameAssembly test was loaded; Cpp2IL only read the
read-only game files.

Fresh native anchors reconfirmed by this run are the four
`MNetherFloorEvents.m_nether_floor_event_part_id_1..4` references, raw
`MNetherFloorEventParts` target/parameter/content fields, raw `MNetherFloorBattles.type`/stage/
Code-drop fields, raw `MItems.type`/rarity, and Shop content identity/amount fields. These raw
fields do not prove Boss/MiniBoss/Normal, Gold/Red rank-five, or Shop-key semantics. Therefore
the A-D repair remains native-aligned: raw popup rewards stay Unknown without an exact
snapshot-scoped typed provider; an ID-less Shop key is retained only with a complete positive
typed identity tuple; a missing or invalid production visible vector pauses instead of using the
legacy comparator; and a managed Event capture must contain exactly four native-compatible slots,
with trailing zero sentinels and local unknown battle parts handled fail-closed.

## Final Recovery fixture and controller E2E gate (2026-08-19)

The Recovery proof repair was limited to the managed `ScriptedRuntimeBridge` fixture seam. Each
captured node preserves its own snapshot-scoped `RecoveryBranchSafetyByPartId`; a bound route map
replaces it only after the production binding step. An explicit drop still supplies an empty map,
so the production `RequireCompleteRecoveryBranchSafety` gate remains fail-closed. No legacy route
comparator or raw native semantic inference was enabled.

The three Recovery tests were run individually before the full gate. Each used the exact bounded
Docker shape below with the test filter changed only to the named method; each restore and build
completed successfully with zero warnings and zero errors:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/root/.nuget/packages --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -uo pipefail; rm -rf /tmp/e2e-src; mkdir -p /tmp/e2e-src; tar -C /src --exclude="AutoNether/bin" --exclude="AutoNether/obj" --exclude="AutoNether.Tests/bin" --exclude="AutoNether.Tests/obj" -cf - AutoNether AutoNether.Tests AutoNether.sln NuGet.config | tar -C /tmp/e2e-src -xf -; cd /tmp/e2e-src; export NUGET_PACKAGES=/root/.nuget/packages; export ABYSS_GAME_DIR=/game; dotnet restore AutoNether.sln --ignore-failed-sources --disable-parallel --nologo -v:minimal; dotnet build AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore -p:ABYSS_GAME_DIR=/game -v:minimal; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore --no-build -p:ABYSS_GAME_DIR=/game --filter "FullyQualifiedName~Production_controller_reconciles_owned_recovery_with_exact_heal_contract" --logger "console;verbosity=normal"'
```

The corresponding two individual filters were
`Production_controller_continues_routing_after_recovery_heal_is_capped_at_full_hp` and
`Production_controller_continues_routing_after_category_skill_applies_erosion_relief`. The
individual results were `1/1 passed`, `1/1 passed`, and `1/1 passed` respectively.

The final full controller gate used this complete command, with both source and game read-only and
all intermediate files in the disposable container `/tmp` or the named NuGet volume:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/root/.nuget/packages --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -uo pipefail; echo START; rm -rf /tmp/e2e-src; mkdir -p /tmp/e2e-src; tar -C /src --exclude="AutoNether/bin" --exclude="AutoNether/obj" --exclude="AutoNether.Tests/bin" --exclude="AutoNether.Tests/obj" -cf - AutoNether AutoNether.Tests AutoNether.sln NuGet.config | tar -C /tmp/e2e-src -xf -; cd /tmp/e2e-src; export NUGET_PACKAGES=/root/.nuget/packages; export ABYSS_GAME_DIR=/game; dotnet restore AutoNether.sln --ignore-failed-sources --disable-parallel --nologo -v:minimal > /tmp/e2e-74-restore.log 2>&1; rs=$?; tail -100 /tmp/e2e-74-restore.log; echo RESTORE_EXIT=$rs; if [ "$rs" -ne 0 ]; then exit "$rs"; fi; dotnet build AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore -p:ABYSS_GAME_DIR=/game -v:minimal > /tmp/e2e-74-build.log 2>&1; bs=$?; tail -160 /tmp/e2e-74-build.log; echo BUILD_EXIT=$bs; if [ "$bs" -ne 0 ]; then exit "$bs"; fi; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore --no-build -p:ABYSS_GAME_DIR=/game --filter "FullyQualifiedName~NetherAutoClimbControllerEndToEndTests" --logger "console;verbosity=normal" > /tmp/e2e-74-test.log 2>&1; ts=$?; tail -320 /tmp/e2e-74-test.log; echo TEST_EXIT=$ts; exit "$ts"'
```

The actual full-gate markers were `RESTORE_EXIT=0`, `BUILD_EXIT=0`, and `TEST_EXIT=0`. The build
reported `0 Warning(s)` and `0 Error(s)`. The controller filter reported `Failed: 0, Passed: 74,
Skipped: 0, Total: 74` in `0.855` seconds. Thus the three Recovery fixtures and the complete
controller E2E group are currently green; no full-gate failure required a follow-up fix.

The fresh native evidence used for RCA and validation was produced in a separate bounded
read-only Docker run before this fixture-only repair:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; echo NATIVE_HASHES_BEGIN; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/*_Data/il2cpp_data/Metadata/global-metadata.dat; echo NATIVE_HASHES_END; apt-get update -qq; apt-get install -y -qq curl ca-certificates >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/recovery-fresh-diffable --output-as diffable-cs; echo CPP2IL_DIFFABLE_EXIT=0; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/recovery-fresh-isil --output-as isil; echo CPP2IL_ISIL_EXIT=0; echo NATIVE_DECOMPLETE=1'
```

That native run returned `CPP2IL_DIFFABLE_EXIT=0`, `CPP2IL_ISIL_EXIT=0`, and
`NATIVE_DECOMPLETE=1`. Its immutable hashes were Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and metadata
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. The run processed 249642
method definitions using Cpp2IL `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`.
The native facts remain raw-only: four Event part references, raw target/parameter/content rows,
raw battle type/stage/drop fields, and exact Shop identity/amount. These facts support the existing
typed-provider and fail-closed boundaries; they do not justify Boss/MiniBoss/Normal or Gold/Red
inference.

## Current Event route-safety proof and affected-filter gate (2026-08-19)

The final fresh-native evidence for this repair is FastCtx job `j-w5ptgw`, with complete log at
`C:/Users/Eden/.fastctx/jobs/j-w5ptgw/output.log`. It used the read-only game mount and disposable
container `/tmp` exactly as follows:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /tmp:exec,size=4g mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; echo NATIVE_HASHES_BEGIN; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/*_Data/il2cpp_data/Metadata/global-metadata.dat; echo NATIVE_HASHES_END; apt-get update -qq; apt-get install -y -qq curl ca-certificates >/dev/null; curl --retry 4 --retry-delay 2 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/current-final-diffable --output-as diffable-cs; echo CPP2IL_DIFFABLE_EXIT=0; timeout 105s /tmp/Cpp2IL --game-path /game --output-to /tmp/current-final-isil --output-as isil; echo CPP2IL_ISIL_EXIT=0; echo NATIVE_DECOMPLETE=1'
```

The Docker job exited `0`; both Cpp2IL runs exited `0` and emitted
`NATIVE_DECOMPLETE=1`. The immutable game hashes were Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and metadata
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. Cpp2IL reported
`2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`, Unity `6000.3.8f1`,
metadata version `39`, and `249642` mapped method definitions; ISIL completed assemblies 1–168.

The fresh anchors are unchanged and decisive: native Event data exposes four raw part references;
native battle data exposes raw type/stage/drop fields without a proven Boss/MiniBoss/Normal map;
and Shop/item data exposes raw identity, amount, type, and rarity without a proven Gold/Red or
rank-five meaning. Therefore the new Event route-safety proof accepts semantic tier only from the
snapshot-scoped typed provider and additionally requires exact projected HP, erosion, and current
combat ownership for the selected `(EventId, EventPartId, OptionNumber, FloorId, NodeId, BattleId)`.
Missing, stale, duplicate, conflicting, or identity-mismatched proof stays local Unknown and cannot
promote a route candidate; raw battle fields remain diagnostics only.

The narrow Event RED/GREEN sequence was recorded in FastCtx jobs `j-r74xdv` (RED) and `j-7qut3i`
(GREEN), with logs under `C:/Users/Eden/.fastctx/jobs/`. The RED Docker build had `0 Warning(s)` and
`0 Error(s)` and the proof-absent public regression reported `1 total / 0 passed / 1 failed / 0 skipped`
(selected unsafe Event node 2 instead of safe node 3). After the minimal provider-backed route-safety
gate, the same narrow Docker gate reported `1 total / 1 passed / 0 failed / 0 skipped`.

The post-fix Event/route filter used FastCtx job `j-lfhbc2`, log
`C:/Users/Eden/.fastctx/jobs/j-lfhbc2/output.log`, and this complete bounded Docker command shape:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /tmp:exec,size=4g --workdir=/src mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -uo pipefail; rm -rf /tmp/work; mkdir -p /tmp/work; tar -C /src --exclude="docs/agents/native-decomp-*" --exclude="docs/agents/evidence-*" --exclude=".git" -cf - . | tar -C /tmp/work -xf -; cd /tmp/work; timeout 180s dotnet restore AutoNether.Tests/AutoNether.Tests.csproj -p:ABYSS_GAME_DIR=/game > /tmp/event-restore.log 2>&1; r=$?; tail -120 /tmp/event-restore.log; echo RESTORE_EXIT=$r; if [ "$r" -ne 0 ]; then exit "$r"; fi; timeout 180s dotnet build AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore -p:ABYSS_GAME_DIR=/game -v:minimal > /tmp/event-build.log 2>&1; b=$?; tail -160 /tmp/event-build.log; echo BUILD_EXIT=$b; if [ "$b" -ne 0 ]; then exit "$b"; fi; timeout 180s dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore --no-build -p:ABYSS_GAME_DIR=/game --filter "FullyQualifiedName~NetherVisibleBranchRoutePlannerTests" --logger "console;verbosity=normal" > /tmp/event-test.log 2>&1; t=$?; tail -320 /tmp/event-test.log; echo TEST_EXIT=$t; exit "$t"'
```

That run returned restore `0`, build `0` with `0 Warning(s)` and `0 Error(s)`, and
`26 total / 26 passed / 0 failed / 0 skipped`.

The two dependent gates were run only after that Event filter passed. Coordinator job `j-d0t6p6`
(`C:/Users/Eden/.fastctx/jobs/j-d0t6p6/output.log`) used the same complete Docker restore/build/test
shape with filter `FullyQualifiedName~NetherRouteSafetyProductionCoordinatorTests`; it returned
restore `0`, build `0` with `0 Warning(s)` and `0 Error(s)`, and `32 total / 32 passed / 0 failed /
0 skipped`. This is the complete class filter; the requested affected subset is included in those
32 tests.

Controller job `j-ltmpb5` (`C:/Users/Eden/.fastctx/jobs/j-ltmpb5/output.log`) then used the same
read-only `/src` and `/game` Docker shape with filter
`FullyQualifiedName~NetherAutoClimbControllerEndToEndTests`; it returned restore `0`, build `0`
with `0 Warning(s)` and `0 Error(s)`, and `74 total / 74 passed / 0 failed / 0 skipped`. No full
1292-test suite was run in this gate, and no native runtime or GameAssembly test was loaded.

## Fixture-only validation and final 1296-test gate (2026-08-19)

The prior full-suite RED was FastCtx job `j-mxzjvc` (`C:/Users/Eden/.fastctx/jobs/j-mxzjvc/output.log`):
`1296 total / 1286 passed / 10 failed / 0 skipped`. Nine failures were route fixtures that had
been left without complete snapshot-scoped visible/horizon evidence (four
`NetherRouteSafetyContextBuilderTests` and five `NetherRuntimePopupProductionTests`). The tenth
failure was `AutoNetherConfigContractTests.Readme_documents_explicit_modes_research_validation_and_native_start_boundaries`.
Its exact cause was the temporary source-copy command: it copied the solution, `NuGet.config`, and
the two project directories, but omitted the repository-root `README.md`; the test correctly
looked for `/tmp/full-suite-source/README.md`. This was a packaging-input omission, not a missing
game file and not a reason to stage decompiled output.

The representative RED-to-GREEN validation used FastCtx job `j-cgtug7`
(`C:/Users/Eden/.fastctx/jobs/j-cgtug7/output.log`). It copied the legitimate repository-root
README and no game files, restored from the read-only named NuGet volume, and produced restore `0`,
build `0 Warning(s)`, `0 Error(s)`, ContextBuilder `1/1`, Shop DTO production `3/3`, and README
contract `1/1`:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget,readonly --tmpfs /tmp:exec,size=4g --workdir=/src -e ABYSS_GAME_DIR=/game -e NUGET_PACKAGES=/nuget mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -euo pipefail; work=/tmp/fixture-validation; rm -rf "$work"; mkdir -p "$work"; cp -a /src/AutoNether.sln /src/NuGet.config /src/README.md /src/AutoNether /src/AutoNether.Tests "$work"/; cd "$work"; echo SOURCE_COPY=README_INCLUDED; dotnet restore AutoNether.sln --configfile NuGet.config -v:minimal; echo RESTORE_EXIT=0; dotnet build AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore -p:ABYSS_GAME_DIR=/game -v:minimal; echo BUILD_EXIT=0; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-build --no-restore -p:ABYSS_GAME_DIR=/game --filter "FullyQualifiedName~NetherRouteSafetyContextBuilderTests.MaximumDepth_IsRetainedForProductionPlannerGate" --logger "console;verbosity=normal"; echo CONTEXT_REP_EXIT=0; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-build --no-restore -p:ABYSS_GAME_DIR=/game --filter "FullyQualifiedName~NetherRuntimePopupProductionTests.Managed_shop_dto_capture_provider_assembles_maps_routes_and_reconciles_key_then_bag" --logger "console;verbosity=normal"; echo SHOP_REP_EXIT=0; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-build --no-restore -p:ABYSS_GAME_DIR=/game --filter "FullyQualifiedName~AutoNetherConfigContractTests.Readme_documents_explicit_modes_research_validation_and_native_start_boundaries" --logger "console;verbosity=normal"; echo README_REP_EXIT=0'
```

The complete affected filter was FastCtx job `j-5zhgid`
(`C:/Users/Eden/.fastctx/jobs/j-5zhgid/output.log`). It used the same read-only mounts and included
the root README in `/tmp/affected-validation`; restore was `0`, build was `0 Warning(s)` and
`0 Error(s)`, and the combined ContextBuilder, RuntimePopup, and README filter returned
`22 total / 22 passed / 0 failed / 0 skipped`.

The final full-suite gate was FastCtx job `j-jq2cdj`
(`C:/Users/Eden/.fastctx/jobs/j-jq2cdj/output.log`). The exact reproducible command was:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget,readonly --tmpfs /tmp:exec,size=4g --workdir=/src -e ABYSS_GAME_DIR=/game -e NUGET_PACKAGES=/nuget mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -uo pipefail; work=/tmp/full-suite-source; rm -rf "$work"; mkdir -p "$work"; cp -a /src/AutoNether.sln /src/NuGet.config /src/README.md /src/AutoNether /src/AutoNether.Tests "$work"/; cd "$work"; echo SOURCE_COPY=README_INCLUDED; dotnet restore AutoNether.sln --configfile NuGet.config -v:minimal > /tmp/full-restore.log 2>&1; restore=$?; tail -80 /tmp/full-restore.log; echo RESTORE_EXIT=$restore; if [ "$restore" -ne 0 ]; then exit "$restore"; fi; dotnet build AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore -p:ABYSS_GAME_DIR=/game -v:minimal > /tmp/full-build.log 2>&1; build=$?; tail -120 /tmp/full-build.log; echo BUILD_EXIT=$build; if [ "$build" -ne 0 ]; then exit "$build"; fi; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-build --no-restore -p:ABYSS_GAME_DIR=/game --logger "console;verbosity=normal" > /tmp/full-test.log 2>&1; test=$?; tail -160 /tmp/full-test.log; echo FULL_TEST_EXIT=$test; exit "$test"'
```

That job returned restore `0`, build `0 Warning(s)` and `0 Error(s)`, and
`1296 total / 1296 passed / 0 failed / 0 skipped` with `FULL_TEST_EXIT=0`. All compilation and
tests ran from the disposable container copy; `/game` and `/src` were read-only, the package
volume was `/nuget:readonly`, and Cpp2IL/native output was not copied or staged.

Before amend, the result was re-verified against the actual uncommitted worktree in FastCtx job
`j-gft8b9` (`C:/Users/Eden/.fastctx/jobs/j-gft8b9/output.log`). The container printed
`WORKTREE_HEAD=82ee463c575d88fd56ca1ce993b8de86885bd2ba` and the seven intended modified source,
test, and evidence files, while preserving the seven untracked `docs/agents/native-decomp-*`
directories. It then used this exact current-worktree command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/nuget,readonly --tmpfs /tmp:exec,size=4g --workdir=/src -e ABYSS_GAME_DIR=/game -e NUGET_PACKAGES=/nuget mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -uo pipefail; echo WORKTREE_HEAD=$(git -C /src rev-parse HEAD); echo WORKTREE_STATUS_BEGIN; git -C /src status --short; echo WORKTREE_STATUS_END; work=/tmp/full-suite-current-worktree; rm -rf "$work"; mkdir -p "$work"; cp -a /src/AutoNether.sln /src/NuGet.config /src/README.md /src/AutoNether /src/AutoNether.Tests "$work"/; cd "$work"; echo SOURCE_COPY=README_INCLUDED; dotnet restore AutoNether.sln --configfile NuGet.config -v:minimal > /tmp/current-full-restore.log 2>&1; restore=$?; tail -80 /tmp/current-full-restore.log; echo RESTORE_EXIT=$restore; if [ "$restore" -ne 0 ]; then exit "$restore"; fi; dotnet build AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-restore -p:ABYSS_GAME_DIR=/game -v:minimal > /tmp/current-full-build.log 2>&1; build=$?; tail -120 /tmp/current-full-build.log; echo BUILD_EXIT=$build; if [ "$build" -ne 0 ]; then exit "$build"; fi; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --no-build --no-restore -p:ABYSS_GAME_DIR=/game --logger "console;verbosity=normal" > /tmp/current-full-test.log 2>&1; test=$?; tail -160 /tmp/current-full-test.log; echo FULL_TEST_EXIT=$test; exit "$test"'
```

The re-verification returned restore `0`, build `0 Warning(s)` and `0 Error(s)`, and
`1296 total / 1296 passed / 0 failed / 0 skipped` with `FULL_TEST_EXIT=0`. The only README copied
was `/src/README.md` from the repository; no README or other file came from `/game`, and no
decompiled/native output was staged. The native design basis remains the separate successful
read-only Cpp2IL run recorded above: Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and metadata
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`; its markers were
`CPP2IL_DIFFABLE_EXIT=0` and `CPP2IL_ISIL_EXIT=0`, with output confined to container `/tmp`.
