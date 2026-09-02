# Abyss AutoNether Strategy

This bounded context defines the language used to automate a Nether run while preserving game rewards, party viability, and authoritative state.

## Language

### Run objectives

**Equipment Run**:
The default run objective, which seeks the strongest safe retained Code portfolio while climbing to a Boss-aligned equipment target.
_Avoid_: Automatic mode detection, Research run

**Research Run**:
An explicitly configured run objective that accumulates Codes for one or two named families and settles at the next Boss once its objective or Code capacity is reached.
_Avoid_: Equipment run, inferred research intent

**Research Family Target**:
A primary or secondary Code Family named in configuration for a Research Run. The primary is considered before the secondary until its research objective is complete.
_Avoid_: Automatically selected family, Combat Lane

**Family Research Wallet**:
The persistent spendable research-point balance held separately for each Code Family.
_Avoid_: Code count, Code Capacity, lifetime tree progress

**Research Completion Threshold**:
The point at which a configured family's authoritative wallet plus any authoritative projected normal settlement reaches 20,000 points.
_Avoid_: Fixed Code count, category-skill threshold

**Conservative Research Priority**:
The rule that the earliest configured wallet below 20,000 remains active when future settlement points are unknown. Unknown projection never means complete.
_Avoid_: Guessing settlement, global pause, treating unknown as complete

**Active Research Family**:
The first configured family whose Research Completion Threshold is not authoritative. A completed primary becomes fallback while an incomplete secondary becomes preferred.
_Avoid_: Permanently pinned primary, combat-value winner

**Saturated Research Run**:
A Research Run whose Code Portfolio has reached Code Capacity. It declines further Code Offers and exits through the next Normal Settlement Window without replacing held Codes.
_Avoid_: Capacity replacement, arbitrary retreat

**Boss-Aligned Target**:
An authoritative Boss floor used as a valid run boundary because ordinary floors do not provide reward-preserving settlement.
_Avoid_: Arbitrary numeric stop, rounded-down depth

**Normal Settlement Window**:
The reward-preserving exit available after defeating a terminal map Boss.
_Avoid_: Ordinary Retreat

**Ordinary Retreat**:
The always-available exit that forfeits rewards other than items already transported. It is a loss fallback, not successful Research settlement.
_Avoid_: Normal Settlement Window

**Lost Signal**:
Insurance offered after an unplanned defeat. It may recover an accident but is never a planned settlement mechanism.
_Avoid_: Retreat alternative, intentional defeat

### Codes and portfolios

**Code Family**:
One of Rush, Impact, Safe, or Risk. Rush opposes Impact, and Safe opposes Risk.
_Avoid_: Crest Dependency, element

**Raw Family Count**:
The number of distinct positive-amount owned Codes in one family before opposing-family subtraction.
_Avoid_: Effective Family Count

**Effective Family Count**:
The Raw Family Count minus the opposing family's Raw Family Count, clamped at zero. Category-skill thresholds use this count.
_Avoid_: Total Code count, ability level

**Code Portfolio**:
The distinct positive-amount Codes currently retained by the run.
_Avoid_: Current Offer, displayed Code list only

**Code Capacity**:
The authoritative maximum size of the Code Portfolio for the current run.
_Avoid_: Fixed 22- or 25-Code limit

**Actual Combat Value**:
The contribution of a Code to the configured party's real recipients, triggers, timelines, caps, coexistence rules, and encounter horizon.
_Avoid_: Displayed combat power, generic weighted score

**Strict Portfolio Improvement**:
An Equipment mutation whose complete retained portfolio has greater Actual Combat Value than the current portfolio after every hard safety rule is applied.
_Avoid_: Candidate-only power increase, mandatory capacity fill

**Reachable-Unquantified Effect**:
An effect with a proven recipient and trigger path but no authoritative numeric cadence or magnitude projection. It may bootstrap an empty Equipment portfolio but cannot prove a later strict improvement.
_Avoid_: Unknown trigger, invented value

**Hard-Excluded Code**:
A Code whose proven mechanic violates an unconditional safety or compatibility rule and therefore cannot be selected for either objective that depends on that rule.
_Avoid_: Merely low-value Code, unknown candidate

**Card-Local Unknown**:
Missing evidence needed by one offered Code. It excludes only that candidate and does not make the popup binding unavailable.
_Avoid_: Whole-offer failure, structural binding drift

**Code Offer Decline**:
The native cancellation of an Offer after no legal candidate remains under the applicable reroll policy. It consumes the Offer normally and does not pause the run by itself.
_Avoid_: Binding failure, route rollback

**Opposed-Family Contamination**:
A Code Portfolio that already contains both sides of Rush/Impact or Safe/Risk. New decisions must not worsen the conflict.
_Avoid_: Deliberate mixed-family strategy

### Crests and combat mechanics

**Crest Dependency**:
The Passion or Impact crest identity a character's skill loop must preserve, obtain, or consume.
_Avoid_: Code Family, element

**Target Scope**:
The exact living party members reached by an effect, such as front, back, assist, or all-party recipients.
_Avoid_: Whole party by default, UI coverage count

**Mixed-Crest Scope**:
A Target Scope containing characters with different Crest Dependencies.
_Avoid_: Mixed party outside the target scope, mixed element

**Uniform Crest Grant**:
An effect that gives one crest identity to every member of its Target Scope. It is incompatible with a Mixed-Crest Scope because the wrong crest can disable a recipient's skill loop.
_Avoid_: Crest Payoff, ordinary parameter buff

**Crest Payoff**:
An effect triggered by receiving or consuming a crest without granting that crest itself.
_Avoid_: Uniform Crest Grant

**Category Crest Threshold**:
An Effective Family Count threshold that grants one crest identity to the whole active party. Crossing it is legal only when every recipient has the matching Crest Dependency.
_Avoid_: Row-only compatibility

**Crest Payoff Reachability**:
The proven provider-and-consumer path required before a Crest Payoff has value.
_Avoid_: Crest identity alone, self-provider requirement

**Native Buff Coexistence**:
The game's rule for combining, limiting, replacing, and removing effects in the same buff group.
_Avoid_: Summing every effect independently, dormant replaced buff

**Native Buff Timeline**:
The ordered windows in which retained effects trigger, coexist, expire, replace one another, or reach a native cap.
_Avoid_: Independent average uptime

**Party-Global Resource**:
A shared battle resource whose benefit is global even when a particular character triggers the grant.
_Avoid_: Per-character recipient assumption

**Rear-Row Priority**:
The Equipment preference for useful back-row or all-party effects over front-only effects, unless the front trigger contributes a proven Party-Global Resource.
_Avoid_: Equal row weighting

**Survival-Repair Proof**:
Authoritative evidence that a candidate changes an unsafe projected encounter into a survivable one.
_Avoid_: Defensive description percentage, assumed repair

**Erosion Situation Gate**:
A Code condition that activates only within a proven erosion range.
_Avoid_: Unconditional value

**Risk Research Band**:
The preferred 50–70 erosion range for a Risk-focused route when a complete recovery path is authoritative.
_Avoid_: Intentional erosion inflation

### Routes and interactions

**Authoritative Snapshot**:
The current server and native-game state used for one strategy decision.
_Avoid_: Cached UI assumption, pre-mutation snapshot reuse

**Route Safety Gate**:
The HP, erosion, ownership, and resource constraints a route must pass before reward priority is compared.
_Avoid_: Weighted safety score, reward compensating for lethality

**Visible Branch**:
A fully authoritative selectable path from the current frontier through the next terminal map Boss.
_Avoid_: Immediate node only, hidden future floors

**Visible-Branch Encounter Vector**:
The lexicographic sequence of proven encounter and reward tiers across a Visible Branch.
_Avoid_: Scalar route score, hidden-node value

**Confirmed Recovery Route**:
A Visible Branch that certainly reduces erosion or repairs HP before the next unsafe combat.
_Avoid_: Probable recovery, random outcome

**Event Choice Commitment**:
The exact Event, part, option, costs, effects, reward or battle, and projected state that justified entering an Event.
_Avoid_: Re-evaluating into an unrelated popup choice

**Authoritative Replan Boundary**:
The point after a confirmed mutation when all unexecuted route valuation is discarded and rebuilt from a fresh Authoritative Snapshot.
_Avoid_: Carrying a route across mutation

**Recovery Code Transform**:
A Recovery action that sacrifices a chosen held Code for a server-selected replacement. It is never deterministic Code Offer replacement.
_Avoid_: Research optimization, guaranteed upgrade

### Treasure, keys, and shops

**Rank-5 Treasure Objective**:
A proven reachable rank-5 equipment Treasure that justifies acquiring or reserving exactly one key on the same Visible Branch.
_Avoid_: Speculative Treasure, rarity guess

**Key Procurement Commitment**:
A branch-local obligation to obtain a key for a Rank-5 Treasure Objective before spending the reserved resources elsewhere.
_Avoid_: Cross-branch reservation, speculative key purchase

**Treasure Payment Priority**:
The rule that an entered Treasure uses a held key first and otherwise uses only an explicitly permitted HP payment that avoids full-party defeat.
_Avoid_: Erosion-payment substitution, requiring every character to survive

**Eligible Late Shop**:
A Shop beyond the late-floor boundary whose exact inventory and current resources prove an affordable rank-5 equipment purchase.
_Avoid_: Possible inventory, currency-blind Shop priority

**Committed Procurement Threshold**:
A Gold threshold created by a known purchase on the selected Visible Branch, not by a possible hidden or alternative destination.
_Avoid_: Speculative budget, cross-branch threshold
