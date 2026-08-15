# 16 — Make strategy decisions auditable and update-tolerant

**What to build:** Explain every strategy selection and local rejection with stable typed reasons while allowing unrelated proven choices to continue after a game update introduces unknown data.

**Blocked by:** 15 — Integrate strategy commitments with controller ownership.

**Status:** ready-for-agent

- [ ] Audit output identifies mode, active Research target, generation/owner/snapshot identity, and authoritative evidence version.
- [ ] Every candidate and option records its first failing hard gate or its selected semantic/combat tier.
- [ ] Portfolio comparisons record retained-Code identity and strict-improvement outcome without relying on displayed power.
- [ ] Route decisions record excluded branches, complete semantic vector, safety projection, resource commitments, and final tie break.
- [ ] Configuration, MasterData, trigger, buff-strategy, party-profile, inventory, and transaction unknowns retain exact reason codes.
- [ ] Unknown future Code mechanics reject only the dependent candidate.
- [ ] Unknown future Event, item, Shop, Treasure, category-skill, or battle rows reject only the dependent option or branch.
- [ ] Automation pauses only when no proven legal choice remains or transaction identity is ambiguous.
- [ ] Logs emit on decision and state transition, not every polling frame or repeated battle-result observation.
- [ ] Characterization tests prove deterministic diagnostics and safe continuation with a mix of known and unknown choices.
