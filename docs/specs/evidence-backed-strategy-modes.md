# Automation Behavior Specification

This is the normative contract for Equipment and Research automation. Terms are defined in the [glossary](../../CONTEXT.md); runtime boundaries and verification gates are defined in the [architecture guide](../design/autonether-architecture.md).

## Evidence and ownership

Decisions require current server state, native master data, party and ability models, buff rules, and exact popup/controller ownership. Hidden floors, translated descriptions, guessed probabilities or battle tiers, and stale snapshots provide no authority. Native Code power is permitted only as the bounded Equipment fallback below.

Missing evidence excludes the smallest dependent candidate, option, inventory row, or branch. Structural ambiguity or the absence of a proven legal action pauses automation. Every mutation uses the current native callback or task, occurs once, and is reconciled before another decision. Raw API fallbacks are outside this contract.

## Configuration and run boundaries

`StrategyMode` is explicit and defaults to `Equipment`. Validate settings before mutation: positive maximum depth, erosion limit 1–99, HP threshold 1–1000 permille, nonnegative reroll reserve, and defined enum values. Research requires a primary family and permits an optional secondary; Rush/Impact and Safe/Risk pairs are invalid.

- **Equipment:** target the first authoritative Boss at or above `MaximumDepth`, or the deepest Boss when the request exceeds the map. Start at the highest unlocked ten-floor elevator checkpoint below the target, using live recovery-floor authority; otherwise start at zero.
- **Research:** use `ResearchMaximumDepth` (default 70), resolved up to the next authoritative Boss, as the ceiling, and start the same way as Equipment. Settle at the next Boss when all configured research objectives are complete or Code capacity is reached; otherwise settle at the ceiling. Missing Boss evidence blocks the run.

Normal settlement preserves rewards. Ordinary Retreat and intentional defeat are not successful completion paths; Lost Signal is not a planned exit.

## Code offers

An offer requires an unambiguous portfolio, distinct candidate identities, and current popup ownership. Select, replace, reroll, and decline are separate owned transactions. Candidate-local rejection ends in native decline when the reroll policy is exhausted; it is not `BindingUnavailable`.

Both modes reject a proven incompatible uniform crest grant. Resolve the exact target scope and each living recipient's crest dependency; every recipient must match the granted crest. Mixed crests outside that scope are irrelevant. Crossing a Rush or Impact category crest threshold applies the same rule to the whole active party.

### Research

Research selects by configured family rather than combat power, general combat valuation, erosion payoff, or Equipment replacement value. A proven incompatible scoped or category crest grant remains disallowed; unavailable general combat valuation alone does not disqualify a structurally valid family candidate.

A family is complete at 20,000 wallet points, or when its wallet plus an authoritative projected normal settlement reaches 20,000. With an unknown projection, keep the earliest incomplete configured family active. Once the primary is complete, prefer an incomplete secondary and retain the primary as fallback.

For each offer below capacity:

1. Take the lowest-ID eligible Code from the active family.
2. Otherwise take the lowest-ID eligible Code from the other configured family.
3. Otherwise reroll once if a native reroll is available and this offer has not rerolled.
4. Otherwise decline.

`CodeReloadReserve` does not impose an Equipment-style reserve on this single Research reroll. At capacity, decline further offers without replacing held Codes and finish at the next Boss.

### Equipment

Apply identity and native-mechanic checks, hard exclusions, erosion and route safety, family and crest compatibility, target/trigger reachability, and native coexistence/caps before comparing value.

- **Spare capacity:** accept eligible new Codes without requiring strict improvement. Rank by complete combat-mechanism valuation when comparable, then native Code power (`MNetherCodes.power × native recipient count`) when richer valuation is unavailable, then stable Code ID.
- **At capacity:** evaluate each candidate/removal pair as a complete resulting portfolio. Require strict improvement. Native power may establish improvement only for an unquantifiable pair; it cannot override a proven zero or negative gain. Otherwise reroll under the configured reserve policy or decline.

Combat tiers, highest first: proven survival repair, useful back-row Force Chain payoff, rear-row or party-wide offense, nonessential rear-row or party-wide defense, then front-only fallback. Front-triggered global resources retain their global value. Lower tiers cannot offset failure in a higher tier.

Portfolio valuation includes trigger order, duration, charge caps, probability ladders, stack timelines, erosion conditions, category thresholds, and buff coexistence. A replaced buff is not assumed to resume without evidence. Neither spare capacity nor native power bypasses a hard gate.

## Route and erosion safety

Both modes require projected character HP, erosion through the next Boss, battle-entry identity, exact interaction costs, and committed Gold/key budgets to be safe before rewards are compared. A projected erosion value of 100 is always forbidden. Erosion is never raised solely for a Code's payoff.

Equipment rejects Risk effects that worsen future erosion, rely on unproven recovery, lack value at projected battle-start erosion, or require erosion of at least 70 without a complete safe horizon. Risk Research retains family-driven acquisition, but routing prefers the 50–70 band and requires authoritative recovery proof for combat above 70.

Compare complete visible branches using encounter vectors. Hidden, locked, unselectable, and unresolved nodes contribute no reward value. Before the late-shop boundary, priority is:

`Terminal Boss > rank-5 Treasure objective > Event Boss > Elite/Event MiniBoss > Direct Code Offer > Normal Battle > ordinary Event > Recovery > Shop`.

Equipment and completed Research put Normal Battle ahead of Direct Code Offer. Late Equipment prioritizes Red rank-5 Treasure, then Gold rank-5 Treasure or an eligible late shop, ahead of Event Boss and the remaining combat/event tiers. Equal Gold-objective counts favor Treasure over Shop; an ineligible shop stays below Recovery. Equal vectors prefer lower peak erosion, higher minimum active-character HP, then deterministic coordinates.

## Events and Recovery

Event valuation and execution use the same exact option policy. Bind event, part, option number, effects, costs, reward/battle, projected state, and branch commitments. Check binding, affordability, route safety, reserved budgets, and mode objective before deterministic option-number tie-breaking. Recheck the commitment before payment.

Unknown content excludes only its dependent option. Raw battle type or Code-drop ratio does not establish battle tier. Ordinary event damage must leave every currently living character above zero; the displayed character is not assumed to be the only target. Gold gains receive procurement value only for an already proven purchase on that branch.

Recovery chooses a deterministic rest or purification result that makes the complete visible branch safe. If both are safe, rest when any active character is below the HP threshold; otherwise purify when erosion is positive; otherwise choose a harmless deterministic option. Research never transforms Codes. Equipment transformation requires explicit opt-in, proven zero value for rest and purification, an exact hard-excluded held Code to sacrifice, and safe branch evidence.

## Treasure, keys, and shops

Use exactly one held key for an entered Treasure. Without a key, HP payment requires proof that the Treasure is a required terminal route or rank-5 objective and at least one living character survives. Only this explicit exception permits individual defeats; full-party defeat is always forbidden. Erosion payment is not a substitute.

A reachable rank-5 objective creates a one-key commitment on its own branch. Prefer reachable affordable currency sources; reserve key costs before optional bags. Buy a committed key first, then a 300-Gold bag only if the remaining balance permits it. Hidden or alternative destinations justify no purchases.

An HP-paid event key also requires the same rank-5 destination, no better reachable currency source, and survival of at least one current character. An erosion-paid key additionally requires no combat above 70 and certain recovery before the next battle. Rank-five identity requires a canonical predicate or authoritative typed provider, rather than display rank or raw rarity.

`ShopMode=Off` uses the native close flow. `EquipmentBags` buys only exact, selected, affordable inventory and reconciles each purchase. Late-shop priority requires floor greater than 90 and an affordable selected 300-Gold rank-5 Gold bag; possible inventory is insufficient.

## Checkpoints and battle lifecycle

Use exact native Continue, Finish, Return, and Boost callbacks, with one mutation per confirmed stage. Equipment continues below its target when a ticket is available. Research continues below its ceiling only while objectives remain incomplete and capacity is available. At a completion boundary, use Boss settlement; at Research capacity, finish instead of spending another ticket.

Checkpoint returns use owned inventory and `CheckpointPreserveItemIds`. Unknown item identity or ambiguous selection pauses before confirmation.

Observe the final native battle-start task, including optional F11 wrapping. Pending blocks progress; success permits one continuation; fault or cancellation pauses. AutoNether does not poll, cancel, or replay F11 work. Lease battle Auto/speed changes and restore them at terminal boundaries.

## Diagnostics and acceptance

Detailed logs identify mode/objective, snapshot and owner generation, candidate/option identity, first failing gate or typed unknown reason, comparison rationale, native action, reconciliation, and pause reason. Log decisions and transitions rather than per-frame polling. Distinguish offer decline from structural binding failure.

Acceptance requires startup compatibility against current packaged assemblies, catalog coverage of patches and reflected mutations, policy and lifecycle regression tests, the full Docker test suite, a warning-free Release build, product-isolation and binary audits, and byte-identical deployment. See the architecture guide for verification commands.

Automatic mode inference, guessed values, generic weighted DPS/EHP scores, additional controllers, mid-segment settlement without a native window, translation, F11 ownership, and a configuration UI are outside this contract.
