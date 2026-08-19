# 15 — Integrate strategy commitments with controller ownership

**What to build:** Run every new strategy choice through the stable controller transaction model so no race, rebound, popup, or scene re-entry can execute a stale plan or duplicate a mutation.

**Blocked by:** 07 — Select and replace Equipment-mode Code offers; 08 — Complete Research-mode progression and settlement; 14 — Compare complete visible branches by semantic vector.

**Status:** complete; implementation and existing evidence verified

Evidence: [`evidence-backed-strategy-modes-13-15-evidence.md`](../../../docs/agents/evidence-backed-strategy-modes-13-15-evidence.md); characterization coverage is also indexed in [`evidence-backed-strategy-modes-17-story-traceability.md`](../../../docs/agents/evidence-backed-strategy-modes-17-story-traceability.md).

- [x] Strategy planning requires current generation, current controller owner, authoritative snapshot, and matching entered subscene.
- [x] The chosen action and its evidence are immutable before native mutation.
- [x] An Event commitment remains owned until exact update confirmation, then hands off only to its exact Code Offer, battle, or ordinary-reward child.
- [x] Code reroll, select, replace, keep, and decline remain one exact owned parent flow without replay.
- [x] Shop sequences, Treasure payment, Recovery, battle, continuation, and settlement each reconcile their exact expected mutation once.
- [x] Route planning cannot run behind an active popup, battle, continuation, or unresolved parent transaction.
- [x] Every confirmed mutation invalidates all unexecuted route valuation and forces a fresh plan from a new authoritative snapshot.
- [x] Continuation handoff, post-battle rebound, and ordinary scene re-entry use the same lifecycle evidence gate.
- [x] F12-off drains already owned mutations safely but starts no new strategy action.
- [x] Ambiguous ownership or mismatched commitments pause before further mutation rather than guessing or polling forever.
- [x] Controller end-to-end tests exercise Equipment and Research flows across Event, Code, battle, Sleep continuation, and result settlement with exact mutation counts.
