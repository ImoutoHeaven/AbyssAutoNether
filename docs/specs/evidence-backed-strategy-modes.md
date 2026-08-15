# Evidence-Backed Equipment and Research Strategy Modes

Status: ready for tracker publication

## Problem Statement

Abyss AutoNether's lifecycle and transaction handling are now stable enough to climb reliably, but its strategy layer still makes decisions from a small set of generic settings and shallow proxies. It does not distinguish an early Research run from a late Equipment run, cannot use the server-authoritative family research wallet as a completion objective, and ranks Code offers primarily from structural family counts and displayed coverage instead of the configured party's real combat interactions.

That gap produces materially wrong choices. A uniform crest grant can overwrite the required crest of characters in a mixed row. A lower research-rate Code can overwrite a stronger technology rate. A card that looks strong on paper can have no reachable trigger, be saturated by a native cap, lose to native buff coexistence rules, or require an unsafe erosion state. Conversely, a back-row Force Chain payoff can be strategically excellent even when exact cadence is unavailable. At capacity, candidate-only power cannot determine whether replacing a held Code improves the retained portfolio.

Route selection has the same problem. The current planner filters basic HP and erosion safety, then orders immediate nodes by generic reward and erosion fields. It does not compare the complete visible branch to the next terminal Boss, resolve Event choices into their exact semantic value, reserve Gold for a known rank-5 Treasure key, recognize an eligible late Shop, or apply the approved Treasure HP-payment exceptions. A locally attractive option can therefore consume a committed resource or select a lower-value branch even when authoritative information already proves a better one.

The strategy must remain evidence-backed and fail closed without returning to the deadlocks that the lifecycle work removed. Missing evidence for one candidate or Event option must exclude only that choice. Every mutation must still run as one owned transaction, reconcile against an authoritative snapshot, and invalidate all unexecuted route valuation before planning again.

## Solution

Add two explicit strategy modes: Equipment and Research. Equipment is the default and maximizes actual combat value while safely climbing to a Boss-aligned equipment target. Research starts at floor zero, pursues explicitly configured primary and secondary Code families using the server-authoritative research wallet plus projected normal settlement, and exits through the next terminal Boss settlement once its objective is complete, with the floor-70 Boss as a hard ceiling.

Deepen the existing Code, Event, Recovery, Treasure, Shop, checkpoint, and route-policy seams rather than adding a second controller. Enrich their immutable inputs with the authoritative party combat profile, Code effect semantics, native buff strategies, family-wallet state, technology research rates, category-skill rows, exact visible Event and inventory rows, and resource commitments. Keep execution in the existing controller transaction model.

Use lexicographic decision pipelines. Safety, hard exclusions, family compatibility, and exact binding always precede objectives. Research then prioritizes its active family and settlement invariant. Equipment then applies the approved combat-tier order and compares the actual marginal value of the complete retained portfolio. Route planning first removes unsafe branches, then compares complete visible-branch encounter vectors, and uses erosion and HP only as equal-vector tie breaks.

## User Stories

1. As a user, I can select `Equipment` or `Research` explicitly so the plugin never guesses my run objective.

2. As a user, a new installation defaults to `Equipment` so ordinary deep-climb behavior remains the safe default.

3. As a user, I can configure a primary Research Code Family from Rush, Impact, Safe, or Risk.

4. As a user, I can configure an optional secondary Research Code Family without enabling automatic family detection.

5. As a user, an opposing primary and secondary pair is rejected before a run starts so Research cannot deliberately mix Rush with Impact or Safe with Risk.

6. As a user, a Rush or Impact Research target that must cross effective family count five is rejected when any active character has the opposite crest dependency.

7. As a user, a nearly complete Rush or Impact Research wallet may proceed below effective count five when projected completion does not require crossing that threshold.

8. As a user, Equipment resolves its stopping target to the first authoritative Boss at or above my requested positive depth.

9. As a user, a requested depth beyond the current map resolves to the deepest authoritative Boss instead of a hard-coded floor number.

10. As a user, an unresolvable Boss target prevents startup instead of producing a mid-segment stop.

11. As a Research user, every run starts from the native floor-zero entry to maximize Code opportunities.

12. As an Equipment user, the run starts from the highest currently unlocked native checkpoint that does not exceed the Boss-aligned target.

13. As a Research user, completion is determined from the configured family's persistent wallet plus projected normal settlement reaching 20,000 points.

14. As a Research user, completion is never inferred from a fixed Code count, Code capacity, or category-skill gauge.

15. As a Research user, the primary family remains the active target until its projected settlement threshold is met.

16. As a Research user, all available offer rerolls are spent before accepting a valid secondary-family fallback while the primary remains incomplete.

17. As a Research user, the secondary family becomes active only after the primary is projected complete.

18. As a Research user, a previously completed family is re-evaluated after every acquisition or replacement and may not be sacrificed below completion.

19. As a Research user, once all configured targets are complete, ordinary Equipment combat value governs later offers until settlement.

20. As a Research user, completion causes the run to continue to the next terminal map Boss and use the normal reward-preserving settlement window.

21. As a Research user, the run never treats ordinary Retreat as successful completion because that forfeits rewards.

22. As a Research user, the run settles normally at the floor-70 Boss even when the research objective remains incomplete.

23. As a user, automation never intentionally loses or proactively consumes a Lost Signal to settle a run.

24. As a user, Code-family counts use distinct positive-amount owned Codes and authoritative opposing-family subtraction rather than ability level or possession amount.

25. As a user, existing opposed-family contamination is repaired by retaining the configured Research side or the greater actual-combat-value Equipment side.

26. As a user, automation never adds the opposing family merely to reduce an incompatible effective family count.

27. As a user, a row-wide uniform crest grant is rejected whenever its target row contains mixed crest dependencies.

28. As a user, a Rush or Impact acquisition that would activate the whole-party count-five crest grant is rejected unless every active character has the matching crest dependency.

29. As a user, an already active incompatible count-five crest grant pauses before battle unless the current offer can deterministically repair it below the threshold.

30. As a user, a crest payoff is valued only when an authoritative provider-and-consumer path can reach its recipients.

31. As a user, a card with an unknown trigger path is rejected without preventing other proven cards in the same offer from being considered.

32. As an Equipment user, a back-row Force Chain payoff receives high qualitative priority when it is a numerical payoff and the party has the supported Force Chain path.

33. As an Equipment user, a corresponding front-row Force Chain payoff remains a fallback rather than sharing back-row priority.

34. As a user, any future Force Chain card that grants a uniform crest must still pass the mixed-row and count-five compatibility rules.

35. As an Equipment user, a survival-repairing rear-row or full-party effect outranks offense when the party is below an authoritative survival threshold.

36. As an Equipment user, once survival is adequate, back-row Force Chain payoff outranks ordinary rear-row or full-party offense.

37. As an Equipment user, ordinary rear-row or full-party offense outranks nonessential rear-row or full-party defense.

38. As an Equipment user, usable rear-row effects outrank front-row-only effects, with front-row effects selected only as fallbacks.

39. As an Equipment user, a front-row trigger that injects a genuinely shared party resource keeps its full party-global value rather than being discounted as a front-only effect.

40. As an Equipment user, defensive alternatives with the same recipients are compared by exact relative effective-HP change rather than description percentage.

41. As an Equipment user, defensive alternatives with different recipients prioritize rear-row coverage, the weakest covered rear character, and then aggregate gain.

42. As an Equipment user, combat utility is evaluated against Boss encounters so periodic effects are not discarded merely because ordinary fights are short.

43. As an Equipment user, native damage relationships are used only when every required party, enemy, and effect input is authoritative; missing inputs do not receive invented weights.

44. As an Equipment user, displayed combat power is audit information only and never decides Code selection or replacement.

45. As a user, native buff coexistence is evaluated from the active strategy for each buff type, including Allow limits and HigherValue replacement behavior.

46. As a user, a displaced weaker HigherValue effect is not assumed to resume after the stronger effect expires.

47. As a user, durations and trigger ordering are compared as a portfolio timeline rather than independent average uptimes.

48. As a user, critical-probability value is clipped only after the native guaranteed-critical threshold is reached.

49. As a user, continuous-attack probability is valued across the complete finite probability ladder rather than using the critical-probability cap.

50. As a user, shared mana, initial skill charge, and recurring skill-charge efficiency use their separate native caps and timelines.

51. As a user, an additional charge card is rejected only when its marginal contribution for all applicable recipients is zero.

52. As a user, stack-linked effects require a proven per-character stack timeline or a guaranteed conservative lower bound; maximum text is not treated as full uptime.

53. As a user, erosion-linked effects are valued at the projected erosion of each confirmed combat through the next Boss instead of their maximum description value.

54. As a user, crossing a category-skill threshold on the current acquisition or replacement contributes its immediate proven delta, but mere proximity to a future threshold has no speculative value.

55. As an Equipment user, a reachable but unquantified effect receives no invented numeric magnitude and cannot by itself prove a strict replacement improvement.

56. As a Research user, a reachable but unquantified active-family Code may still contribute to settlement progress when it passes every safety and compatibility rule.

57. As an Equipment user, a zero- or negative-marginal candidate is rerolled according to the configured reserve and otherwise declined even when capacity is available.

58. As an Equipment user, a full portfolio replaces a held Code only when the retained portfolio is a strict actual-combat improvement.

59. As a Research user at capacity, replacement removes a hard-excluded Code first, then an opposed-family Code, then an ordinary non-target Code, then a provable surplus from a completed family.

60. As a Research user, an active-target Code is never removed for a non-target candidate.

61. As a Research user, a same-family replacement must preserve family contribution and improve actual combat value.

62. As a user, a direct Code Offer remains a real route opportunity at capacity because the later offer can be rerolled, replaced, or declined.

63. As a user, declining every candidate consumes the Code Offer normally and never attempts a route rollback.

64. As a user, a lower or equal research-rate Code is rejected because it can overwrite rather than add to the technology rate.

65. As an Equipment user, every research-rate Code is rejected because it adds no combat value.

66. As a Research user, a research-rate Code is accepted only when it matches the active family and its authoritative rate is strictly greater than the current technology rate.

67. As a user, current Risk Codes 40010 through 40019 are hard excluded because they require erosion at or above 70.

68. As a user, current Risk Code 40024 is hard excluded because it worsens future erosion gain and reduction.

69. As a user, Risk Codes 40022 and 40023 are eligible only when projected battle-start erosion stays within 50–70 and the visible route proves recovery.

70. As a user, other linear high-erosion Risk Codes are valued at actual projected erosion below 70 and never justify intentionally raising erosion.

71. As a dedicated Risk Research user, the planner prefers the 50–70 erosion band and generally avoids exceeding 70.

72. As a user, a transient value above 70 is allowed only when the authoritative visible route proves no unsafe battle and certain recovery.

73. As a user, reaching 70 without a confirmed recovery route pauses before the next mutation so I can decide manually.

74. As a user, route safety removes lethal HP or erosion branches before any encounter reward is compared.

75. As a user, once branches pass safety, the planner does not minimize erosion ahead of encounter value.

76. As a user, route comparison evaluates the complete authoritative visible branch through the next terminal map Boss.

77. As a user, hidden, locked, unselectable, or unresolved nodes contribute no speculative reward.

78. As a Research user with an incomplete family before the late-shop boundary, safe branches follow `Terminal Boss > known rank-5 Treasure objective > Event Boss > Elite/Event MiniBoss > Direct Code Offer > Normal Battle > ordinary Event > Recovery > Shop`.

79. As an Equipment user, or after Research completion, the pre-boundary order places Normal Battle before Direct Code Offer.

80. As an Equipment user above floor 90, safe branches follow `Terminal Boss > known Red rank-5 Treasure > known Gold rank-5 Treasure or eligible late Shop > Event Boss > Elite/Event MiniBoss > Normal Battle > Direct Code Offer > ordinary Event > Recovery`.

81. As an Equipment user, a direct tie between a known Gold rank-5 Treasure and an eligible late Shop favors the Treasure to preserve 300 Nether Gold.

82. As a user, an Event contributes the exact semantic tier of the option the Event policy would select, including Boss, MiniBoss, Normal Battle, direct Code Offer, or ordinary reward.

83. As a user, an Event Boss is valued as a nonterminal Boss-grade encounter and never mistaken for a normal settlement window.

84. As a user, an Event MiniBoss shares the Elite tier and an Event Normal Battle shares the Normal Battle tier.

85. As a user, a missing or stale Event battle row rejects only that option rather than the entire Event.

86. As a user, exact Event choices first satisfy binding, resources, route safety, and committed budgets; only then do they apply the active mode objective and deterministic option-number tie break.

87. As a user, Items, Nether Gold, keys, Code Offers, and battles are compared by their approved semantic rules rather than a generic benefit count.

88. As an Equipment user, ordinary Event rewards prioritize exact Red rank-5 and Gold rank-5 bags, then exact committed procurement thresholds, direct Code Offer, uncommitted Gold, and lower-rank bags.

89. As a Research user with incomplete targets, a mandatory known-rank-5 key objective remains first and otherwise a direct Code Offer outranks ordinary Gold and item rewards.

90. As a user, an Event resource gain receives threshold value only when the same selected visible safe branch already proves a reachable 200-, 300-, or 500-Gold purchase before its Boss.

91. As a user, ordinary Event HP damage must leave every currently living character above zero.

92. As a user, the Event popup presenter is not treated as the sole HP target; exact HP effects are projected against every living party character.

93. As a user, ordinary Event erosion increases require full visible-route recoverability rather than a local post-choice check.

94. As a user, ordinary Event Gold costs preserve committed key and bag budgets.

95. As a user, an Event option requiring unknown future content is rejected while other exact options remain eligible.

96. As a user, an entered Recovery chooses the deterministic HP or erosion repair needed to make a complete visible branch safe.

97. As a user, when rest and purification both preserve safety, Recovery chooses rest if any active character is below the HP soft floor, otherwise purification when erosion is above zero.

98. As a user, when both deterministic Recovery choices have zero marginal value, the plugin chooses a harmless deterministic option.

99. As a Research user, random Code transformation at Recovery is always rejected.

100. As an Equipment user, random Code transformation is disabled by default and can be opted into only for removing a hard-excluded Code when rest and purification have zero value.

101. As a user entering Treasure with a key, the plugin spends exactly one key.

102. As a user entering Treasure without a key, the plugin chooses the 40- or 80-percent HP payment when the Treasure is the only terminal route or its exact reward is rank five.

103. As a user, Treasure HP payment may defeat individual characters and is forbidden only when every currently living character would end at zero or below.

104. As a user, Treasure never substitutes an erosion-payment option for the approved HP-payment path.

105. As a user, a known reachable rank-5 Treasure without a held key creates a mandatory key-procurement objective ahead of nonterminal combat but below safety and terminal Boss.

106. As a user, a key is bought for 150 Gold at the exact Event or 200 Gold at the exact Shop only when the same visible branch proves the rank-5 Treasure.

107. As a user, if no permitted key source is reachable or affordable, the plugin uses the approved Treasure HP payment rather than pausing.

108. As a user, a known rank-5 Treasure reserves 200 Gold for its Shop key ahead of an eligible 300-Gold late-shop bag.

109. As a user with at least 500 Gold, the Shop buys the 200-Gold key first and then the 300-Gold bag when both commitments are proven.

110. As a user with 300–499 Gold and a committed rank-5 Treasure key need, the Shop skips the 300-Gold bag and buys the key.

111. As a user, an Event's 80-percent-HP key option is used only for a known rank-5 Treasure, when no better currency source exists and the full party survives as a group.

112. As a user, an Event's 80-point-erosion key option is used only when no battle occurs above 70 and recovery to 70 or below is certain before the next battle.

113. As an Equipment user, a Shop receives late-shop priority only strictly above floor 90, with at least 300 Gold and exact selected inventory containing a 300-cost rank-5 Gold bag.

114. As a user, an ineligible Shop remains legal only as necessary safe transit and otherwise ranks below Recovery.

115. As a user, low-rarity Treasure does not cause a voluntary early detour or key spend, except for a key proven to expire unused at the final reachable opportunity.

116. As a user, equal visible-branch encounter vectors are broken by lower peak erosion, then higher minimum active-character HP, then deterministic coordinates.

117. As a user, selecting an Event commits the exact Event, part, option, effects, reward or battle, and projected state used to justify that route.

118. As a user, any popup mismatch with the committed Event stops before payment instead of silently choosing a different option.

119. As a user, a committed Event remains owned until its exact update confirms and then hands off to its exact Code Offer, battle, or ordinary-reward child.

120. As a user, every confirmed Event, Code, Shop, Treasure, Recovery, battle, continuation, or settlement mutation invalidates all unexecuted route valuation.

121. As a user, route planning resumes only from a fresh authoritative snapshot after the current transaction reaches terminal confirmation or an exact downstream handoff.

122. As a user, a strategy decision is bound to the current generation, current controller owner, authoritative snapshot, and corresponding entered subscene before it may mutate native state.

123. As a user, a missing card-specific fact rejects only that candidate; a missing option-specific fact rejects only that option.

124. As a user, automation pauses only after no proven legal choice remains after the applicable reroll or fallback policy is exhausted.

125. As a user diagnosing a decision, logs expose deterministic reason codes, the authoritative input identity, excluded alternatives, active objective, and selected semantic tier without polling spam.

## Implementation Decisions

1. Extend the existing strategy settings with an explicit mode, primary and optional secondary Research families, and an Equipment-only opt-in for random Recovery transformation. Remove automatic lane inference from decision-making; legacy automatic-lane configuration migrates to explicit defaults and is never used to infer Research intent.

2. Validate all cross-setting invariants before starting or resuming automation: known mode, valid family values, non-opposed Research targets, category crest-threshold compatibility, resolvable Boss-aligned target, and complete authoritative inputs required by the selected mode.

3. Resolve Equipment's target from live map and floor MasterData. Normalize a requested positive non-Boss depth upward to the next Boss and cap an out-of-range request at the deepest authoritative Boss. Resolve Research's ceiling independently as the authoritative floor-70 Boss.

4. Derive the mode start floor rather than exposing another manual setting. Research uses floor zero. Equipment uses the highest live unlocked checkpoint at or below its resolved target.

5. Expand the immutable strategy snapshot instead of letting policy code read Unity objects or stale reflection state. It carries generation and owner identity, server snapshot identity, the active party combat profile, owned Codes, capacity, rerolls, family wallets, projected settlement inputs, technology research rates, category-skill rows, effect models, buff strategies, and exact visible map semantics.

6. Build the party combat profile from the live party model owned by the current Code-offer flow. Capture position, element, ManaType, level, limit break, HP state, native parameter inputs, and character, equipment, and general ability-effect models. Displayed target coverage is retained only for diagnostics.

7. Decode Code semantics from current MasterData and ability assets into typed mechanics. Runtime behavior remains authoritative across updates; known current Code identifiers are regression evidence, not the only classification mechanism.

8. Keep one Code decision pipeline with ordered gates: structural validity, hard exclusions, erosion safety, family compatibility, trigger reachability, active Research objective or Equipment tier, portfolio marginal value, and deterministic identifier tie break. A later stage cannot compensate for failure in an earlier stage.

9. Model Rush/Impact and Safe/Risk as opposing pairs. Effective count is distinct owned Codes on one side minus distinct owned Codes on its opponent, clamped at zero. Evaluate category-skill threshold deltas against the resulting complete portfolio.

10. Classify uniform crest grants separately from crest payoffs. Use ManaType for crest identity and a provider-consumer graph for trigger reachability. Enforce row-level compatibility for offer grants and whole-party compatibility before crossing the count-five category grant.

11. Repair an already incompatible count-five state only through a deterministic replacement that lowers the effective count. If the current offer cannot repair it, emit a safety pause before combat.

12. Implement native buff coexistence as a portfolio simulation driven by the active buff strategy. Respect grouping, Allow limits, HigherValue comparison, disable duration, removal, trigger order, and overlapping windows. Never revive a displaced effect without native evidence.

13. Use mechanism-specific marginal-value models. Separate shared mana, initial charge, recurring charge efficiency, critical probability, continuous attacks, stack-linked effects, erosion-linked effects, parameter chains, Force Chain messages, and category-skill threshold deltas.

14. Keep Equipment comparison lexicographic. Survival repair comes first when needed; otherwise back-row Force Chain payoff, rear/full-party offense, nonessential rear/full-party defense, and front-only fallbacks follow in order. The party-global resource exception is applied only to a proven shared resource.

15. Compare Equipment replacement outcomes as complete retained portfolios. Require a strict improvement and decline a non-positive candidate even below capacity. Reachable-unquantified mechanics can retain a documented qualitative tier but never receive invented numeric cadence or prove a magnitude-only replacement.

16. Implement Research as a settlement objective separate from combat value. Determine the active family from wallet plus projected normal settlement, consume all rerolls while it remains incomplete, preserve every completed-family invariant, and apply the approved deterministic replacement order at capacity.

17. Compare a Research-rate Code against the authoritative current technology rate using overwrite semantics. Reject unknown, equal, lower, wrong-family, and all Equipment-mode cases.

18. Treat the current Risk identifier set as characterization coverage while classifying effects from their runtime gates. Always reject 70-plus gated cards and the adverse erosion-adjustment card. Project conditionally eligible and linear Risk cards across confirmed battle-start erosion on the visible horizon.

19. Deepen the current production route-safety context with exact semantic branch entries rather than replacing its safety coordinator. Each safe frontier carries its complete visible-branch encounter vector, projected peak erosion, minimum active-character HP, exact Event choice, resource commitments, and selected battle projection.

20. Apply safety once as a hard eligibility filter. Include ordinary all-character HP survival, the narrow Treasure and HP-paid-key full-party exceptions, the hard lethal erosion boundary, the 70-point recoverability policy, and exact battle-entry evidence. Do not reuse projected erosion delta as a reward comparator after safety passes.

21. Compare branches lexicographically through the next terminal map Boss using the active mode's semantic order. Count like-tier encounters across the whole visible branch, prefer fewer Recovery nodes only after semantic equality, then apply erosion, HP, and deterministic-coordinate tie breaks.

22. Resolve a visible Event through the same Event option policy that will execute after entry. Store the resulting exact option and projection as the route commitment. Hidden or unresolved Event data contributes no value.

23. Replace generic Event scoring with ordered eligibility and mode-objective stages. Evaluate all exact effects on an option, reject only the failing option, preserve committed budgets, and use option number only as the final tie break.

24. Project Event HP effects across every living character. Treat the popup character identifier only as presentation evidence because the authoritative update operates on party statuses without a target character identifier.

25. Represent rank-5 Treasure access and key procurement as explicit commitments tied to one selected visible safe branch. Reserve currency only for an exact destination before the terminal Boss; clear or recompute the commitment after every mutation.

26. Execute sequential Shop purchases in commitment order: required key first, then an eligible rank-5 bag if funds remain. Validate exact inventory, cost, content, and post-purchase snapshot after each child mutation.

27. Encode Treasure payment as its own exception-bearing policy. Prefer a held key; otherwise allow the exact HP option under the approved only-route or rank-5 objective rules, and require only group survival. Never choose erosion as a substitute.

28. Make Recovery evaluate the next complete visible branch. Prefer the deterministic repair needed for safety, use the HP/erosion tie policy when both are safe, and isolate random transform behind its Equipment-only opt-in.

29. Preserve the existing owned transaction and reconcile architecture. A committed mutation remains immutable until terminal confirmation or an exact child handoff. After confirmation, discard all remaining route valuation and rebuild from a fresh authoritative snapshot.

30. Require the common lifecycle evidence gate for continuation handoff, post-battle rebound, ordinary scene re-entry, and strategy execution: current generation, new controller owner, authoritative snapshot, and matching entered subscene.

31. Add typed audit reason codes for configuration rejection, candidate and option exclusion, safety failure, active Research objective, combat tier, portfolio delta, route-vector comparison, resource commitment, transaction identity, and final selection. Log state transitions and decisions, not per-frame polling.

32. Preserve fail-closed locality. Unknown mechanics invalidate the smallest candidate, option, inventory row, or branch that depends on them. Pause only when the full legal choice set is exhausted or when transaction identity itself is ambiguous.

## Testing Decisions

1. Use the production controller with its runtime-bridge test double as the highest acceptance seam. End-to-end scenarios must drive authoritative snapshots through floor selection, popup ownership, Code selection or decline, optional battle, reconciliation, continuation, and normal Boss settlement while asserting exactly one mutation per committed stage.

2. Keep the production route-safety wiring as the highest pure route seam. Tests provide one immutable snapshot plus runtime safety, party, Event, Shop, Treasure, wallet, and semantic evidence, then assert the selected node, complete audit, immutable planned action, and captured battle or interactive commitment.

3. Use the Code policy seam for exhaustive candidate and replacement matrices. Cover both modes, all family pairs, mixed and homogeneous rows, count-five crossings and repair, trigger reachability, Risk exclusions, research-rate overwrite, reroll rules, capacity replacement, zero marginal value, and card-local evidence failure.

4. Use the Event, Recovery, Treasure, and Shop policy seams for option-local matrices. Cover all-character HP projection, partial-death exceptions, 80-point erosion semantics, deterministic Recovery behavior, exact content binding, sequential Shop budgets, late-shop eligibility, unknown-option locality, and deterministic tie breaks.

5. Use route-planner tests for complete-branch lexicographic comparisons. Include the approved pre-90 Research, pre-90 Equipment, and late Equipment orders; exact Event semantic substitution; like-tier counts; rank-5 key procurement; late Shop/Treasure ties; recovery transit; and erosion/HP tie breaks.

6. Extend configuration-contract tests to assert the default Equipment mode, explicit family parsing, absence of automatic mode inference, invalid opposed targets, transform opt-in default, Boss normalization, dynamic deepest Boss, and mode-derived start floor.

7. Add MasterData and native-mapper characterization fixtures for the decompiled mechanics on which policy depends: wallet threshold and settlement fields, Code capacity, opposing-family thresholds, whole-party crest grants, Risk gates and erosion adjustment, research-rate overwrite, buff coexistence, probability ladders, charge caps, Force Chain activation, Event HP scope, key costs, Treasure payment parameters, shop inventory, and Boss identities.

8. Test update tolerance by supplying unknown future Code effects, Event content, inventory rows, and category skills. Assert that only the dependent choice becomes ineligible, diagnostics retain the exact missing evidence, and other authoritative choices continue.

9. Add regression scenarios for contaminated portfolios, previously completed Research families, a full 25/25 Code inventory, direct Offer decline, multiple reroll epochs, Recovery transform opt-in and rejection, erosion exactly 70, a transient recoverable value above 70, and no confirmed safe route.

10. Add resource-commitment scenarios for 150-Gold Event keys, 200-Gold Shop keys, 300-Gold bags, and combined 500-Gold procurement. Verify commitments are branch-local, survive only their owned transaction, and are recomputed after each confirmed mutation.

11. Add target and settlement scenarios for Research completion before a segment Boss, completion at the floor-70 ceiling, incomplete Research at floor 70, a non-Boss configured Equipment depth, a future map with a deeper Boss, no qualifying positive checkpoint, and a normal Boss result. Assert that ordinary Retreat and proactive Lost Signal are never invoked.

12. Retain all existing race and ownership regressions. New strategy tests must prove that selection cannot occur without current generation, current owner, authoritative snapshot, and matching entered subscene; that no route is planned behind an active child popup or battle; and that a fresh mutation causes a fresh plan.

13. Prefer observable decisions, planned actions, audit reason codes, runtime calls, and reconciled snapshots over assertions on private scoring helpers. Numerical tests should assert exact native projections only where all inputs are authoritative; qualitative tiers should assert ordering without inventing a numeric score.

14. Verification is complete only when the full test suite and production build pass, the deployed DLL comes from the verified build output, and a detailed-log smoke run shows one decision record per state transition without battle-result polling spam.

## Out of Scope

- Automatic detection of Research versus Equipment intent.
- Automatic selection of primary or secondary Research family.
- Changes to F11 save/load behavior or equipment-drop reroll automation.
- Deliberate defeat, proactive Lost Signal consumption, or treating ordinary Retreat as successful settlement.
- Invented probabilities or values for hidden future floors, unknown Code candidates, random Event outcomes, or random Recovery transformation.
- A generic displayed-combat-power optimizer or a single DPS/EHP weighted score.
- A new in-game configuration UI.
- Mid-segment settlement at an arbitrary non-Boss depth.
- Replacing the stable controller transaction model with a parallel strategy controller.
- Hard-coding current floor 130, capacity 25, or a fixed number of Codes as permanent game rules.

## Further Notes

- The domain glossary and approved Q1–Q77 decisions are recorded in the repository's root `CONTEXT.md` and remain the authority when terminology in an implementation discussion is ambiguous.
- Current decompilation evidence is intentionally captured as characterization tests. Runtime MasterData, live party models, native ability assets, and native buff strategies remain authoritative when the game updates.
- The repository already has the required acceptance seams: controller end-to-end tests, production route-safety wiring, focused Code and Event policies, checkpoint policy, configuration contracts, and lifecycle/reconcile regressions. Implementation should deepen these seams rather than introduce a broad new orchestration layer.
- The specification is locally ready for an implementation issue. Tracker publication and the `ready-for-agent` label remain pending because GitHub CLI is not installed in the current environment.
