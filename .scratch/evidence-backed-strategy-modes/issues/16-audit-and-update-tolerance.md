# 16 — Make strategy decisions auditable and update-tolerant

**What to build:** Explain every strategy selection and local rejection with stable typed reasons while allowing unrelated proven choices to continue after a game update introduces unknown data.

**Blocked by:** 15 — Integrate strategy commitments with controller ownership.

**Status:** implementation complete; post-push final-review repair in progress after the current single reviewer returned FAIL. Re-review is pending; no commit or push has been made for this repair.

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

The current repair addresses the final-review findings around Recovery
Transform eligibility versus deterministic Rest/Purification tie loss, exact
route loser characterization, durable ticket-17 traceability, and the current
US-019/US-093/US-115 semantic story mappings. Fresh native-backed focused GREEN
is 5/5, the valid in-repository full suite is 1325/1325, and the Release build
is 0 warnings/0 errors; the final isolation/audit gate is recorded in the
evidence ledger. The current single-reviewer re-review remains pending. Earlier
dual-reviewer PASS claims are superseded and are not a re-review completion
claim.
