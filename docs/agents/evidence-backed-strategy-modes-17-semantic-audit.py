from pathlib import Path
import re
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]
MAP_PATH = ROOT / "docs/agents/evidence-backed-strategy-modes-17-story-traceability.md"
LEDGER_PATH = ROOT / "docs/agents/evidence-backed-strategy-modes-16-17-evidence.md"
SPEC_PATH = ROOT / "docs/specs/evidence-backed-strategy-modes.md"
README_PATH = ROOT / ".scratch/evidence-backed-strategy-modes/README.md"
ISSUE_DIR = ROOT / ".scratch/evidence-backed-strategy-modes/issues"
CURRENT_HEAD = "b25f8ea36b4a29ac42d7b866e7efd6b14ced9864"
CURRENT_NATIVE_EVIDENCE_ID = "final-sol-current-world-native-20260820-b"
CURRENT_RELEASE_EVIDENCE_ID = "final-sol-current-world-release-20260820-j"
CURRENT_SEMANTIC_EVIDENCE_ID = "final-sol-current-world-semantic-20260820-k"
CURRENT_PRESERVATION_EVIDENCE_ID = "task10-us100-current-preservation-20260820-x"
CURRENT_FINAL_AUDIT_EVIDENCE_ID = "final-sol-current-world-audit-20260820-l"
CURRENT_RELEASE_DLL_SHA256 = "412a66cfe3e70a2225b2b34940b78f7da585e3fa26d5e8bf05ff0aa7946e8d71"
CURRENT_PROJECT_DLL_SHA256 = "033a5d1e92df1f90d15b4f33312fb935327fd2baa87811b7860b227d6c1c75f4"
CURRENT_GAME_ASSEMBLY_SHA256 = "f2ad94781c161fe93040463b884c328599a40c78079aecacbe17a9b78edfc767"
CURRENT_METADATA_SHA256 = "d7dffa623675ac493a0a4c7cfe8dc729bc37846b455a5284af94a901c1e25c27"
EXPECTED_STORY_MAP_ROWS = 125
EXPECTED_METHOD_LINKS = 156

METHOD_LINK = re.compile(r"\[([^]]+)\]\(([^)#]+)#L(\d+)\)")
EVIDENCE_LINK = re.compile(r"\[([^]]+)\]\(([^)#]+)#([^)]+)\)")
SPECIAL_ASSERTIONS = {
    "US-005": (
        "research_rejects_opposed_primary_and_secondary_families_before_native_action",
        "research-families-are-opposed",
        "netherpausereason.invalidconfiguration",
        "trycapture",
    ),
    "US-006": (
        "crossing_effective_count_five_requires_every_active_character_matching_family_crest",
        "nethercodedecisionkind.keep",
        "nethercodedecisionkind.select",
        "activeparty",
    ),
    "US-019": (
        "completed_research_targets_delegate_later_code_offers_to_equipment_native_portfolio_value_when_displayed_power_is_reversed",
        "netherstrategymode.research",
        "retainedportfoliostrictimprovement",
        "strictimprovementproven",
        "displaypowerusedfordecision",
    ),
    "US-048": (
        "critical_clips_at_guaranteed_threshold_but_continuous_attack_uses_finite_decreasing_ladder",
        "criticalprobabilitymarginalpermille(950,100)",
        "continuousattackexpectedadditionalmicros(950,3)",
        "continuousattackexpectedadditionalmicros(1_050,3)",
    ),
    "US-093": (
        "production_event_erosion_requires_complete_visible_recovery_before_the_terminal_boss",
        "nethereffectkind.erosion",
        "nethereffectkind.erosionheal",
        "netherfloornodetype.event",
        "netherfloornodetype.recovery",
        "netherfloornodetype.boss",
    ),
    "US-100": (
        "production_complete_recovery_branch_proof_preserves_known_losers_and_selects_the_only_safe_transform",
        "trydeciderecoveryfromcompletebranchevidence",
        "requirecompleterecoverybranchsafety",
        "equipmentrecoverycodetransformenabled",
        "deterministicrecoverychoiceshavezerovalue",
        "hardexcludedcodes",
        "transformonlysafe",
        "selected-by-complete-branch-proof",
    ),
    "US-101": (
        "production_controller_reconciles_owned_treasure_with_exact_key_contract",
        "controllerroutewiring_preferskeywhenitisavailable",
        "treasurekeyused",
        "safeoptionnumberbyeventid",
    ),
    "US-109": (
        "committed_rank_five_shop_orders_key_then_skips_bag_until_500_gold",
        "keycontentid=2001",
        "keycost=200",
        "bagcontentid=3001",
        "bagcost=300",
        "gold:500",
    ),
    "US-115": (
        "low_rarity_treasure_does_not_detour_or_spend_held_key_unless_it_is_the_final_reachable_opportunity",
        "treasurekeycount=1",
        "treasurekeyused",
        "expectedeffects",
        "projectedtreasurekeys",
    ),
    "US-116": (
        "production_route_tie_break_prefers_lower_peak_erosion_for_equal_known_vectors",
        "production_route_tie_break_prefers_higher_minimum_hp_after_equal_peak_for_equal_known_vectors",
        "production_route_tie_break_uses_coordinates_after_equal_vector_peak_and_hp",
        "peak-erosion",
        "active-hp",
        "coordinates",
    ),
}


DECLARATION_MODIFIERS = {
    "abstract",
    "async",
    "extern",
    "file",
    "internal",
    "new",
    "override",
    "partial",
    "private",
    "protected",
    "public",
    "readonly",
    "ref",
    "sealed",
    "static",
    "unsafe",
    "virtual",
}
DECLARATION_PREFIX_FORBIDDEN = re.compile(r"[{}();=,]|=>")
DECLARATION_PREFIX_TOKEN = re.compile(r"@?[A-Za-z_][A-Za-z0-9_]*")
DECLARATION_CONTROL_WORDS = {
    "await",
    "base",
    "case",
    "catch",
    "checked",
    "default",
    "do",
    "else",
    "false",
    "foreach",
    "for",
    "goto",
    "if",
    "lock",
    "nameof",
    "new",
    "null",
    "return",
    "sizeof",
    "stackalloc",
    "switch",
    "this",
    "throw",
    "true",
    "try",
    "typeof",
    "unchecked",
    "using",
    "var",
    "while",
    "yield",
}


def _code_mask(source):
    """Mark C# code characters while ignoring comments and literals."""
    mask = bytearray(b"\x01") * len(source)
    state = "code"
    index = 0
    raw_delimiter = None
    while index < len(source):
        char = source[index]
        next_char = source[index + 1] if index + 1 < len(source) else ""
        if state == "line-comment":
            mask[index] = 0
            if char == "\n":
                state = "code"
            index += 1
            continue
        if state == "block-comment":
            mask[index] = 0
            if char == "*" and next_char == "/":
                mask[index + 1] = 0
                state = "code"
                index += 2
            else:
                index += 1
            continue
        if state == "string":
            mask[index] = 0
            if char == "\\" and index + 1 < len(source):
                mask[index + 1] = 0
                index += 2
            elif char == '"':
                state = "code"
                index += 1
            else:
                index += 1
            continue
        if state == "verbatim-string":
            mask[index] = 0
            if char == '"':
                if next_char == '"':
                    mask[index + 1] = 0
                    index += 2
                else:
                    state = "code"
                    index += 1
            else:
                index += 1
            continue
        if state == "char":
            mask[index] = 0
            if char == "\\" and index + 1 < len(source):
                mask[index + 1] = 0
                index += 2
            elif char == "'":
                state = "code"
                index += 1
            else:
                index += 1
            continue
        if state == "raw-string":
            if source.startswith(raw_delimiter, index):
                for offset in range(len(raw_delimiter)):
                    mask[index + offset] = 0
                index += len(raw_delimiter)
                state = "code"
            else:
                mask[index] = 0
                index += 1
            continue

        if char == "/" and next_char == "/":
            mask[index] = 0
            mask[index + 1] = 0
            state = "line-comment"
            index += 2
        elif char == "/" and next_char == "*":
            mask[index] = 0
            mask[index + 1] = 0
            state = "block-comment"
            index += 2
        elif source.startswith('"""', index):
            for offset in range(3):
                mask[index + offset] = 0
            raw_delimiter = '"""'
            state = "raw-string"
            index += 3
        elif char == '"':
            mask[index] = 0
            state = "verbatim-string" if index > 0 and source[index - 1] == "@" else "string"
            index += 1
        elif char == "'":
            mask[index] = 0
            state = "char"
            index += 1
        else:
            index += 1
    return mask


def _matching_delimiter(source, start, opening, closing, mask):
    assert source[start] == opening
    depth = 0
    for index in range(start, len(source)):
        if not mask[index]:
            continue
        if source[index] == opening:
            depth += 1
        elif source[index] == closing:
            depth -= 1
            if depth == 0:
                return index
    raise AssertionError("unbalanced method parameter list")


def _matching_body_end(source, start, mask=None):
    if mask is None:
        mask = _code_mask(source)
    assert source[start] == "{"
    depth = 0
    for index in range(start, len(source)):
        if not mask[index]:
            continue
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return index
    raise AssertionError("unbalanced method body")


def _is_anchored_declaration_prefix(prefix):
    prefix = prefix.strip()
    if not prefix or DECLARATION_PREFIX_FORBIDDEN.search(prefix):
        return False
    words = [word.lstrip("@") for word in DECLARATION_PREFIX_TOKEN.findall(prefix)]
    if not words or any(word in DECLARATION_CONTROL_WORDS for word in words):
        return False
    return any(word not in DECLARATION_MODIFIERS for word in words)


def _method_declaration_span(source, line_start, name):
    mask = _code_mask(source)
    line_end = source.find("\n", line_start)
    if line_end < 0:
        line_end = len(source)
    token_pattern = re.compile(
        r"(?<![\w.@])"
        + re.escape(name)
        + r"(?!\w)\s*(?:<[^>\n{};]+>\s*)?\("
    )
    for match in token_pattern.finditer(source, line_start, line_end):
        token_start = match.start()
        if not all(mask[index] for index in range(token_start, match.end())):
            continue
        if not _is_anchored_declaration_prefix(source[line_start:token_start]):
            continue
        open_parenthesis = source.rfind("(", token_start, match.end())
        close_parenthesis = _matching_delimiter(
            source,
            open_parenthesis,
            "(",
            ")",
            mask,
        )
        body_open = close_parenthesis + 1
        while body_open < len(source):
            if not mask[body_open] or source[body_open].isspace():
                body_open += 1
                continue
            break
        if body_open >= len(source) or source[body_open] != "{":
            continue
        body_end = _matching_body_end(source, body_open, mask)
        return token_start, close_parenthesis, body_open, body_end
    return None


def _is_method_declaration(lines, index, name):
    if not 0 <= index < len(lines):
        return False
    source = "\n".join(lines)
    line_start = sum(len(line) + 1 for line in lines[:index])
    try:
        return _method_declaration_span(source, line_start, name) is not None
    except AssertionError:
        return False


def extract_method(path, name, anchor):
    source = path.read_text(encoding="utf-8")
    lines = source.splitlines()
    anchor_line = int(anchor)
    assert 1 <= anchor_line <= len(lines), (name, anchor, len(lines))
    start = 0
    for _ in range(anchor_line - 1):
        newline = source.find("\n", start)
        assert newline >= 0, (name, anchor, anchor_line)
        start = newline + 1
    span = _method_declaration_span(source, start, name)
    assert span is not None, (name, anchor)
    _, _, _, end_offset = span
    end_line = source.count("\n", 0, end_offset) + 1
    assert end_line < len(lines), (name, anchor, end_line, len(lines))
    return "\n".join(lines[anchor_line - 1:end_line]), end_line, len(lines)


def method_body(path, name, anchor):
    body, _, _ = extract_method(path, name, anchor)
    return body


def audit_method_extractor():
    with tempfile.TemporaryDirectory() as directory:
        fixture = Path(directory) / "method-extraction-fixture.cs"
        fixture.write_text(
            "class Fixture {\n"
            "    public void Caller()\n"
            "    {\n"
            "        Target(\n"
            "            1\n"
            "        );\n"
            "    }\n"
            "    private void Target(int value)\n"
            "    {\n"
            "        return;\n"
            "    }\n"
            "    public void After() {\n"
            "    }\n"
            "}\n",
            encoding="utf-8",
        )
        lines = fixture.read_text(encoding="utf-8").splitlines()
        call_index = next(index for index, line in enumerate(lines) if "Target(" in line)
        assert not _is_method_declaration(lines, call_index, "Target")
        try:
            extract_method(fixture, "Target", call_index + 1)
        except AssertionError:
            pass
        else:
            raise AssertionError("invocation accepted as a method declaration")
        body, end_line, line_count = extract_method(fixture, "Target", 8)
        assert "private void Target" in body
        assert "public void After" not in body
        assert end_line < line_count
    print("ADVERSARIAL_CALL_ACCEPTED_AS_DECLARATION=0")
    print("ADVERSARIAL_CALL_EXTRACT_ACCEPTED=0")
    print("SEMANTIC_EXTRACTOR_DECLARATION_GATE=PASS")


def read_rows():
    rows = []
    for line in MAP_PATH.read_text(encoding="utf-8").splitlines():
        match = re.match(r"^\| (US-\d{3}) \| (.*?) \| (.*?) \| (.*?) \|$", line)
        if match:
            rows.append(match.groups())
    return rows


def audit_map():
    rows = read_rows()
    assert len(rows) == EXPECTED_STORY_MAP_ROWS
    assert [row[0] for row in rows] == [f"US-{index:03d}" for index in range(1, EXPECTED_STORY_MAP_ROWS + 1)]
    spec = SPEC_PATH.read_text(encoding="utf-8").splitlines()
    semantic_mismatches = []
    evidence_failures = []
    method_count = 0
    method_eof_extractions = 0
    us100_method_eof_extractions = 0
    for story_id, story, methods, evidence in rows:
        spec_match = re.search(r"Spec L(\d+)", story)
        assert spec_match, story_id
        spec_line = int(spec_match.group(1))
        assert re.search(r"\b" + str(int(story_id[3:])) + r"\.", spec[spec_line - 1]), story_id
        links = METHOD_LINK.findall(methods)
        assert links, story_id
        combined = methods
        for label, relative, anchor in links:
            method_count += 1
            source = (MAP_PATH.parent / relative).resolve()
            assert source.is_file(), (story_id, source)
            body, end_line, line_count = extract_method(
                source,
                label.rsplit(".", 1)[-1],
                anchor,
            )
            if end_line >= line_count:
                method_eof_extractions += 1
                if story_id == "US-100":
                    us100_method_eof_extractions += 1
            combined += " " + label + " " + body
        normalized = re.sub(r"\s+", "", combined.lower())
        if story_id in SPECIAL_ASSERTIONS:
            missing = [term for term in SPECIAL_ASSERTIONS[story_id] if term not in normalized]
            if missing:
                semantic_mismatches.append((story_id, missing))
        for _, relative, anchor in EVIDENCE_LINK.findall(evidence):
            target = (MAP_PATH.parent / relative).resolve()
            if not target.is_file() or anchor not in target.read_text(encoding="utf-8"):
                evidence_failures.append((story_id, relative, anchor))
    assert not semantic_mismatches, semantic_mismatches
    assert not evidence_failures, evidence_failures
    assert method_count == EXPECTED_METHOD_LINKS, (method_count, EXPECTED_METHOD_LINKS)
    assert method_eof_extractions == 0, method_eof_extractions
    assert us100_method_eof_extractions == 0, us100_method_eof_extractions
    return len(rows), method_count, method_eof_extractions, us100_method_eof_extractions


def audit_tracker_and_ledger():
    ledger = LEDGER_PATH.read_text(encoding="utf-8")
    readme = README_PATH.read_text(encoding="utf-8")
    current_marker = "## Current-world adversarial parser/native reconciliation — 2026-08-20"
    assert current_marker in ledger
    current = ledger.split(current_marker, 1)[1].split("\n<a id=", 1)[0]
    assert f"CURRENT_HEAD: `{CURRENT_HEAD}`" in current
    assert subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip() == CURRENT_HEAD
    assert f"CURRENT_NATIVE_EVIDENCE_ID={CURRENT_NATIVE_EVIDENCE_ID}" in current
    assert f"CURRENT_RELEASE_EVIDENCE_ID={CURRENT_RELEASE_EVIDENCE_ID}" in current
    assert f"CURRENT_RELEASE_DLL_SHA256: `{CURRENT_RELEASE_DLL_SHA256}`" in current
    assert f"PROJECT_DLL_SHA256={CURRENT_PROJECT_DLL_SHA256}" in current
    assert f"GAME_ASSEMBLY_SHA256={CURRENT_GAME_ASSEMBLY_SHA256}" in current
    assert f"GLOBAL_METADATA_SHA256={CURRENT_METADATA_SHA256}" in current
    assert "TARGET_GREEN=1/1" in current
    assert "FOCUSED_GREEN=5/5" in current
    assert "FULL_GREEN=1328/1328" in current
    assert "RELEASE_GREEN=0_WARNINGS_0_ERRORS" in current
    assert "CURRENT_EXPECTED_METHOD_LINKS=156" in current
    assert "STORY_MAP_METHOD_LINKS=156" in current
    assert "STORY_MAP_METHOD_EOF_EXTRACTIONS=0" in current
    assert "US100_METHOD_EOF_EXTRACTIONS=0" in current
    assert "METHOD_EXTRACTOR_REGRESSION=PASS" in current
    assert f"SEMANTIC_EVIDENCE_ID={CURRENT_SEMANTIC_EVIDENCE_ID}" in current
    assert f"PATH_REMOTE_ISOLATION_EVIDENCE_ID={CURRENT_PRESERVATION_EVIDENCE_ID}" in current
    assert f"FINAL_PRESERVATION_PATH_REMOTE_ISOLATION_AUDIT_EVIDENCE_ID={CURRENT_FINAL_AUDIT_EVIDENCE_ID}" in current
    assert "GAME_BEFORE_AFTER_CONTENT_UNCHANGED=1" in current
    assert "GAME_BEFORE_AFTER_METADATA_UNCHANGED=1" in current
    assert "GAME_BEFORE_AFTER_PATHS_UNCHANGED=1" in current
    assert "PRESERVATION_AUDIT_STATUS=PASS" in current
    assert "PATH_REMOTE_ISOLATION_ARTIFACT_AUDIT=PASS" in current
    assert "PRESERVATION_AUDIT_EXIT=0" in current
    assert "FINAL_PRESERVATION_PATH_REMOTE_ISOLATION_AUDIT=PASS" in current
    assert "SOURCE_MOUNT=/src:readonly" in current
    assert "GAME_MOUNT=/game:readonly" in current
    assert "ADVERSARIAL_CALL_ACCEPTED_AS_DECLARATION=0" in current
    assert "ADVERSARIAL_CALL_EXTRACT_ACCEPTED=0" in current
    assert "SEMANTIC_EXTRACTOR_DECLARATION_GATE=PASS" in current
    assert CURRENT_NATIVE_EVIDENCE_ID in current
    assert CURRENT_PRESERVATION_EVIDENCE_ID in current
    assert "/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly" in current
    assert CURRENT_PROJECT_DLL_SHA256 in current
    assert CURRENT_GAME_ASSEMBLY_SHA256 in current
    assert CURRENT_METADATA_SHA256 in current
    assert "53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300" not in current
    assert "573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb" not in current
    assert "ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5" not in current
    assert "dotabyss_x_cl" in ledger
    assert "dotabyss_xcl" not in ledger
    assert "repair-focused Docker GREEN is 5/5" in readme
    assert "full suite is 1328/1328" in readme
    assert "current final US-100 production behavior" in readme
    assert "exactly 156 method" in readme
    assert "adversarial invocation" in readme
    assert CURRENT_NATIVE_EVIDENCE_ID in readme
    assert CURRENT_RELEASE_EVIDENCE_ID in readme
    for issue in sorted(ISSUE_DIR.glob("*.md")):
        text = issue.read_text(encoding="utf-8")
        status = [line.lower() for line in text.splitlines() if line.startswith("**Status:**")]
        assert status and "complete" in status[0], issue
        assert "ready-for-agent" not in text.lower(), issue
        assert "unchecked" not in text.lower(), issue


def main():
    audit_method_extractor()
    rows, methods, method_eof_extractions, us100_method_eof_extractions = audit_map()
    audit_tracker_and_ledger()
    print("STORY_MAP_ROWS=" + str(rows))
    print("STORY_MAP_ORDER_UNIQUE=1")
    print("STORY_MAP_METHOD_LINKS=" + str(methods))
    print("EXPECTED_METHOD_LINKS=" + str(EXPECTED_METHOD_LINKS))
    print("STORY_MAP_METHOD_EOF_EXTRACTIONS=" + str(method_eof_extractions))
    print("US100_METHOD_EOF_EXTRACTIONS=" + str(us100_method_eof_extractions))
    print("METHOD_EXTRACTOR_REGRESSION=PASS")
    print("STORY_MAP_METHOD_ANCHOR_MISMATCHES=0")
    print("STORY_MAP_STORY_SEMANTIC_MISMATCHES=0")
    print("STORY_MAP_EVIDENCE_LINK_FAILURES=0")
    print("US005_US006_US019_US048_US093_US100_US101_US109_US115_US116=PASS")
    print("TRACKER_01_17_STATUS_AUDIT=PASS")
    print("LEDGER_CURRENT_IDS_AUDIT=PASS")
    print("SEMANTIC_MAP_AUDIT=PASS")


if __name__ == "__main__":
    main()
