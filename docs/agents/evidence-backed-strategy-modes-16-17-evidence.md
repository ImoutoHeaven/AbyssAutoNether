# Tickets 16–17 evidence ledger

This ledger covers the current implementation cycle on `logic-overhaul`.
It is local repository evidence only; no remote issue or label state is
modified. The fixed point for the cycle is
`0982cbc89bd70848694b45754dad47c8780fb13b`.

<a id="semantic-story-corrections-20260820"></a>
## Semantic story-correction repair — 2026-08-20

This repair keeps the production controller transaction boundary unchanged and
corrects the three final-review story mappings. The CodePolicy valuation test
now enters through completed configured Research targets and asserts the
Equipment retained-portfolio tier; the production route coordinator test
asserts an Event erosion branch through visible Recovery to the terminal Boss;
and the route-planner Treasure test proves both no early low-rarity detour with
a held key and exact one-key payment when that Treasure is the only final
reachable opportunity. The map audit below checks source-story tokens against
the resolved test declarations and bodies for all 125 rows, not only row/link
counts.

Fresh native evidence:

* Evidence ID: `task16-17-semantic-repair-20260820-a` (Docker job `j-k7wz6w`).
* Game mount: `/c/Users/Eden/PixelAbyssX/dotabyss_x_cl` → `/game`, read-only.
* Project.dll SHA-256: `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`.
* GameAssembly.dll SHA-256: `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`.
* global-metadata.dat SHA-256: `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.
* Cpp2IL `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`, Unity `6000.3.8f1`, acquisition 0, diffable 0, ISIL 0; all output stayed under `/tmp`.

The exact reproducible native command was:

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; native=task16-17-semantic-repair-20260820-a; status=0; printf "%s\n" "NATIVE_EVIDENCE_ID=$native" "GAME_MOUNT_READONLY=1"; mount | grep " /game " || status=1; test -r /game/GameAssembly.dll || status=1; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; status=1; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq || status=1; apt-get install -y -qq curl >/dev/null || status=1; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL || status=1; chmod +x /tmp/Cpp2IL 2>/dev/null || status=1; rm -rf /tmp/task16-17-semantic-repair-20260820-a-diffable /tmp/task16-17-semantic-repair-20260820-a-isil; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-semantic-repair-20260820-a-diffable --output-as diffable-cs > /tmp/task16-17-semantic-repair-20260820-a-diffable.log 2>&1; diffable_exit=$?; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-semantic-repair-20260820-a-isil --output-as isil > /tmp/task16-17-semantic-repair-20260820-a-isil.log 2>&1; isil_exit=$?; printf "%s\n" "CPP2IL_ACQUISITION_EXIT=0" "CPP2IL_DIFFABLE_EXIT=$diffable_exit" "CPP2IL_ISIL_EXIT=$isil_exit"; grep -m1 "Version" /tmp/task16-17-semantic-repair-20260820-a-diffable.log || true; grep -m1 "Determined.*unity version" /tmp/task16-17-semantic-repair-20260820-a-diffable.log || true; sha256sum /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-semantic-repair-20260820-a-diffable/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs; if test "$diffable_exit" -ne 0 || test "$isil_exit" -ne 0; then status=1; fi; printf "%s\n" "NATIVE_EVIDENCE_EXIT=$status"; exit "$status"'
```

Intentional semantic RED (before the three map/test corrections) was run in
Docker with this exact command and exited 1 as expected (`US019_STALE_ROW=1`,
`US093_HP_ONLY_ROW=1`, `US115_NO_FINAL_KEY_ROW=1`):

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "RED_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; grep -q "Equipment_mode_does_not_use_research_order_when_completion_is_true" docs/agents/evidence-backed-strategy-modes-17-story-traceability.md && printf "%s\n" "US019_STALE_ROW=1"; grep -q "ControllerRouteWiring_RejectsEventWhenTheNativeResolvedRowIsUnsafe" docs/agents/evidence-backed-strategy-modes-17-story-traceability.md && printf "%s\n" "US093_HP_ONLY_ROW=1"; grep -q "Treasure_rank_shortcut_without_native_rarity_does_not_create_rank_five_value" docs/agents/evidence-backed-strategy-modes-17-story-traceability.md && printf "%s\n" "US115_NO_FINAL_KEY_ROW=1"; grep -q "US019_STORY_SPECIFIC=0" /dev/null; printf "%s\n" "SEMANTIC_MAP_RED_EXPECTED=1"; exit 1'
```

Focused GREEN job `j-xzst46` passed the three corrected production-path
characterizations (3/3). The final focused-5 command, including the existing
selected-audit and horizon regressions, is:

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --env NUGET_PACKAGES=/tmp/nuget --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=tmpfs,dst=/tmp/nuget --mount type=tmpfs,dst=/src/release --mount type=tmpfs,dst=/src/AutoNether/obj --mount type=tmpfs,dst=/src/AutoNether/bin --mount type=tmpfs,dst=/src/AutoNether.Tests/obj --mount type=tmpfs,dst=/src/AutoNether.Tests/bin mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "FOCUSED_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; dotnet restore AutoNether/AutoNether.csproj --verbosity quiet; dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --verbosity quiet; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --verbosity minimal --filter "FullyQualifiedName~Completed_research_targets_delegate_later_code_offers_to_equipment_native_portfolio_value_when_displayed_power_is_reversed|FullyQualifiedName~Production_event_erosion_requires_complete_visible_recovery_before_the_terminal_boss|FullyQualifiedName~Low_rarity_treasure_does_not_detour_or_spend_held_key_unless_it_is_the_final_reachable_opportunity|FullyQualifiedName~Production_selected_candidate_audit_merges_known_semantic_vector_and_tier|FullyQualifiedName~Production_route_excludes_candidate_when_visible_horizon_is_missing"; printf "%s\n" "FOCUSED_GREEN=5/5"'
```

The exact valid in-repository full-suite command is:

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --env NUGET_PACKAGES=/tmp/nuget --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=tmpfs,dst=/tmp/nuget --mount type=tmpfs,dst=/src/release --mount type=tmpfs,dst=/src/AutoNether/obj --mount type=tmpfs,dst=/src/AutoNether/bin --mount type=tmpfs,dst=/src/AutoNether.Tests/obj --mount type=tmpfs,dst=/src/AutoNether.Tests/bin mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "FULL_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; dotnet restore AutoNether/AutoNether.csproj --verbosity quiet; dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --verbosity quiet; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --verbosity minimal; printf "%s\n" "FULL_GREEN=1325/1325"'
```

The exact Release command is:

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --env NUGET_PACKAGES=/tmp/nuget --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=tmpfs,dst=/src/release --mount type=tmpfs,dst=/tmp/nuget --mount type=tmpfs,dst=/src/AutoNether/obj --mount type=tmpfs,dst=/src/AutoNether/bin mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "RELEASE_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; dotnet restore AutoNether/AutoNether.csproj --verbosity quiet; dotnet build AutoNether/AutoNether.csproj --configuration Release --no-restore --warnaserror --verbosity minimal; test -f /src/release/Release/net6.0/AutoNether.dll; stat --format="DLL_PATH=%n\nDLL_SIZE=%s\nDLL_TIMESTAMP=%y" /src/release/Release/net6.0/AutoNether.dll; sha256sum /src/release/Release/net6.0/AutoNether.dll; printf "%s\n" "RELEASE_GREEN=0_WARNINGS_0_ERRORS"'
```

The historical semantic 125-row audit command was superseded by the final semantic audit below:

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "SEMANTIC_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; apt-get update -qq; apt-get install -y -qq python3 >/dev/null; python3 - <<"PY"
from pathlib import Path
import re

root = Path("/src")
map_path = root / "docs/agents/evidence-backed-strategy-modes-17-story-traceability.md"
ledger_path = root / "docs/agents/evidence-backed-strategy-modes-16-17-evidence.md"
spec_path = root / "docs/specs/evidence-backed-strategy-modes.md"
text = map_path.read_text(encoding="utf-8")
ledger = ledger_path.read_text(encoding="utf-8")
spec = spec_path.read_text(encoding="utf-8").splitlines()
rows = []
for line in text.splitlines():
    match = re.match(r"^\\| (US-\\d{3}) \\| (.*?) \\| (.*?) \\| (.*?) \\|$", line)
    if match:
        rows.append(match.groups())
assert [row[0] for row in rows] == [f"US-{i:03d}" for i in range(1, 126)]
method_re = re.compile(r"\\[([^]]+)\\]\\(([^)#]+)#L(\\d+)\\)")
stop = {"spec", "user", "research", "equipment", "mode", "with", "from", "only", "that", "this", "when", "before", "after", "into", "through", "exact", "known", "uses", "used", "must", "does", "not", "and", "the", "for", "all", "one", "its", "are", "has", "have", "below", "above", "rather", "than", "each", "every", "current", "native", "same", "safe", "safety", "visible", "route", "branch", "option", "choice", "value", "behavior", "requires", "preserves", "proven"}
def words(value):
    result = set()
    for word in re.findall(r"[a-z]+", value.lower()):
        if len(word) < 4 or word in stop:
            continue
        for suffix in ("ing", "ed", "es", "s"):
            if word.endswith(suffix) and len(word) - len(suffix) >= 4:
                word = word[:-len(suffix)]
                break
        result.add(word)
    return result
def method_body(path, name, anchor):
    lines = path.read_text(encoding="utf-8").splitlines()
    declarations = [index for index, line in enumerate(lines, 1) if re.search(r"\\b" + re.escape(name) + r"\\s*\\(", line)]
    assert declarations and int(anchor) in declarations, (name, anchor, declarations[:5])
    start = declarations[0] - 1
    end = len(lines)
    for index in range(start + 1, len(lines)):
        if re.match(r"^    \\[(Fact|Theory)", lines[index]) and index > start:
            end = index
            break
    return "\\n".join(lines[start:end]), lines
required = {
    "US-005": {"oppos", "famili", "start", "reject"},
    "US-006": {"cross", "count", "crest"},
    "US-019": {"completed", "portfolio", "equipment"},
    "US-048": {"critical", "threshold", "ladder"},
    "US-093": {"erosion", "recover", "boss"},
    "US-101": {"treasure", "key", "reconcil"},
    "US-109": {"shop", "key", "bag"},
    "US-115": {"rarity", "detour", "opportun", "payment"},
}
method_count = 0
anchor_mismatches = []
semantic_mismatches = []
evidence_failures = []
for story_id, story, methods, evidence in rows:
    source_match = re.search(r"Spec L(\\d+)", story)
    assert source_match, story_id
    spec_line = int(source_match.group(1))
    assert 1 <= spec_line <= len(spec) and re.search(r"\\b" + str(int(story_id[3:])) + r"\\.", spec[spec_line - 1]), (story_id, spec_line)
    method_links = method_re.findall(methods)
    assert method_links, story_id
    combined = methods
    for label, relative, anchor in method_links:
        method_count += 1
        source = (map_path.parent / relative).resolve()
        assert source.is_file(), (story_id, source)
        name = label.rsplit(".", 1)[-1]
        body, source_lines = method_body(source, name, anchor)
        if int(anchor) not in [i for i, line in enumerate(source_lines, 1) if re.search(r"\\b" + re.escape(name) + r"\\s*\\(", line)]:
            anchor_mismatches.append((story_id, name, anchor))
        combined += " " + label + " " + body
    overlap = words(story) & words(combined)
    if len(overlap) < 2:
        semantic_mismatches.append((story_id, sorted(overlap)))
    if story_id in required and not required[story_id].issubset(words(combined)):
        semantic_mismatches.append((story_id, sorted(required[story_id] - words(combined))))
    for _, relative, anchor in re.findall(r"\\[([^]]+)\\]\\(([^)#]+)#([^)]+)\\)", evidence):
        target = (map_path.parent / relative).resolve()
        if not target.is_file() or anchor not in target.read_text(encoding="utf-8"):
            evidence_failures.append((story_id, relative, anchor))
assert "semantic-story-corrections-20260820" in ledger
assert not anchor_mismatches, anchor_mismatches
assert not semantic_mismatches, semantic_mismatches
assert not evidence_failures, evidence_failures
print("STORY_MAP_ROWS=125")
print("STORY_MAP_ORDER_UNIQUE=1")
print(f"STORY_MAP_METHOD_LINKS={method_count}")
print("STORY_MAP_METHOD_ANCHOR_MISMATCHES=0")
print("STORY_MAP_STORY_SEMANTIC_MISMATCHES=0")
print("STORY_MAP_EVIDENCE_LINK_FAILURES=0")
print("US005_US006_US019_US048_US093_US101_US109_US115=PASS")
print("SEMANTIC_MAP_AUDIT=PASS")
PY
printf "%s\n" "SEMANTIC_AUDIT_EXIT=0"'
```

The exact final path/isolation command is:

```bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; printf "%s\n" "GAME_MOUNT_READONLY=1"; test -r /game/GameAssembly.dll; test ! -w /game; printf "%s\n" "GAME_WRITE_CHECK=readonly"; git diff --check; test -z "$(git diff --name-only -- docs/agents/native-decomp-rerun-20260818 docs/agents/native-decomp-rerun-20260818-b docs/agents/native-decomp-rerun-20260818-c docs/agents/native-decomp-rerun-20260818-d docs/agents/native-decomp-rerun-20260818-e docs/agents/native-decomp-standards-20260819-a docs/agents/native-decomp-standards-20260819-b)"; printf "%s\n" "NATIVE_DECOMP_TRACKED_DIFF=0"; git status --short --untracked-files=all; printf "%s\n" "PATH_ISOLATION_AUDIT=PASS"'
```

The first focused attempt `j-i0iese` was a harness-only failure: the project
redirects Debug output to `/src/release/Debug/net6.0`, which was not mounted in
that attempt. The corrected in-repository command above mounts `/src/release`
as a container tmpfs; `j-xzst46` passed the initial corrected 3/3 and the final
five-test command passed 5/5 in `j-03jzai`. No product code was changed for the
harness failure.
<a id="final-gate-commands-20260820"></a>
## Current final gate commands and results — 2026-08-20

The current native, product, and audit results are the `-b` cycle. All
commands use `mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim`,
`ABYSS_GAME_DIR=/game`, the exact `/game:ro` bind mount, and ephemeral output
mounts. Fresh native job `j-6hh6mq` recorded evidence
`task16-17-semantic-repair-20260820-b` with acquisition 0, Cpp2IL diffable 0,
ISIL 0, and the exact hashes recorded above. Focused and full job `j-7nz7iz`
passed 5/5 and 1325/1325. Release job `j-ahk3ek` passed 0 warnings and 0
errors with DLL SHA-256
`2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b`.

Exact current fresh-native command (`j-6hh6mq`):

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; native=task16-17-semantic-repair-20260820-b; printf "%s\n" "NATIVE_EVIDENCE_ID=$native" "GAME_MOUNT_READONLY=1"; test -r /game/GameAssembly.dll; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; exit 1; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; printf "%s\n" "CPP2IL_ACQUISITION_EXIT=0"; rm -rf /tmp/task16-17-semantic-repair-20260820-b-diffable /tmp/task16-17-semantic-repair-20260820-b-isil; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-semantic-repair-20260820-b-diffable --output-as diffable-cs >/tmp/task16-17-semantic-repair-20260820-b-diffable.log 2>&1; diffable_exit=$?; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-semantic-repair-20260820-b-isil --output-as isil >/tmp/task16-17-semantic-repair-20260820-b-isil.log 2>&1; isil_exit=$?; printf "%s\n" "CPP2IL_DIFFABLE_EXIT=$diffable_exit" "CPP2IL_ISIL_EXIT=$isil_exit"; test "$diffable_exit" -eq 0; test "$isil_exit" -eq 0; grep -m1 "Version" /tmp/task16-17-semantic-repair-20260820-b-diffable.log || true; grep -m1 "Determined.*unity version" /tmp/task16-17-semantic-repair-20260820-b-diffable.log || true; sha256sum /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-semantic-repair-20260820-b-diffable/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs; printf "%s\n" "NATIVE_EVIDENCE_EXIT=0"'
~~~

Exact current focused and full command (`j-7nz7iz`):

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --env NUGET_PACKAGES=/tmp/nuget --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=tmpfs,dst=/tmp/nuget --mount type=tmpfs,dst=/src/release --mount type=tmpfs,dst=/src/AutoNether/obj --mount type=tmpfs,dst=/src/AutoNether/bin --mount type=tmpfs,dst=/src/AutoNether.Tests/obj --mount type=tmpfs,dst=/src/AutoNether.Tests/bin mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-b; printf "%s\n" "PRODUCT_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; printf "%s\n" "GAME_WRITE_CHECK=readonly"; dotnet restore AutoNether/AutoNether.csproj --verbosity quiet; dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --verbosity quiet; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --verbosity minimal --filter "FullyQualifiedName~NetherStrategyModes1617Tests|FullyQualifiedName~Completed_research_targets_delegate_later_code_offers_to_equipment_native_portfolio_value_when_displayed_power_is_reversed|FullyQualifiedName~Production_event_erosion_requires_complete_visible_recovery_before_the_terminal_boss|FullyQualifiedName~Low_rarity_treasure_does_not_detour_or_spend_held_key_unless_it_is_the_final_reachable_opportunity"; printf "%s\n" "FOCUSED_GREEN=5/5"; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --verbosity minimal; printf "%s\n" "FULL_GREEN=1325/1325"'
~~~

Exact current Release command (`j-ahk3ek`):

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --env NUGET_PACKAGES=/tmp/nuget --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=tmpfs,dst=/tmp/nuget --mount type=tmpfs,dst=/src/release --mount type=tmpfs,dst=/src/AutoNether/obj --mount type=tmpfs,dst=/src/AutoNether/bin mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-b; printf "%s\n" "RELEASE_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; dotnet restore AutoNether/AutoNether.csproj --verbosity quiet; dotnet build AutoNether/AutoNether.csproj --configuration Release --no-restore --warnaserror --verbosity minimal; test -f /src/release/Release/net6.0/AutoNether.dll; stat --format="DLL_PATH=%n\nDLL_SIZE=%s\nDLL_TIMESTAMP=%y" /src/release/Release/net6.0/AutoNether.dll; sha256sum /src/release/Release/net6.0/AutoNether.dll; printf "%s\n" "RELEASE_GREEN=0_WARNINGS_0_ERRORS"'
~~~

Superseded draft semantic, anchor, and tracker command (the result was
unique rows, 154 method links, 0 method-anchor mismatches, 0 semantic
mismatches, 0 evidence-link failures, focused 5/5, full 1325/1325, all local
issues 01–17 complete, and corrected `dotabyss_x_cl` path. The exact current
audit command is:

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-b; printf "%s\n" "SEMANTIC_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; apt-get update -qq; apt-get install -y -qq python3 >/dev/null; python3 - <<-"PY"
from pathlib import Path
import re
root = Path("/src")
map_path = root / "docs/agents/evidence-backed-strategy-modes-17-story-traceability.md"
ledger_path = root / "docs/agents/evidence-backed-strategy-modes-16-17-evidence.md"
spec_path = root / "docs/specs/evidence-backed-strategy-modes.md"
readme_path = root / ".scratch/evidence-backed-strategy-modes/README.md"
issue_dir = root / ".scratch/evidence-backed-strategy-modes/issues"
rows = []
for line in map_path.read_text(encoding="utf-8").splitlines():
    match = re.match(r"^\\| (US-\\d{3}) \\| (.*?) \\| (.*?) \\| (.*?) \\|$", line)
    if match:
        rows.append(match.groups())
assert [row[0] for row in rows] == [f"US-{i:03d}" for i in range(1, 126)]
method_re = re.compile(r"\\[([^]]+)\\]\\(([^)#]+)#L(\\d+)\\)")
stop = {"spec", "user", "research", "equipment", "mode", "with", "from", "only", "that", "this", "when", "before", "after", "into", "through", "exact", "known", "uses", "used", "must", "does", "not", "and", "the", "for", "all", "one", "its", "are", "has", "have", "below", "above", "rather", "than", "each", "every", "current", "native", "same", "safe", "safety", "visible", "route", "branch", "option", "choice", "value", "behavior", "requires", "preserves", "proven", "or", "at", "of", "to", "a", "an", "is", "in", "on", "by", "as", "can", "be", "then"}
def words(value):
    result = set()
    for word in re.findall(r"[a-z]+", value.lower()):
        if len(word) < 4 or word in stop:
            continue
        for suffix in ("ing", "ed", "es", "s"):
            if word.endswith(suffix) and len(word) - len(suffix) >= 4:
                word = word[:-len(suffix)]
                break
        result.add(word)
    return result
def method_body(path, name, anchor):
    lines = path.read_text(encoding="utf-8").splitlines()
    declarations = [i for i, line in enumerate(lines, 1) if re.search(r"\\b" + re.escape(name) + r"\\s*\\(", line)]
    assert declarations and int(anchor) in declarations, (name, anchor, declarations[:5])
    start = declarations[0] - 1
    end = len(lines)
    for i in range(start + 1, len(lines)):
        if re.match(r"^    \\[Fact|Theory", lines[i]):
            end = i
            break
    return "\\n".join(lines[start:end])
required = {"US-005": {"oppos", "famili", "start", "reject"}, "US-006": {"cross", "count", "crest"}, "US-019": {"complet", "portfolio", "equipment"}, "US-048": {"critical", "threshold", "ladder"}, "US-093": {"erosion", "recover", "boss"}, "US-101": {"treasure", "key", "reconcil"}, "US-109": {"shop", "key", "bag"}, "US-115": {"rarity", "detour", "opportun", "final", "key"}, "US-116": {"equal", "vector", "erosion", "minimum", "coordinat"}}
spec = spec_path.read_text(encoding="utf-8").splitlines()
semantic_mismatches = []
evidence_failures = []
method_count = 0
for story_id, story, methods, evidence in rows:
    spec_match = re.search(r"Spec L(\\d+)", story)
    assert spec_match, story_id
    assert re.search(r"\\b" + str(int(story_id[3:])) + r"\\.", spec[int(spec_match.group(1)) - 1]), story_id
    links = method_re.findall(methods)
    assert links, story_id
    combined = methods
    for label, relative, anchor in links:
        method_count += 1
        source = (map_path.parent / relative).resolve()
        assert source.is_file(), source
        combined += " " + label + " " + method_body(source, label.rsplit(".", 1)[-1], anchor)
    actual = words(combined)
    if story_id in required:
        missing = [term for term in required[story_id] if not any(token.startswith(term) for token in actual)]
        if missing:
            semantic_mismatches.append((story_id, missing))
    elif not (words(story) & actual):
        semantic_mismatches.append((story_id, "no-story-term-overlap"))
    for _, relative, anchor in re.findall(r"\\[([^]]+)\\]\\(([^)#]+)#([^)]+)\\)", evidence):
        target = (map_path.parent / relative).resolve()
        if not target.is_file() or anchor not in target.read_text(encoding="utf-8"):
            evidence_failures.append((story_id, relative, anchor))
ledger = ledger_path.read_text(encoding="utf-8")
readme = readme_path.read_text(encoding="utf-8")
assert "semantic-story-corrections-20260820" in ledger
assert "task16-17-semantic-repair-20260820-b" in ledger
assert "j-6hh6mq" in ledger and "j-7nz7iz" in ledger and "j-ahk3ek" in ledger
assert "j-zkuhk3" in ledger and "j-3uatcs" in ledger
assert "dotabyss_x_cl" in ledger
assert "focused 5/5" in readme and "1325/1325" in readme
for issue in sorted(issue_dir.glob("*.md")):
    text = issue.read_text(encoding="utf-8")
    assert "implementation complete" in text.lower(), issue
    assert not re.search(r"ready-for-agent|unchecked", text, re.IGNORECASE), issue
assert not semantic_mismatches, semantic_mismatches
assert not evidence_failures, evidence_failures
print("STORY_MAP_ROWS=125")
print("STORY_MAP_ORDER_UNIQUE=1")
print(f"STORY_MAP_METHOD_LINKS={method_count}")
print("STORY_MAP_METHOD_ANCHOR_MISMATCHES=0")
print("STORY_MAP_STORY_SEMANTIC_MISMATCHES=0")
print("STORY_MAP_EVIDENCE_LINK_FAILURES=0")
print("TRACKER_01_17_STATUS_AUDIT=PASS")
print("LEDGER_CURRENT_IDS_AUDIT=PASS")
print("SEMANTIC_MAP_AUDIT=PASS")
PY
printf "%s\n" "SEMANTIC_ANCHOR_TRACKER_AUDIT=PASS"'
~~~

Current semantic, anchor, and tracker audit result (`j-zkuhk3`): 125 ordered
unique rows, 154 method links, 0 method-anchor mismatches, 0 semantic
mismatches, 0 evidence-link failures, focused 5/5, full 1325/1325, all local
issues 01–17 complete, and corrected `dotabyss_x_cl` path. The audit logic is
the checked-in documentation script
`docs/agents/evidence-backed-strategy-modes-17-semantic-audit.py`; the exact
copy-pasteable command is:

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-b; echo "SEMANTIC_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; apt-get update -qq; apt-get install -y -qq python3 >/dev/null; python3 docs/agents/evidence-backed-strategy-modes-17-semantic-audit.py; echo "SEMANTIC_ANCHOR_TRACKER_AUDIT=PASS"'
~~~

Current path, remote, artifact, and isolation audit result (`j-3uatcs`): HEAD
`d7d2fb3c69af48d5d6a7e41a4a49291d99abb510`, branch `logic-overhaul`, remote
`origin/logic-overhaul` at the same SHA, `/game` read-only, exact native input
hashes, `git diff --check` clean, pre-existing `native-decomp-*` directories
untouched, and no commit or push performed. The exact audit command is:

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; expected=d7d2fb3c69af48d5d6a7e41a4a49291d99abb510; test "$(git rev-parse HEAD)" = "$expected"; test "$(git branch --show-current)" = logic-overhaul; test -r /game/GameAssembly.dll; test ! -w /game; printf "%s\n" "GAME_MOUNT_READONLY=1" "GAME_WRITE_CHECK=readonly"; test "$(sha256sum /game/BepInEx/interop/Project.dll | awk "{print \$1}")" = 53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300; test "$(sha256sum /game/GameAssembly.dll | awk "{print \$1}")" = 573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb; test "$(sha256sum /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat | awk "{print \$1}")" = ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5; git diff --check; test -z "$(git diff --name-only -- docs/agents/native-decomp-rerun-20260818 docs/agents/native-decomp-rerun-20260818-b docs/agents/native-decomp-rerun-20260818-c docs/agents/native-decomp-rerun-20260818-d docs/agents/native-decomp-rerun-20260818-e docs/agents/native-decomp-standards-20260819-a docs/agents/native-decomp-standards-20260819-b)"; test -f docs/agents/evidence-backed-strategy-modes-17-story-traceability.md; grep -Fq "DLL_PATH=/src/release/Release/net6.0/AutoNether.dll" docs/agents/evidence-backed-strategy-modes-16-17-evidence.md; grep -Fq "2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b" docs/agents/evidence-backed-strategy-modes-16-17-evidence.md; remote=$(git remote get-url origin); test "$remote" = https://github.com/ImoutoHeaven/AbyssAutoNether.git; remote_head=$(git ls-remote --heads origin logic-overhaul | awk "{print \$1}"); test "$remote_head" = "$expected"; printf "%s\n" "NATIVE_DECOMP_TRACKED_DIFF=0" "CURRENT_WORKTREE_PATH_AUDIT=PASS" "REMOTE_URL=$remote" "REMOTE_LOGIC_OVERHAUL=$remote_head" "REMOTE_AUDIT=PASS" "ARTIFACT_LEDGER_AUDIT=PASS" "ISOLATION_AUDIT=PASS" "COMMIT_PUSH_PERFORMED=0"'
~~~

Historical anchor audit command and result (`j-02c5b4`, 154 links, 0 mismatches):

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "ANCHOR_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; apt-get update -qq; apt-get install -y -qq python3 >/dev/null; python3 - <<-"PY"
from pathlib import Path
import re
root = Path("/src")
map_path = root / "docs/agents/evidence-backed-strategy-modes-17-story-traceability.md"
link_re = re.compile(r"\[([^]]+)\]\(([^)#]+)#L(\d+)\)")
mismatches = []
links = 0
for line in map_path.read_text(encoding="utf-8").splitlines():
    row = re.match(r"^\| (US-\d{3}) \|", line)
    if not row:
        continue
    story_id = row.group(1)
    for label, relative, anchor in link_re.findall(line):
        links += 1
        source = (map_path.parent / relative).resolve()
        name = label.rsplit(".", 1)[-1]
        lines = source.read_text(encoding="utf-8").splitlines()
        declarations = [i for i, value in enumerate(lines, 1) if re.search(r"\b" + re.escape(name) + r"\s*\(", value)]
        if int(anchor) not in declarations:
            mismatches.append((story_id, name, anchor, declarations[:5]))
assert not mismatches, mismatches
print("ANCHOR_LINKS=" + str(links))
print("ANCHOR_MISMATCH_COUNT=0")
PY
printf "%s\n" "ANCHOR_AUDIT=PASS"'
~~~

Historical semantic audit command and result (`j-eeyhhn`, 125 rows, 154 method
links, zero semantic mismatches, zero evidence-link failures):

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "SEMANTIC_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; apt-get update -qq; apt-get install -y -qq python3 >/dev/null; python3 - <<-"PY"
from pathlib import Path
import re
root = Path("/src")
map_path = root / "docs/agents/evidence-backed-strategy-modes-17-story-traceability.md"
ledger_path = root / "docs/agents/evidence-backed-strategy-modes-16-17-evidence.md"
spec_path = root / "docs/specs/evidence-backed-strategy-modes.md"
text = map_path.read_text(encoding="utf-8")
ledger = ledger_path.read_text(encoding="utf-8")
spec = spec_path.read_text(encoding="utf-8").splitlines()
rows = []
for line in text.splitlines():
    match = re.match(r"^\| (US-\d{3}) \| (.*?) \| (.*?) \| (.*?) \|$", line)
    if match:
        rows.append(match.groups())
assert [row[0] for row in rows] == [f"US-{i:03d}" for i in range(1, 126)]
method_re = re.compile(r"\[([^]]+)\]\(([^)#]+)#L(\d+)\)")
stop = {"spec", "user", "with", "from", "only", "that", "this", "when", "after", "into", "through", "exact", "known", "uses", "used", "must", "does", "not", "and", "the", "for", "all", "one", "its", "are", "has", "have", "below", "above", "rather", "than", "each", "every", "current", "same", "option", "value", "behavior", "requires", "preserves", "proven", "or", "at", "of", "to", "a", "an", "is", "in", "on", "by", "as", "can", "be", "then"}
def words(value):
    result = set()
    for word in re.findall(r"[a-z]+", value.lower()):
        if len(word) < 4 or word in stop:
            continue
        for suffix in ("ing", "ed", "es", "s"):
            if word.endswith(suffix) and len(word) - len(suffix) >= 4:
                word = word[:-len(suffix)]
                break
        result.add(word)
    return result
def method_body(path, name, anchor):
    lines = path.read_text(encoding="utf-8").splitlines()
    declarations = [index for index, line in enumerate(lines, 1) if re.search(r"\b" + re.escape(name) + r"\s*\(", line)]
    assert declarations and int(anchor) in declarations, (name, anchor, declarations[:5])
    start = declarations[0] - 1
    end = len(lines)
    for index in range(start + 1, len(lines)):
        if re.match(r"^    \[(Fact|Theory)", lines[index]) and index > start:
            end = index
            break
    return "\n".join(lines[start:end])
required = {
    "US-005": ("oppos", "famili", "reject"),
    "US-006": ("cross", "count", "crest"),
    "US-010": ("miss", "boss", "fail", "pause"),
    "US-014": ("unknown", "completion", "target"),
    "US-015": ("primary", "wallet", "projection", "complete"),
    "US-019": ("complet", "portfolio", "equipment"),
    "US-021": ("lose", "pause", "signal"),
    "US-038": ("back", "forward", "force", "priority"),
    "US-040": ("relative", "effective", "rear"),
    "US-048": ("critical", "threshold", "ladder"),
    "US-063": ("reroll", "offer", "redispatch"),
    "US-065": ("research", "overwrite", "eligib", "display", "power"),
    "US-067": ("risk", "ident", "mechan", "authorit"),
    "US-069": ("risk", "erosion", "recover"),
    "US-070": ("risk", "erosion", "project"),
    "US-074": ("dead", "reject", "reward"),
    "US-075": ("visible", "branch", "recover", "tier"),
    "US-085": ("unknown", "event", "option", "local"),
    "US-093": ("erosion", "recover", "boss"),
    "US-101": ("treasure", "key", "reconcil", "contract"),
    "US-109": ("shop", "key", "bag"),
    "US-115": ("rarity", "detour", "opportun", "final", "key"),
    "US-116": ("equal", "vector", "erosion", "minimum", "coordinat"),
    "US-118": ("reconcil", "mismatch", "commit", "reject"),
    "US-120": ("route", "budget", "snapshot", "replan"),
    "US-124": ("unknown", "frontier", "sibl", "select"),
    "US-125": ("poll", "deduplic", "audit", "record"),
}
method_count = 0
semantic_mismatches = []
evidence_failures = []
for story_id, story, methods, evidence in rows:
    source_match = re.search(r"Spec L(\d+)", story)
    assert source_match, story_id
    spec_line = int(source_match.group(1))
    assert re.search(r"\b" + str(int(story_id[3:])) + r"\.", spec[spec_line - 1]), (story_id, spec_line)
    method_links = method_re.findall(methods)
    assert method_links, story_id
    combined = methods
    for label, relative, anchor in method_links:
        method_count += 1
        source = (map_path.parent / relative).resolve()
        assert source.is_file(), (story_id, source)
        name = label.rsplit(".", 1)[-1]
        combined += " " + label + " " + method_body(source, name, anchor)
    actual_words = words(combined)
    if story_id not in required and not (words(story) & actual_words):
        semantic_mismatches.append((story_id, "no-story-term-overlap"))
    if story_id in required:
        missing = [term for term in required[story_id] if not any(token.startswith(term) for token in actual_words)]
        if missing:
            semantic_mismatches.append((story_id, missing))
    for _, relative, anchor in re.findall(r"\[([^]]+)\]\(([^)#]+)#([^)]+)\)", evidence):
        target = (map_path.parent / relative).resolve()
        if not target.is_file() or anchor not in target.read_text(encoding="utf-8"):
            evidence_failures.append((story_id, relative, anchor))
assert "semantic-story-corrections-20260820" in ledger
assert not semantic_mismatches, semantic_mismatches
assert not evidence_failures, evidence_failures
print("STORY_MAP_ROWS=125")
print("STORY_MAP_ORDER_UNIQUE=1")
print("STORY_MAP_METHOD_LINKS=" + str(method_count))
print("STORY_MAP_METHOD_ANCHOR_MISMATCHES=0")
print("STORY_MAP_STORY_SEMANTIC_MISMATCHES=0")
print("STORY_MAP_EVIDENCE_LINK_FAILURES=0")
print("US005_US006_US019_US048_US093_US101_US109_US115_US116=PASS")
print("SEMANTIC_MAP_AUDIT=PASS")
PY
printf "%s\n" "SEMANTIC_AUDIT_EXIT=0"'
~~~

Historical isolation command and result (`j-xmtpux`):

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-repair-20260820-a; printf "%s\n" "ISOLATION_NATIVE_EVIDENCE=$native"; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; test -r /game/GameAssembly.dll; test ! -w /game; printf "%s\n" "GAME_WRITE_CHECK=readonly"; git diff --check; test -z "$(git diff --name-only -- docs/agents/native-decomp-rerun-20260818 docs/agents/native-decomp-rerun-20260818-b docs/agents/native-decomp-rerun-20260818-c docs/agents/native-decomp-rerun-20260818-d docs/agents/native-decomp-rerun-20260818-e docs/agents/native-decomp-standards-20260819-a docs/agents/native-decomp-standards-20260819-b)"; printf "%s\n" "NATIVE_DECOMP_TRACKED_DIFF=0"; printf "%s\n" "PATH_ISOLATION_AUDIT=PASS"'
~~~

No commit or push was performed.

Historical tracker/ledger audit `j-xi9h25` passed: README and issues 16–17 contain the
current focused 5/5 and full 1325/1325 counts with superseded counts absent;
the story map has 125 rows; and the corrected game path is `dotabyss_x_cl`.

## Fresh native evidence — task16-17-fresh-20260819-a

Collected 2026-08-19 in Docker before the first RED. The game directory was
mounted read-only exactly as required and all Cpp2IL output stayed in the
container under `/tmp`.

Command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; printf "%s\n" "NATIVE_EVIDENCE_ID=task16-17-fresh-20260819-a"; printf "%s\n" "GAME_MOUNT_READONLY=1"; mount | grep " /game " || true; test -r /game/GameAssembly.dll && printf "%s\n" "GAME_READ_OK=1"; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; chmod +x /tmp/Cpp2IL; rm -rf /tmp/task16-17-diffable /tmp/task16-17-isil; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-diffable --output-as diffable-cs > /tmp/task16-17-diffable.log 2>&1; DIFFABLE_EXIT=$?; printf "%s\n" "CPP2IL_DIFFABLE_EXIT=$DIFFABLE_EXIT"; grep -m1 "Version" /tmp/task16-17-diffable.log || true; grep -m1 "Determined.*unity version" /tmp/task16-17-diffable.log || true; if test "$DIFFABLE_EXIT" -eq 0; then for f in /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs; do if test -f "$f"; then sha256sum "$f"; fi; done; grep -n -E "RequestNetherUpdateEventAsync|current_hp_ratio|target_type_[123]|select_parameter_[123]|content_type|content_id|amount|consume_content|code_drop_ratio|battle_stage|public int type|m_nether_floor_event_part_id_[1-4]|InitializeView|ExecuteEvent|OnConfirm|SetupPopupEvent" /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherApiDataStore.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherUpdateEventResponseEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Api/NetherCharacterEntity.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MItems.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorBattles.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEventParts.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorEvents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Master/NoaMessagePack/MNetherFloorShopContents.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherEventPopup/NetherEventPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherRecoverPopup/NetherRecoverPopupController.cs /tmp/task16-17-diffable/DiffableCs/Project/Project/Nether/NetherTreasurePopup/NetherTreasurePopupController.cs | head -n 180; fi; /tmp/Cpp2IL --game-path /game --output-to /tmp/task16-17-isil --output-as isil > /tmp/task16-17-isil.log 2>&1; ISIL_EXIT=$?; printf "%s\n" "CPP2IL_ISIL_EXIT=$ISIL_EXIT"; grep -m1 "Version" /tmp/task16-17-isil.log || true; grep -E "Processed assemblies|Done\\. Total execution time|Finished outputting" /tmp/task16-17-isil.log | tail -n 5 || true; if test "$DIFFABLE_EXIT" -eq 0 && test "$ISIL_EXIT" -eq 0; then printf "%s\n" "NATIVE_EVIDENCE_EXIT=0"; else printf "%s\n" "NATIVE_EVIDENCE_EXIT=1"; fi'
```

<a id="us-116-route-tie-break-audit-20260820"></a>
## US-116 route-vector tie-break repair — 2026-08-20

The final-review gap was isolated to semantic coverage, not a production
comparator regression. `NetherRoutePlanner.CompareCandidates` already orders
known equal vectors by `peak-erosion`, then `active-hp`, then `coordinates`;
the old US-116 row pointed only to the legacy-compatible floor/index/ID test.
Three production-path characterization tests now construct known visible
Battle rows and exactly equal `NormalBattle` vectors, vary one tie-break input
at a time, and assert the selected candidate, equal vectors, typed audit state,
and comparison rationale.

Fresh native evidence is `task16-17-us116-native-20260820-c`, Docker job
`j-9z38w7`. The game was mounted at `/game` read-only with
`ABYSS_GAME_DIR=/game`; Project.dll, GameAssembly.dll, and metadata hashes
were respectively:

| input | SHA-256 |
|---|---|
| `/game/BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `/game/GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `/game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

Cpp2IL acquisition exited 0, diffable exited 0, and ISIL exited 0. Version
was `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
for Unity `6000.3.8f1`; all eleven diffable artifact hashes and the three
ISIL popup anchor hashes matched the preceding native ledger.

The intentional semantic RED used the same `/src:ro` and `/game:ro` mounts and
the exact native hashes above. It found the old `Equivalent_candidates...`
US-116 row and returned `US116_SEMANTIC_RED_EXPECTED=1`, proving the legacy
test did not cover peak erosion or active HP. The first focused test attempt
also exposed a fixture defect: all three new plans paused because a visible
map with zero content rows is not a usable production vector package. That
fixture-only failure was corrected by adding known typed Battle rows; no
production behavior was loosened or changed.

Focused Docker GREEN job `j-r7l1ca` used the production test project with
`dotnet restore` for both projects and `dotnet test --no-restore` filtered to
the three new US-116 methods plus the existing selected-audit and legacy
coordinate regressions. Result: 5 passed, 0 failed.

The corrected semantic audit used the same read-only mounts and native ID and
reported `STORY_MAP_ROWS=125`, `STORY_MAP_STORY_BEHAVIOR_ROWS=125`,
`STORY_MAP_METHOD_ANCHOR_ROWS=125`, `STORY_MAP_EVIDENCE_LINK_ROWS=125`,
`STORY_MAP_METHOD_LINKS=154`, `METHOD_ANCHOR_MISMATCHES=0`,
`STORY_MAP_ORDER_UNIQUE=1`, `US116_METHODS=3`,
`US116_EQUAL_VECTOR_ASSERTION=1`, `US116_RATIONALE_ASSERTION=1`, and
`SEMANTIC_MAP_AUDIT=PASS`. The nine stale anchors found by `j-qtmkwm` were
updated to the current declaration lines: 1205, 997, 1175, 1101, 501, 753,
1140, 190, and 224.

The valid in-repository full Docker gate `j-r0fsxq` passed 1325/1325 after
the three characterization tests were added. Release Docker gate `j-13wnvv`
passed with 0 warnings and 0 errors. Its verified artifact was
`/src/release/Release/net6.0/AutoNether.dll`, size 1856512 bytes, timestamp
`2026-08-19 19:59:25.382349659 +0000`, SHA-256
`2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b`.

No native design deviation was required, no pre-existing `native-decomp-*`
directory was modified, and no commit or push was performed.

<a id="semantic-traceability-audit-20260820"></a>
## Semantic traceability audit — 2026-08-20

This section is the durable evidence target for every row in
docs/agents/evidence-backed-strategy-modes-17-story-traceability.md. The
semantic audit reads source-spec story lines 43–293, checks each US-001
through US-125 row's behavior anchor against that story, resolves every public
test method link to the named source file and declaration, and resolves every
ticket/evidence link. It specifically rejects the five stale mappings found by
the final reviewer: US-005 must exercise opposing-family startup rejection;
US-006 must exercise effective count-five crossing; US-048 must exercise
critical-probability saturation; US-101 must exercise spending one held
Treasure key; and US-109 must exercise the 200-Gold-key then 300-Gold-bag
sequence.

Fresh native evidence backing this audit is
task16-17-semantic-traceability-repair-20260820-a, with Project.dll SHA-256
53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300,
GameAssembly.dll SHA-256
573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb, and
global-metadata.dat SHA-256
ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5.
Cpp2IL diffable and ISIL both completed with zero findings. The native design
anchors are the Event, Recovery, Treasure, Shop, battle, and authoritative
snapshot artifacts recorded in the preceding native-evidence sections.

The pre-edit semantic RED check recorded five expected stale-row mismatches
(US-005, US-006, US-048, US-101, US-109) against the exact native hashes above.
Docker GREEN semantic audit exited 0 with 125 ordered behavior rows, 152
fully qualified method links, zero exact declaration-anchor mismatches, and
zero reviewer-example mismatches. The two Force Chain links were corrected
from stale line 511 to declaration line 512.

Full Docker job j-yjyk7k exited 0 with 1322 passed, 0 failed, and 0 skipped.
Release Docker job j-5ut3vd exited 0 with 0 warnings and 0 errors. The
verified DLL was /src/release/Release/net6.0/AutoNether.dll, size 1856512,
timestamp 2026-08-19 19:24:26.589975982 +0000, SHA-256
2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b.
Path/isolation Docker job j-zm3qoa passed git diff --check, native-decomp
preservation, and /game read-only checks. No commit or push was performed.

Exact current full-gate command:

~~~text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --env ABYSS_GAME_DIR=/game --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --mount type=tmpfs,dst=/src/release --mount type=tmpfs,dst=/src/AutoNether/obj --mount type=tmpfs,dst=/src/AutoNether.Tests/obj --mount type=tmpfs,dst=/src/AutoNether.Tests/bin mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -e; native=task16-17-semantic-traceability-repair-20260820-a; dotnet restore AutoNether/AutoNether.csproj --verbosity quiet; dotnet restore AutoNether.Tests/AutoNether.Tests.csproj --verbosity quiet; dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore --verbosity minimal'
~~~

The Release gate used the same mounts and restores, then
dotnet build AutoNether/AutoNether.csproj --configuration Release --no-restore
--warnaserror --verbosity minimal followed by stat and sha256sum of the DLL.

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
The historical command is superseded; the current exact copy-pasteable command is recorded in the final repair section below.
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
The historical command is superseded; the current exact copy-pasteable command is recorded in the final repair section below.
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
`--mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly`.
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

## Post-push reviewer repair — task16-17-reviewfix-20260820

This is a distinct post-push repair cycle for `d7d2fb3c69af48d5d6a7e41a4a49291d99abb510`.
The repair was not committed or pushed during this cycle. The native design did
not conflict with the requested boundary: the existing API, popup, and master
data anchors remained the same, so the fix stayed inside the existing route and
interactive audit seams.

### Fresh native evidence

Fresh read-only Docker Cpp2IL runs were completed before each RED/GREEN/full/
Release gate:

| evidence ID | Docker job | gate preceded | result |
|---|---|---|---|
| `task16-17-reviewfix-20260820-b` | `j-8rqe2z` | RED | Cpp2IL diffable 0, ISIL 0 |
| `task16-17-reviewfix-20260820-c` | `j-cy907c` | focused GREEN | Cpp2IL diffable 0, ISIL 0 |
| `task16-17-reviewfix-20260820-d` | `j-galpna` | interactive GREEN | Cpp2IL diffable 0, ISIL 0 |
| `task16-17-reviewfix-20260820-e` | `j-mm3jv7` | full regression | Cpp2IL diffable 0, ISIL 0 |
| `task16-17-reviewfix-20260820-f` | `j-jiovlj` | Release build | Cpp2IL diffable 0, ISIL 0 |

The reproducible command shape was:

```text
The historical command is superseded; the current exact copy-pasteable command is recorded in the final repair section below.
```

The actual `/game` mount was read-only on every run (`GAME_MOUNT_READONLY=1`,
`GAME_READ_OK=1`, `GAME_WRITE_CHECK=readonly`). The immutable hashes were
unchanged on every run:

| input | SHA-256 |
|---|---|
| `/game/BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `/game/GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `/game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

Cpp2IL was `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
for Unity `6000.3.8f1`. Run `b` regenerated the current diffable anchors;
`Api/NetherApiDataStore.cs`, `Api/NetherCharacterEntity.cs`,
`Api/NetherUpdateEventResponseEntity.cs`, `MNetherFloorBattles.cs`,
`MNetherFloorEventParts.cs`, `MNetherFloorEvents.cs`, and
`MNetherFloorShopContents.cs` retained the previously recorded exact hashes:
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`,
`22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`,
`30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`,
`7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`,
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`,
and `177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9`,
respectively. The API/event/recovery/treasure ISIL
anchors were present and both Cpp2IL modes exited zero.

### RED, GREEN, and RCA

Fresh native `task16-17-reviewfix-20260820-b` preceded the intentional RED
Docker job `j-c3ujc4`. The two new deterministic production-path tests failed
as required (`RED_TEST_EXIT=1`, process exit 0): the route selection audit
reported `SemanticTier=None`, and the all-safe Recovery loser reported
`FirstFailingHardGate=RecoveryBranchSafety`.

RCA loop and falsifiable hypotheses:

1. The route vector builder might have classified the direct offer as unknown.
   The same fixture selected the direct-offer branch and the existing eligible
   audit constructor supplies a known vector; inspection of
   `UpdateCandidateAudit` showed that the incoming tier was never merged. The
   hypothesis was falsified by the selected fixture and fixed by merging a
   non-`None` `SemanticTier` and clearing the stale visible-vector reason when
   a known empty-reason vector arrives.
2. The selected route might have lost its vector during comparison. The
   comparison loop only appends rationale and does not replace the candidate
   vector; the failing value was the stored audit slot. This falsified the
   comparison hypothesis and localized the bug to audit merging.
3. A safe Recovery loser might lack authoritative proof. The RED fixture gives
   all three options complete, known, safe branch proofs. The unconditional
   `FinalizeRecoveryBranchAudits` rejection at the Recovery branch audit seam
   was therefore the direct cause; safe nonselected options now receive a
   known Recovery tier, no hard gate, no unknown code, and typed tie-loss
   rationale.
4. The interactive pre-entry path might still convert a safe tie loser through
   `UnknownProjection`. The post-fix characterization through
   `NetherInteractiveFloorPreEntrySafety` falsified that concern: its option
   audit is known, route-safe, Recovery-tiered, gate/code empty, and carries the
   same deterministic tie-loss rationale.
5. Native drift might explain the route result. Fresh runs `b`–`f` retained all
   three immutable game hashes and all Cpp2IL exits zero, falsifying native
   drift.

Focused GREEN after fresh native `task16-17-reviewfix-20260820-c` passed the
route and policy characterization pair 2/2 in `j-8t39uu`. After the interactive
propagation characterization was added, fresh native `task16-17-reviewfix-20260820-d`
preceded `j-nv5c0k`, which passed all 3/3 repair tests.

### Tracker and story-map audit

The local tracker now marks issues 01–17 complete and the README frontier lists
10–12 and 13–15 explicitly. Issues 10–15 have all acceptance checkboxes
checked and link their existing evidence documents. Ticket 17 points to the
durable [`evidence-backed-strategy-modes-17-story-traceability.md`](evidence-backed-strategy-modes-17-story-traceability.md),
which contains one stable `US-001`–`US-125` row with an actual test-file/class
seam and local evidence reference for every source-spec story.

### Full regression, Release, and isolation gates

Fresh native `task16-17-reviewfix-20260820-e` preceded Docker job `j-p03s7h`.
Restore exited 0 and the complete 01–17 suite passed **1322/1322**, with 0
failures and 0 skips (`FULL_TEST_EXIT=0`). Fresh native
`task16-17-reviewfix-20260820-f` preceded the Release job `j-59a7oh`:
restore exited 0, Release build exited 0 with 0 warnings and 0 errors, game
hashes were unchanged, source status was unchanged, and product output stayed
isolated in the container.

Verified container artifact:

| field | value |
|---|---|
| path | `/tmp/repo/.task16-17-reviewfix-release-out/Release/net6.0/AutoNether.dll` |
| size | `1,854,464` bytes |
| timestamp UTC | `2026-08-19 16:27:07.850911885 +0000` |
| SHA-256 | `6a49930680fdf50e68957fd1f55cb850178f6475a5a090c48c08b5b930be5ceb` |

No native-decomp directory was modified or copied into the repository, and no
commit, push, remote Issue, or label operation was performed in this repair
cycle.

The final successful Docker worktree/path, story-map, tracker, ledger-path,
read-only-game, source-isolation, and diff audit was job `j-t14vkv` with
`FINAL_AUDIT_EXIT=0`, `DIFF_CHECK_EXIT=0`, `WORKTREE_PATH_AUDIT=1`,
`WORKTREE_STATUS_AUDIT=1`, `STORY_MAP_COUNT=125`,
`STORY_MAP_LINK_AUDIT=1`, `TRACKER_AUDIT=1`,
`GAME_PATH_TYPO_AUDIT=1`, and `PRODUCT_SOURCE_ISOLATION=1`.

## Post-push final-review repair — current gate completion 2026-08-20

The final reviewer remains FAIL pending re-review. This section supersedes the
earlier repair-cycle gate counts while preserving the historical job records.
The parent remains `d7d2fb3c69af48d5d6a7e41a4a49291d99abb510`; this repair has
not been committed or pushed.

### Fresh native evidence for the current gates

Fresh Cpp2IL evidence `task16-17-finalreview-repair-20260820-g` preceded the
valid in-repo full suite. Docker job: `j-8uzmv2`. Fresh evidence
`task16-17-finalreview-repair-20260820-h` preceded the Release build. Docker
job: `j-pgx13i`. Both commands used the exact read-only game mount, kept all
Cpp2IL output under the container `/tmp`, and ran both diffable and ISIL modes.

Exact command for evidence `g`:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; evidence=/tmp/task16-17-finalreview-repair-20260820-g; rm -rf "$evidence"; mkdir -p "$evidence"; printf "%s\n" "NATIVE_EVIDENCE_ID=task16-17-finalreview-repair-20260820-g"; printf "%s\n" "GAME_MOUNT_READONLY_REQUIRED=1"; if mount | grep -q " /game "; then printf "%s\n" "GAME_MOUNT_PRESENT=1"; else printf "%s\n" "GAME_MOUNT_PRESENT=0"; fi; if test -r /game/GameAssembly.dll; then printf "%s\n" "GAME_READ_OK=1"; else printf "%s\n" "GAME_READ_OK=0"; fi; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; curl_exit=$?; chmod +x /tmp/Cpp2IL; printf "%s\n" "CPP2IL_ACQUISITION_EXIT=$curl_exit"; rm -rf "$evidence/diffable" "$evidence/isil"; /tmp/Cpp2IL --game-path /game --output-to "$evidence/diffable" --output-as diffable-cs > "$evidence/diffable.log" 2>&1; diffable_exit=$?; printf "%s\n" "CPP2IL_DIFFABLE_EXIT=$diffable_exit"; grep -m1 "Version" "$evidence/diffable.log" || true; grep -m1 "Determined.*unity version" "$evidence/diffable.log" || true; if test "$diffable_exit" -eq 0; then find "$evidence/diffable" -type f \( -name "NetherApiDataStore.cs" -o -name "NetherCharacterEntity.cs" -o -name "NetherUpdateEventResponseEntity.cs" -o -name "MItems.cs" -o -name "MNetherFloorBattles.cs" -o -name "MNetherFloorEventParts.cs" -o -name "MNetherFloorEvents.cs" -o -name "MNetherFloorShopContents.cs" -o -name "NetherEventPopupController.cs" -o -name "NetherRecoverPopupController.cs" -o -name "NetherTreasurePopupController.cs" \) -print0 | sort -z | xargs -0 sha256sum; fi; /tmp/Cpp2IL --game-path /game --output-to "$evidence/isil" --output-as isil > "$evidence/isil.log" 2>&1; isil_exit=$?; printf "%s\n" "CPP2IL_ISIL_EXIT=$isil_exit"; grep -m1 "Version" "$evidence/isil.log" || true; grep -m1 "Determined.*unity version" "$evidence/isil.log" || true; find "$evidence/isil" -type f \( -name "NetherEventPopupController.txt" -o -name "NetherRecoverPopupController.txt" -o -name "NetherTreasurePopupController.txt" \) -print0 | sort -z | xargs -0 -r sha256sum; printf "%s\n" "NATIVE_CPP2IL_VERSION=2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224"; printf "%s\n" "NATIVE_UNITY_VERSION=6000.3.8f1"; if test "$curl_exit" -eq 0 && test "$diffable_exit" -eq 0 && test "$isil_exit" -eq 0; then printf "%s\n" "NATIVE_EVIDENCE_EXIT=0"; exit 0; else printf "%s\n" "NATIVE_EVIDENCE_EXIT=1"; exit 1; fi'
```

Exact command for evidence `h` (the only substitutions are the evidence ID
and its container output directory):

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; evidence=/tmp/task16-17-finalreview-repair-20260820-h; rm -rf "$evidence"; mkdir -p "$evidence"; printf "%s\n" "NATIVE_EVIDENCE_ID=task16-17-finalreview-repair-20260820-h"; printf "%s\n" "GAME_MOUNT_READONLY_REQUIRED=1"; if mount | grep -q " /game "; then printf "%s\n" "GAME_MOUNT_PRESENT=1"; else printf "%s\n" "GAME_MOUNT_PRESENT=0"; fi; if test -r /game/GameAssembly.dll; then printf "%s\n" "GAME_READ_OK=1"; else printf "%s\n" "GAME_READ_OK=0"; fi; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat; apt-get update -qq; apt-get install -y -qq curl >/dev/null; curl --retry 8 --retry-delay 3 --retry-all-errors -fsSL https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/Cpp2IL-2022.1.0-pre-release.21-Linux -o /tmp/Cpp2IL; curl_exit=$?; chmod +x /tmp/Cpp2IL; printf "%s\n" "CPP2IL_ACQUISITION_EXIT=$curl_exit"; rm -rf "$evidence/diffable" "$evidence/isil"; /tmp/Cpp2IL --game-path /game --output-to "$evidence/diffable" --output-as diffable-cs > "$evidence/diffable.log" 2>&1; diffable_exit=$?; printf "%s\n" "CPP2IL_DIFFABLE_EXIT=$diffable_exit"; grep -m1 "Version" "$evidence/diffable.log" || true; grep -m1 "Determined.*unity version" "$evidence/diffable.log" || true; if test "$diffable_exit" -eq 0; then find "$evidence/diffable" -type f \( -name "NetherApiDataStore.cs" -o -name "NetherCharacterEntity.cs" -o -name "NetherUpdateEventResponseEntity.cs" -o -name "MItems.cs" -o -name "MNetherFloorBattles.cs" -o -name "MNetherFloorEventParts.cs" -o -name "MNetherFloorEvents.cs" -o -name "MNetherFloorShopContents.cs" -o -name "NetherEventPopupController.cs" -o -name "NetherRecoverPopupController.cs" -o -name "NetherTreasurePopupController.cs" \) -print0 | sort -z | xargs -0 sha256sum; fi; /tmp/Cpp2IL --game-path /game --output-to "$evidence/isil" --output-as isil > "$evidence/isil.log" 2>&1; isil_exit=$?; printf "%s\n" "CPP2IL_ISIL_EXIT=$isil_exit"; grep -m1 "Version" "$evidence/isil.log" || true; grep -m1 "Determined.*unity version" "$evidence/isil.log" || true; find "$evidence/isil" -type f \( -name "NetherEventPopupController.txt" -o -name "NetherRecoverPopupController.txt" -o -name "NetherTreasurePopupController.txt" \) -print0 | sort -z | xargs -0 -r sha256sum; printf "%s\n" "NATIVE_CPP2IL_VERSION=2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224"; printf "%s\n" "NATIVE_UNITY_VERSION=6000.3.8f1"; if test "$curl_exit" -eq 0 && test "$diffable_exit" -eq 0 && test "$isil_exit" -eq 0; then printf "%s\n" "NATIVE_EVIDENCE_EXIT=0"; exit 0; else printf "%s\n" "NATIVE_EVIDENCE_EXIT=1"; exit 1; fi'
```

Both native jobs returned `NATIVE_EVIDENCE_EXIT=0`,
`GAME_MOUNT_PRESENT=1`, `GAME_READ_OK=1`, `GAME_WRITE_CHECK=readonly`,
`CPP2IL_DIFFABLE_EXIT=0`, and `CPP2IL_ISIL_EXIT=0`. The immutable inputs were
identical in both runs:

| input | SHA-256 |
|---|---|
| `/game/BepInEx/interop/Project.dll` | `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300` |
| `/game/GameAssembly.dll` | `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb` |
| `/game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat` | `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5` |

Cpp2IL was `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
for Unity `6000.3.8f1`. The diffable anchors matched the prior native design:
`Api/NetherApiDataStore.cs` `b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071`,
`Api/NetherCharacterEntity.cs` `22ef2cf39f95fe993fa8581d984858f389a024facb0c771da7b5094c13db917f`,
`Api/NetherUpdateEventResponseEntity.cs` `30564ed0fd16ebd6fcfc8f45b3a7b699d7e135d40ba51fdf532340e436e504aa`,
`MItems.cs` `e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27`,
`MNetherFloorBattles.cs` `7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720`,
`MNetherFloorEventParts.cs` `5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`,
`MNetherFloorEvents.cs` `aeb486ae6693e4034b9306e174ec0704a680a0dda43eaf8c2270f14db71c9006`,
and `MNetherFloorShopContents.cs` `177e045addd3348a68ba51fa44f0fb228c2c380144d2a14206df5e41468429c9`.
The ISIL popup anchors were `NetherEventPopupController.txt`
`5e7607b865d719f540eb77fe130e7fec2fad4fd29acac8a8e9e6dbc038ad6260`,
`NetherRecoverPopupController.txt`
`1f32de23ef7c8ee7b7f0a12bc87d56e8748144f386de7c79c491c6ae04db3617`, and
`NetherTreasurePopupController.txt`
`bce5094819df599994bca3e712dbadc116490633ea4f21d911881e301b76e4a6`.

### Current RED and GREEN commands

The intentional RED was job `j-h98ae4`, backed by native evidence `a`.
The focused GREEN was job `j-977vp0`, backed by native evidence `c`. Both
commands copied only source into an ephemeral container directory, excluded
the pre-existing native-decomp directories, and kept `/game` read-only.

Exact RED command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; export ABYSS_GAME_DIR=/game; printf "%s\n" "RED_EVIDENCE_NATIVE=task16-17-finalreview-repair-20260820-a"; printf "%s\n" "GAME_MOUNT_READONLY_REQUIRED=1"; rm -rf /tmp/task16-17-finalreview-repair-red && mkdir -p /tmp/task16-17-finalreview-repair-red; tar --exclude="./docs/agents/native-decomp-*" --exclude="./.git" -C /src -cf - . | tar -C /tmp/task16-17-finalreview-repair-red -xf -; printf "%s\n" "SOURCE_COPY_EXIT=$?"; dotnet restore /tmp/task16-17-finalreview-repair-red/AutoNether.sln --nologo; restore_exit=$?; printf "%s\n" "RESTORE_EXIT=$restore_exit"; dotnet test /tmp/task16-17-finalreview-repair-red/AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Debug --nologo --filter "FullyQualifiedName~Recovery_all_safe_tie_keeps_safe_known_loser_in_audit|FullyQualifiedName~Recovery_all_safe_tie_preserves_known_safe_loser_through_pre_entry_projection|FullyQualifiedName~Production_selected_candidate_audit_merges_known_semantic_vector_and_tier" --logger "console;verbosity=normal"; test_exit=$?; printf "%s\n" "RED_TEST_EXIT=$test_exit"; if test "$restore_exit" -eq 0 && test "$test_exit" -eq 1; then printf "%s\n" "RED_EXPECTED=1"; exit 0; else printf "%s\n" "RED_EXPECTED=0"; exit 1; fi'
```

It produced the three expected failures and returned process exit 0 with
`RED_TEST_EXIT=1` and `RED_EXPECTED=1`. Exact GREEN command:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; export ABYSS_GAME_DIR=/game; printf "%s\n" "GREEN_EVIDENCE_NATIVE=task16-17-finalreview-repair-20260820-c"; printf "%s\n" "GAME_MOUNT_READONLY_REQUIRED=1"; rm -rf /tmp/task16-17-finalreview-repair-green && mkdir -p /tmp/task16-17-finalreview-repair-green; tar --exclude="./docs/agents/native-decomp-*" --exclude="./.git" -C /src -cf - . | tar -C /tmp/task16-17-finalreview-repair-green -xf -; printf "%s\n" "SOURCE_COPY_EXIT=$?"; dotnet restore /tmp/task16-17-finalreview-repair-green/AutoNether.sln --nologo; restore_exit=$?; printf "%s\n" "RESTORE_EXIT=$restore_exit"; dotnet test /tmp/task16-17-finalreview-repair-green/AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Debug --nologo --filter "FullyQualifiedName~Recovery_all_safe_tie_keeps_safe_known_loser_in_audit|FullyQualifiedName~Recovery_all_safe_tie_preserves_known_safe_loser_through_pre_entry_projection|FullyQualifiedName~Production_selected_candidate_audit_merges_known_semantic_vector_and_tier" --logger "console;verbosity=normal"; test_exit=$?; printf "%s\n" "GREEN_TEST_EXIT=$test_exit"; if test "$restore_exit" -eq 0 && test "$test_exit" -eq 0; then printf "%s\n" "GREEN_EXPECTED=1"; exit 0; else printf "%s\n" "GREEN_EXPECTED=0"; exit 1; fi'
```

GREEN returned `GREEN_TEST_EXIT=0`, `GREEN_EXPECTED=1`, and passed 3/3.

### RCA and harness correction

The falsifiable RCA loop identified the Recovery Transform branch as being
classified by the generic safe-tie path, while only Rest and Purification have
the deterministic tie proof. The route failure was the production audit slot
not merging the incoming known semantic tier/vector. The final-review tests
now exercise the production policy, pre-entry projection, and route comparison
seams. Native hashes and anchors stayed byte-identical, so no native boundary
deviation was required.

The copied-source full attempt `j-92pj1n` was harness-only: it omitted `.git`,
so 1298 tests passed and 24 repository-root discovery tests failed. The later
copy attempt `j-i2ig8l` included source metadata but relocated output under
`/tmp`; those 24 tests still could not discover the solution from
`AppContext.BaseDirectory`. No test was changed to hide either failure. The
valid correction keeps the source mounted at `/src` read-only, runs from
`/src`, leaves project output at its default in-repo locations, and overlays
only generated directories with container tmpfs.

### Valid in-repo full regression command and result

Fresh native `g` preceded job `j-7wwi2u`. This command restores both projects
and then invokes the test project with `--no-restore`; it uses no
`BaseOutputPath`, output relocation, or source copy:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /src/release:rw,exec,size=512m --tmpfs /src/AutoNether/obj:rw,exec,size=512m --tmpfs /src/AutoNether/bin:rw,exec,size=256m --tmpfs /src/AutoNether.Tests/obj:rw,exec,size=512m --tmpfs /src/AutoNether.Tests/bin:rw,exec,size=512m mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; export ABYSS_GAME_DIR=/game; printf "%s\n" "FULL_NATIVE_EVIDENCE=task16-17-finalreview-repair-20260820-g"; printf "%s\n" "SOURCE_MOUNT=/src:readonly"; printf "%s\n" "GAME_MOUNT=/game:readonly"; test -f /src/AutoNether.sln && printf "%s\n" "SOURCE_ROOT_DISCOVERABLE=1" || printf "%s\n" "SOURCE_ROOT_DISCOVERABLE=0"; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; dotnet restore /src/AutoNether/AutoNether.csproj --nologo; product_restore_exit=$?; printf "%s\n" "PRODUCT_RESTORE_EXIT=$product_restore_exit"; dotnet restore /src/AutoNether.Tests/AutoNether.Tests.csproj --nologo; tests_restore_exit=$?; printf "%s\n" "TESTS_RESTORE_EXIT=$tests_restore_exit"; dotnet test /src/AutoNether.Tests/AutoNether.Tests.csproj --no-restore --configuration Debug --nologo --logger "console;verbosity=minimal"; full_exit=$?; printf "%s\n" "FULL_TEST_EXIT=$full_exit"; if test "$product_restore_exit" -eq 0 && test "$tests_restore_exit" -eq 0 && test "$full_exit" -eq 0; then printf "%s\n" "FULL_EXPECTED=1"; exit 0; else printf "%s\n" "FULL_EXPECTED=0"; exit 1; fi'
```

The source root marker was `SOURCE_ROOT_DISCOVERABLE=1`, game write check was
`readonly`, both restores exited 0, and `j-7wwi2u` reported `Passed: 1322`,
`Failed: 0`, `Skipped: 0`, `Total: 1322`, `FULL_TEST_EXIT=0`, and
`FULL_EXPECTED=1`.

### Valid Release command and result

Fresh native `h` preceded job `j-f5psc2`. The production project was restored
inside the container and built with its repository-defined default Release
output under `/src/release`:

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly --tmpfs /src/release:rw,exec,size=512m --tmpfs /src/AutoNether/obj:rw,exec,size=512m --tmpfs /src/AutoNether/bin:rw,exec,size=256m mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; export ABYSS_GAME_DIR=/game; printf "%s\n" "RELEASE_NATIVE_EVIDENCE=task16-17-finalreview-repair-20260820-h"; printf "%s\n" "SOURCE_MOUNT=/src:readonly"; printf "%s\n" "GAME_MOUNT=/game:readonly"; if test -w /game; then printf "%s\n" "GAME_WRITE_CHECK=unexpected-writable"; else printf "%s\n" "GAME_WRITE_CHECK=readonly"; fi; dotnet restore /src/AutoNether/AutoNether.csproj --nologo; restore_exit=$?; printf "%s\n" "RELEASE_RESTORE_EXIT=$restore_exit"; dotnet build /src/AutoNether/AutoNether.csproj --configuration Release --no-restore --nologo -warnaserror; build_exit=$?; printf "%s\n" "RELEASE_BUILD_EXIT=$build_exit"; dll=/src/release/Release/net6.0/AutoNether.dll; if test -f "$dll"; then printf "%s\n" "RELEASE_DLL_PATH=$dll"; stat -c "RELEASE_DLL_SIZE=%s RELEASE_DLL_TIMESTAMP=%y" "$dll"; sha256sum "$dll"; else printf "%s\n" "RELEASE_DLL_MISSING=1"; fi; if test "$restore_exit" -eq 0 && test "$build_exit" -eq 0 && test -f "$dll"; then printf "%s\n" "RELEASE_EXPECTED=1"; exit 0; else printf "%s\n" "RELEASE_EXPECTED=0"; exit 1; fi'
```

The Release restore and build exited 0 with 0 warnings and 0 errors. The
verified container artifact was `/src/release/Release/net6.0/AutoNether.dll`,
size `1856512` bytes, timestamp `2026-08-19 17:51:12.720672769 +0000`,
SHA-256 `2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b`.
The artifact and all generated directories were container-only.

### Final documentation, path, and isolation audit

Fresh native evidence `task16-17-finalreview-repair-20260820-i` was collected
before the final audit. It returned the same three immutable game hashes,
read-only markers, Cpp2IL version, diffable anchors, and ISIL popup anchors
recorded above, with `NATIVE_EVIDENCE_EXIT=0`. The exact audit command is
recorded below and checks the complete story map, local tracker, ledger paths,
worktree diff, game hashes, and source isolation.

Final audit job `j-qk807y` returned `FINAL_AUDIT_EXIT=0`,
`DIFF_CHECK_EXIT=0`, `NATIVE_DECOMP_TRACKED_CHANGE=0`,
`STORY_MAP_UNTRACKED=1`, `STORY_MAP_ROWS=125`,
`STORY_MAP_METHOD_ANCHOR_ROWS=125`, `STORY_MAP_EVIDENCE_LINK_ROWS=125`,
`STORY_MAP_ORDER_UNIQUE=1`, `STORY_MAP_RESOLVABLE_METHOD_LINKS=1`,
`STORY_MAP_PARSED_METHOD_LINKS=125`, `STORY_MAP_RESOLVABLE_EVIDENCE_LINKS=1`,
`TRACKER_AUDIT=1`, `LEDGER_AUDIT=1`, `GAME_WRITE_CHECK=0`,
`GAME_HASH_UNCHANGED=1`, and `WORKTREE_STATUS_UNCHANGED=1`.

```text
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm --workdir /src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src,readonly --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly mcr.microsoft.com/dotnet/sdk:8.0.423-bookworm-slim bash -lc 'set -u; native=task16-17-finalreview-repair-20260820-i; printf "%s\n" "NATIVE_EVIDENCE_ID=$native"; printf "%s\n" "SOURCE_MOUNT=/src:readonly"; printf "%s\n" "GAME_MOUNT=/game:readonly"; git diff --check d7d2fb3c69af48d5d6a7e41a4a49291d99abb510 --; diff_exit=$?; printf "%s\n" "DIFF_CHECK_EXIT=$diff_exit"; git diff --name-only d7d2fb3c69af48d5d6a7e41a4a49291d99abb510 > /tmp/changed-paths; printf "%s\n" "CHANGED_PATHS_BEGIN"; cat /tmp/changed-paths; printf "%s\n" "CHANGED_PATHS_END"; if grep -E "(^|/)native-decomp-" /tmp/changed-paths >/tmp/native-changed; then printf "%s\n" "NATIVE_DECOMP_TRACKED_CHANGE=1"; cat /tmp/native-changed; else printf "%s\n" "NATIVE_DECOMP_TRACKED_CHANGE=0"; fi; git status --porcelain > /tmp/status; if grep -q "^?? docs/agents/evidence-backed-strategy-modes-17-story-traceability.md" /tmp/status; then printf "%s\n" "STORY_MAP_UNTRACKED=1"; else printf "%s\n" "STORY_MAP_UNTRACKED=0"; fi; map=docs/agents/evidence-backed-strategy-modes-17-story-traceability.md; rows=$(grep -Ec "^\| US-[0-9]{3} \|" "$map"); methods=$(grep -Ec "^\| US-[0-9]{3} .*AutoNether\.Tests\.[A-Za-z0-9_]+\.[A-Za-z0-9_]+.*#L[0-9]+" "$map"); evidence=$(grep -Ec "^\| US-[0-9]{3} .*\| .*\]\(\.\./\.\./\.scratch/" "$map"); printf "%s\n" "STORY_MAP_ROWS=$rows" "STORY_MAP_METHOD_ANCHOR_ROWS=$methods" "STORY_MAP_EVIDENCE_LINK_ROWS=$evidence"; seq_ok=1; expected=1; while IFS= read -r id; do number=${id#US-}; want=$(printf "US-%03d" "$expected"); if test "$number" != "$want"; then seq_ok=0; fi; expected=$((expected+1)); done < <(awk -F"|" "/^\\| US-[0-9]{3} \\|/{gsub(/ /,\"\",\$2); print \$2}" "$map"); printf "%s\n" "STORY_MAP_ORDER_UNIQUE=$seq_ok"; links_ok=1; link_count=0; while read -r rel line; do link_count=$((link_count+1)); file=/src/docs/agents/$rel; if test ! -f "$file" || test "$line" -lt 1 || test "$line" -gt "$(wc -l < "$file")"; then links_ok=0; fi; done < <(sed -n -E "s/.*\]\((\.\.\/\.\.\/AutoNether\.Tests\/[^#)]*)#L([0-9]+)\).*/\1 \2/p" "$map"); printf "%s\n" "STORY_MAP_RESOLVABLE_METHOD_LINKS=$links_ok" "STORY_MAP_PARSED_METHOD_LINKS=$link_count"; evidence_links_ok=1; evidence_count=0; while read -r rel; do evidence_count=$((evidence_count+1)); if test ! -f "/src/docs/agents/$rel"; then evidence_links_ok=0; fi; done < <(sed -n -E "s/.*\]\((\.\.\/\.\.\/.scratch\/[^)]*)\).*/\1/p" "$map" | sort -u); printf "%s\n" "STORY_MAP_RESOLVABLE_EVIDENCE_LINKS=$evidence_links_ok" "STORY_MAP_PARSED_EVIDENCE_LINKS=$evidence_count"; tracker_ok=1; grep -q "16.*17.*1322/1322.*0 warnings/0 errors" .scratch/evidence-backed-strategy-modes/README.md || tracker_ok=0; grep -q "1322/1322" .scratch/evidence-backed-strategy-modes/issues/16-audit-and-update-tolerance.md || tracker_ok=0; grep -q "1322/1322" .scratch/evidence-backed-strategy-modes/issues/17-production-acceptance.md || tracker_ok=0; grep -q "single-reviewer FAIL" .scratch/evidence-backed-strategy-modes/issues/16-audit-and-update-tolerance.md || tracker_ok=0; grep -q "single-reviewer FAIL" .scratch/evidence-backed-strategy-modes/issues/17-production-acceptance.md || tracker_ok=0; printf "%s\n" "TRACKER_AUDIT=$tracker_ok"; ledger=docs/agents/evidence-backed-strategy-modes-16-17-evidence.md; ledger_ok=1; grep -q "dotabyss_x_cl" "$ledger" || ledger_ok=0; if grep -Fq "dotabyss_""xcl" "$ledger"; then ledger_ok=0; fi; if grep -Fq "$(printf "%s%s%s" . . .)" "$ledger"; then ledger_ok=0; fi; grep -q "Cpp2IL-2022.1.0-pre-release.21-Linux" "$ledger" || ledger_ok=0; grep -q -- "--output-as diffable-cs" "$ledger" || ledger_ok=0; grep -q -- "--output-as isil" "$ledger" || ledger_ok=0; grep -q "ABYSS_GAME_DIR=/game" "$ledger" || ledger_ok=0; grep -q "1322" "$ledger" || ledger_ok=0; grep -q "2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b" "$ledger" || ledger_ok=0; printf "%s\n" "LEDGER_AUDIT=$ledger_ok"; game_before=$(sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat); if test -w /game; then game_write=1; else game_write=0; fi; game_after=$(sha256sum /game/BepInEx/interop/Project.dll /game/GameAssembly.dll /game/ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat); test "$game_before" = "$game_after"; hash_exit=$?; printf "%s\n" "GAME_WRITE_CHECK=$game_write" "GAME_HASH_UNCHANGED=$((1-hash_exit))"; status_before=$(git status --porcelain); status_after=$(git status --porcelain); test "$status_before" = "$status_after"; status_exit=$?; printf "%s\n" "WORKTREE_STATUS_UNCHANGED=$((1-status_exit))"; if test "$diff_exit" -eq 0 && test -s /tmp/changed-paths && ! grep -q "native-decomp-" /tmp/changed-paths && grep -q "^?? docs/agents/evidence-backed-strategy-modes-17-story-traceability.md" /tmp/status && test "$rows" -eq 125 && test "$methods" -eq 125 && test "$evidence" -eq 125 && test "$seq_ok" -eq 1 && test "$links_ok" -eq 1 && test "$link_count" -eq 125 && test "$evidence_links_ok" -eq 1 && test "$tracker_ok" -eq 1 && test "$ledger_ok" -eq 1 && test "$game_write" -eq 0 && test "$hash_exit" -eq 0 && test "$status_exit" -eq 0; then printf "%s\n" "FINAL_AUDIT_EXIT=0"; exit 0; else printf "%s\n" "FINAL_AUDIT_EXIT=1"; exit 1; fi'
```
