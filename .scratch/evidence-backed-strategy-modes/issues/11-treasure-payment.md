# 11 — Execute Treasure payment without deadlock

**What to build:** Open an entered Treasure with a held key or the approved HP-payment exception, including the 80-percent variant, without pausing merely because an individual character may die.

**Blocked by:** 02 — Expand authoritative strategy evidence contracts; 03 — Project route-horizon safety and erosion recoverability.

**Status:** complete; implementation and existing evidence verified

Evidence: [`evidence-backed-strategy-modes-10-12-evidence.md`](../../../docs/agents/evidence-backed-strategy-modes-10-12-evidence.md); characterization coverage is also indexed in [`evidence-backed-strategy-modes-17-story-traceability.md`](../../../docs/agents/evidence-backed-strategy-modes-17-story-traceability.md).

- [x] A held Treasure key is always the first payment choice and exactly one key is committed.
- [x] Without a key, the exact 40- or 80-percent HP option is allowed when Treasure is the only terminal-reaching route or its exact reward is rank five.
- [x] HP payment projects each living character independently.
- [x] Individual character deaths are permitted; payment is rejected only when every currently living character would end at zero or below.
- [x] The ordinary configured HP soft floor cannot pre-empt this explicit exception.
- [x] Erosion payment is never substituted for the approved HP path.
- [x] Unknown Treasure variant, reward, cost, or content remains fail closed.
- [x] The payment identity and exact expected key, HP, and reward changes are reconciled as one owned transaction.
- [x] Tests cover held key, no key, 40 percent, 80 percent, partial deaths, full-party defeat, only-route transit, rank-five objective, and unknown data.
