# AutoNether Domain Glossary

Shared terms for Nether automation. The [behavior specification](docs/specs/evidence-backed-strategy-modes.md) defines policy; this glossary defines meaning.

## Runs and objectives

| Term | Meaning |
|---|---|
| Equipment Run | A run that builds the strongest safe retained Code portfolio and climbs to a Boss-aligned equipment target. |
| Research Run | A run that collects Codes from one or two configured families and settles when its objectives, capacity, or ceiling are reached. |
| Research Family Target | A configured primary or secondary Code family; separate from combat-lane preference. |
| Family Research Wallet | The spendable research-point balance for one family, separate from Code count and lifetime tree progress. |
| Research Completion Threshold | 20,000 points from the wallet plus any authoritative projected normal settlement. |
| Conservative Research Priority | Keeping the first incomplete configured family active when future settlement points are unknown. |
| Active Research Family | The first configured family whose completion has not been established. A completed primary becomes fallback while the secondary is active. |
| Saturated Research Run | A run at Code capacity that declines further offers and settles at the next Boss without replacements. |
| Boss-Aligned Target | A known Boss floor that provides a reward-preserving run boundary. |
| Normal Settlement Window | The reward-preserving exit after a terminal map Boss. |
| Ordinary Retreat | An exit that forfeits rewards except items already transported. |
| Lost Signal | Defeat insurance for an accident, rather than a planned settlement method. |

## Codes and portfolios

| Term | Meaning |
|---|---|
| Code Family | `Rush`, `Impact`, `Safe`, or `Risk`. Rush opposes Impact; Safe opposes Risk. |
| Code Portfolio | The distinct owned Codes with positive amounts that the run currently retains. |
| Code Capacity | The current authoritative maximum portfolio size. |
| Raw Family Count | The number of distinct retained Codes in one family. |
| Effective Family Count | Raw count minus the opposing family's raw count, clamped at zero; used for category thresholds. |
| Actual Combat Value | A Code's contribution under the party's real recipients, triggers, timing, caps, coexistence rules, and encounter horizon. |
| Native Code Power | Master-data power multiplied by native recipient count; a fallback when richer combat valuation is unavailable. |
| Strict Portfolio Improvement | An at-capacity replacement that improves the complete retained portfolio after safety and compatibility checks. |
| Reachable-Unquantified Effect | A proven recipient and trigger path whose numeric magnitude or cadence is unavailable. |
| Hard-Excluded Code | A Code whose proven mechanic violates an unconditional safety or compatibility rule. |
| Card-Local Unknown | Missing evidence for one candidate; separate from a structural offer-binding failure. |
| Code Offer Decline | Native cancellation of an offer after the selection and reroll policy finds no legal candidate. |
| Opposed-Family Contamination | A portfolio containing both sides of an opposing family pair. |

## Crests and combat

| Term | Meaning |
|---|---|
| Crest Dependency | The Passion or Impact crest a character's skill loop requires; separate from Code family and element. |
| Target Scope | The exact living recipients of an effect, such as front, back, assist, or the whole party. |
| Mixed-Crest Scope | A target scope whose members have different crest dependencies. |
| Uniform Crest Grant | An effect that gives the same crest to every recipient in its scope. |
| Crest Payoff | An effect activated by receiving or consuming a crest, rather than granting it. |
| Category Crest Threshold | An effective family-count threshold that grants one crest to the whole active party. |
| Crest Payoff Reachability | A proven provider-and-consumer path that makes a crest payoff usable. |
| Native Buff Coexistence | The game's rules for combining, capping, replacing, and removing effects in a buff group. |
| Native Buff Timeline | The ordered windows in which effects trigger, coexist, expire, replace one another, or reach a cap. |
| Party-Global Resource | A shared battle resource, even when one character triggers its grant. |
| Rear-Row Priority | Preference for useful back-row or party-wide effects over front-only effects, with global-resource exceptions. |
| Survival-Repair Proof | Evidence that an effect makes an unsafe projected encounter survivable. |
| Erosion Situation Gate | An effect condition that requires a particular erosion range. |
| Risk Research Band | The preferred 50–70 erosion range for a Risk-focused route, subject to proven recovery and safety. |

## Routes and interactions

| Term | Meaning |
|---|---|
| Authoritative Snapshot | Current server and native-game state for one decision. |
| Route Safety Gate | HP, erosion, ownership, and resource constraints applied before reward comparison. |
| Visible Branch | A fully known, selectable path from the current frontier through the next terminal map Boss. |
| Visible-Branch Encounter Vector | Counts of proven encounter and reward tiers across a branch, compared in priority order rather than by a weighted score. |
| Confirmed Recovery Route | A branch that certainly repairs HP or reduces erosion before unsafe combat. |
| Event Choice Commitment | The exact event, part, option, costs, effects, outcome, and projected state that justified entering an event. |
| Authoritative Replan Boundary | A confirmed state change after which unexecuted route valuation is rebuilt from fresh evidence. |
| Recovery Code Transform | Sacrificing a held Code for a server-selected replacement; separate from deterministic offer replacement. |

## Treasure and procurement

| Term | Meaning |
|---|---|
| Rank-5 Treasure Objective | A proven reachable rank-5 equipment Treasure that justifies reserving or acquiring one key on the same branch. |
| Key Procurement Commitment | A branch-local obligation to obtain that key before spending the reserved resources elsewhere. |
| Treasure Payment Priority | Use a held key first; otherwise use only explicitly authorized HP payment that avoids full-party defeat. |
| Eligible Late Shop | A shop above floor 90 whose selected inventory and current Gold prove an affordable 300-Gold rank-5 Gold bag. |
| Committed Procurement Threshold | A Gold requirement for a known purchase on the selected branch. |
