# Evidence-Backed Strategy Modes

This specification defines the current normative behavior of Equipment and Research automation. The domain vocabulary is defined in [`CONTEXT.md`](../../CONTEXT.md); runtime and compatibility boundaries are defined in [`docs/design/autonether-architecture.md`](../design/autonether-architecture.md).

## Authority

Strategy decisions use only the current authoritative server snapshot, native MasterData, live party models, native ability assets, native buff strategies, and popup/controller ownership.

The following are never decision authorities:

- displayed combat power;
- translated description text;
- hidden or future floors;
- guessed battle tiers, probabilities, cadence, or settlement points;
- stale snapshots or popup models from another owner generation;
- raw API requests that bypass the proven native client flow.

Missing evidence excludes the smallest dependent candidate, option, inventory row, or branch. Automation pauses only when no proven legal action remains or structural ownership is ambiguous.

## Configuration

`StrategyMode` is explicit and defaults to `Equipment`. AutoNether never infers Research intent from the party, research tree, Code list, or UI.

Research requires `ResearchPrimaryFamily` to be one of `Rush`, `Impact`, `Safe`, or `Risk`. `ResearchSecondaryFamily` may be `Unknown` or another valid family. Opposing Rush/Impact and Safe/Risk primary-secondary pairs are invalid.

Configuration is rejected before a run mutation when:

- maximum depth is less than one;
- soft erosion limit is outside 1–99;
- minimum character HP is outside 1–1000 permille;
- reroll reserve is negative;
- a configured enum value is unknown;
- Research has no primary family or uses an opposing pair;
- the requested run boundary cannot resolve to an authoritative Boss;
- the native compatibility precheck fails.

## Run boundaries

### Equipment

Equipment resolves `MaximumDepth` to the first authoritative Boss at or above the requested depth. A request beyond the current map resolves to its deepest authoritative Boss.

Equipment starts from the highest unlocked native checkpoint not above that target. If no positive checkpoint qualifies, it starts from floor zero.

### Research

Research starts from floor zero to preserve every Code opportunity. Its normal ceiling is the authoritative floor-70 Boss.

Research exits at the next Boss settlement when either:

- all configured research objectives are complete; or
- the Code Portfolio reaches Code Capacity.

If neither condition occurs, Research still settles normally at the floor-70 Boss. It never treats Ordinary Retreat as successful completion and never seeks defeat or proactively consumes a Lost Signal.

## Common Code Offer rules

Every Offer must have a complete, unambiguous portfolio and distinct candidate identities. Structural ambiguity pauses before mutation.

An acquisition must use the exact current popup owner and native callback/task. Select, reroll, replace, and decline each form one owned mutation followed by authoritative reconciliation.

A confirmed candidate-local unknown excludes only that candidate. If no legal candidate remains after the applicable reroll rule, AutoNether invokes native decline/cancel and continues; it does not report the decision as `BindingUnavailable`.

Uniform crest grants are a hard compatibility boundary in both modes:

1. Resolve the effect's exact Target Scope.
2. Resolve the Crest Dependency of every living recipient in that scope.
3. Accept only when every recipient requires the granted crest.

A mixed party outside the Target Scope does not invalidate the candidate. A front-only grant is legal when all living front recipients match even if the back row is mixed; an all-back grant is illegal when any living back recipient requires the opposite crest.

The same whole-party rule applies when an acquisition crosses the effective Rush or Impact category threshold that grants a crest to every living ally.

## Research Code Offer policy

Research is family-driven. General combat value, displayed power, MinimumErosion conditions, Risk payoff, and Equipment replacement value do not outrank a configured family candidate.

The only candidate-level safety exception specific to Research selection is a positively identified incompatible Uniform Crest Grant or category-threshold grant. Missing general combat-mechanic valuation does not disqualify an otherwise structurally valid configured-family candidate.

The active family is determined in configured order:

1. A wallet already at 20,000 points is complete.
2. An authoritative wallet plus authoritative projected normal settlement reaching 20,000 is complete.
3. When projection is unavailable, the earliest configured wallet below 20,000 remains active conservatively.
4. Once the primary is complete, an incomplete secondary becomes active and the primary becomes fallback.

For each non-saturated Offer:

1. Select the lowest-ID eligible candidate from the active family when present.
2. Otherwise select the lowest-ID eligible candidate from the other configured family when present.
3. Otherwise, when this Offer has not yet rerolled and at least one native reroll is available, reroll exactly once.
4. After that reroll, or when reroll is unavailable, decline the Offer.

`CodeReloadReserve` governs Equipment reserve behavior; it does not turn Research fallback into multiple rerolls.

At Code Capacity, Research does not replace any held Code, including a completed primary-family Code. It declines the Offer and marks the run for normal settlement at the next Boss.

## Equipment Code Offer policy

Equipment applies these gates in order:

1. candidate identity and native mechanic completeness;
2. unconditional hard exclusions;
3. erosion and route safety;
4. family and crest compatibility;
5. target and trigger reachability;
6. native buff coexistence and caps;
7. complete retained-portfolio Actual Combat Value;
8. deterministic Code ID tie break.

An empty Code Portfolio may select one `Reachable-Unquantified` candidate only when all hard gates pass and the acquisition removes no Code. The stable lowest Code ID wins among equivalent bootstrap candidates.

Once the portfolio contains a Code, `Reachable-Unquantified` evidence cannot prove a new acquisition or replacement. A candidate must have a complete mutation valuation and must strictly improve the retained portfolio.

At capacity, every possible candidate/removal pair is evaluated as the complete resulting portfolio. A replacement is legal only when it is structurally compatible and a strict improvement; otherwise the Offer is rerolled according to the configured reserve or declined.

Displayed power, raw family count, and spare capacity never force an Equipment selection.

### Combat ordering

After hard gates, Equipment uses this lexicographic order:

1. authoritative survival repair when the current route is unsafe;
2. useful back-row Force Chain payoff;
3. rear-row or all-party offense;
4. nonessential rear-row or all-party defense;
5. front-only fallback.

A front-row trigger retains full value when it grants a proven party-global resource. Lower tiers cannot compensate for failure in a higher tier through a weighted score.

Native buff coexistence, durations, trigger order, charge caps, probability ladders, stack timelines, erosion-linked values, and category-threshold deltas are evaluated over the complete retained portfolio. A displaced replacing buff is not assumed to resume without native evidence.

## Erosion and Risk

Route safety always applies regardless of strategy mode. An acquisition preference does not authorize an unsafe next battle.

Equipment rejects a Risk effect that:

- requires erosion at or above 70 without a complete safe horizon;
- worsens future erosion gain or reduction;
- depends on an unproven recovery path;
- has no authoritative value at the projected battle-start erosion.

A dedicated Risk Research objective may select its configured family without Equipment's value gates, but route planning still prefers the 50–70 band and avoids combat above 70 unless a complete authoritative recovery route makes the transition safe.

Erosion is never raised merely to increase a Code's theoretical payoff. A projected value of 100 is always lethal and ineligible.

## Route selection

Route Safety Gate removes lethal or structurally unknown branches before reward comparison. Safety includes:

- per-character HP projection;
- projected erosion through the next Boss;
- battle-entry ownership and identity;
- exact Event, Treasure, Shop, and Recovery costs;
- committed Gold and key budgets;
- any explicit Treasure or key-payment exception.

Safe branches are compared over their complete authoritative Visible-Branch Encounter Vector through the next terminal Boss. Hidden, locked, unselectable, or unresolved nodes contribute no value.

Before the late-shop boundary, incomplete Research uses:

```text
Terminal Boss
> known rank-5 Treasure objective
> Event Boss
> Elite / Event MiniBoss
> Direct Code Offer
> Normal Battle
> ordinary Event
> Recovery
> Shop
```

Equipment and completed Research swap `Normal Battle` ahead of `Direct Code Offer`.

Late Equipment uses:

```text
Terminal Boss
> known Red rank-5 Treasure
> known Gold rank-5 Treasure / eligible late Shop
> Event Boss
> Elite / Event MiniBoss
> Normal Battle
> Direct Code Offer
> ordinary Event
> Recovery
```

An ineligible Shop ranks below Recovery. A Gold Treasure/Shop tie favors Treasure to preserve Gold.

Equal encounter vectors prefer lower peak erosion, then higher minimum active-character HP, then deterministic coordinates.

## Events

An Event is valued and executed through the same exact option policy. Each option binds its Event, part, option number, effects, costs, reward or battle, projected state, and route-owned commitments.

Eligibility precedes reward priority:

1. exact master-data and popup binding;
2. sufficient resources;
3. Route Safety Gate;
4. committed budget preservation;
5. active mode objective;
6. deterministic option-number tie break.

Unknown content invalidates only the dependent option. Raw battle type or Code-drop ratio does not prove Boss, MiniBoss, Elite, or Normal semantics; an authoritative typed mapping is required.

Ordinary Event HP damage must leave every currently living character above zero. The popup presentation character is not treated as the sole target.

An Event Gold gain receives procurement-threshold value only when the same selected safe branch already proves the corresponding purchase. An Event cost may not consume Gold or keys reserved for a committed rank-5 Treasure objective.

The popup must still match the committed Event choice before payment. A mismatch stops before mutation.

## Recovery

Recovery first chooses the deterministic rest or purification result required to make a complete visible branch safe.

When both choices preserve safety:

- choose rest when any active character is below the configured HP soft floor;
- otherwise choose purification when erosion is above zero;
- when both have zero marginal value, choose a deterministic harmless option.

Research never uses random Code transform. Equipment may use it only when explicitly enabled, rest and purification both have zero value, and an exact hard-excluded held Code can be sacrificed.

## Treasure and key procurement

An entered Treasure uses exactly one held key when available.

Without a key, HP payment is legal only when the exact policy proves the Treasure is the required terminal route or a rank-5 objective, and the payment does not defeat every living character. Individual character defeat may be accepted only under this explicit exception; full-party defeat is always forbidden.

Erosion payment is not a substitute for the approved HP-payment path.

A known reachable rank-5 Treasure creates a branch-local key commitment. Currency sources are preferred when reachable and affordable; no key is bought for a hidden, alternative, or merely possible Treasure.

Reserved key budget precedes an optional 300-Gold bag. When a Shop can fulfill both commitments, buy the key first and then the bag only if the post-key balance permits it.

An HP-paid Event key requires the same proven rank-5 destination, absence of a better reachable currency source, and projected survival of at least one current character. An erosion-paid key additionally requires no combat above 70 and certain recovery before the next battle.

Rank-five identity requires the canonical predicate or an authoritative typed provider. Raw rarity and display rank do not prove it.

## Shop

`ShopMode=Off` leaves through the proven native close flow.

`EquipmentBags` purchases only exact, selected, affordable inventory. Every purchase is reconciled before the next purchase.

A Shop receives late-shop priority only strictly above floor 90 when current Gold and exact selected inventory prove an affordable 300-Gold rank-5 Gold bag. Possible relation inventory is insufficient.

## Checkpoints, continuation, and settlement

Checkpoint actions use the exact native Continue, Finish, Return, and Boost callbacks. One mutation is issued per confirmed stage.

Equipment continues while below its Boss-aligned target and a ticket is available. Research continues while below its ceiling, its configured objective is incomplete, and Code Capacity is not saturated.

At a Research capacity boundary, AutoNether finishes normally instead of spending another ticket. At any configured or Research completion boundary, it preserves rewards through the Boss result rather than Ordinary Retreat.

Returned checkpoint items follow exact owned inventory and `CheckpointPreserveItemIds`; unknown identity or an ambiguous selection pauses before confirmation.

## Battle ownership and F11

AutoNether observes the final native battle-start task. Optional F11 wrapping may keep that task pending, but AutoNether neither polls F11 state nor cancels or replays its request.

Pending blocks scene progress. Success continues exactly once. Fault or cancellation produces a named pause.

Battle Auto and speed changes are leased and restored on every terminal boundary.

## Audit requirements

Detailed logs expose:

- mode and active objective;
- snapshot and owner generation;
- candidate or option identity;
- first failing hard gate and typed unknown reason;
- selected tier and comparison rationale;
- native action and reconciliation result;
- pause reason when no legal action remains.

Logs record state transitions and decisions, not per-frame polling spam. Candidate-local decline must remain distinguishable from structural `BindingUnavailable`.

## Acceptance requirements

The implementation is acceptable only when:

- the startup native precheck passes against the current packaged game assemblies;
- every Harmony patch and reflected mutation is represented in the shared catalog;
- Research priority, single-reroll fallback, capacity settlement, and scoped crest compatibility tests pass;
- Equipment empty-portfolio bootstrap and strict nonempty improvement tests pass;
- route, Event, Recovery, Treasure, Shop, checkpoint, lifecycle, and reconciliation regressions pass;
- the full Docker test suite and warning-free Release build pass;
- product-isolation and Release binary audits pass;
- any deployed DLL is byte-identical to the verified artifact.

## Out of scope

- Automatic strategy-mode or Research-family detection.
- Displayed-power optimization or a generic DPS/EHP weighted score.
- Invented values for unknown effects, battles, Event outcomes, or hidden floors.
- A second controller or raw API fallback around the native transaction model.
- Mid-segment reward-preserving settlement where the game exposes no such window.
- Translation, F11 Auto-SL ownership, or a new in-game configuration UI.
