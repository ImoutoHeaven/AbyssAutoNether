from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[2]
MAP_PATH = ROOT / "docs/agents/evidence-backed-strategy-modes-17-story-traceability.md"
LEDGER_PATH = ROOT / "docs/agents/evidence-backed-strategy-modes-16-17-evidence.md"
SPEC_PATH = ROOT / "docs/specs/evidence-backed-strategy-modes.md"
README_PATH = ROOT / ".scratch/evidence-backed-strategy-modes/README.md"
ISSUE_DIR = ROOT / ".scratch/evidence-backed-strategy-modes/issues"

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


def method_body(path, name, anchor):
    lines = path.read_text(encoding="utf-8").splitlines()
    declarations = [
        index for index, line in enumerate(lines, 1)
        if re.search(r"\b" + re.escape(name) + r"\s*\(", line)
    ]
    assert declarations and int(anchor) in declarations, (name, anchor, declarations[:5])
    start = declarations[0] - 1
    end = len(lines)
    for index in range(start + 1, len(lines)):
        if re.match(r"^    \[(Fact|Theory)", lines[index]):
            end = index
            break
    return "\n".join(lines[start:end])


def read_rows():
    rows = []
    for line in MAP_PATH.read_text(encoding="utf-8").splitlines():
        match = re.match(r"^\| (US-\d{3}) \| (.*?) \| (.*?) \| (.*?) \|$", line)
        if match:
            rows.append(match.groups())
    return rows


def audit_map():
    rows = read_rows()
    assert [row[0] for row in rows] == [f"US-{index:03d}" for index in range(1, 126)]
    spec = SPEC_PATH.read_text(encoding="utf-8").splitlines()
    semantic_mismatches = []
    evidence_failures = []
    method_count = 0
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
            combined += " " + label + " " + method_body(source, label.rsplit(".", 1)[-1], anchor)
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
    return len(rows), method_count


def audit_tracker_and_ledger():
    ledger = LEDGER_PATH.read_text(encoding="utf-8")
    readme = README_PATH.read_text(encoding="utf-8")
    assert "semantic-story-corrections-20260820" in ledger
    assert "task16-17-semantic-repair-20260820-b" in ledger
    for job in ("j-6hh6mq", "j-7nz7iz", "j-ahk3ek", "j-zkuhk3", "j-3uatcs"):
        assert job in ledger, job
    assert "dotabyss_x_cl" in ledger
    assert "dotabyss_xcl" not in ledger
    assert "repair-focused Docker GREEN is 5/5" in readme
    assert "full suite is 1325/1325" in readme
    assert "2a1af0fbc8f2ed17a773dc228af68a41eaa3b4ddcbe7c4f00c5bd6110e0f275b" in ledger
    assert "DLL_PATH=/src/release/Release/net6.0/AutoNether.dll" in ledger
    for issue in sorted(ISSUE_DIR.glob("*.md")):
        text = issue.read_text(encoding="utf-8")
        status = [line.lower() for line in text.splitlines() if line.startswith("**Status:**")]
        assert status and "complete" in status[0], issue
        assert "ready-for-agent" not in text.lower(), issue
        assert "unchecked" not in text.lower(), issue


def main():
    rows, methods = audit_map()
    audit_tracker_and_ledger()
    print("STORY_MAP_ROWS=" + str(rows))
    print("STORY_MAP_ORDER_UNIQUE=1")
    print("STORY_MAP_METHOD_LINKS=" + str(methods))
    print("STORY_MAP_METHOD_ANCHOR_MISMATCHES=0")
    print("STORY_MAP_STORY_SEMANTIC_MISMATCHES=0")
    print("STORY_MAP_EVIDENCE_LINK_FAILURES=0")
    print("US005_US006_US019_US048_US093_US101_US109_US115_US116=PASS")
    print("TRACKER_01_17_STATUS_AUDIT=PASS")
    print("LEDGER_CURRENT_IDS_AUDIT=PASS")
    print("SEMANTIC_MAP_AUDIT=PASS")


if __name__ == "__main__":
    main()
