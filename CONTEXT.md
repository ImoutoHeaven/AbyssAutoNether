# Abyss AutoNether Strategy

This context describes the game concepts used when choosing, rerolling, and replacing Abyss Codes for an automated Nether run.

## Language

**Code Family**:
One of the four mutually opposed progression families: Rush, Impact, Safe, or Risk. Rush opposes Impact, and Safe opposes Risk.
_Avoid_: Buff color, crest type

**Crest Dependency**:
The Passion or Impact crest type that a character's skill loop needs to preserve, obtain, or consume.
_Avoid_: Code family, element

**Crest-Dependency Authority**:
The live `NetherPartyCharacterModel.ManaType` for each active character. Current character MasterData assigns every playable character either Passion or Impact, and the live model preserves that value independently of whether the character can self-grant its required crest. Uniform-grant compatibility uses this authority; payoff valuation separately requires evidence that the party can actually grant or consume the trigger.
_Avoid_: Self-grant capability as crest identity, element inference

**Mixed-Crest Row**:
A front or back row containing characters with different crest dependencies. A uniform crest grant to this row is destructive because it can overwrite a character's required crest and suppress that character's skill loop.
_Avoid_: Mixed-element row, mixed-color row

**Uniform Crest Grant**:
An Abyss Code effect that grants the same crest type to every character in one row. It is incompatible with a mixed-crest row even when another character can generate or consume that crest.
_Avoid_: Row-wide combat buff

**Crest Payoff**:
An effect triggered by receiving or consuming a crest without granting that crest itself. Crest payoffs remain compatible with mixed-crest rows when their trigger can actually occur.
_Avoid_: Uniform crest grant

**Technology Research-Point Rate**:
The `SpherePointRatio` unlocked by Nether research technology and used when settling research points. It is separate from selectable Code mechanics and from the family-point amounts returned by normal settlement.
_Avoid_: Code rate, settlement family points, additive research bonus

**Settlement Family Points**:
The Rush, Impact, Safe, and Risk point amounts returned by the server after a normal Nether result. They are settlement outcomes, not selectable Code mechanics or evidence that an offered Code changes a research-point rate.
_Avoid_: Research-rate Code, technology rate

**Safe Erosion Adjustment**:
The Safe-family adjustment that reduces future erosion gains and improves future erosion reductions.
_Avoid_: Generic erosion ±5

**Risk Erosion Adjustment**:
The Risk-family adjustment that increases future erosion gains and weakens future erosion reductions.
_Avoid_: Generic erosion ±5

**Raw Family Count**:
The number of distinct owned Codes in one family before subtracting Codes in its opposing family.
_Avoid_: Effective family count, total Code count

**Effective Family Count**:
The count used by category-skill thresholds: the number of owned Codes in one family minus the number in its opposing family, clamped at zero. It governs category-skill activation and is not the same as a raw owned-Code count.
_Avoid_: Raw family count
_See_: `AutoNether/Services/NetherActiveCodeErosionProjectionMapper.cs`

**Selectable Risk Erosion Card**:
An ordinary Risk Code offer whose effect applies the Risk erosion adjustment. It can be rejected or rerolled and is therefore a hard-exclusion candidate.
_Avoid_: Risk category skill

**Risk Category Skill Ladder**:
The current server MasterData activates three combat abilities at effective Risk counts 5, 10, and 15: high-erosion attack, high-erosion chain rate, and 0.3 mana at erosion 70 or above. None applies the Risk erosion adjustment; that negative adjustment belongs only to selectable Code `40024`. Category-skill rows remain authoritative runtime inputs and must be re-read rather than assumed stable across game updates.
_Avoid_: Automatic Risk erosion penalty, hard-coded category ladder
_See_: `AutoNether/Services/NetherActiveCodeErosionProjectionMapper.cs`

**Rush/Impact Category Crest Grant**:
The unavoidable category skill activated at effective Rush or Impact count 5. Current MasterData and ability assets prove that it grants two Passion or Impact crests respectively to every living allied character at battle start, with unrestricted party-position and element scope. Avoiding the eight selectable Uniform Crest Grants does not avoid this family threshold.
_Avoid_: Offer-only crest safety analysis, row-limited category skill

**Shared Mana Injection**:
A `ChargeMana` effect applied to the battle's shared mana pool, whose native range is 0–10. The ability executes once for every character matched by its element/position scope, applies enabled mana-charge modifiers, and then clamps the shared result at 10. A matching front-row character therefore contributes to a party-global resource rather than receiving a front-only benefit.
_Avoid_: Per-character mana pool, unbounded target-count multiplication

**Initial Skill Charge**:
A one-time per-character `SkillCharge` effect expressed as a fraction of that character's required charge. Native readiness activates when current charge reaches the character's maximum; excess initial charge has no additional first-cast value, and the gauge resets after an action skill.
_Avoid_: Shared mana, permanent charge-rate increase

**Skill-Charge Efficiency**:
Buff type 120, which multiplies every subsequent `ActionSkillCharge.AddChargeCount` operation for each covered character. It continues to matter after an action skill resets the gauge and therefore must not be assigned zero value merely because the first cast begins fully charged.
_Avoid_: Initial Skill Charge, shared mana injection

**Category Crest-Threshold Compatibility**:
The whole-party compatibility required before effective Rush or Impact count may reach 5. Because the resulting category skill grants two uniform crests to every living ally, every active character must have the matching Crest-Dependency Authority; row-local homogeneity is insufficient. An incompatible Research configuration is rejected at startup when completion requires crossing the threshold. A nearly complete wallet may proceed only when projected completion remains below an effective count of 5, which becomes a hard ceiling.
_Avoid_: Row-only threshold validation, unconditional Research-family accumulation

**Category Crest-Threshold Repair**:
The fail-closed response when an incompatible loaded portfolio already has effective Rush or Impact count at least 5. Automation may use a deterministic full-capacity replacement to reduce the count below 5, but it never adds the opposing family merely to subtract from the effective count. If the current offer cannot deterministically repair the portfolio, automation pauses before the next battle.
_Avoid_: Automatic opposing-family antidote, continuing with an incompatible category skill

**Charge Marginal Value**:
The incremental resource value remaining after every active source and native cap is projected. Shared Mana Injection is clipped to the shared pool's remaining capacity; Initial Skill Charge is clipped independently at each covered character's ready threshold; Skill-Charge Efficiency remains a recurring boss-cycle benefit. A charge Code is valueless only when its applicable marginal contribution is zero, not merely because another charge effect exists.
_Avoid_: Gross charge amount, treating all charge mechanics alike

**Native Buff Coexistence**:
The runtime combination rule supplied by the current `IBuffStrategy` for each `BuffType`; it must be read from the authoritative strategy rather than approximated with a hand-maintained list of exceptional effects. `Allow` effects contribute together inside their matching Buff Group, subject to any positive cumulative `Limit` declared by a member of that group. For `HigherValue`, a candidate replaces the existing matching group only when its value is strictly higher, or when its value is equal and its disable trigger has longer remaining time. An accepted replacement removes the old group completely, so a displaced weaker effect does not resume after the stronger effect expires.
_Avoid_: Summing every owned Code independently, a DamageUp-only special case, dormant weaker-effect fallback

**Native Buff Timeline Valuation**:
The portfolio-level simulation of proven trigger ordering, effect duration, native coexistence, and parameter saturation. `Allow` effects are combined only in windows where they are simultaneously active; `HigherValue` effects contribute only while they actually win their matching Buff Group, and a removed weaker effect is never assumed to return. Candidate and held-Code effects must not be scored as independent uptime percentages.
_Avoid_: Candidate-only scoring, independent average-uptime addition

**Critical-Probability Saturation**:
The point beyond which additional critical probability has no combat marginal value. Native total critical probability has no arithmetic maximum clamp, but the current critical check rolls an integer from 0 through 999 and succeeds when that roll is less than or equal to a positive probability value; 999 is therefore the current guaranteed-critical threshold. Valuation clips only the marginal probability above this native threshold.
_Avoid_: Displayed-power gain above guaranteed critical, applying this cap to continuous attacks

**Continuous-Attack Probability Ladder**:
The finite sequence used to value continuous-attack probability. A character begins with `TotalAttackContinuousProbability`; after each successful extra attack, the remaining extra-attack count decreases by one and the next roll's probability decreases by 100. The sequence ends at the live `AttackContinuousCntMax`. Probability above 1000 can therefore still improve later extra-attack rolls and must be evaluated across the full ladder rather than clipped at the first guaranteed roll.
_Avoid_: A universal 1000 probability cap, infinite-chain expectation

**Reachable-Unquantified Effect**:
An effect whose authoritative target and complete trigger path are proven but whose activation cadence cannot be derived precisely. This is distinct from Card-Specific Evidence Failure: Equipment normally assigns it no invented numerical value and it cannot prove a magnitude-based strict replacement improvement, while Research may still accept it for active-family settlement progress if every safety and compatibility rule passes. A documented mechanism-specific qualitative priority, such as Force-Chain Payoff Priority, may still rank it without fabricating a cadence. If the trigger path itself is not proven, the candidate remains ineligible.
_Avoid_: Invented cadence, treating trigger reachability and trigger frequency as the same unknown, discarding an agreed qualitative priority

**Immediate Category-Threshold Delta**:
The Actual Combat Value caused when the current acquisition or replacement immediately crosses an authoritative category-skill threshold after opposed-family subtraction. Equipment values the newly activated or deactivated category effect only for that resulting portfolio; it assigns no speculative option value merely for moving closer to a threshold that a future unknown offer might cross. Research-family progression remains valuable through its separate settlement objective.
_Avoid_: Distance-to-threshold bonus, ignoring a threshold crossed by the current candidate

**Equipment Zero-Marginal Offer**:
A candidate whose complete retained-portfolio delta is non-positive after native coexistence, probability saturation, charge caps, party compatibility, erosion safety, and any Immediate Category-Threshold Delta are applied. Equipment rerolls according to its configured reserve and otherwise declines this candidate even when Code Capacity is not yet full. Research may accept a safe active-target candidate for settlement progress despite zero quantified combat value.
_Avoid_: Filling spare capacity automatically, displayed-power fallback

**Party-Global Resource Exception**:
The narrow exception to Rear-Row Priority for an effect whose matched character triggers a genuinely shared party resource. A front-row match for Shared Mana Injection contributes its full global mana amount and is not discounted as a front-only buff. Parameter buffs that actually affect only the front row remain fallback choices.
_Avoid_: Treating a scope trigger as the benefit recipient, weakening Rear-Row Priority generally

**Crest Payoff Reachability**:
The authoritative party-wide provider/consumer path required before valuing a Crest Payoff. A consume-trigger payoff requires a matching consumer and a reachable grant source; a grant-trigger payoff requires a source that can reach the target. Sources may come from another character, a compatible row/all-party effect, or a compatible active category skill. `ManaType` alone proves crest identity but not this trigger path; an incomplete graph causes Card-Specific Evidence Failure.
_Avoid_: Self-provider requirement, ManaType-only trigger assumption

**Rear-Row Priority**:
The strategic rule that every usable rear-row offense or defense benefit outranks a front-row-only benefit because a normal party has one defensive front-row character and six damage-oriented rear-row characters. Front-row benefits are fallback choices only when no usable rear-row choice remains.
_Avoid_: Equal row weighting

**Equipment Combat Tier Order**:
The lexicographic combat-value order used by Equipment after all hard exclusions, erosion safety, and party compatibility checks pass. If the party is below an authoritative survival threshold, a rear-row or full-party survivability effect that repairs that deficit comes first. Once survival passes, a Back-row Force-Chain numerical payoff comes first, followed by ordinary rear-row or full-party offense, then nonessential rear-row or full-party defence, and finally Forward-row or other front-only fallbacks. No invented DPS/EHP weighted sum may trade a lower tier against a higher one.
_Avoid_: Display-power ordering, offence/defence weighted sum, front-only percentage leapfrogging a usable rear effect

**Survival-Repair Proof Boundary**:
Survival repair remains the highest Equipment tier, but a Code may enter it only when the current lifecycle contains an authoritative before/after survival projection. In the current client, Event HP becomes authoritative in `NetherUpdateEventResponseEntity.t_nether_characters`, battle HP becomes authoritative in `NetherClearBattleResponseEntity.t_nether_characters`, and future combat damage is resolved by the live `UnitDamageCalculator`, including randomness. The Code Offer lifecycle therefore cannot prove that an offered maximum-HP or defence mutation repairs an already-proven route deficit. It must preserve that deficit, mark only the repair relationship unknown, and reject the dependent candidate rather than fabricate a positive repair. A future game version may make this tier reachable only by exposing an exact pre-entry damage/survival contract.
_Avoid_: Treating a defensive percentage as proof of survival, erasing a known deficit when repair evidence is missing

**Defensive Portfolio Comparison**:
For effects with the same recipient set, compare the exact per-character relative effective-HP change through the native HP, defence, and taken-damage chains. For different recipient sets, compare lexicographically by the number of benefiting rear-row characters, then the weakest benefiting rear character's effective-HP gain, then the remaining aggregate gain. A front-only effect cannot outrank a usable rear-row defence merely because its description percentage is larger.
_Avoid_: Raw description percentage, aggregate gain that hides an unprotected rear character

**Boss-Value Reference**:
The rule that combat utility is evaluated against boss encounters rather than only short ordinary battles. A periodic effect remains relevant when its first trigger occurs during a boss fight.
_Avoid_: Trash-fight-only valuation

**Actual Combat Value**:
The expected contribution of a Code to the configured party's real skill, crest, position, erosion, and capacity interactions. The displayed combat-power number is not an authority for this value.
_Avoid_: Displayed combat power

**Native Damage Projection**:
A bounded before/after projection that uses the current native damage and parameter relationships only where every required input is authoritative. The current unit-damage chain includes attack divided by the square root of defence, critical and element modifiers, damage and quest modifiers, resistance, and randomness; attack, defence, maximum HP, damage modifiers, critical probability, and continuous-attack probability therefore cannot be exchanged through one displayed-power scalar. Missing enemy or timing inputs remain unknown rather than being filled with invented weights.
_Avoid_: Displayed-power conversion, guessed DPS-to-EHP exchange rate

**Projected-Erosion Value**:
The value of an erosion-linked effect at the erosion projected for every confirmed combat on the current authoritative route horizon through the next Boss. Native `AbilityErosionLinkedBuff` reacts to the live erosion ratio and linearly interpolates between its configured threshold/value endpoints. The projection is recomputed after every floor; the maximum description value is not assumed active and unknown future floors contribute no fabricated erosion or uptime.
_Avoid_: Maximum-text valuation, run-wide fixed erosion snapshot

**Stack-Linked Value**:
The per-character value of `AbilityStackLinkedBuff`, calculated natively as its configured per-stack effect multiplied by the linked `StackBuffBase.Stack` at that point in the battle. Valuation requires proven grants, consumes, duration, and the actual per-character stack timeline. A guaranteed battle-start minimum may be used as a conservative lower bound. Current crest buffs have a native maximum stack of 20, but knowing only that cap or the maximum description value makes the effect Reachable-Unquantified; it never proves full-stack value.
_Avoid_: Maximum-stack uptime, generic stack cap

**Completed Force-Chain Activation**:
The only authoritative runtime activation of a completed-Force-Chain payoff: receipt of an actual `ForceChainFinishedMessage`. The supported party is assumed to contain a Force Chain path, and current character and limit-break MasterData can verify that path; exact cadence is not required to recognize the mechanism's strategic value, although no fictitious frequency is assigned.
_Avoid_: Missing-Force-Chain default assumption, Force-Chain capability as an invented activation frequency, periodic fallback

**Force-Chain Payoff Priority**:
The qualitative high priority assigned to a Code effect triggered by Force Chain when its useful recipients are Back characters. In the current client, Impact `10027` and Rush `20027` apply a 15-second, 50-percent critical-damage increase to the Back row; their native trigger subscribes to every completed `ForceChainFinishedMessage` and does not inspect its participant payload, so a Force Chain completed with Assist participation still activates the Back-scoped payoff even though Assist is not itself a recipient. The corresponding Forward Codes `10026` and `20026` remain low-priority fallbacks. These four current effects are pure numerical buffs and do not grant crests. Any future Force Chain effect that grants a uniform crest must still pass Mixed-Crest Row and Category Crest-Threshold Compatibility before this priority applies.
_Avoid_: Zero-valuing Force Chain because cadence is unknown, requiring a Back character in the completion payload, treating Assist as a Back recipient, treating Forward and Back recipients equally, bypassing crest compatibility

**Authoritative Party Combat Profile**:
An immutable strategy snapshot derived from the live `NetherPartyModel` owned by the current Code-offer flow and bound to that popup owner and generation. Its character models expose party position, element, mana type, level, limit break, current HP state, native character-parameter inputs, and all character/equipment/general ability-effect models. The existing displayed-target count exposes only native UI coverage and cannot prove crest dependency, trigger satisfaction, or combat value.
_Avoid_: Display coverage as trigger evidence, stale party inference

**Known Uniform Crest-Grant Set**:
The current client assets prove eight row-wide grant Codes: Impact `10009`, `10010`, `10020`, and `10021`, plus Rush `20009`, `20010`, `20020`, and `20021`. They grant one uniform crest to a front or rear row either at battle start or every 30 seconds; they are not ordinary parameter buffs.
_Avoid_: Description-only classification, treating grants as payoffs

**Element Cohort**:
The actual target split used by current Safe and Risk element-linked Codes. Asset scope flag `28` targets Fire, Water, and Earth; the complementary cohort targets Artifact, Light, and Dark. Actual Combat Value depends on which active party members the cohort covers, not just the Code's displayed power.
_Avoid_: One-element assumption, party-size-only coverage

**Erosion Situation Gate**:
A native ability Situation that tests erosion before applying a Code effect. Current assets independently prove Safe below-50 mana, Risk above-50 mana, and Risk above-70 parameter-buff gates; these thresholds are runtime conditions rather than score hints.
_Avoid_: Treating gated card text as unconditional value

**Card-Specific Evidence Failure**:
The fail-closed response when authoritative party or effect evidence required to evaluate one candidate is unavailable. Only that candidate becomes ineligible; other proven candidates and rerolls remain usable. If no proven candidate remains after the applicable reroll budget is exhausted, automation pauses for the user instead of falling back to displayed power.
_Avoid_: Rejecting the whole offer prematurely, guessing from UI coverage

**Strategy Decision Order**:
The lexicographic order used after candidate effects are decoded: hard exclusions, erosion safety, and party compatibility first; the active Research Family Target second in Research runs; and Equipment Combat Tier Order in Equipment runs. Within the applicable tier, compare actual coverage, native magnitude, proven trigger behaviour, and repeatable boss value. A weighted sum must never compensate for a violation in an earlier tier.
_Avoid_: Single weighted score, displayed-power compensation

**Risk Code Eligibility**:
The current Risk portfolio is divided by proven mechanics. Codes `40010` through `40019`, whose effects require erosion at or above 70, and Code `40024`, which worsens future erosion adjustment, are always rejected. Codes `40022` and `40023`, which grant five mana above 50 erosion, are eligible only when projected battle-start erosion remains within 50–70 and the route is recoverable. Linear high-erosion Codes `40000` through `40009`, `40020`, and `40021` may be valued at projected actual erosion below 70, but automation never raises erosion merely to improve them.
_Avoid_: Treating every Risk Code alike, intentional erosion inflation

**Opposed-Family Contamination**:
A repair state in which the run already owns Codes from both sides of Rush/Impact or Safe/Risk. Automation stops adding the weaker side and removes it first at replacement. Research retains its configured target side; Equipment retains the side whose remaining portfolio has greater Actual Combat Value, never the side with merely greater Raw Family Count or displayed power.
_Avoid_: Continuing both sides, raw-count tie breaking

**Capacity Replacement Improvement**:
At Code Capacity, Equipment replaces a held Code only when the candidate strictly improves the Actual Combat Value of the complete retained portfolio; otherwise it declines the offer. Research may trade immediate combat value for an active-target Code only while every hard exclusion, safety rule, and Research Completion Invariant remains satisfied. If no legal replacement exists, it declines the offer.
_Avoid_: Mandatory replacement, candidate-only comparison

**Research Settlement**:
The server-authoritative conversion of the run's owned Codes into four family point totals when the run ends. The client displays an acquired-Code count derived from the returned family points and settlement bonus; it does not contain an authoritative fixed "22 Codes means complete" rule.
_Avoid_: Client-side research completion formula

**Family Research Wallet**:
The persistent, spendable research-point balance maintained separately for each Code Family. The current game treats 20,000 points as the wallet's full threshold; it is neither the run's Code Capacity nor a measure of total research-tree completion.
_Avoid_: Family Code count, lifetime research progress

**Code Capacity**:
The live total number of distinct positive-amount Abyss Codes that the run can hold. The current count is the number of positive-amount `NetherCodeData` entries, while the maximum is the dynamic `NetherPointData.MaxNetherCode` value. Research upgrades can therefore change the maximum; it must not be hard-coded as 22.
_Avoid_: Fixed Code limit

**Research Completion Threshold**:
The point at which the configured primary family's current wallet plus its projected normal Research Settlement reaches the Family Research Wallet's 20,000-point full threshold. It is not a fixed Raw Family Count and must not be inferred from either a family-gauge activation threshold or total Code Capacity.
_Avoid_: Family gauge level, fixed 22-Code target, total Code Capacity

**Equipment Run**:
The default strategy mode, whose objective is to maximize actual combat value and survive a deep climb for equipment.
_Avoid_: Automatic mode detection

**Research Run**:
An explicitly selected strategy mode that reaches the configured primary family's Research Completion Threshold, then continues only as far as the next boss and uses the boss-unlocked Normal Settlement Window. It never continues beyond the floor-70 boss; ordinary Retreat is not successful Research completion.
_Avoid_: Automatically detected research mode

**Mode Start-Floor Policy**:
Research always starts from the native initial point at floor level 0 so that it preserves the maximum number of Code opportunities. Equipment starts from the highest currently unlocked native checkpoint that does not exceed its Boss-Aligned Target; the unlocked checkpoint set is derived from the live `NetherPointData.RecoveryFloorLevel` in ten-floor increments, matching the native floor-selection flow. If no positive checkpoint qualifies, Equipment starts from 0. Start floor is mode-derived rather than exposed as another manual strategy setting.
_Avoid_: Starting Research from an elevator, hard-coding an unlocked elevator, accepting a checkpoint beyond the target Boss

**Boss-Aligned Target**:
The only valid stopping target is an authoritative Boss floor because Normal Settlement is unavailable mid-segment. A configured positive depth that is not itself a Boss floor is normalized upward to the first authoritative Boss at or above it. A configured depth beyond the map cap resolves to the deepest authoritative Boss within the current map. Failure to resolve such a Boss is invalid configuration and prevents the run from starting; automation never pauses mid-segment merely because an arbitrary numeric depth was reached.
_Avoid_: Mid-segment target pause, rounding down before the requested depth, assuming every ten-floor multiple remains a Boss after an update

**Authoritative Deepest Boss**:
Equipment's default target is the deepest Boss resolved from the current `MNetherMaps` and `MNetherMapFloors`, not a hard-coded floor 130. Current MasterData resolves floor 130, while a future map extension is followed automatically. Research instead exits at the first Boss after completing its configured family objective and always uses the floor-70 Boss as its hard ceiling; if its research objective remains incomplete there, it still settles normally rather than continuing.
_Avoid_: Fixed 130-floor assumption, extending Research past floor 70, ordinary Retreat at the target

**Research Family Target**:
A primary or secondary Code family chosen explicitly in configuration for a research run. The plugin does not infer this target from the research tree, and an opposing primary/secondary pair is invalid configuration that must prevent the run from starting.
_Avoid_: Automatically selected family

**Research Offer Priority**:
The rule that a valid primary-family offer is preferred until its Research Completion Threshold. The configured secondary family then becomes the target until its own threshold; once both are complete, Actual Combat Value governs. Before the primary threshold, all available rerolls are spent before taking a valid secondary-family offer, and hard exclusions override both targets.
_Avoid_: Secondary-before-reroll, family target overriding a hard ban

**Research Reroll Budget**:
While either configured Research Family Target remains incomplete, the automation spends every available offer reroll before falling back from that active target, regardless of the general CodeReloadReserve setting. Equipment runs continue to honor the configured reserve.
_Avoid_: Reserving a reroll during incomplete Research, applying the Research override to Equipment

**Research Completion Invariant**:
A configured family's completion is re-evaluated after every Code acquisition or replacement. A previously projected-complete family must not be allowed to fall below its Research Completion Threshold while pursuing the next family or Actual Combat Value; completion is not a one-way latch before settlement.
_Avoid_: Latched projected completion, sacrificing completed-family settlement points

**Research Replacement Order**:
At Code Capacity, replacement removes a hard-excluded held Code first, then an opposing-family Code, then an ordinary non-target Code, and only then a provable surplus from a projected-complete family. An active-target Code is never removed for a non-target candidate. A same-family swap is allowed only when it preserves the family contribution, violates no hard exclusion, and improves Actual Combat Value.
_Avoid_: Display-power-only replacement, active-target regression

**Research-Rate Code Evidence**:
A future/update-tolerant candidate classification that requires an authoritative selectable Code mechanic to expose both family and overwrite rate. The current client exposes no such Code row: `MNetherCodes` and `NetherCodeModel.CreateModel` resolve ability and erosion effects, while `SpherePointRatio` belongs to technology and server `nether_code_points` belong to settlement; absent exact future evidence, only that candidate is rejected.
_Avoid_: Inferring a Code rate from settlement points, technology rate, description text, or displayed power

**Recovery Code Transform**:
The recovery-floor action in which the player chooses an owned Code to sacrifice but the server determines the replacement. It is not Code Offer replacement. Research runs always reject it; Equipment runs reject it by default and may consider it only when an explicitly enabled random-transform policy can remove a hard-excluded Code while both purification and rest have no actual value.
_Avoid_: Deterministic replacement, Research optimization

**Event Option Decision Pipeline**:
The lexicographic policy used only after entering an Event and resolving every displayed option through the authoritative `MNetherFloorEvents`, `MNetherFloorEventParts`, content, item, and optional-battle rows. First apply hard eligibility constraints such as complete binding, resource sufficiency, Route Safety Gate, and committed budget preservation; then apply the active mode's objective; finally use a deterministic option-number tie break. An unresolved, stale, or unrecognized future content/effect/battle row makes only that option ineligible without guessed probability or value, and automation pauses only when no option remains. Items, Nether Gold, keys, Code Offers, and battles are not interchangeable unit benefits and must never be collapsed into a generic scalar score.
_Avoid_: Generic benefit count, erosion-first sorting after safety, pausing because one of several options is unknown

**Resolved Event Route Value**:
The value assigned before entry to a currently visible, unlocked, and selectable Event is the exact admissible option that the Event Option Decision Pipeline would choose from the current authoritative snapshot. Hidden, locked, unselectable, or unresolved Events contribute no speculative semantic reward to route comparison.
_Avoid_: Generic Event penalty, valuing hidden map knowledge, assuming an unresolved option

**Event Battle Value**:
An Event option whose authoritative part resolves to a battle is valued as its actual combat type rather than penalized merely for starting a battle. An Event Boss is a non-terminal boss-grade encounter and never creates a Normal Settlement Window; an Event MiniBoss shares the Elite tier, and an Event Normal Battle shares the Normal Battle tier only when an authoritative typed battle provider supplies that semantic tier. The fresh native client exposes raw battle `type`, stage, and `code_drop_ratio` fields but does not prove a local Boss/MiniBoss/Normal mapping, so raw values and drop ratios remain Unknown/fail-closed. The route vector invokes the same Event Option Decision Pipeline with the current snapshot, resources, active mode, and route-owned commitments; only the returned Event/part/option contributes a tier. A missing, stale, or untyped battle row invalidates only that option/part while exact non-battle siblings remain eligible.
_Avoid_: Treating every optional battle alike, confusing an Event Boss with a terminal map Boss, guessing a semantic tier from raw battle type or drop ratio, counting an unselected Event part, assuming an unresolved battle is Normal or Elite

**Event Battle Eligibility**:
An Event battle is eligible only when its exact battle and stage identity, projected HP and erosion state, and current combat ownership are authoritative enough to satisfy the same Route Safety Gate and battle-entry guarantees as a map combat. Missing proof rejects only that battle option; the remaining Event options are still evaluated, and automation pauses only when none is eligible.
_Avoid_: Optional-battle safety exemption, rejecting the whole Event because one battle option is unknown

**Event Non-Battle Reward Priority**:
While Research is incomplete, a mandatory known-rank-5 key-procurement objective remains first; otherwise a direct Code Offer outranks the ordinary Gold and item rewards present in current Event MasterData. After Research completion, use Equipment ordering. Equipment orders exact Red rank-5 and Gold rank-5 equipment bags first, then a resource gain that immediately crosses a committed 200-Gold key, 300-Gold bag, or 500-Gold key-plus-bag procurement threshold, then a direct Code Offer, then uncommitted Nether Gold, then lower-rank equipment bags. Current ordinary Event data contains no direct Gold or Red rank-5 bag and tops out at a Gold rank-4 bag. A direct Code Offer retains this opportunity tier even at Code Capacity, but it assumes neither a specific unknown candidate nor mandatory acceptance; a later decline consumes the Event normally and causes no rollback.
_Avoid_: Paper-power comparison, speculative Gold threshold, treating an opened Code Offer as mandatory acquisition

**Committed Procurement Threshold**:
A resource reward crosses a 200, 300, or 500 procurement threshold only when an exact reachable Shop or Treasure on the same selected visible safe branch before its terminal Boss is already known and the post-reward balance meets that exact cost. Hidden nodes, alternative branches, unresolved destinations, and random future inventory create neither a threshold bonus nor a reserved budget.
_Avoid_: Speculative threshold crossing, cross-branch budget, reserving for unknown inventory

**Event Choice Commitment**:
The exact Event, part, option, effect, reward or battle identity, and projected state that justified a selected route form one commitment. The displayed Event must still match that commitment before any cost is paid; a mismatch requires user intervention rather than silently choosing a different option under assumptions that did not justify the route.
_Avoid_: Re-evaluating into an unrelated popup choice, mutating after stale route evidence

**Interactive Commitment Handoff**:
An Event commitment remains authoritative until the exact Event update is confirmed. A Code Offer result then hands control to an independent Code Offer commitment, an Event Battle result hands control to its exact battle projection, and an ordinary reward completes directly; route planning resumes only after the resulting transaction reaches a fresh stable state.
_Avoid_: Releasing the commitment on click, overlapping route planning with a downstream popup or battle

**Authoritative Replan Boundary**:
Every confirmed Event, Code, Shop, Treasure, Recovery, Battle, or Continue mutation invalidates all unexecuted route valuation. The next route is planned from the new authoritative snapshot, while the currently executing commitment remains fixed until its terminal confirmation or downstream handoff.
_Avoid_: Carrying a future route through state mutation, replanning an in-flight commitment

**Event HP Scope**:
An Event Heal or Damage parameter is projected against every currently living party character, using each character's own authoritative HP state. The `_mCharacterId` carried by the native Event popup is the run's `PlayableCharacterModel` used for character presentation; it is not an effect target. The native update request contains no character identifier, and its response returns the party-character statuses. Ordinary Event damage therefore remains subject to the all-survivors requirement in Ordinary Event Cost Gate rather than a single-presenter check.
_Avoid_: Treating the popup presenter as the HP target, applying one aggregate HP check without per-character projection

**Ordinary Event Cost Gate**:
Optional Event rewards may be selected only after their exact HP, erosion, Nether Gold, and key costs pass hard constraints. Ordinary HP damage must leave every currently living character above zero; the only partial-death exceptions are Treasure Payment Priority and HP-Paid Event Key. Increased erosion requires a complete authoritative visible-route safety proof, not an instantaneous post-choice threshold check. Gold spending must preserve every committed 200, 300, or 500 procurement budget. A key is not spent on an ordinary Code Offer, Gold reward, or lower-rank item; it may be spent only for an exact Gold or Red rank-5 reward, or at the final pre-Boss opportunity when the authoritative route proves the key otherwise expires unused.
_Avoid_: Optional-reward character sacrifice, speculative future recovery, breaking committed purchase budget, spending a key on a low-tier reward

**Recovery Choice Priority**:
Current Recovery MasterData presents purification for 30 erosion, rest for 30 percent HP, and random Abyss Code transformation. First choose whichever purification or rest result is necessary for the next complete visible branch to pass Route Safety Gate; if neither alone can make any branch safe, stop instead of pretending either solves it. When both preserve a safe branch, choose rest if any active character is below the configured HP soft floor, otherwise choose purification whenever erosion is above zero. If both have zero marginal value, choose a deterministic harmless option. Transformation remains governed by Recovery Code Transform: Research always rejects it, while Equipment requires explicit opt-in and zero actual value from both deterministic choices.
_Avoid_: Weighted HP-versus-erosion score, random transform as ordinary fallback, selecting a locally attractive recovery that leaves no safe branch

**Normal Settlement Window**:
The reward-preserving exit made available after defeating a boss. A Research run that reaches its completion condition must continue to this window rather than using the ordinary Retreat action.
_Avoid_: Ordinary Retreat

**Ordinary Retreat**:
The always-available retreat action whose native confirmation explicitly forfeits every reward except already transported items. It is a loss fallback, not a successful Research settlement.
_Avoid_: Normal settlement, reward-preserving exit

**Lost Signal**:
The rare consumable insurance offered by the native game-over flow after the party has already been defeated. Automation may use it as accident recovery but must never seek defeat in order to trigger it; it is distinct from ordinary Retreat and normal post-boss settlement.
_Avoid_: Retreat alternative, proactive settlement button, planned defeat

**Risk Research Band**:
The preferred erosion operating band for a dedicated Risk research run, centered between 50 and 70 while generally avoiding any rise above 70. A transient rise above 70 is acceptable only along a Confirmed Recovery Route; reaching 70 without one requires user intervention.
_Avoid_: High-erosion build

**Confirmed Recovery Route**:
A route already present on the authoritative map whose complete projection remains below lethal erosion and reaches a certain erosion reduction. It may contain necessary battles, but unknown future floors and random recovery outcomes do not count as confirmation.
_Avoid_: Probable recovery, random-event recovery

**Erosion Recoverability**:
The route-level property that a Confirmed Recovery Route can return projected erosion to the preferred band before it becomes lethal. At erosion 70 or above, absence of such a route requires the automation to stop for the user; necessary combat on a confirmed route remains permitted.
_Avoid_: Pointwise 70 hard cap

**Treasure Payment Priority**:
An entered Treasure uses one key whenever a key is available. If no key is available, automation chooses the Treasure's HP-payment option instead of pausing when either the Treasure is the only terminal-reaching route or its exact authoritative reward is a rank-5 equipment bag. Current MasterData expresses that HP payment as 40 or 80 percent depending on the Treasure variant; the 80-percent variant is explicitly permitted. Individual characters may be reduced to zero by this payment. The route is forbidden only when every currently living character's projected post-payment HP is zero or below and no key is held or can be bought beforehand. The erosion-payment option is not substituted for the HP option. Native Treasure UI eligibility checks resource sufficiency for Gold and keys but does not impose an equivalent client-side HP-solvency gate, so the ordinary per-character HP soft limit must not pre-empt this policy-authorized selection.
_Avoid_: Requiring every character to survive, intentional full-party defeat, KeyOnly deadlock, choosing the erosion payment when HP payment exists

**Rank-5 Treasure Key Procurement**:
When an authoritative reachable rank-5 equipment-bag Treasure is known in advance and no key is held, a reachable key source becomes a mandatory route objective. It outranks the ordinary Elite-first encounter preference, but never Boss or the Route Safety Gate, and its route must still reach the Treasure and terminal Boss. Currency-paid sources are preferred: current MasterData charges 150 Nether Gold at the key-granting Event and 200 at Shop. If no permitted key source can be reached or afforded, Treasure Payment Priority still requires the HP-payment option rather than a pause. A key is never bought speculatively without a known reachable rank-5 Treasure.
_Avoid_: Saving currency while leaving a known rank-5 Treasure unopened, speculative key purchase, key procurement outranking Boss or safety

**Rank-5 Key Budget Priority**:
For a known reachable rank-5 Treasure, key budget is reserved ahead of an Eligible Late Shop's 300-Gold rank-5 bag even when the later Treasure's HP payment is projected survivable. At a Shop, the 200-Gold key is bought first; with 300-499 Gold the current 300-Gold bag is skipped, while at 500 or more Gold the key is bought first and the bag second. Native Shop purchases are sequential, so ordering deliberately secures Treasure access if the second transaction cannot complete.
_Avoid_: Buying the current Gold bag first, treating safe later HP payment as permission to spend the reserved key budget

**HP-Paid Event Key**:
The Event option that exchanges 80 percent HP for one key is eligible only when an authoritative reachable rank-5 Treasure is already known, no better reachable and affordable currency-paid key source exists, and the projected payment does not defeat every currently living character. Individual character deaths are permitted under the same full-party threshold as Treasure Payment Priority.
_Avoid_: Speculative HP payment for a key, requiring every character to survive, intentional full-party defeat

**Erosion-Paid Event Key**:
Current MasterData's key Event parameter `80` is a direct 80-point erosion increase, not an 8-point or per-mille value. It is eligible only for a known reachable rank-5 Treasure when the complete authoritative visible route proves that no battle occurs while erosion exceeds 70 and that erosion returns to 70 or below before the next battle. Otherwise the choice and any route that depends on it are ineligible.
_Avoid_: Treating 80 as 8, accepting probable future recovery, fighting above 70 after the payment

**Route Safety Gate**:
The hard constraint applied before encounter-reward priority. A route whose projected erosion or HP state violates the active safety policy is ineligible regardless of its encounter value; a lower-priority recovery path may therefore be required when it is the Confirmed Recovery Route. Treasure Payment Priority and HP-Paid Event Key are the narrow HP exceptions: partial character loss is allowed, but a projected full-party defeat remains ineligible. Encounter ranking is applied only among routes that pass this gate. Once unsafe routes are removed, the planner must not minimize erosion delta again ahead of encounter value.
_Avoid_: Paying arbitrary erosion for a preferred encounter, mixing safety into a weighted route score, erosion-first sorting after the safety filter

**Eligible Late Shop**:
A Shop destination strictly above floor 90 for which current Nether Gold is at least 300 and the authoritative floor `ExtendId` resolves to actual inventory containing a 300-cost rank-5 Gold equipment bag. Current MasterData contains 44 such 300-cost shop rows, all `DropRarityLevel.Gold`; it contains no Red shop-bag row. Only an Eligible Late Shop receives late-shop priority, and failure to resolve the selected inventory is ineligible rather than treated probabilistically. Any other Shop ranks below Recovery, although it remains legal transit when it is the only safe route that can still reach the terminal Boss.
_Avoid_: Floor-90-or-earlier priority, currency-blind priority, relation-level possibility in place of selected inventory, claiming a current Red shop bag

**Treasure Route Priority**:
Treasure value is classified from the exact authoritative `ExtendId`, event part, and `MItems` row rather than the generic Treasure node type. In either mode, a known canonical rank-five Treasure and its mandatory key-procurement objective rank below the terminal map Boss and Route Safety Gate but above every non-terminal combat. The canonical rank-five predicate or an authoritative typed provider is the sole current authority; raw Gold/Red rarity, display Rank, or an inferred raw combination does not create rank-five value. Other Treasure rewards do not cause a voluntary Research detour. In Equipment before floor 96, lower-rank Treasure likewise does not cause a voluntary detour because keys are reserved for rank-five opportunities. From floor 96 onward, the non-boss order begins with a known canonical Red rank-five Treasure, then a known canonical Gold rank-five Treasure and an Eligible Late Shop at the same reward tier; a direct tie favours the Treasure to preserve 300 Nether Gold. Silver, Purple, and Nether-Gold Treasure rewards do not cause a voluntary key spend, except that an otherwise expiring key may be used at the final reachable Treasure opportunity. Necessary transit still follows Treasure Payment Priority.
_Avoid_: Generic Treasure score, rank-five objective below a non-terminal combat, spending a reserved key on an early low-rank bag, treating raw or unknown reward rarity as canonical rank-five

**Visible-Branch Encounter Vector**:
The complete authoritative visible branch from a safe frontier choice through the next terminal map Boss, rather than only its immediate node. When that Boss itself is selectable it remains the highest-priority choice. A currently visible, unlocked, selectable Event contributes its Resolved Event Route Value only after the same Event Option Decision Pipeline selects an exact option from the current snapshot, mode, resources, and route-owned commitments. Only that selected part contributes an exact Boss, MiniBoss, Normal Battle, direct Code Offer, or ordinary reward tier; an unknown battle tier invalidates only its part, and known siblings remain eligible. Hidden, locked, and unresolved Event contents do not contribute value. Branches are compared lexicographically under the active mode and floor's Treasure Route Priority and encounter order, then like-tier encounter counts and finally fewer Recovery nodes. The comparison is not a weighted scalar; for example, `Normal -> Elite -> Elite` outranks `Elite -> Recovery -> Recovery` before floor 90.
_Avoid_: Immediate-node greediness, fixed priority over unselected Event parts, generic Event counting despite exact evidence, raw battle-tier or rank-five guesses, hidden-node valuation, erosion-delta tie-break before encounter value

**Route-Vector Tie Break**:
Only after two safe branches have identical complete Visible-Branch Encounter Vectors, prefer the branch with the lower projected peak erosion through the next Boss, then the higher minimum projected active-character HP across that horizon, then deterministic floor and node coordinates. These are tie breaks and never precede encounter value.
_Avoid_: Erosion-first routing, nondeterministic equal-vector choice

**Pre-90 Encounter Priority**:
The shared route objective before the late-shop threshold begins with terminal map Boss, then a known Red or Gold rank-5 Treasure and its mandatory key-procurement objective, then non-terminal Event Boss, then map Elite and Event MiniBoss. While either Research family remains incomplete, a direct Code Offer follows those combat tiers and precedes map or Event Normal Battle; Equipment and completed Research put Normal Battle before the direct offer. Ordinary resolved Event rewards, Recovery, and ineligible early Shop follow according to their established policies. Subject to Route Safety Gate, the strict semantic orders are `Terminal Boss > known rank-5 Treasure objective > Event Boss > Elite/Event MiniBoss > Direct Code Offer > Normal Battle > ordinary Event > Recovery > Shop` for incomplete Research and `Terminal Boss > known rank-5 Treasure objective > Event Boss > Elite/Event MiniBoss > Normal Battle > Direct Code Offer > ordinary Event > Recovery > Shop` otherwise. An early Shop remains allowed as necessary safe transit to the Boss.
_Avoid_: Treating all Events as one tier, putting a known rank-5 objective below non-terminal combat, granting settlement semantics to Event Boss, Recovery-first routing while a higher safe tier exists

**Late-Shop Encounter Priority**:
The Equipment route objective after the floor-90 shop threshold. Current 130-floor MasterData corroborates the boundary operationally: floor-89 selected inventory still uses rank-4 bags, while floor 95 is the first shop floor whose possible selected inventories include rank-5 Gold bags priced at 300 Nether Gold. Subject to Route Safety Gate, the semantic order is `Terminal Boss > known Red rank-5 Treasure > known Gold rank-5 Treasure / Eligible Late Shop > Event Boss > Elite/Event MiniBoss > Normal Battle > Direct Code Offer > ordinary Event > Recovery`; a direct Gold-Treasure/Shop tie favours Treasure, and an ineligible Shop ranks below Recovery. This ordering is applied over the Visible-Branch Encounter Vector.
_Avoid_: Shop priority before floor 90, relation possibility as actual inventory, granting settlement semantics to Event Boss, Shop outranking terminal Boss or known Red Treasure

## Fresh native design boundary (2026-08-17)

The current native design is authoritative for tickets 07–09. Fresh Docker read-only Cpp2IL runs
used `Project.dll` SHA-256
`53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`, `GameAssembly.dll` SHA-256
`573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
`global-metadata.dat` SHA-256
`ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`. Exact commands and output
anchors are recorded in `docs/agents/evidence-backed-strategy-modes-07-09-evidence.md`.

The native `MNetherFloorEvents` row binds four `MNetherFloorEventParts` IDs; each part contains
three raw target/parameter pairs and one raw content tuple. `MNetherFloorBattles` exposes a raw
integer `type`, stage ID, and Code-drop ratio, but the fresh native type/member evidence does not
prove a local semantic Boss/MiniBoss/Normal enum. `NetherEventPopupController` carries the exact
Event row, part array, and presenter `_mCharacterId`, while
`NetherApiDataStore.RequestNetherUpdateEventAsync` accepts only floor level/index, selected option
number, and Code-change ID. Event/Part IDs therefore remain client-side commitment correlation;
they are never invented as request arguments. Exact runtime battle options with no typed semantic
provider are rejected locally, and raw battle type or Code-drop ratio is not used as a guessed tier.

Fresh result evidence also shows `NetherResultRequestEntity` contains only Nether/map IDs and the
insurance flag, `NetherResultResponseEntity.nether_code_points` is a post-result four-family
outcome, and `NetherPointData.SpherePointRatio` is separate technology state. Research completion
therefore remains unknown in production until a server-authoritative pre-settlement projection is
available; policy accepts only an exact typed projection and never substitutes Code count,
capacity, gauge, technology rate, settlement points, or displayed power.

## Fresh native design boundary (2026-08-18 second-review repair)

The second-review repair re-ran Cpp2IL in Docker with the game directory mounted read-only. The
fresh game hashes, decompiled artifact hashes, exact command, and output anchors are recorded in
`docs/agents/evidence-backed-strategy-modes-07-09-evidence.md` under `j-cx00jc`. The native design
did not conflict with the ticket/spec contracts: `MNetherFloorEvents` declares four part IDs,
`MNetherFloorEventParts` declares raw target/content rows, `MItems` and `MNetherFloorBattles`
provide exact item/battle rows, and `NetherEventPopupController` exposes the popup's Event/part
arrays. `NetherApiDataStore.RequestNetherUpdateEventAsync` still accepts only floor level, floor
index, option number, and Code-change ID. Therefore EventId/EventPartId and floor/node identity
are retained as an immutable client commitment and stale-guard key, while only the proven native
floor/index/option/Code arguments are sent to the game. Raw battle `type` remains semantically
unproven; production rejects a battle option lacking a typed semantic provider rather than
guessing. Research's production completion projection remains unknown because native result
`nether_code_points` is post-result settlement data and `SpherePointRatio` is technology state.

## Fresh native design boundary (2026-08-18 third-review repair)

Post-fix fresh native evidence is recorded under job `j-g1etxg` in
`docs/agents/evidence-backed-strategy-modes-07-09-evidence.md`. It re-confirmed the exact native
item/battle row fields and IDs, the four Event part references, the popup's Event/part arrays, and
the floor/index/option/Code-only update signature. No native design conflict was found. Research
therefore uses the effective target family after primary reroll exhaustion, while Event row
identity remains option-local client evidence. Malformed item or battle rows retain their original
native ID for local invalidation; a valid sibling with the same ID does not mask the malformed
dependency, and unrelated exact options remain eligible.

## Fresh native design boundary (2026-08-18 final spec-axis repair)

The final spec-axis repair used fresh Docker Cpp2IL evidence from jobs `j-2rngms` (pre-fix) and
`j-a9j9qd` (post-fix), both with `/c/Users/Eden/PixelAbyssX/dotabyss_x_cl` mounted as read-only
`/game`; exact commands, logs, game hashes, and decompiled artifact hashes are recorded in
`docs/agents/evidence-backed-strategy-modes-07-09-evidence.md`. The authoritative post-fix
anchors remain `MNetherFloorBattles.cs:4-15`, `MNetherFloorEvents.cs:4-25`,
`MNetherFloorEventParts.cs:4-29`, and `NetherApiDataStore.cs:284-290`.

Native `MNetherFloorBattles.type` is only a raw integer in this build. Values 1–8 therefore do
not prove Boss/MiniBoss/Normal semantics; production keeps exact battle, stage, and content
identity but leaves the semantic tier unknown until an authoritative typed provider is present.
The native Event update seam accepts only floor level, floor index, selected option number, and
Code-change ID, so Event/Part IDs remain client-side commitment correlation. Research now resolves
the effective target family after all primary rerolls and before hard eligibility, candidate
filtering, capacity removal, retained-family resolution, and same-family strict-improvement
decisions. No native/spec/context deviation was needed.

## Fresh native design boundary (2026-08-18 closing repair)

Closing repair evidence is Docker job `j-fy65yc`, with the game mounted read-only at `/game` and
`CPP2IL_EXIT=0`/`DIFFABLE_EXIT=0`. `MNetherFloorEventParts.cs` hash
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128` output lines 13-29 proves
only raw target/parameter and content fields; `MNetherFloorBattles.cs` hash
`7034adf207379ef2f42aa6eb8aa3155252928d08cfd1c4643635c61368cbd720` lines 7-15 proves raw
battle type/stage/drop fields only; and `NetherApiDataStore.cs` hash
`b970836a0c0457174405d227b3e100a41dcf3a7a3b7a8a6abe1d6fe036a18071` lines 287-288 proves the
native Event update accepts floor level, floor index, selected option, and Code-change ID.
Production consequently uses one shared Event mapping: target type 7 with a nonzero parameter and
content type 160 with a nonzero content ID are unknown and fail-closed; Event/Part IDs remain
client-only correlation, and raw battle tiers remain unknown. The closing public GREEN is
`j-voopjz` (196/196); the final Docker gate records the exact amended HEAD from its
`git rev-parse HEAD` output.

## Fresh native design boundary (2026-08-18 raw ItemType overflow repair)

Fresh read-only Docker Cpp2IL jobs `j-bghfub` (pre-fix) and `j-5l2ncz` (post-fix) both returned
`CPP2IL_EXIT=0` and `DIFFABLE_EXIT=0`. `MItems.cs` hash
`e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27`, output lines 4-19,
has `MItems.type` as raw `long` at source line 11; `MNetherFloorEventParts.cs` hash
`5ad97670122ba462fb0c9d4f9197fa7e934d988b148f70b307478b86da44e128`, output lines 4-29,
retains raw Event target/parameter/content fields. The immutable game hashes were
Project.dll `53806a5b4dec186357e2fe8ba5b8a72e4f85674be9231479e207e500e2bd1300`,
GameAssembly.dll `573fa800171b8b37800cb4425b918351ec84a340bca9a46c32249d7af965c1fb`, and
global-metadata.dat `ac0c6d43ca487456a5de68a5d357f634fedd9fa0a87d80d5b6545360fb133ea5`.

No narrower closed native item-type domain is proven. Production therefore maps raw item types
only within the existing Int32 evidence seam; a positive or negative raw value outside that
domain is option-local unknown/paused in visible map, pre-entry, runtime, and commitment binding,
with no checked cast escaping. This preserves native-first behavior without inventing an item
type semantic domain.

## Fresh native design boundary (2026-08-19 tickets 13–15 review repair)

The bounded repair reran Cpp2IL in Docker with `dotabyss_x_cl` mounted read-only at `/game`, with
`CPP2IL_DIFFABLE_EXIT=0` and `CPP2IL_ISIL_EXIT=0`; the exact command, immutable game hashes, and
fresh output hashes are recorded in `docs/agents/evidence-backed-strategy-modes-13-15-evidence.md`.
The current native rows reconfirm that one `MNetherFloorEvents` record references four raw Event
parts, `MNetherFloorBattles.type` remains an unclassified integer, and Shop update input preserves
exact content identity and amount. No fresh native anchor maps raw battle type or drop ratio to
Boss/MiniBoss/Normal, so only an authoritative typed battle provider may enable those semantic
tiers; the current route vector must use Unknown/fail-closed rather than invent a mapping. The route
implementation therefore invokes the actual Event policy with the current snapshot, mode, resources,
and route-owned commitments, counts only its selected Event part, and invalidates only the affected
unknown part. A nullable pre-settlement Research completion remains unknown and pauses only a
Research-mode mode-sensitive visible-vector comparison; Equipment mode follows its explicit target
and order without treating Research null/true as a Research signal. Rank-five Treasure value uses the
canonical rank-five predicate or an authoritative typed provider; raw Gold/Red plus display Rank is
not a fallback. A materialized Shop with an unknown or malformed sibling is not an eligible late
Shop, even when another sibling is an exact 300-Gold row.
