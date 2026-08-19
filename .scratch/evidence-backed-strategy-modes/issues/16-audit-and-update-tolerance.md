# 16 — Make strategy decisions auditable and update-tolerant

**What to build:** Explain every strategy selection and local rejection with stable typed reasons while allowing unrelated proven choices to continue after a game update introduces unknown data.

**Blocked by:** 15 — Integrate strategy commitments with controller ownership.

**Status:** complete; dual-reviewer convergence PASS (Standards PASS, Spec PASS)

- [x] Audit output identifies mode, active Research target, generation/owner/snapshot identity, and authoritative evidence version.
- [x] Every candidate and option records its first failing hard gate or its selected semantic/combat tier.
- [x] Portfolio comparisons record retained-Code identity and strict-improvement outcome without relying on displayed power.
- [x] Route decisions record excluded branches, complete semantic vector, safety projection, resource commitments, and final tie break.
- [x] Configuration, MasterData, trigger, buff-strategy, party-profile, inventory, and transaction unknowns retain exact reason codes.
- [x] Unknown future Code mechanics reject only the dependent candidate.
- [x] Unknown future Event, item, Shop, Treasure, category-skill, or battle rows reject only the dependent option or branch.
- [x] Automation pauses only when no proven legal choice remains or transaction identity is ambiguous.
- [x] Logs emit on decision and state transition, not every polling frame or repeated battle-result observation.
- [x] Characterization tests prove deterministic diagnostics and safe continuation with a mix of known and unknown choices.

Evidence: `docs/agents/evidence-backed-strategy-modes-16-17-evidence.md`.

Second Spec-axis repair closed the blocking findings: decision-audit
serialization is complete for candidates, branches, options, Codes, and
route bounds; unknown frontier nodes reject locally; typed source unknowns
survive safety finalization; configuration, trigger, and buff-strategy codes
remain distinct; and Recovery/Treasure have per-option characterization
coverage. Focused tests pass 29/29 and the clean full suite passes 1319/1319.
Persistent dual-reviewer convergence is PASS on both Standards and Spec axes.
