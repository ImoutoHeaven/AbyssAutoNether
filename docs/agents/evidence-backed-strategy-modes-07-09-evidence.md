# Evidence record: strategy-mode tickets 07–09

Date: 2026-08-17
Branch: `logic-overhaul`
Parent fixed point: `5f3de38572d5526e73e8576ffe505669c1c8dbc3`
Historical pre-repair task-group commit (not final): `1e1e7a0d6f0215910e9b7d1254c7771d217326ea`

This note is the durable evidence record for the 07–09 implementation group. The game tree was
never mounted writable. Every command below used an ephemeral `docker run --rm` container and a
read-only `/game` bind mount.

## Fresh native evidence

The latest successful targeted Cpp2IL run was job `j-tcu0vv` (log:
`C:/Users/Eden/.fastctx/jobs/j-tcu0vv/output.log`). Its exact command is the final native
mode-wiring cycle command recorded below; the result-focused job `j-2rbzrh` is retained as an
additional anchor.

The exact artifact hashes were:

| Artifact | SHA-256 |
|---|---|
| `BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

`DIFFABLE_EXIT=0` was emitted. The same hashes and successful `DIFFABLE_EXIT=0` were repeated
in targeted jobs `j-cc2p40`, `j-7wxqkq`, `j-w729yh`, `j-ebx82o`, `j-q4hn3d`, `j-7vonvg`, `j-1bje1b`,
`j-a4muxx`, `j-vmps0m`, and `j-ovo3zv`; the final mode-wiring cycle native command is `j-tcu0vv`
below. The final
review-cycle native command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -e; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; items=$(find /tmp/decomp/DiffableCs -type f -name "MItems.cs" | head -n 1); parts=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEventParts.cs" | head -n 1); battles=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorBattles.cs" | head -n 1); events=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEvents.cs" | head -n 1); controller=$(find /tmp/decomp/DiffableCs -type f -name "NetherEventPopupController.cs" | head -n 1); api=$(find /tmp/decomp/DiffableCs -type f -name "NetherApiDataStore.cs" | head -n 1); result=$(find /tmp/decomp/DiffableCs -type f -name "NetherResultResponseEntity.cs" | head -n 1); point=$(find /tmp/decomp/DiffableCs -type f -name "NetherPointData.cs" | head -n 1); nl -ba "$items" | sed -n "1,25p"; nl -ba "$parts" | sed -n "1,32p"; nl -ba "$battles" | sed -n "1,20p"; nl -ba "$events" | sed -n "1,30p"; nl -ba "$controller" | sed -n "115,135p"; nl -ba "$api" | sed -n "284,290p"; nl -ba "$result" | sed -n "3,16p"; nl -ba "$point" | sed -n "104,110p"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; echo DIFFABLE_EXIT=0'
```

The final native option-local-unknown compatibility-cycle command was job `j-ovo3zv`:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -e; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; parts=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEventParts.cs" | head -n 1); event=$(find /tmp/decomp/DiffableCs -type f -name "NetherEventPopupController.cs" | head -n 1); recover=$(find /tmp/decomp/DiffableCs -type f -name "NetherRecoverPopupController.cs" | head -n 1); treasure=$(find /tmp/decomp/DiffableCs -type f -name "NetherTreasurePopupController.cs" | head -n 1); nl -ba "$parts" | sed -n "4,29p"; nl -ba "$event" | sed -n "115,135p"; nl -ba "$recover" | sed -n "59,83p"; nl -ba "$treasure" | sed -n "108,124p"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; echo DIFFABLE_EXIT=0'
```

The final native mode-wiring cycle command was job `j-tcu0vv`:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -e; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; items=$(find /tmp/decomp/DiffableCs -type f -name "MItems.cs" | head -n 1); parts=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEventParts.cs" | head -n 1); battles=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorBattles.cs" | head -n 1); events=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEvents.cs" | head -n 1); controller=$(find /tmp/decomp/DiffableCs -type f -name "NetherEventPopupController.cs" | head -n 1); api=$(find /tmp/decomp/DiffableCs -type f -name "NetherApiDataStore.cs" | head -n 1); nl -ba "$items" | sed -n "4,19p"; nl -ba "$parts" | sed -n "4,29p"; nl -ba "$battles" | sed -n "4,15p"; nl -ba "$events" | sed -n "4,25p"; nl -ba "$controller" | sed -n "115,135p"; nl -ba "$api" | sed -n "284,290p"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; echo DIFFABLE_EXIT=0'
```

Authoritative anchors from fresh output:

- `j-cc2p40` `BATTLES` lines 7–15: `MNetherFloorBattles` exposes only `id`,
  `m_nether_map_floor_id`, raw `type`, `m_nether_battle_stage_id`, and `code_drop_ratio`.
- `j-cc2p40` `PARTS` lines 7–29: `MNetherFloorEventParts` exposes the three raw target/parameter
  pairs plus `content_type`, `content_id`, and `amount`.
- `j-cc2p40` `EVENTS` lines 7–25: `MNetherFloorEvents` owns the event identity and four exact
  `m_nether_floor_event_part_id_*` bindings.
- `j-w729yh` lines 115–135: the native popup owns `_mCharacterId`, `_mNetherEvents`,
  `_mNetherEventPartsArray`, and `InitializeView(long mNetherMapFloorId, long extendId,
  long mCharacterId, string charaAssetId, Action<NetherEventResultModel> onConfirm)`.
- `j-w729yh` lines 287–288: native event update binds `floorLevel`, `floorIndex`,
  `selectedNumber`, and `changeTargetMNetherCodeId`; it does not accept Event ID or Event-Part ID.
- `j-cc2p40` `RESULT` lines 413–431: the result model retains `MNetherFloorEventPartsId` and
  has update overloads for the native model/code-change flow.
- `j-q4hn3d` `REQUEST_ENTITY` lines 3–25: normal result request contains only Nether/map IDs and
  insurance flag; `CODE_POINTS` lines 3–13: settlement returns four `NetherCodePointEntity`
  totals (`Skill`, `Attack`, `ErosionResist`, `ErosionUp`). `SPHERE` lines 104–110 separately
  identify `NetherPointData.SpherePointRatio`.
- `j-2rbzrh` lines 3–12 and 281–282: `NetherResultResponseEntity.nether_code_points` is a
  result response field and `CreateNetherResultModelAsync` is the result-view consumer.
- `j-7vonvg` lines 1–160: the fresh decomp resolves the actual native namespaces and confirms
  `MNetherFloorBattles` lines 4–15, `MNetherFloorEventParts` lines 4–29,
  `MNetherFloorEvents` lines 4–25, `NetherEventPopupController` lines 115–133,
  `NetherApiDataStore` lines 287–288, and `NetherPointData.SpherePointRatio` lines 104–110.
- `j-a4muxx` `MItems.cs` lines 4–19: `MItems` has exact `id`, raw `type`, `rarity`, value, and
  possession fields; `MNetherFloorEventParts.cs` lines 4–29 reconfirm the event-part content tuple,
  and `MNetherFloorBattles.cs` lines 4–15 reconfirm the raw battle row. These are the fresh native
  anchors for the final review fixes: exact item options require a positive MItems identity, item
  type, rarity, and amount, and exact commitments fingerprint all effect evidence and projected
  state before payment.
- `j-ovo3zv` lines 59–83 and 108–124: the native Recovery and Treasure popup controllers both own
  `_mNetherEvents` and `_mNetherEventPartsArray`, but their `InitializeView` signatures omit the
  Event presenter character ID; this supports keeping exact Event-row binding on Event popups while
  preserving the existing non-Event Recovery/Treasure mapping path.
- `j-tcu0vv` `MItems.cs` lines 4–19, `MNetherFloorEventParts.cs` lines 4–29,
  `MNetherFloorBattles.cs` lines 4–15, `MNetherFloorEvents.cs` lines 4–25,
  `NetherEventPopupController.cs` lines 115–135, and `NetherApiDataStore.cs` lines 287–288
  reconfirm the exact master, popup, and native request seams used by the final Equipment-mode
  default wiring.

Native-first deviations recorded from these anchors:

1. Event ID and Event-Part ID are immutable client-side correlation/commitment evidence only. The
   production action continues to submit the native floor/option/code arguments; it does not add
   unsupported IDs to the request.
2. The current native battle row has a raw integer `type` but no freshly proven local semantic
   Boss/MiniBoss/Normal enum. Runtime mapping therefore keeps an exact battle option's semantic
   tier unknown and rejects that option locally; the typed policy accepts a future authoritative
   tier provider without guessing from `type` or `code_drop_ratio`.
3. The current client exposes settlement family points only after the normal result response and
   exposes `SpherePointRatio` as technology. Production research projection remains unknown until
   a server-authoritative pre-settlement projection is supplied; policy never manufactures a
   completion from a Code count, gauge, technology rate, or displayed power.

## Implementation and test evidence

07 adds complete retained-portfolio Equipment comparison, hard-excluded/opposed removal tie order,
strict capacity improvement, candidate-local unknown handling, and the Equipment/Research reroll
split. 08 adds wallet-plus-known-projected-settlement completion, primary/secondary transition,
forced Research rerolls, ordered capacity removal, completed-family protection, same-family swaps,
and Reachable-Unquantified settlement acceptance. 09 adds exact native row mapping, option-local
unknown effects/items/battles, all-living-character HP checks, mode-aware semantic priorities,
immutable Event commitments, popup propagation, and pre-payment stale-commitment pauses.

Fresh Docker red/green and fix records:

- Job `j-oynlw4` (after fresh decomp job `j-g31qc4`) ran the full test command and produced the
  intentional red for the old Research reserve assertion: `1097 passed / 1 failed / 1098 total`,
  expected `Keep`, actual `Reload`. The test was updated to assert the ticket-08 rule that an
  incomplete Research target spends an available reroll above the Equipment reserve.
- Job `j-smdtq5` (after fresh decomp job `j-7on4tr`) ran the same full command and passed
  `1098/1098`.
- Job `j-wpyh0r` (after fresh targeted decomp job `j-cc2p40`) found a production-only definite
  assignment error in the new option-local mapper. The mapper was fixed by initializing the
  option-local `effects` and `detail` values before the short-circuit binding call; no game
  behavior was inferred or broadened.
- Job `j-z0n2z2` (after fresh targeted decomp job `j-w729yh`) passed the production Release build
  with `0 Warning(s)` and `0 Error(s)`. The verified artifact is
  `/src/release/Release/net6.0/AutoNether.dll`; job `j-2ggpcm` recorded SHA-256
  `ff2c746b4412b7f81c36fe6b43735bff068aae91da59df6953d15b2d2cd4c706`.
- Job `j-b3ssdw` (after the mapper fix, with the same fresh RO game evidence) passed the full
  suite `1098/1098`.
- Job `j-4xbwmu` (after fresh review-cycle native jobs `j-7vonvg` and `j-1bje1b`) passed the
  focused Code/Event/popup group `69/69` after tightening exact MItems reward validation.
- Job `j-2hgvsl` (after the same fresh review-cycle native jobs) passed the final full suite
  `1098/1098`.
- Job `j-phlue8` (after the same fresh review-cycle native jobs) passed the final production
  Release build with `0 Warning(s)` and `0 Error(s)`. The final artifact is
  `/src/release/Release/AutoNether.dll` with SHA-256
  `b77511d4aa6b94db896eb7db3f07479e59f18d2d82b907373e7b2eb9851030c2`.
- Job `j-f6hhod` (after fresh final native job `j-a4muxx`) passed the focused group `70/70` after
  the exact commitment fingerprint and projected-state review fix.
- Job `j-36khjr` (after the same fresh final native job) passed the final full suite `1099/1099`.
- Job `j-f10fai` (after the same fresh final native job) passed the final production Release build
  with `0 Warning(s)` and `0 Error(s)`. The final artifact is
  `/src/release/Release/AutoNether.dll` with SHA-256
  `2f6a0d58fe0d4bf798bace93e4a34d4a327dc6b79ab916f7b65a2ebe8aec39fd`.
- Job `j-37m1y1` (after fresh native compatibility-cycle job `j-vmps0m`) passed the focused group
  `70/70` after preserving the Recovery/Treasure popup seam.
- Job `j-v8biww` (after the same fresh native compatibility-cycle job) passed the final full suite
  `1099/1099`.
- Job `j-vi2t4w` (after the same fresh native compatibility-cycle job) passed the final production
  Release build with `0 Warning(s)` and `0 Error(s)`. The final artifact is
  `/src/release/Release/AutoNether.dll` with SHA-256
  `2e7a7202d1c87ac8c054fd7c1b20447125fcde4471cb04b87d4881432cf163b1`.
- Job `j-t1d56n` (after fresh final native job `j-ovo3zv`) passed the focused group `70/70` after
  adding the option-local unknown fallback for invalid event-part amounts/effect counts.
- Job `j-hd7i4m` (after the same fresh final native job) passed the final full suite `1099/1099`.
- Job `j-o9dlc9` (after the same fresh final native job) passed the final production Release build
  with `0 Warning(s)` and `0 Error(s)`. The final artifact is
  `/src/release/Release/AutoNether.dll` with SHA-256
  `3d73c05f37f9573950497c06cd6af33deae7c4d22d2eccdf7a465db26e48e894`.

- Job `j-hdn2tx` (after fresh final native job `j-tcu0vv`) passed the focused group `71/71` after
  wiring explicit Equipment mode into the production Event reward ordering.
- Job `j-3h48rq` (after the same fresh final native job) passed the final full suite `1100/1100`.
- Job `j-eey3n2` (after the same fresh native job) passed the final production Release build with
  `0 Warning(s)` and `0 Error(s)`. The final artifact is
  `/src/release/Release/AutoNether.dll` with SHA-256
  `cb10db2ad0ae1cdd03dbb2fba72f404d06886d69a59c6c0f9db99e3188fd35da`.

Focused verification command (Docker, writable repo bind only for build intermediates, read-only
game bind):

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src -e ABYSS_GAME_DIR=/game mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --filter "FullyQualifiedName~NetherCodePolicyTests|FullyQualifiedName~NetherEventPolicyTests|FullyQualifiedName~NetherPopupDispatchPolicyTests" --logger "console;verbosity=minimal"'
```

Job `j-hdn2tx` passed the final focused group `71/71` after the production mapper fix, exact MItems
reward validation, exact commitment fingerprint/projected-state review fix, the native-compatible
Recovery/Treasure mapping correction, the option-local unknown fallback, and explicit Equipment mode
wiring. Final full suite job `j-3h48rq` passed `1100/1100`, and final Release build job `j-eey3n2`
passed with `0 Warning(s)` and `0 Error(s)`.

All review and fix claims for this group use the native anchors and hashes above. No remote issue,
label, or game-file state was changed.

## Review-gate repair cycle

This repair cycle amended the provisional 07–09 work in place. It began from
`7265f60985c427b9105fc998520b354c04781b20` and kept the game tree strictly read-only. No fresh
native fact contradicted the approved spec: the native client still provides only the raw Code
category/effect/power fields, the normal-result settlement points, and the raw Event/battle rows
listed below. Where the client does not provide a pre-settlement Research projection or a typed
battle semantic tier, production remains fail-closed.

### Fresh native artifacts and anchors

Fresh diffable decomp job `j-1c76xw` completed with `CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. Its exact
Docker command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; echo "=== FRESH REPAIR NATIVE DESIGN ==="; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; status=$?; echo "CPP2IL_EXIT=$status"; tail -n 80 /tmp/cpp2il.log; test "$status" = 0; codes=$(find /tmp/decomp/DiffableCs -type f -name "MNetherCodes.cs" | head -n 1); skills=$(find /tmp/decomp/DiffableCs -type f -name "MNetherCodeCategorySkills.cs" | head -n 1); points=$(find /tmp/decomp/DiffableCs -type f -name "NetherCodePointEntity.cs" | head -n 1); response=$(find /tmp/decomp/DiffableCs -type f -name "NetherResultResponseEntity.cs" | head -n 1); point=$(find /tmp/decomp/DiffableCs -type f -name "NetherPointData.cs" | head -n 1); api=$(find /tmp/decomp/DiffableCs -type f -name "NetherApiDataStore.cs" | head -n 1); events=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEvents.cs" | head -n 1); parts=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEventParts.cs" | head -n 1); battles=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorBattles.cs" | head -n 1); controller=$(find /tmp/decomp/DiffableCs -type f -name "NetherEventPopupController.cs" | head -n 1); for spec in "$codes|1|80" "$skills|1|80" "$points|1|20" "$response|1|40" "$point|100|112" "$api|280|292" "$events|1|30" "$parts|1|35" "$battles|1|20" "$controller|115|145"; do IFS="|" read -r f a b <<< "$spec"; echo "=== $f"; sha256sum "$f"; nl -ba "$f" | sed -n "${a},${b}p"; done; echo DIFFABLE_EXIT=0'
```

The game artifacts in `j-1c76xw` were unchanged and hashed as follows:

| Artifact | SHA-256 |
|---|---|
| `BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

Fresh decompiled artifact hashes and authoritative anchors:

| Decomp artifact | SHA-256 | Anchor |
|---|---|---|
| `MNetherCodes.cs` | `f321ea53eeb23c9130f8fa4b090c7c9b6f574cca6bbfa9eb8a21dec46c0046f5` | lines 4–29: `id`, `category`, raw effect parameters, and `power` |
| `MNetherCodeCategorySkills.cs` | `f6c6d27f32c4e873707598a7440f4e0aa5497f2a0cc985a903e91ca72f894749` | lines 4–25: category-skill `category`, raw effect parameters, no family-wallet settlement |
| `NetherCodePointEntity.cs` | `ab647fdb2152d9c94c54742107f54036bdc486772d5f7793d128c4b8992da77f` | lines 4–9: `Skill`, `Attack`, `ErosionResist`, `ErosionUp` |
| `NetherResultResponseEntity.cs` | `662f993792e1376cfc4051e16082c3f03ecbb9b91a0da3f47ddeefbef312e0a2` | lines 4–12: `nether_code_points` is on the normal result response |
| `NetherPointData.cs` | `7fb76a026bbae620a66474b4b51c07838ae4eafaa65430b1d044e4c494b94c97` | lines 104–110: `SpherePointRatio` is technology data |
| `NetherApiDataStore.cs` | `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` | lines 284–288: event update takes floor level/index, option, change-code ID, cancellation only |
| `MNetherFloorEvents.cs` | `aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006` | lines 4–25: Event ID and four exact part IDs |
| `MNetherFloorEventParts.cs` | `5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` | lines 4–29: three target/parameter pairs plus content type/ID/amount |
| `MNetherFloorBattles.cs` | `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` | lines 4–15: battle ID, raw type, stage ID, drop ratio |
| `NetherEventPopupController.cs` | `a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf` | lines 115–135: `_mCharacterId`, `_mNetherEvents`, `_mNetherEventPartsArray`, `ExecuteEvent`, `InitializeView`, `OnPanelSelected` |

Fresh ISIL control-flow job `j-f1gj1p` also completed with `CPP2IL_EXIT=0` and `ISIL_EXIT=0`. Its
exact Docker command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; echo "=== FRESH REPAIR NATIVE ISIL ==="; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/isil --output-as isil >/tmp/cpp2il_isil.log 2>&1; status=$?; echo "CPP2IL_EXIT=$status"; tail -n 40 /tmp/cpp2il_isil.log; test "$status" = 0; api=$(find /tmp/isil/IsilDump -type f -name "NetherApiDataStore.txt" | head -n 1); flow=$(find /tmp/isil/IsilDump -type f -name "NetherEventFloorEventFlow_NestedType__HandleEventConfirmedAsync_d__7.txt" | head -n 1); result=$(find /tmp/isil/IsilDump -type f -name "NetherEventResultModel.txt" | head -n 1); echo "=== API REQUEST ANCHOR ==="; sha256sum "$api"; grep -n -A70 -B8 -E "RequestNetherUpdateEventAsync\\(System.Int32" "$api" | head -n 170; echo "=== EVENT RESULT ANCHOR ==="; sha256sum "$result"; grep -n -A120 -B8 -E "Method: .*RequestNetherUpdateEventAsync|Method: .*CreateModelByEventStarted" "$result" | head -n 260; echo "=== EVENT FLOW ANCHOR ==="; sha256sum "$flow"; grep -n -A220 -B12 -E "RequestNetherUpdateEventAsync|TransitionNetherBattleAsync" "$flow" | head -n 280; echo ISIL_EXIT=0'
```

`j-f1gj1p` hash/control-flow anchors are `NetherApiDataStore.txt`
`af0762b43921e2c78cd46d5612a3d14292bb3274b9147e1c7e1a502c98a7b235`, lines 55–125 of the job
output (the native event request signature and argument stores), and
`NetherEventFloorEventFlow_NestedType__HandleEventConfirmedAsync_d__7.txt`
`191af3d4db0107061afc79589895219d17cb2aaaafe369d209f2d0bc6c6a47f6`, job-output lines 490–530
and 659–673. The latter shows the native flow calling `NetherEventResultModel.RequestNetherUpdateEventAsync`
before the result continuation and opening the battle transition only on the later branch. Event
and part IDs therefore remain client correlation/commitment fields; they are not fabricated native
request parameters.

### Repair RED, RCA, and GREEN evidence

The public-seam repair RED run used the writable repository bind and the same read-only `/game`
bind. It produced `Failed: 9, Passed: 51, Skipped: 0, Total: 60`, covering hard-exclusion
precedence, multi-epoch Research fallback, opposed-family replacement in both orientations,
Research evidence gating, reward commitment mismatch, and duplicate item/battle locality. The
command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc "export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --filter 'FullyQualifiedName~NetherCodePolicyTests|FullyQualifiedName~NetherPopupDispatchPolicyTests|FullyQualifiedName~NetherStrategyVisibleEvidenceMapperTests' --logger 'console;verbosity=minimal'"
```

The first post-fix focused run `j-e5ns9e` passed `88/88`. The fresh native evidence above was
re-read for the repair cycle before that GREEN result. The full-suite RCA run `j-tslvaq` then
reported `11 failed / 1104 passed / 1115 total`, all in the old reload/keep transaction fixtures.
The ranked hypothesis was confirmed by the failure shape and the public policy seam: removing the
old all-opposing pre-rejection made those fixtures' synthetic `DefaultEquipmentEvidence` and
`ScriptedCodePolicyEvidence` mutation rows describe legal strict improvements, so the controller
correctly selected before rerolling. The minimal regression repair removed only those non-authoritative
opposing-family mutation rows from the test evidence; it did not restore the production pre-reject
or weaken ticket-07/08 replacement evaluation. No native design conflict was found.

After that fixture repair, focused public-seam coverage job `j-7cz5nm` passed `165/165`; after the
small fail-closed null guard in the production evidence binder, final focused job `j-tybx5a` also
passed `165/165`. The final full Docker suite job `j-cr3kw8` passed `1115/1115`.

The final focused command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --filter "FullyQualifiedName~NetherBattleResultCodeCoordinatorTests|FullyQualifiedName~NetherAutoClimbControllerEndToEndTests|FullyQualifiedName~NetherCodePolicyTests|FullyQualifiedName~NetherEventPolicyTests|FullyQualifiedName~NetherPopupDispatchPolicyTests|FullyQualifiedName~NetherStrategyVisibleEvidenceMapperTests|FullyQualifiedName~NetherEventProductionEvidenceBindingTests" --logger "console;verbosity=minimal"'
```

The final full-suite command was:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --logger "console;verbosity=minimal"'
```

The final full-suite rerun after this evidence update was job `j-dcx46k` and passed `1115/1115`.
The final production Release rebuild job `j-okrpcd` succeeded with `0 Warning(s)` and `0 Error(s)`;
the build output was `/src/release/Release/net6.0/AutoNether.dll` with SHA-256
`23b8e420c977c2cd57f7485748d25aed41820e2b2cb18ef5561b6dd7f4850bfb`. Docker `git diff --check`
job `j-36csfc` exited 0;
Git emitted only the repository's CRLF-to-LF autocrlf warnings and no whitespace errors.

All six reviewer findings now have public-seam RED/GREEN coverage and fresh native evidence. No
remote issue or label state, and no file under `dotabyss_x_cl`, was changed.

### Second review repair closure (2026-08-18)

The second review repair used a new Docker `run --rm` Cpp2IL invocation, with
`C:/Users/Eden/PixelAbyssX/dotabyss_x_cl` mounted only as read-only `/game`. The exact fresh
game inputs were:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

That Docker job was `j-cx00jc` (exit 0; full log
`C:/Users/Eden/.fastctx/jobs/j-cx00jc/output.log`). It downloaded
`Cpp2IL-2022.1.0-pre-release.21-Linux`, ran
`Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs`, and returned
`DIFFABLE_EXIT=0`. The exact decompiled artifacts and hashes were:

```text
MItems.cs                       e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27
MNetherFloorEventParts.cs      5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128
MNetherFloorBattles.cs         7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720
MNetherFloorEvents.cs          aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006
NetherEventPopupController.cs  a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf
NetherApiDataStore.cs          b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071
```

Authoritative anchors from that fresh output are `MItems.cs:4-19` (item id, raw type,
rarity, value, possession limit), `MNetherFloorEventParts.cs:4-29` (part id, three raw
target/select-parameter pairs, content type/id/amount), `MNetherFloorBattles.cs:4-15`
(battle id, map-floor id, raw integer type, stage id, Code-drop ratio), and
`MNetherFloorEvents.cs:4-25` (Event id, map-floor id, type, and four declared part IDs).
`NetherEventPopupController.cs:115-135` captures `_mCharacterId`, `_mNetherEvents`, and
`_mNetherEventPartsArray`; `NetherApiDataStore.cs:284-290` proves that the native update
seam is `RequestNetherUpdateEventAsync(int floorLevel, int floorIndex, int selectedNumber,
long changeTargetMNetherCodeId, CancellationToken ct)`. Thus Event/Part IDs, floor/node
identity, and the full option identity remain immutable client correlation/commitment data;
they are not fabricated native request parameters. The raw battle `type` still has no proven
local semantic tier, so an option without a typed tier provider remains fail-closed. No
ticket/spec/CONTEXT deviation was required: the implementation follows the native boundary.

The repair RED/GREEN and RCA evidence, all run with the same writable repository bind and
read-only `/game` bind, is:

```text
j-tglrmq  RED  3 failed / 3 total:
  mixed Equipment complete mutation selected Keep instead of Select;
  missing exact partial-death proof was accepted;
  equal Event options chose EventId 9922 instead of the deterministic 9920 tie.
j-qsj6xj  GREEN  3 passed / 3 total after the minimal policy, proof, and tie fixes.
j-mxkuu9  RED  43 passed / 1 failed / 44 total through the real production mapper and
          popup seam: the exact sibling lost its mapped Amount and became unknown.
j-nrltgu  GREEN  24 passed / 24 total after mapping the native target parameter as Amount.
j-d4mw2t  RCA  150 passed / 2 failed / 152 total; the two regressions were the generic
          battle-trigger expectation and an incompatible count-five fixture.
j-bjfhzq  GREEN  152 passed / 152 total after the option-local null/fixture guards.
j-m3qi02  GREEN  44 passed / 44 total for CodePolicy plus the production opposed-family
          retention regression after requiring authoritative retained-family evidence.
j-pa88x7  GREEN  153 passed / 153 total before the final warning-only nullability cleanup.
j-ol2eza  GREEN  212 passed / 212 total after that cleanup (expanded focused suite).
```

The real production mixed-known/unknown test now builds its rows through
`NetherStrategyVisibleEvidenceMapper.Map`, duplicates one item and one battle row, binds
Research/Equipment evidence, and dispatches the exact surviving sibling. The binder indexes
parts/items/battles independently, invalidates only the dependent option, and pauses only when
no legal option remains. Research and Equipment both require exact route/resource/semantic
evidence; partial-death authorization and rank-five proof are option-local and missing proof
fails closed. Equipment complete replacement evaluates each legal candidate/removal pair
against the complete retained portfolio, preserving hard exclusions and strict improvement.
Event commitment lookup and stale guarding include EventId, EventPartId, floor/node, option,
effects, reward-or-battle identity, projected state/resources, and deterministic tie fields.

The initial full-suite run `j-ps0n1s` was a diagnostic RED (`1134 passed / 1 failed / 1135`)
for a synthetic opposed-family evidence row; `j-m3qi02` is the focused GREEN for that RCA.
The final focused rerun `j-bfecd7` passed `78/78`; the final expanded focused rerun `j-ol2eza`
passed `212/212`; and the final full-suite rerun `j-zzmpii` passed `1135/1135`. The production
Release Docker build `j-gm41ek` succeeded with
`0 Warning(s)` and `0 Error(s)` and produced `/src/release/Release/net6.0/AutoNether.dll` with
SHA-256 `a52a1c5936e63666f7c14eba5156a048b0e3a3414f525047dd45fd7377c00f8d`. The Docker
verification command also ran `git diff --check` and exited 0; its only output was the existing
repository CRLF-to-LF autocrlf warnings, with no whitespace errors. Every listed Docker command
used `docker run --rm`, a writable repository bind only where needed, and the game bind at
`/game,readonly`.

### Third review repair closure (2026-08-18)

The third-review cycle ran a post-fix fresh Docker Cpp2IL decompile as job `j-g1etxg` (exit 0;
full log `C:/Users/Eden/.fastctx/jobs/j-g1etxg/output.log`). The invocation used
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly` and invoked
`/tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs`, returning
`DIFFABLE_EXIT=0`. The exact game hashes were unchanged and freshly captured:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

The fresh diffable artifact hashes remained:

```text
MItems.cs                       e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27
MNetherFloorEventParts.cs      5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128
MNetherFloorBattles.cs         7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720
MNetherFloorEvents.cs          aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006
NetherEventPopupController.cs  a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf
NetherApiDataStore.cs          b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071
```

The authoritative anchors are unchanged: `MItems.cs:4-19`, `MNetherFloorEventParts.cs:4-29`,
`MNetherFloorBattles.cs:4-15`, `MNetherFloorEvents.cs:4-25`,
`NetherEventPopupController.cs:115-135`, and `NetherApiDataStore.cs:284-290`. Native design
still exposes exact row IDs and required raw fields, while the update seam accepts only floor
level/index, option number, and Code-change ID. No native/spec conflict was found; malformed-row
identity preservation and client-side commitment locality follow the native boundary.

The third-review public-seam diagnosis and repair loop was:

```text
j-yu7ocy  RED  5 failed / 65 passed / 70 total:
  both primary->secondary orientations removed the lower secondary Code as an ordinary fallback;
  active and secondary hard-excluded held targets were skipped in favour of ordinary removal;
  malformed item identity was collapsed to zero and its valid sibling masked the dependency.
j-cldkml  GREEN  70 passed / 70 total after effective-target, hard-exclusion, and row-identity fixes.
j-i8su68  GREEN  314 passed / 314 total (expanded focused suite).
j-e81p32  GREEN  70 passed / 70 total (final focused suite).
j-qbk2mm  GREEN  1142 passed / 1142 total (full suite).
j-g4egni  GREEN  Release build: 0 Warning(s), 0 Error(s).
```

The post-fix policy uses the effective `targetFamily` for capacity filtering, removal legality,
priority, and same-family replacement. Hard-excluded held Codes are admitted ahead of target-family
protection, but `IsPortfolioHardSafe`/opposed-repair checks remain mandatory; ordinary active-target
Codes and non-surplus completed-family Codes remain protected. The production capture now preserves
the native ID on malformed item/battle rows and marks the ID invalid/ambiguous when a valid sibling
shares it. The actual mixed capture-to-popup test keeps the exact heal sibling dispatchable while
 rejecting only the item and battle options.

Final post-cleanup validation after the parameter-name cleanup was rerun with the same fresh
native evidence and the same read-only game mount:

```text
j-zs1lyj  GREEN  focused public-seam suite: 70 passed / 70 total.
j-viie4x  GREEN  expanded focused suite: 314 passed / 314 total.
j-32c293  GREEN  full suite: 1142 passed / 1142 total.
j-5rfnhf  GREEN  Release build: 0 Warning(s), 0 Error(s).
j-25iyqe  GREEN  Docker git diff --check exit 0; Release/AutoNether.dll SHA-256
          ccd531192136f819559bc0459fc12642112601d710c630792e5ca19e69c15e23.
```

The final validation jobs are retained at `C:/Users/Eden/.fastctx/jobs/`. The only output from
`git diff --check` was the repository's existing CRLF-to-LF autocrlf warnings; no whitespace
errors were reported. The game tree remained read-only and no remote state was changed.

### Final spec-axis repair closure (2026-08-18)

This final repair cycle used the native-first rule for both remaining findings. The pre-fix
fresh decompile was job `j-2rngms` (exit 0; log
`C:/Users/Eden/.fastctx/jobs/j-2rngms/output.log`) and the post-fix fresh decompile was job
`j-a9j9qd` (exit 0; log `C:/Users/Eden/.fastctx/jobs/j-a9j9qd/output.log`). Both used this
Docker command shape with the game mounted read-only and all decompiler output in the ephemeral
container:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -e; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; test $? = 0; battles=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorBattles.cs" | head -n 1); events=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEvents.cs" | head -n 1); parts=$(find /tmp/decomp/DiffableCs -type f -name "MNetherFloorEventParts.cs" | head -n 1); api=$(find /tmp/decomp/DiffableCs -type f -name "NetherApiDataStore.cs" | head -n 1); nl -ba "$battles" | sed -n "4,15p"; nl -ba "$events" | sed -n "4,25p"; nl -ba "$parts" | sed -n "4,29p"; nl -ba "$api" | sed -n "284,290p"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; echo DIFFABLE_EXIT=0'
```

The fresh game input hashes were identical in both runs:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

The post-fix diffable artifacts were also freshly hashed:

```text
MNetherFloorBattles.cs      7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720
MNetherFloorEvents.cs       aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006
MNetherFloorEventParts.cs   5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128
NetherApiDataStore.cs       b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071
```

Native anchors are `MNetherFloorBattles.cs:4-15` (the battle `type` is only raw `int`, with
exact battle/map-floor/stage/drop-ratio fields), `MNetherFloorEvents.cs:4-25` (Event identity
and four exact part IDs), `MNetherFloorEventParts.cs:4-29` (three raw target/parameter pairs
and exact content tuple), and `NetherApiDataStore.cs:284-290` (the update seam accepts only
floor level, floor index, selected option number, and Code-change ID). Therefore raw battle
types 1–8 do not prove Boss/MiniBoss/Normal semantics; production now retains exact battle and
stage/content identity while leaving semantic evidence unknown unless a separately typed,
authoritative provider supplies it. Event/Part IDs remain client correlation and are not sent
as invented native request parameters. No ticket/spec/CONTEXT deviation was required because
the implementation now matches this native boundary.

The public-seam RED/GREEN loop was tied to those fresh native runs:

```text
j-9tdags  RED    10 failed / 51 passed / 61 total:
  raw battle types 1–8 incorrectly claimed known semantic evidence;
  the existing exact event-battle mapper assertion made the same overclaim;
  a mixed Rush+Impact Research portfolio kept a valid secondary offer.
j-wwler8  GREEN  61 passed / 61 total after the minimal mapper and effective-target fixes.
```

The RCA was bounded to the native seams above: the Research effective target was selected only
after the primary reroll epochs were exhausted, before hard eligibility, candidate filtering,
capacity removal, and same-family comparison; the raw battle field was retained only as raw
identity. The expanded focused suite `j-gmybcz` passed `323/323` and the full Docker suite
`j-czkkaa` passed `1151/1151`. The production Release build `j-djprcx` passed with `0 Warning(s)`
and `0 Error(s)`, producing `/src/release/Release/net6.0/AutoNether.dll`; a Docker hash check
reported SHA-256 `e71683ee3f4db5cec8efcaeccbabce46fdf7a70b64df244f8315da7277c761c0`. The
post-documentation Docker verification used the repository bind at `/src` and the same read-only
`/game` bind, then ran `git diff --check`; it returned `DIFF_CHECK_EXIT=0`, a clean worktree, and
`git rev-list --count 5f3de38572d5526e73e8576ffe505669c1c8dbc3..HEAD = 1`. Every test/build/decompile
command used `docker run --rm`; the game directory was never written.

### Final gate repair closure: projected commitment and production assembly boundary (2026-08-18)

This final gate was run from the in-place repair worktree before amending the one task commit on
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The game directory remained read-only. Fresh native
evidence was collected both before the public-seam RED and after the fixes:

```text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; status=$?; echo CPP2IL_EXIT=$status; test "$status" = 0; for spec in "NetherApiDataStore.cs|270|315" "NetherEventPopupController.cs|100|145" "MNetherFloorEvents.cs|1|30" "MNetherFloorEventParts.cs|1|35" "NetherUpdateEventResponseEntity.cs|1|20"; do name=${spec%%|*}; rest=${spec#*|}; start=${rest%%|*}; end=${rest##*|}; file=$(find /tmp/decomp/DiffableCs -type f -name "$name" | head -n 1); echo "=== $name ==="; sha256sum "$file"; nl -ba "$file" | sed -n "${start},${end}p"; done; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; echo DIFFABLE_EXIT=0'
```

The pre-RED run was job `j-tvq0jv` and the post-fix run was `j-5stp8d`; both returned
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. In both runs the game hashes were exactly:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

The fresh decomp artifact hashes were unchanged across the two cycles:

```text
NetherApiDataStore.cs              b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071
NetherEventPopupController.cs      a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf
MNetherFloorEvents.cs              aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006
MNetherFloorEventParts.cs          5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128
NetherUpdateEventResponseEntity.cs 30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa
```

Authoritative anchors are `NetherApiDataStore.cs:284-290`, where
`RequestNetherUpdateEventAsync` accepts only floor level, floor index, selected option, Code-change
ID, and cancellation; `NetherEventPopupController.cs:115-135`, which owns the presenter character,
Event, and Event-Part rows; `MNetherFloorEvents.cs:4-25`, which owns Event identity and four exact
part IDs; `MNetherFloorEventParts.cs:4-29`, which owns raw target/parameter and content fields; and
`NetherUpdateEventResponseEntity.cs:4-12`, which returns character, floor, and Code state. Native
design therefore still treats Event/Part IDs as client correlation and keeps the request parameter
shape unchanged. No ticket/spec/CONTEXT deviation was required.

The public-seam RED was job `j-d2vg2g`, `5 failed / 46 passed / 51 total`: four projected
erosion/HP-delta/Gold/key mismatch cases were incorrectly `Applied`, and the assembly-boundary test
observed the linked test copy as `AutoNether.Tests`. After the minimal fixes, focused GREEN job
`j-fqejbx` passed `68/68`, expanded focused job `j-r7q5jc` passed `380/380`, and full-suite job
`j-08wpm7` passed `1157/1157`.

The commitment fix carries projected erosion, HP delta, Nether Gold, and Treasure Keys through
dispatch, popup stages, transaction composition, and reconcile. `NetherEventCommitment.Matches`
now compares those projections together with exact identity, effects, reward/battle evidence, and
partial-death evidence. Reconcile rejects a stale projected state before treating the native update
as Applied; composition rejects the same mismatch before the native update is sent. The test covers
each projected-state divergence while identity and effects remain equal.

The standards fix removed the linked production `Compile Include/Link` source from
`AutoNether.Tests/AutoNether.Tests.csproj`, added the approved in-repository `ProjectReference` to
`AutoNether/AutoNether.csproj`, and uses `InternalsVisibleTo` for the public-seam tests. The boundary
test now asserts that `NetherEventProductionEvidenceBinding` is loaded from the `AutoNether`
assembly. Product-isolation job `j-9xopoe` returned `product isolation: PASS` and
`PRODUCT_ISOLATION_EXIT=0`.

Final verification results:

```text
j-fqejbx  focused Docker GREEN: 68 passed / 68 total
j-r7q5jc  expanded focused Docker GREEN: 380 passed / 380 total
j-08wpm7  full Docker suite GREEN: 1157 passed / 1157 total
j-nhee2f  Release build GREEN: 0 Warning(s), 0 Error(s)
j-ilunpc  Release audit GREEN: PASS, exit 0
j-9xopoe  product isolation GREEN: PASS, exit 0
j-frpn0d  Docker git diff --check GREEN: exit 0; no whitespace errors
```

The exact Docker command forms used for the final test/build/audit checks were:

```text
MSYS_NO_PATHCONV=1 docker run --rm -e ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --logger "console;verbosity=minimal"'
MSYS_NO_PATHCONV=1 docker run --rm -e ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'export NUGET_PACKAGES=/tmp/nuget; dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --verbosity minimal'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'apt-get update -qq; apt-get install -y -qq binutils >/dev/null; sh scripts/verify-release.sh /src/release/Release/net6.0/AutoNether.dll'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'git diff --check'
```

All claims in this addendum are tied to the fresh native runs above; the game tree was never
written, remote issue state was untouched, and the final task history remains one amendable commit
on top of the fixed point.

## Partial-death downstream repair closure (2026-08-18)

This repair was driven by the production public-seam repro: the immutable Treasure partial-death
proof and projected HP survived composition, but downstream projection calibration and
`NetherActionReconcilePolicy.HasExactHpDelta` rejected the authoritative response
`[1,0,inactive]; [2,300,active]`. The RED jobs were fresh against the RO game evidence: `j-n92867`
recorded `3 failed / 128 passed / 131 total` across controller/reconcile/composer seams, and
`j-dj6ldi` recorded the minimized calibration failure `1 failed / 0 passed / 1 total` with
`hp-projection-drift:1`. Clean GREEN `j-xn1k1b` passed `137/137`, including partial, full-party,
unauthorized, projected-state mismatch, composer, controller, and calibration cases.

The minimal fix does four things: carries Event/Part/floor/node/commitment through the parent
composer; authorizes active-to-inactive only when the exact immutable partial-death proof is valid;
requires an active survivor and HP zero for each permitted death; and lets projection calibration
ignore only zero-or-below projected members under that same authorization while rejecting full-party
death. Ordinary Event damage and unauthorized transitions remain all-active/fail-closed. No
displayed power or inferred character whitelist is used; the existing exact character IDs and
normalized active state are reconciled against the authoritative response.

The final fresh native Docker/RO decomp is `j-d221gi` using
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`; it returned
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. Game hashes are:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

Artifact hashes: `NetherCharacterEntity.cs`
`22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`,
`NetherUpdateEventResponseEntity.cs`
`30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`,
`NetherApiDataStore.cs` `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`,
`NetherEventResultModel.cs` `f79123d206000bfc369af7bad485fc22b60fd749048c36d6d49a0f504ab52f83`, and
`NetherEventPopupController.cs`
`a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf`. Authoritative output
anchors are `j-d221gi/output.log:11-17` (`id`, `user_id`, `m_character_id`,
`current_hp_ratio`; no native `IsAlive`), `:28-32` (update response returns character/floor/Code
rows), `:411-415` (native update method takes floor level/index, selected option, and Code-change
ID), `:417-423` (EventResult update flow), and `:397-403` (popup presenter/Event/Event-Part
fields). Native design therefore preserves client correlation for Event/Part IDs and the existing
native parameter seam; no ticket/spec/CONTEXT conflict was found and no native battle/death
semantic mapping was invented.

Current validation after the clean GREEN is `j-7r5idl` expanded focused `313/313`, `j-b0tcg9`
full suite `1166/1166`, `j-v08z84` Release build `0 Warning(s), 0 Error(s)`, and `j-fpmbq6`
product-isolation PASS through the production `ProjectReference` with no linked binder source.

## Post-repair Docker revalidation (2026-08-18)

This continuation made no further implementation change. A new fresh native decompile was run first
in job `j-hvd4j4` with the game mounted exactly as
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`; it returned
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. The immutable native input hashes were:

```text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
```

Fresh diffable artifacts were hashed as follows:

```text
NetherCharacterEntity.cs          22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f
NetherUpdateEventResponseEntity.cs 30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa
NetherApiDataStore.cs              b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071
NetherEventResultModel.cs           f79123d206000bfc369af7bad485fc22b60fd749048c36d6d49a0f504ab52f83
NetherEventPopupController.cs       a8b4cc6079d6b22229107ec4fe67d2adfaad4f24326ae8e113a9a7c16bc8ccbf
```

Authoritative fresh anchors are `j-hvd4j4/output.log:15-18` (`id`, `m_character_id`, and
`current_hp_ratio`, with no native `IsAlive`), `:31-33` (update response character/floor/Code
state), `:399-404` (popup `_mCharacterId`, Event, and Event-Part fields), and `:411-424`
(native floor/index/selected-option/Code request and EventResult update state machines). The
native boundary therefore remains unchanged: Event/Part IDs are client correlation only, and the
partial-death repair reconciles the authoritative HP-ratio rows without fabricating request fields
or hidden battle semantics. No native/spec/CONTEXT conflict was found.

The repair RED evidence remains `j-n92867` (`3 failed / 128 passed / 131 total`) plus minimized
`j-dj6ldi` (`1 failed / 0 passed / 1 total`); this revalidation's clean public-seam GREEN results
are focused `j-rh669t` (`137/137`), expanded `j-ehq5wo` (`313/313`), and full `j-f5znai`
(`1166/1166`). Release `j-mets3c` succeeded with `0 Warning(s), 0 Error(s)`; product isolation
`j-x71jbk` returned `PRODUCT_ISOLATION_PASS=1`; Docker diff checking is the next recorded amend
gate. All commands used `docker run --rm`, and `/game` was read-only.
## Budget, malformed-target, and typed ResearchRateOverwrite repair closure (2026-08-18)
This repair closed the final procurement, malformed Event-target, and ResearchRateOverwrite
evidence gaps in the same working tree before amending the single task commit. The native game
directory was never written. The final fresh RO Cpp2IL run was job j-bbli4l:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; /tmp/Cpp2IL --game-path /game --output-to /tmp/decomp --output-as diffable-cs >/tmp/cpp2il.log 2>&1; status=$?; echo CPP2IL_EXIT=$status; test "$status" = 0; for spec in "MNetherFloorEventParts.cs|1|35" "MNetherCodes.cs|1|35" "NetherResultResponseEntity.cs|1|16" "NetherPointData.cs|1|30" "NetherApiDataStore.cs|284|291" "NetherUpdateEventResponseEntity.cs|1|12"; do name=${spec%%|*}; rest=${spec#*|}; start=${rest%%|*}; end=${rest##*|}; file=$(find /tmp/decomp/DiffableCs -type f -name "$name" | head -n 1); test -n "$file"; echo "=== $name ==="; sha256sum "$file"; nl -ba "$file" | sed -n "${start},${end}p"; done; echo DIFFABLE_EXIT=0'
~~~

It returned CPP2IL_EXIT=0 and DIFFABLE_EXIT=0. The immutable game hashes were:

~~~text
Project.dll       53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300
GameAssembly.dll  573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb
global-metadata   ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5
~~~

Fresh artifact hashes and authoritative anchors are recorded in j-bbli4l/output.log:

- MNetherFloorEventParts.cs: hash 5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128; output lines 19-35 prove the raw target_type_1/select_parameter_1 through target_type_3/select_parameter_3 fields and exact content_type/content_id/amount fields.
- MNetherCodes.cs: hash f321ea53eeb23c9130f8fa4b090c7c9b6f574cca6bbfa9eb8a21dec46c0046f5; output lines 46-71 prove the native Code row ends at effect_type, three effect parameters, asset_id, and power.
- NetherResultResponseEntity.cs: hash 662f993792e1376cfc4051e16082c3f03ecbb9b91a0da3f47ddeefbef312e0a2; output lines 82-90 expose result rank/rewards, Nether point, and nether_code_points only.
- NetherPointData.cs: hash 7fb76a026bbae620a66474b4b51c07838ae4eafaa65430b1d044e4c494b94c97; output lines 100-126 expose SpherePointRatio and other technology/progression fields, not a selectable research-rate overwrite.
- NetherApiDataStore.cs: hash b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071; output lines 129-133 prove RequestNetherUpdateEventAsync(floorLevel, floorIndex, selectedNumber, changeTargetMNetherCodeId, ct).
- NetherUpdateEventResponseEntity.cs: hash 30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa; output lines 142-146 expose only character, floor, and Code response rows.

Native-first conclusion: the current native design proves the raw Event-part pair fields but does
not assign semantics to target_type_1=0/select_parameter_1=999; the production mapper therefore
keeps that option unknown and fail-closed instead of dropping the target. Native MNetherCodes and
the result/technology rows do not prove a selectable research-rate mechanic, so production leaves
ResearchRateOverwrite NotPresent and the typed assembler accepts only a separately authoritative
typed Known value. No ticket/spec/CONTEXT deviation was required, and no displayed power or hidden
native field was inferred.

The public-seam RED/GREEN loop was:

- j-pvy9dv: RESTORE_EXIT=0, then 2 failed / 0 passed / 2 total. The failures were the malformed target being selected as option 1 and the typed ResearchRateOverwrite remaining 0 instead of the future typed value 15.
- j-wbk5zy: 1 failed / 0 passed / 1 total. The exact branch-local Gold budget did not yet produce a production commitment.
- j-m4o95q: 5 passed / 5 total after the minimal malformed-target, assembler, and Gold-budget fixes.
- j-jzex6q: 1 passed / 1 total for the final compound Gold+Key production commitment seam.

Every RED/GREEN/fix claim above was checked against the fresh immutable native hashes and anchors
from j-bbli4l (the same RO game inputs were also freshly decompiled in the pre-fix cycle
j-yf451h). The final production path now carries route-owned procurement minima through
pre-entry capture, option projection, production Event binding, immutable commitment, and the
pre-payment budget gate; malformed target rows remain option-local; and the typed
ResearchRateOverwrite field is copied into hard eligibility while current native evidence remains
fail-closed.

Final Docker validation after the last code/test edit:

~~~text
j-1cnj9d focused production repair suite: 115 passed / 115 total
j-7hd5ac expanded focused suite: 293 passed / 293 total
j-5627oo full suite: 1173 passed / 1173 total
j-cp03on Release build: 0 Warning(s), 0 Error(s)
j-dfb5zz Release audit: release audit: PASS
j-kqomi3 product isolation: PRODUCT_ISOLATION_PASS=1
~~~

The final focused, expanded, full, and Release commands all used the repository bind plus the
read-only game bind and the established read-only NuGet cache:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -e ABYSS_GAME_DIR=/game -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -e ABYSS_GAME_DIR=/game -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; export NUGET_PACKAGES=/tmp/nuget; dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --nologo --verbosity minimal'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; grep -Fq "ProjectReference Include=\"..\\AutoNether\\AutoNether.csproj\"" AutoNether.Tests/AutoNether.Tests.csproj; ! grep -Fq "NetherEventProductionEvidenceBinding.cs" AutoNether.Tests/AutoNether.Tests.csproj; echo PRODUCT_ISOLATION_PASS=1'
~~~

The final Docker diff check was run as git diff --check after the documentation update; it returned
exit 0 with no whitespace errors. The game tree remained read-only in every Docker invocation,
remote Issues were untouched, and this task remains one amendable commit on top of the fixed point.
### Historical pre-repair Docker gate pin (not final) (2026-08-18)

The historical pre-repair task-group snapshot was explicitly pinned as
`1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, with parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The current closing Docker logs are exact and
reproducible: clean-restore RED `C:/Users/Eden/.fastctx/jobs/j-urwq7m/output.log` records
NU1101 for `BepInEx.Unity.IL2CPP` and `BepInEx.PluginInfoProps` from nuget.org-only restore;
`--source` flags. Threshold-focused `j-l9m0j6` is 111/111, expanded focused `j-529vvn` is
230/230, full `j-nyu1bj` is 1186/1186, Release `j-3ulak2` is `Build succeeded` with 0 Warning(s) and
0 Error(s), and evidence audit `j-ju3ij7` records `EVIDENCE_AUDIT_PASS=1`,
`DIFF_CHECK_EXIT=0`, `HEAD=1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, and the exact parent.
Every log was produced by `docker run --rm` with
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`; no gate is
deferred to an unrecorded command.

### Current closing repair evidence (2026-08-18)

Fresh native evidence is Docker job `j-53m4lb` (`CPP2IL_EXIT=0`, `DIFFABLE_EXIT=0`, log
`C:/Users/Eden/.fastctx/jobs/j-53m4lb/output.log`) with the game mounted only as
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`. The immutable
game hashes are `Project.dll` `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll` `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat` `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.

Fresh artifact hashes/anchors: `MNetherFloorEventParts.cs`
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`, output lines 13-29,
proves raw target/parameter pairs and content fields; `MNetherFloorBattles.cs`
`7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`, lines 7-15, exposes only
raw battle `type`, stage, and drop ratio; `MNetherFloorEvents.cs`
`aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`, lines 7-25, exposes Event
identity and four part IDs; and `NetherApiDataStore.cs`
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`, lines 287-288, proves the
native update seam is floor level/index/selected number/Code-change ID. Native evidence does not
prove target type 7's nonzero parameter as a Code ID or raw battle semantic tiers; production
remains exact and fail-closed.

The public-seam RED/GREEN records are `j-y33pp2` (6 failed / 119 passed / 125 total before
minima propagation and mapping fixes), decoder-focused base-commit RED `j-5l8cza` (2 failed /
0 passed / 2 total: raw type 7 was reported known and the malformed target lacked the new
UnknownMasterData classification), and `j-voopjz` GREEN (196/196). These claims are supported by
the fresh native hashes and anchors above. The route E2E test proves positive Gold/Key minima are
bound before pre-entry semantics and unsafe spending is rejected; popup, composer, reconcile,
malformed-target, content-160, and typed ResearchRateOverwrite public seams remain covered.

### Closing P1 repair: route-owned procurement durability and exact ordinary HP (2026-08-18)

The final repair keeps route-owned procurement state in
`NetherRouteOwnedEventProcurementProducer`, separate from the one-shot pending pre-entry map.
`NetherRuntimeBridge` now captures that durable producer for route safety, commits exact
option-projection budgets after a successful pre-entry capture, and clears it only at runtime
registration teardown. The public production E2E no longer uses `RouteSafetyOverride`: it seeds the
route-owned producer, captures it twice, and proves positive Gold/Key minima reach the option-local
pre-entry budget gate before unsafe Event spending can be selected.

Ordinary Event HP reconciliation now requires exact clamped projected HP and an unchanged active
identity for every character that was living before the Event. Lethal ordinary projections remain
fail-closed. The explicitly authorized Treasure/HP-paid partial-death path still accepts only exact
dead/survivor states with at least one active survivor; full-party death and unauthorized death stay
rejected. `NetherActionProjectionCalibration` uses the same exact per-character/active-set rule
instead of a minimum-HP comparison.

Fresh native evidence for this cycle was collected only from the read-only game mount
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`:

- `j-qcpyn6/output.log:1-3,4-37,39-58,60-89,91-100` returned `CPP2IL_EXIT=0` and
  `DIFFABLE_EXIT=0`; `MNetherFloorEventParts.cs` hash
  `5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` anchors lines 17-33
  (raw target/parameter and content fields), `MNetherFloorBattles.cs` hash
  `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` anchors lines 46-54
  (raw battle type/stage only), and `NetherApiDataStore.cs` hash
  `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` anchors lines 95-96
  (floor/index/selected-number/Code native Event request).
- `j-8924sc/output.log:1-3,4-18,20-32,34-43` returned `CPP2IL_EXIT=0` and
  `DIFFABLE_EXIT=0`; `NetherCharacterEntity.cs` hash
  `22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f` anchors lines 10-14
  (`m_character_id` and `current_hp_ratio`), and `NetherUpdateEventResponseEntity.cs` hash
  `30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa` anchors lines 24-28
  (`t_nether_characters`, floor, and Code response rows). The fresh game hashes were
  `Project.dll` `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
  `GameAssembly.dll` `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
  `global-metadata.dat` `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.

The public-seam RED command (fresh native evidence above, before the code fix) reported 3 failed,
50 passed, 53 total: the route-owned budget was missing after the safety handoff, ordinary partial
party damage was Applied, and the calibration accepted a minimum-only HP result. The GREEN repair
loop is `j-hqq7hz` (53/53 before the lethal ordinary-death tightening), final focused GREEN is
`j-n05h3m` (53/53), expanded focused is `j-ulroy1` (313/313), and full Docker coverage is
`j-6h5nv7` (1183/1183). Release build `j-qhiwpe` reports `0 Warning(s), 0 Error(s)` and product
isolation `j-anvi44` reports `PRODUCT_ISOLATION_PASS=1`. All these commands used `docker run --rm`,
the established read-only NuGet cache where applicable, and the game read-only mount. No native
semantic field, displayed power, hidden budget, or character-target assumption was inferred; the
native evidence only determines which repository-owned commitment and response fields may be used.

### Documentation integrity addendum (2026-08-18)

This historical pre-repair record and the ticket evidence previously referenced the same reviewed task-group commit:
`1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The closing Docker results above are the recorded results,
not a placeholder for a future `git rev-parse` invocation; the `/game` mount was read-only and no
remote issue state was touched.

The same `j-schqct` log independently records the pre-amend `HEAD=1e1e7a0d6f0215910e9b7d1254c7771d217326ea`
and parent, and its isolation/diff check is the recorded current result rather than a deferred
`git rev-parse` or build command.

### Final-gate route-owned procurement repair (2026-08-18)

The repair started from clean `HEAD=71408c42573ed797b8c82446c8b3d77f627991f3`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The route-owned producer now accepts only exact,
valid option keys and budgets, the production route plan carries proven option-projection
commitments, `NetherAutoClimbController.PlanRoute` publishes that plan-owned map, and the real
`NetherRuntimeBridge` promotes non-empty exact handoffs while retaining them across empty/repeated
captures. Popup commitments are also promoted only after immutable Event binding. The old
ScriptedRuntimeBridge/manual-seed E2E was removed; the new public test instantiates the real
production bridge, captures positive Gold/Key minima twice, and proves the option-local pre-entry
budget gate rejects unsafe spending.

Fresh RED was Docker job `j-vii6om` (1 failed / 0 passed / 1 total): the real production bridge's
bound route map was empty at `CaptureRouteOwnedEventProcurementCommitments`; its failure is at
`output.log:12-27`. The RED loop used:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -e; export NUGET_PACKAGES=/tmp/nuget; export ABYSS_GAME_DIR=/game; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --filter FullyQualifiedName~Real_runtime_bridge_promotes_bound_route_budget_into_durable_capture_without_a_route_override --logger "console;verbosity=normal"'
~~~

Fresh post-fix native evidence is Docker job `j-9lqbic` (`output.log:1-87`,
`CPP2IL_EXIT=0`, `DIFFABLE_EXIT=0`) with the game mounted only as
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`. The exact game
hashes are `Project.dll` `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll` `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat` `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`
(`output.log:2-4`). Decompiled artifact hashes are `MNetherFloorEventParts.cs`
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`MNetherFloorBattles.cs` `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`,
`MNetherFloorEvents.cs` `aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`, and
`NetherApiDataStore.cs` `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`
(`output.log:32-35`). Authoritative anchors are raw Event target/parameter/content fields at
`output.log:37-53`, raw battle `type`/stage/drop ratio at `54-63`, Event/part identity at
`64-83`, and the native update seam `RequestNetherUpdateEventAsync(floorLevel, floorIndex,
selectedNumber, changeTargetMNetherCodeId, ct)` at `84-86`. Native therefore proves only the
raw rows and floor/index/option/Code request; the positive procurement proof remains repository-
owned exact commitment state, with no displayed-power or hidden native budget inference.

Post-fix GREEN was `j-dyqez9` (real bridge + option-local unsafe-spend E2E, 1/1), `j-xq26l5`
(binding/coordinator focused group, 39/39), and `j-am5nzl` (expanded repair group, 109/109).
The subsequent full Docker suite `j-vtk92z` passed 1184/1184; Release build `j-xw2sb3` passed
with 0 Warning(s)/0 Error(s); product isolation `j-6r4fg3` returned `PRODUCT_ISOLATION_PASS=1`.
All used `docker run --rm`, the read-only game bind, and the established read-only NuGet cache
where compilation was required. The exact full/build/isolation commands were:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -e; export NUGET_PACKAGES=/tmp/nuget; export ABYSS_GAME_DIR=/game; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --nologo --logger "console;verbosity=minimal"'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=bind,src=/c/Users/Eden/AppData/Local/Temp/opencode/nuget/packages,dst=/tmp/nuget,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; export NUGET_PACKAGES=/tmp/nuget; export ABYSS_GAME_DIR=/game; dotnet build AutoNether/AutoNether.csproj --no-restore --configuration Release --nologo --verbosity minimal'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; grep -Fq "ProjectReference Include=\"..\\AutoNether\\AutoNether.csproj\"" AutoNether.Tests/AutoNether.Tests.csproj; ! grep -Fq "NetherEventProductionEvidenceBinding.cs" AutoNether.Tests/AutoNether.Tests.csproj; echo PRODUCT_ISOLATION_PASS=1'
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/workspace,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /workspace mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'git diff --check'
~~

The historical pre-repair evidence snapshot (not final) was pinned to reviewed task-group commit `1e1e7a0d6f0215910e9b7d1254c7771d217326ea`
with parent `5f3de38572d5526e73e8576ffe505669c1c8dbc3`; the current restore and gate logs above
are the reproducible records for that snapshot. The in-place amend necessarily changes the Git
object ID because this durable note is part of the amended tree; the final post-amend SHA is
reported from the closing Docker audit.

### Current canonical-restore closing record (2026-08-18)

`NuGet.config` is repository-owned and clears inherited sources before adding `nuget.org`,
`https://nuget.bepinex.dev/v3/index.json`, and `https://nuget.samboy.dev/v3/index.json`. The exact
clean command documented in the migration plan is:

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; export ABYSS_GAME_DIR=/game; export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --nologo --logger "console;verbosity=minimal"'
~~~

That exact command passed 1184/1184 in `j-g4xeyq/output.log:1-9`; its preceding RED is
`j-urwq7m/output.log:1-6`, which proves the failure was the missing repository feed, not a
test bypass. Fresh RO native evidence is `j-53m4lb/output.log:1-150`: game hashes are
`Project.dll=53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll=573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat=ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`;
Cpp2IL artifact hashes are at lines 30-33, raw Event target/parameter/content fields at
lines 47-63, raw battle type/stage/drop fields at lines 79-84, Event/part identity at
lines 97-115, and native update parameters at lines 126-144. The run ends with
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. No native semantic or hidden procurement field was
inferred; repository-owned route commitments remain exact and fail-closed.

### P1 procurement-threshold priority repair (2026-08-18)

The production Event seam now treats an exact committed procurement threshold as a proven
priority: the option must start below its committed Gold or key minimum and its own projected
resource reward must reach that minimum. This threshold priority is 700, above ordinary direct
Code Offer (600) and uncommitted Gold (400), while the existing rank-five reward and battle
priorities remain authoritative. It does not infer a threshold from displayed power or hidden
future inventory.

The public production RED is `j-kmf1l2/output.log:1-52`: both Gold and key threshold tests
selected option 2 (the direct Code Offer) instead of option 1. Fresh native RO evidence for
the RED/GREEN cycle is `j-86iu89/output.log:1-150`, with `CPP2IL_EXIT=0` and
`DIFFABLE_EXIT=0`; game hashes are `Project.dll=53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll=573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat=ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
Decompiled artifact hashes are at lines 30-33; raw Event target/parameter/content fields are
at lines 47-63, raw battle type/stage/drop fields at 79-84, Event/part identity at 97-115,
and the native update seam at 126-144. The GREEN `j-1ro4y6/output.log:1-19` passed both
production-seam tests. Historical full validation is recorded by `j-l9m0j6` (111/111),
`j-529vvn` (230/230), `j-nyu1bj` (1186/1186), `j-3ulak2` (Release 0/0), and
`j-ju3ij7` (read-only game evidence audit and diff check). All use `docker run --rm` and
the exact read-only game mount. The historical pre-amend commit pin (not final) for this repair cycle was
`1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`.

### Current final-repair audit before the amend (2026-08-18)

The prior SHA pins in this note are historical pre-repair records, not the final task-group
commit. The current implementation tree is `beb8824604298da985965bad332b24ac9d7845c7` with
parent `5f3de38572d5526e73e8576ffe505669c1c8dbc3`; it is the exact tree that will be amended
in place. The post-amend Docker audit will print the actual content-addressed `HEAD`, `HEAD^`,
tree, and clean status. Because this note is itself part of the amended tree, embedding the
SHA of that same final commit would change the SHA; therefore no pre-amend or historical SHA is
labelled as final here. The post-amend audit log and final handoff are authoritative for the
exact final object.

Fresh native-first evidence for this repair is Docker job `j-cg7xis`
(`C:/Users/Eden/.fastctx/jobs/j-cg7xis/output.log:1-161`), with
`GAME_MOUNT_READONLY=1`, `CPP2IL_EXIT=0`, and `DIFFABLE_EXIT=0`. Immutable game hashes are:
`Project.dll=53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll=573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat=ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
Native artifact hashes/anchors are `MItems.cs=e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27`
lines 7-19 (`rarity` is raw `int`), `ContentRarity.cs=a2dc61f2a794ea73128d85603853322a99e2e65eb884a36412388de98b532971`
lines 3-11 (`0..5`), `DropRarityLevel.cs=2d5d9ab816deeb47f639939d6303dcfb0a4d8cdfc24481f33120a76db5997f69`
lines 3-11 (`0..5`), `MNetherFloorEventParts.cs=5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`
lines 13-29 (raw target/parameter/content fields), `MNetherFloorBattles.cs=7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`
lines 7-15 (raw battle type/stage/drop fields), `MNetherFloorEvents.cs=aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`
lines 7-25 (Event and four Part IDs), and `NetherApiDataStore.cs=b970836a0c0457174405d227b3e100a41dcf3a7a3b8a6abe1d6fe036a18071`
lines 287-288 (floor/index/selected-number/Code native update parameters). These anchors prove
the closed rarity domain and raw Event/update boundary; they do not invent a hidden procurement
budget or semantic battle tier.

The public-seam RED/GREEN cycles were:

- `j-x4qiyb`: 3 failed / 1 passed / 4 total when raw rarity `999` was temporarily allowed by
  the old normalization behavior; restoring the closed native range made the production
  pre-entry, runtime-capture, visible-mapper, and binder tests GREEN.
- `j-xlvwrk`: 2 failed / 0 passed / 2 total when the route-owned visible-branch producer was
  temporarily disabled; this reproduced missing Gold/Key commitments in both the coordinator
  and controller seams. The restored producer GREEN is included in `j-b6bdes`.
- `j-b6bdes`: focused public seam `128/128`, including negative resource IDs, overflow
  fail-closed projection, raw target type 7/content 160 locality, out-of-domain rarity,
  route-owned Gold/Key production, and unsafe-spend rejection.

Final pre-amend Docker gates are `j-g23akv` expanded focused `337/337`, `j-n7g4ih` full
`1201/1201`, `j-dbw8t4` Release build `0 Warning(s), 0 Error(s)`, and `j-1lixdk` product
isolation `PRODUCT_ISOLATION_PASS=1` plus `DIFF_CHECK_EXIT=0`. Every gate used `docker run --rm`
with `/c/Users/Eden/PixelAbyssX/dotabyss_x_cl` mounted at `/game` with `readonly`; no game or
remote issue state was changed.

### Same-branch route identity and invalidation repair (2026-08-18; pre-amend)

The current repair tree is `d38ed66c7bd247129266d01b53e0daa85f3a90a1`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`, tree
`4aa969ea9f6becde6afa9001411ad27b6e8cff19`; this is a pre-amend identity, not the final
task-group SHA. `NetherRoutePlan` now carries the selected path and a snapshot-bound branch
fingerprint derived from the existing horizon `Steps`; the producer accepts interactive and
visible procurement proof only on that exact path. `BeginRouteReplan` and every authoritative
floor-scene confirmation retire the prior proof, while changed snapshot fingerprints invalidate
the durable map before new Event semantics or payment.

The public RED was run before the production change with the exact Docker command below and
failed both new route-coordinator tests (2 failed / 0 passed / 2 total): the alternate safe
branch and post-event stale commitment were both incorrectly retained. The command used the
read-only game mount and the repository NuGet volume:

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=volume,src=abyss-autonether-nuget,dst=/tmp/nuget -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'export NUGET_PACKAGES=/tmp/nuget; export ABYSS_GAME_DIR=/game; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --filter "FullyQualifiedName~Production_route_plan_rejects_procurement_proof_from_a_safe_alternate_branch|FullyQualifiedName~Production_route_plan_does_not_reuse_procurement_proof_after_authoritative_snapshot_changes" --logger "console;verbosity=normal"'
~~~

Fresh native RO evidence for this RED/GREEN/fix cycle is `j-ard4dj/output.log:1-96`, with
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. The immutable game hashes are
`Project.dll=53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll=573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat=ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
The fresh decompiled artifact hashes are `MItems.cs=e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27`,
`MNetherFloorEventParts.cs=5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`MNetherFloorBattles.cs=7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`,
`MNetherFloorEvents.cs=aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`, and
`NetherApiDataStore.cs=b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`.
Anchors are `j-ard4dj` lines 4-19 (raw MItems rarity), 4-29 (raw Event target/parameter/content),
4-15 (raw battle fields), 4-25 (Event plus four Part IDs), and 284-288 (floor/index/selected
number/Code-only native update). Native evidence exposes no alternate branch commitment or
hidden budget field and no Event/Part request parameters; the implementation therefore uses the
repository's existing selected horizon path and preserves native request semantics.

GREEN evidence: focused public route tests plus the production alternate-branch E2E and bridge
invalidation test passed `j-0t56g0/output.log:1-21` (4/4); the expanded production seam set passed
`j-e6tp0m/output.log:1-9` (120/120); the full suite passed `j-7lbau3/output.log:1-9`
(1205/1205). A clean Release restore/test passed `j-54geyq/output.log:1-12` (1205/1205),
and the Release build passed `j-bt997x/output.log` with `0 Warning(s), 0 Error(s)`. Product
isolation passed `j-xzpkyj/output.log:1` and the Docker diff check passed `j-xzpkyj/output.log`
with `DIFF_CHECK_EXIT=0`. All commands were `docker run --rm`; `/game` was read-only and no
remote issue state was changed.

The final amend will change the content-addressed SHA because this evidence file is in the tree.
After the last amend, the authoritative `HEAD`, parent, tree, Docker gate IDs, and native job ID
will be written to Git note ref `refs/notes/logic-overhaul-evidence`; that note is the durable
post-amend identity record and will be pushed separately with the branch. No pre-amend SHA above
is labelled final.

### Procurement invalidation and permitted-source repair (2026-08-18; pre-amend)

This repair starts from clean pre-amend `HEAD=b837c5ce1822b3b05990ff34df62ad75a974877e`,
parent `5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The bridge now compares its previous
authoritative snapshot key before accepting an incoming key and clears pending, committed, and
route-owned procurement state before the new capture can merge it. The route producer now accepts
only the spec-proven Gold thresholds 200/300/500 from a known `UsesNetherGold` shop row with a
positive quantity, and a rank-five key objective requires the exact equipment-bag `ItemType=91`.
Unsupported shop costs and non-equipment rank-five rows remain option-local and produce no budget.

Fresh public-seam RED `j-xyz67j` (Docker output `output.log:1-40`) was 3 failed / 0 passed / 3
total: stale pending procurement survived a changed snapshot, a 150-cost shop row produced a Gold
budget, and a rarity-five non-equipment row produced a key budget. The repair GREEN
`j-3l24iu/output.log:1-9` was 3/3. Focused modified production suites `j-4gqmbu/output.log:1-9`
passed 123/123; expanded focused suites `j-u32ye3/output.log:1-9` passed 189/189; full
pre-amend Docker tests `j-eg7dic/output.log:1-6` passed 1208/1208; Release build
`j-hrbptk/output.log:1-7` passed with 0 Warning(s), 0 Error(s). All commands used
`docker run --rm` with the repository writable only for build outputs, the established NuGet
volume, and `/c/Users/Eden/PixelAbyssX/dotabyss_x_cl` mounted at `/game` read-only.

Fresh native-first evidence for the RED/GREEN/fix cycle is `j-p18rn7/output.log:1-28`, with
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. Immutable game hashes are Project.dll
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, GameAssembly.dll
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and global-metadata.dat
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` (`output.log:1-3`).
The fresh decompiled artifacts are `MNetherFloorEventParts.cs`
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` with raw
`target_type_1`, `select_parameter_1`, `content_type`, and `content_id` anchors at source lines
13, 15, 25, and 27 (`output.log:4-7`), `MNetherFloorBattles.cs`
`7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` with raw type/stage/drop
anchors at source lines 7-15 (`output.log:8-12`), and `NetherApiDataStore.cs`
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` with the native Event
update signature at source lines 287-288 (`output.log:20-22`). A second fresh RO decomp
`j-phv0bh/output.log:1-74` confirms native `MNetherFloorShopContents.consume_content_type`
is a raw field (`output.log:62`) and that native item rarity is raw data; it does not prove a
different permitted purchase threshold or hidden budget. The implementation therefore follows the
existing repository/spec threshold and exact item-type seam, while preserving native request
parameters and fail-closed behavior. No native conflict, displayed power, or hidden procurement
semantics was inferred.

The post-amend Docker audit and Git note are the authoritative final identity record; this
pre-amend SHA is historical for this repair cycle and is not labelled final.

### Positive raw MItems.type overflow repair (2026-08-18; pre-amend)

This closing repair starts from `HEAD=ffa3ef96ba7862456e668195fdea6207b69543a5`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`. The first public-seam RED was `j-lzlkcg`:
the corrected exact-row test failed `1/1` with the reproduced `OverflowException` escaping
`NetherEventProductionEvidenceBinding.FindReward`. The initial attempted RED was deliberately
discarded as a masked reproduction: it appended a second item row, so the dependent-row count
became two and the binder selected the existing valid sibling. The test was corrected to mutate
the sole exact dependent row before the RED was accepted.

Fresh native RO Cpp2IL before the fix is `j-bghfub/output.log:1-52`, and post-fix is
`j-5l2ncz/output.log:1-25`; both returned `CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0` with `/game`
mounted read-only. Both runs recorded the immutable game hashes:
`Project.dll=53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
`GameAssembly.dll=573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat=ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
The authoritative native artifacts are `MItems.cs=e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27`
(`j-bghfub`/`j-5l2ncz` output lines 4-19; source line 11 is raw `long type`) and
`MNetherFloorEventParts.cs=5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`
(`j-bghfub` output lines 4-29; source lines 13-29 are raw target/parameter/content fields).
Native evidence proves the raw `long` transport field but no narrower closed item-type domain;
the implementation therefore narrows only within the proven `Int32` evidence seam and keeps
out-of-domain values option-local unknown/paused. No native or spec deviation was required.

The minimal fix is shared `NetherEventNativeMapping.TryMapItemType`: visible-map item rows,
pre-entry reward evidence, runtime Event/shop/return mappings, and production commitment lookup
all fail closed before any narrowing cast. Public GREEN `j-ofgd9w` passed the new test `1/1`
and the warning-free focused run `j-8dph58` passed `207/207`; expanded focused `j-ie7n3h`
passed `477/477`, full Docker tests `j-1y9bfs` passed `1209/1209`, and the final Release/
isolation/diff container `j-gwj93w` passed Release `0 Warning(s), 0 Error(s)`, release audit,
product isolation, and `DIFF_CHECK_EXIT=0`. The Release artifact SHA was
`2c792ed250d73c8f6c046c138f6eccf3c1f2fe0bb8d5fe50a736da808ce91987`.

The content-addressed amend changes the commit SHA because this evidence file is in the tree.
The post-amend Docker audit and `refs/notes/logic-overhaul-evidence` are the authoritative final
`HEAD`/parent/tree and gate identity; this pre-amend SHA is historical and is not labelled final.
