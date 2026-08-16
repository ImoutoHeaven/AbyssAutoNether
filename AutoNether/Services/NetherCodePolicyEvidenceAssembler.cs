#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Pure production mapper from one accepted strategy package plus freshly captured offered-Code
/// mechanics into the Code-policy contract. Missing lifecycle data stays candidate/component local;
/// this module never derives value from displayed power or invents a future combat timeline.
/// </summary>
internal static class NetherCodePolicyEvidenceAssembler
{
    private const int ResearchCompletionPoints = 20_000;

    public static NetherRuntimeCodePolicyEvidenceResult Assemble(
        NetherStrategyEvidencePackage package,
        NetherSnapshot snapshot,
        IReadOnlyList<NetherCodeCandidate> candidates,
        IReadOnlyList<NetherStrategyNativeMechanic> candidateMechanics,
        NetherAutoClimbSettings settings,
        NetherCodePolicyRouteEvidence? routeEvidence
    )
    {
        if (package == null || snapshot == null || candidates == null
            || candidateMechanics == null || settings == null)
        {
            return NetherRuntimeCodePolicyEvidenceResult.Failure(
                "code-policy-assembly-input-unavailable"
            );
        }
        if (package.Server == null
            || package.Identity.SnapshotFingerprint != snapshot.Fingerprint)
        {
            return NetherRuntimeCodePolicyEvidenceResult.Failure(
                "code-policy-strategy-snapshot-mismatch"
            );
        }

        IReadOnlyList<NetherStrategyPartyMember>? party = package.Party.IsKnown
            ? package.Party.Value!.Members
            : null;
        IReadOnlyList<NetherStrategyResearchFamilyState>? research = package.Research.IsKnown
            ? package.Research.Value!.Families
            : null;
        IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics = package.NativeMechanics.IsKnown
            ? package.NativeMechanics.Value!.Mechanics
            : snapshot.Codes.Count == 0
                ? Array.Empty<NetherStrategyNativeMechanic>()
                : null;
        NetherStrategyOwnedCodeEvidence? ownedCodes = package.OwnedCodes.IsKnown
            ? package.OwnedCodes.Value
            : null;
        NetherCodeFamily activeResearch = ResolveActiveResearchFamily(settings, research);

        var mechanicById = candidateMechanics
            .Where(row => row != null && row.MechanicId > 0)
            .GroupBy(row => row.MechanicId)
            .ToDictionary(group => group.Key, group => group.First());
        var hard = new Dictionary<long, NetherCodeHardEligibilityEvidence>();
        var values = new Dictionary<long, NetherMechanismValue>();
        foreach (NetherCodeCandidate candidate in candidates
            .Where(row => row != null)
            .GroupBy(row => row.CodeId)
            .Select(group => group.First()))
        {
            if (!mechanicById.TryGetValue(candidate.CodeId, out NetherStrategyNativeMechanic? mechanic))
            {
                string reason = "offered-code-native-mechanic-unavailable:" + candidate.CodeId;
                hard[candidate.CodeId] = new NetherCodeHardEligibilityEvidence
                {
                    IsKnown = false,
                    UnknownReason = reason,
                };
                values[candidate.CodeId] = NetherMechanismValue.Missing(reason);
                continue;
            }
            hard[candidate.CodeId] = MapHardEligibility(candidate, mechanic);
            values[candidate.CodeId] = MapMechanismValue(mechanic, party, routeEvidence);
        }

        var mutations = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>();
        foreach (NetherCodeCandidate candidate in candidates
            .Where(row => row != null)
            .GroupBy(row => row.CodeId)
            .Select(group => group.First()))
        {
            if (!values.TryGetValue(candidate.CodeId, out NetherMechanismValue mechanism))
                continue;
            IEnumerable<long> removals = snapshot.Codes.Count < snapshot.CodeCapacity
                ? new long[] { 0 }
                : snapshot.Codes.Where(code => code != null && code.PossessionAmount > 0)
                    .Select(code => code.CodeId);
            foreach (long removal in removals)
            {
                mechanicById.TryGetValue(
                    candidate.CodeId,
                    out NetherStrategyNativeMechanic? candidateMechanic
                );
                NativeSpecialComparisonMapResult nativeComparisonResult = MapNativeComparisons(
                    candidateMechanic,
                    removal,
                    party,
                    ownedMechanics
                );
                IReadOnlyList<NetherNativeSpecialComparisonEvidence> nativeComparisons =
                    nativeComparisonResult.Comparisons;
                NetherNativeSpecialComparisonEvidence nativeComparison = nativeComparisons
                    .FirstOrDefault() ?? NetherNativeSpecialComparisonEvidence.None;
                bool portfolioKnown = TryBuildNativePortfolioComparison(
                    candidateMechanic,
                    removal,
                    party,
                    routeEvidence,
                    ownedMechanics,
                    out NetherNativePortfolioComparisonInput nativePortfolio,
                    out string portfolioError
                );
                NetherMechanismValue mutationMechanism = !nativeComparisonResult.IsKnown
                        ? NetherMechanismValue.ReachableUnquantified(
                            nativeComparisonResult.Error
                        )
                        : !portfolioKnown
                            ? NetherMechanismValue.ReachableUnquantified(portfolioError)
                            : mechanism;
                mutationMechanism = ApplyImmediateCategoryThresholdDelta(
                    mutationMechanism,
                    snapshot,
                    candidate,
                    removal,
                    ownedCodes
                );
                NetherMechanismPortfolioComparisonEvidence mechanismPortfolio =
                    BuildMechanismPortfolioComparison(
                        candidateMechanic,
                        removal,
                        party,
                        routeEvidence,
                        ownedMechanics
                    );
                mutations[new NetherCodeMutationKey(candidate.CodeId, removal)] = new(
                    candidate.CodeId,
                    removal,
                    nativePortfolio,
                    mutationMechanism
                )
                {
                    CombatTier = MapCombatTier(candidateMechanic, party),
                    RemovedCombatTier = MapCombatTier(
                        FindOwnedMechanic(ownedMechanics, removal),
                        party
                    ),
                    Survival = MapSurvivalRepair(routeEvidence),
                    NativeComparison = nativeComparison,
                    NativeComparisons = nativeComparisons,
                    MechanismPortfolio = mechanismPortfolio,
                    RecipientPositions = MapRecipientPositions(party),
                };
            }
        }

        return NetherRuntimeCodePolicyEvidenceResult.Success(new NetherCodePolicyEvidence
        {
            MechanicsByCodeId = hard,
            MechanismValuesByCodeId = values,
            EquipmentMutationValuesByKey = mutations,
            ActiveParty = party,
            Research = research,
            ActiveResearchFamily = activeResearch,
            FamilyRetentionByPair = BuildFamilyRetentionEvidence(
                snapshot,
                party,
                routeEvidence,
                ownedMechanics
            ),
            ErosionHorizonKnown = routeEvidence?.IsKnown == true,
            ProjectedMinimumErosion = routeEvidence?.MinimumBattleStartErosion ?? 0,
            ProjectedMaximumErosion = routeEvidence?.MaximumBattleStartErosion ?? 0,
            RecoverableToFiftySeventyBand =
                routeEvidence?.RecoverableToFiftySeventyBand == true,
        });
    }

    private static NetherStrategyNativeMechanic? FindOwnedMechanic(
        IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics,
        long mechanicId
    )
    {
        if (mechanicId <= 0 || ownedMechanics == null)
            return null;
        NetherStrategyNativeMechanic[] matches = ownedMechanics
            .Where(row => row != null && row.MechanicId == mechanicId)
            .ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static NetherSurvivalRepairEvidence MapSurvivalRepair(
        NetherCodePolicyRouteEvidence? routeEvidence
    )
    {
        if (routeEvidence?.IsKnown != true || !routeEvidence.SurvivalBaselineKnown)
            return NetherSurvivalRepairEvidence.Unknown;
        if (!routeEvidence.HasSurvivalDeficit)
            return NetherSurvivalRepairEvidence.Known(hasDeficit: false, repairsDeficit: false);

        // The current T03 horizon proves that a deficit exists, but it does not expose an exact
        // incoming-damage model from which a Code's HP/defence mutation could prove repair. Event
        // and battle HP become authoritative only in their server response character rows. Keep
        // the proven deficit while making only the repair relationship unknown.
        return NetherSurvivalRepairEvidence.UnknownFor(
            hasDeficit: true,
            "survival-repair-proof-unavailable:server-authoritative-event-or-battle-result"
        );
    }

    private static NetherMechanismPortfolioComparisonEvidence BuildMechanismPortfolioComparison(
        NetherStrategyNativeMechanic? candidate,
        long removalCodeId,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        NetherCodePolicyRouteEvidence? routeEvidence,
        IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics
    )
    {
        if (candidate == null || ownedMechanics == null)
        {
            return NetherMechanismPortfolioComparisonEvidence.Unknown(
                "complete-mechanism-portfolio-input-unavailable"
            );
        }

        NetherMechanismPortfolioEntry[] before = ownedMechanics
            .Where(mechanic => mechanic != null && mechanic.MechanicId > 0)
            .Select(mechanic => new NetherMechanismPortfolioEntry(
                mechanic.MechanicId,
                MapMechanismValue(mechanic, party, routeEvidence)
            ))
            .ToArray();
        NetherMechanismPortfolioEntry[] after = before
            .Where(entry => entry.CodeId != removalCodeId)
            .Append(new NetherMechanismPortfolioEntry(
                candidate.MechanicId,
                MapMechanismValue(candidate, party, routeEvidence)
            ))
            .ToArray();
        return NetherMechanismPortfolioComparisonEvidence.Known(before, after);
    }

    private static NetherMechanismValue ApplyImmediateCategoryThresholdDelta(
        NetherMechanismValue candidateValue,
        NetherSnapshot snapshot,
        NetherCodeCandidate candidate,
        long removalCodeId,
        NetherStrategyOwnedCodeEvidence? ownedCodes
    )
    {
        if (ownedCodes == null || ownedCodes.CategorySkills.Count == 0)
            return candidateValue;

        NetherCodeFamily[] beforeFamilies = snapshot.Codes
            .Where(code => code != null && code.PossessionAmount > 0)
            .GroupBy(code => code.CodeId)
            .Select(group => group.First().Family)
            .ToArray();
        NetherCodeFamily[] afterFamilies = snapshot.Codes
            .Where(code => code != null && code.PossessionAmount > 0
                && code.CodeId != removalCodeId && code.CodeId != candidate.CodeId)
            .GroupBy(code => code.CodeId)
            .Select(group => group.First().Family)
            .Append(candidate.Family)
            .ToArray();
        NetherCodeEffectiveLevels before = NetherCodePolicy.CalculateEffectiveLevels(beforeFamilies);
        NetherCodeEffectiveLevels after = NetherCodePolicy.CalculateEffectiveLevels(afterFamilies);
        NetherStrategyCategorySkill[] changed = ownedCodes.CategorySkills
            .Where(skill => skill != null && skill.Counter > 0)
            .Where(skill =>
                (EffectiveCount(before, skill.Family) >= skill.Counter)
                != (EffectiveCount(after, skill.Family) >= skill.Counter))
            .OrderBy(skill => skill.SkillId)
            .ToArray();
        if (changed.Length == 0)
            return candidateValue;

        // MNetherCodeCategorySkills proves which exact effect row activates, but the accepted
        // package does not yet carry that row's decoded native ability mechanic. Do not let the
        // candidate's independent ordinary buff hide an unknown newly activated/deactivated
        // category effect, and do not invent a cross-mechanism scalar from raw parameters.
        return NetherMechanismValue.ReachableUnquantified(
            "category-threshold-native-effect-mechanic-unavailable:"
                + string.Join(",", changed.Select(skill => skill.SkillId))
        );
    }

    private static int EffectiveCount(
        NetherCodeEffectiveLevels levels,
        NetherCodeFamily family
    ) => family switch
    {
        NetherCodeFamily.Safe => levels.Safe,
        NetherCodeFamily.Risk => levels.Risk,
        NetherCodeFamily.Rush => levels.Rush,
        NetherCodeFamily.Impact => levels.Impact,
        _ => 0,
    };

    private static IReadOnlyDictionary<NetherOpposedFamilyPair, NetherFamilyRetentionEvidence>
        BuildFamilyRetentionEvidence(
            NetherSnapshot snapshot,
            IReadOnlyList<NetherStrategyPartyMember>? party,
            NetherCodePolicyRouteEvidence? routeEvidence,
            IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics
        )
    {
        var result = new Dictionary<NetherOpposedFamilyPair, NetherFamilyRetentionEvidence>();
        AddFamilyRetention(
            NetherOpposedFamilyPair.RushImpact,
            NetherCodeFamily.Rush,
            NetherCodeFamily.Impact
        );
        AddFamilyRetention(
            NetherOpposedFamilyPair.SafeRisk,
            NetherCodeFamily.Safe,
            NetherCodeFamily.Risk
        );
        return result;

        void AddFamilyRetention(
            NetherOpposedFamilyPair pair,
            NetherCodeFamily first,
            NetherCodeFamily second
        )
        {
            NetherCodeState[] current = snapshot.Codes
                .Where(code => code != null && code.PossessionAmount > 0)
                .GroupBy(code => code.CodeId)
                .Select(group => group.First())
                .ToArray();
            if (!current.Any(code => code.Family == first)
                || !current.Any(code => code.Family == second))
            {
                return;
            }
            if (party == null || ownedMechanics == null)
            {
                result[pair] = NetherFamilyRetentionEvidence.Unknown(
                    "opposed-family-native-portfolio-input-unavailable"
                );
                return;
            }

            Dictionary<long, NetherStrategyNativeMechanic> mechanicsById = ownedMechanics
                .Where(mechanic => mechanic != null && mechanic.MechanicId > 0)
                .GroupBy(mechanic => mechanic.MechanicId)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single());
            long[] currentCodeIds = current
                .Select(code => code.CodeId)
                .ToArray();
            if (currentCodeIds.Any(codeId => !mechanicsById.ContainsKey(codeId)))
            {
                result[pair] = NetherFamilyRetentionEvidence.Unknown(
                    "opposed-family-owned-native-mechanic-unavailable"
                );
                return;
            }

            NetherStrategyNativeMechanic[] commonMechanics = current
                .Where(code => code.Family != first && code.Family != second)
                .Select(code => mechanicsById[code.CodeId])
                .ToArray();
            NetherStrategyNativeMechanic[] firstSideMechanics = current
                .Where(code => code.Family == first)
                .Select(code => mechanicsById[code.CodeId])
                .ToArray();
            NetherStrategyNativeMechanic[] secondSideMechanics = current
                .Where(code => code.Family == second)
                .Select(code => mechanicsById[code.CodeId])
                .ToArray();
            NetherStrategyNativeMechanic[] firstMechanics = commonMechanics
                .Concat(firstSideMechanics)
                .ToArray();
            NetherStrategyNativeMechanic[] secondMechanics = commonMechanics
                .Concat(secondSideMechanics)
                .ToArray();
            NativeSpecialComparisonMapResult specialComparisons = MapNativeComparisons(
                firstMechanics,
                secondMechanics,
                party
            );
            if (!specialComparisons.IsKnown)
            {
                result[pair] = NetherFamilyRetentionEvidence.Unknown(
                    specialComparisons.Error
                );
                return;
            }
            bool firstKnown = TryBuildNativePortfolioWindows(
                    firstMechanics,
                    party,
                    routeEvidence?.BossDurationSeconds ?? 0,
                    out IReadOnlyList<NetherNativeBuffWindow> firstWindows,
                    out string firstError
                );
            bool secondKnown = TryBuildNativePortfolioWindows(
                    secondMechanics,
                    party,
                    routeEvidence?.BossDurationSeconds ?? 0,
                    out IReadOnlyList<NetherNativeBuffWindow> secondWindows,
                    out string secondError
                );
            if (routeEvidence?.BossDurationKnown != true
                || routeEvidence.BossDurationSeconds <= 0
                || !firstKnown || !secondKnown)
            {
                result[pair] = NetherFamilyRetentionEvidence.Unknown(
                    routeEvidence?.BossDurationKnown != true
                        ? routeEvidence?.BossDurationUnknownReason
                            ?? "boss-duration-unavailable"
                        : firstError.Length > 0 ? firstError : secondError
                );
                return;
            }

            NetherMechanismPortfolioEntry[] firstValues = firstMechanics
                .Select(mechanic => new NetherMechanismPortfolioEntry(
                    mechanic.MechanicId,
                    MapMechanismValue(mechanic, party, routeEvidence)
                ))
                .ToArray();
            NetherMechanismPortfolioEntry[] secondValues = secondMechanics
                .Select(mechanic => new NetherMechanismPortfolioEntry(
                    mechanic.MechanicId,
                    MapMechanismValue(mechanic, party, routeEvidence)
                ))
                .ToArray();
            NetherCompletePortfolioComparison comparison = new NetherEquipmentCodeValuePolicy()
                .CompareCompletePortfolios(
                    new NetherNativePortfolioComparisonInput(
                        firstWindows,
                        secondWindows,
                        routeEvidence.BossDurationSeconds
                    ),
                    NetherMechanismPortfolioComparisonEvidence.Known(
                        firstValues,
                        secondValues
                    ),
                    specialComparisons.Comparisons,
                    MapRecipientPositions(party)
                );
            NetherCodeFamily preferred = comparison.Preference switch
            {
                NetherCompletePortfolioPreference.Left => first,
                NetherCompletePortfolioPreference.Right => second,
                _ => NetherCodeFamily.Unknown,
            };
            result[pair] = comparison.Preference switch
            {
                NetherCompletePortfolioPreference.Left or NetherCompletePortfolioPreference.Right =>
                    NetherFamilyRetentionEvidence.Known(preferred),
                NetherCompletePortfolioPreference.Equal =>
                    NetherFamilyRetentionEvidence.Equal(comparison.Detail),
                _ => NetherFamilyRetentionEvidence.Unknown(comparison.Detail),
            };
        }
    }

    private static NetherCodeHardEligibilityEvidence MapHardEligibility(
        NetherCodeCandidate candidate,
        NetherStrategyNativeMechanic mechanic
    )
    {
        if (!mechanic.IsKnown)
        {
            return new NetherCodeHardEligibilityEvidence
            {
                IsKnown = false,
                UnknownReason = string.IsNullOrWhiteSpace(mechanic.UnknownReason)
                    ? "offered-code-native-mechanic-unknown:" + mechanic.MechanicId
                    : mechanic.UnknownReason,
            };
        }

        NetherCodeRiskRule risk = mechanic.SourceEffectType is
            NetherCodeMasterEffectType.ErosionAdditionUp or
            NetherCodeMasterEffectType.ErosionRateUp
                ? NetherCodeRiskRule.AdverseErosionAdjustment
                : NetherCodeRiskRule.None;
        bool aboveSeventy = mechanic.Triggers.Any(trigger =>
            trigger.IsKnown
            && trigger.Kind == NetherStrategyTriggerKind.AboveErosion
            && trigger.Parameter1 >= 70
        );
        if (aboveSeventy)
            risk = NetherCodeRiskRule.MinimumErosionSeventy;
        bool aboveFifty = mechanic.Triggers.Any(trigger =>
            trigger.IsKnown
            && trigger.Kind == NetherStrategyTriggerKind.AboveErosion
            && trigger.Parameter1 >= 50
        );
        bool startsBattle = mechanic.Triggers.Any(trigger =>
            trigger.IsKnown && trigger.Kind == NetherStrategyTriggerKind.StartBattle
        );
        bool isCurrentRiskManaThreshold = candidate.Family == NetherCodeFamily.Risk
            && startsBattle
            && aboveFifty
            && mechanic.AbilityEffect.Kind == NetherStrategyAbilityEffectKind.ChargeMana
            && mechanic.AbilityEffect.ParametersKnown
            && mechanic.AbilityEffect.ManaEnergy > 0;
        // Fresh current native assets 40022/40023 are Risk-family StartBattle mana grants guarded
        // by AboveErosion(50). There is no native BelowErosion(70) situation: 70 is the strategic
        // route-horizon ceiling enforced by IsHardEligible after this exact classification.
        if (risk == NetherCodeRiskRule.None && isCurrentRiskManaThreshold)
            risk = NetherCodeRiskRule.ConditionalFiftyToSeventy;

        NetherCodeFamily crestFamily = mechanic.BuffStrategies.Any(
            row => row.IsKnown
                && row.BuffType.Value == (int)NetherKnownBuffType.CrestPassion
        ) ? NetherCodeFamily.Rush : mechanic.BuffStrategies.Any(
            row => row.IsKnown
                && row.BuffType.Value == (int)NetherKnownBuffType.CrestImpact
        ) ? NetherCodeFamily.Impact : NetherCodeFamily.Unknown;
        bool mappedCrestTarget = crestFamily == NetherCodeFamily.Unknown
            || TryMapTargetRow(mechanic.Target, out _, out _);
        NetherCodeTargetRow targetRow = crestFamily == NetherCodeFamily.Unknown
            ? NetherCodeTargetRow.None
            : TryMapTargetRow(mechanic.Target, out NetherCodeTargetRow mapped, out _)
                ? mapped
                : NetherCodeTargetRow.None;
        bool crestTargetKnown = crestFamily == NetherCodeFamily.Unknown
            || mappedCrestTarget;

        return new NetherCodeHardEligibilityEvidence
        {
            IsKnown = crestTargetKnown,
            UnknownReason = crestTargetKnown
                ? string.Empty
                : "uniform-crest-target-row-unavailable:" + mechanic.MechanicId,
            UniformCrestFamily = crestFamily,
            UniformCrestTargetRow = targetRow,
            RiskRule = risk,
        };
    }

    private static NetherMechanismValue MapMechanismValue(
        NetherStrategyNativeMechanic mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        NetherCodePolicyRouteEvidence? routeEvidence
    )
    {
        if (!mechanic.IsKnown)
            return NetherMechanismValue.Missing(mechanic.UnknownReason);
        NetherMechanismClassification classification = ClassifyMechanism(mechanic);
        if (classification.Kind is not (
                NetherMechanismClassificationKind.Unsupported
                or NetherMechanismClassificationKind.SharedMana)
            && !TryMapTargetRow(
                mechanic.Target,
                out _,
                out string targetError
            ))
        {
            return NetherMechanismValue.Missing(
                targetError + ":" + mechanic.MechanicId
            );
        }
        if (classification.Parameter != null)
        {
            if (!classification.Parameter.IsKnown || party == null)
            {
                return NetherMechanismValue.Missing(
                    "native-target-filter-parameters-unavailable:" + mechanic.MechanicId
                );
            }
            NetherTargetMatch? unknownTarget = party
                .Where(member => member != null && member.IsAlive)
                .Select(member => MatchTarget(mechanic, classification.Parameter, member))
                .FirstOrDefault(match => match.Kind == NetherTargetMatchKind.Unknown);
            if (unknownTarget != null)
                return NetherMechanismValue.Missing(unknownTarget.Detail);
        }
        if (classification.Kind == NetherMechanismClassificationKind.ForceChain)
        {
            NetherStrategyTriggerEvidence force = mechanic.Triggers.First(trigger =>
                trigger.Kind == NetherStrategyTriggerKind.ActivateForceChain
            );
            return new NetherMechanismSpecificValuation().EvaluateForceChainPayoff(
                new NetherForceChainPayoffInput(
                    CompletionTriggerKnown: force.IsKnown,
                    CompletionMessageReachable: force.IsKnown,
                    TargetRow: TryMapTargetRow(
                        mechanic.Target,
                        out NetherCodeTargetRow forceTarget,
                        out _
                    ) ? forceTarget : NetherCodeTargetRow.None,
                    NumericalEffectKnown: mechanic.AbilityEffect.IsKnown
                )
            );
        }
        if (classification.Kind is NetherMechanismClassificationKind.CriticalProbability
            or NetherMechanismClassificationKind.ContinuousAttackProbability)
        {
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.None,
                0,
                "native-special-probability-comparison"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.Defense)
        {
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.None,
                0,
                "native-special-defense-comparison"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.OrdinaryPortfolio)
        {
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.None,
                0,
                "native-retained-portfolio-comparison"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.SharedMana)
        {
            if (party == null)
                return NetherMechanismValue.Missing("shared-mana-target-party-unavailable");
            NetherTargetMatch[] targetRows = party
                .Where(member => member != null && member.IsAlive)
                .Select(member => MatchAbilityTarget(mechanic, member))
                .ToArray();
            NetherTargetMatch? unknownTarget = targetRows.FirstOrDefault(row =>
                row.Kind == NetherTargetMatchKind.Unknown);
            if (unknownTarget != null)
                return NetherMechanismValue.Missing(unknownTarget.Detail);
            int recipients = targetRows.Count(row => row.Kind == NetherTargetMatchKind.Match);
            if (recipients == 0)
            {
                return NetherMechanismValue.Quantified(
                    NetherMechanismQuantityKind.SharedManaEnergy,
                    0,
                    "shared-mana-no-authoritative-trigger-recipient"
                );
            }
            return NetherMechanismValue.ReachableUnquantified(
                "code-offer-lifecycle-shared-mana-pool-and-modifier-chain-unavailable;"
                    + "exact-trigger-recipient-count=" + recipients
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.InitialSkillCharge)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "code-offer-lifecycle-live-skill-charge-recipient-state-unavailable"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.StackLinked)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "stack-timeline-or-guaranteed-lower-bound-unavailable"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.ErosionLinked)
        {
            return MapErosionLinkedValue(mechanic, party, routeEvidence);
        }
        if (classification.Kind == NetherMechanismClassificationKind.CrestPayoff)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "crest-provider-consumer-ability-paths-unavailable"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.RecurringSkillCharge)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "code-offer-lifecycle-recurring-skill-charge-timeline-unavailable"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.UniformCrestGrant)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "crest-provider-consumer-ability-paths-unavailable"
            );
        }
        return NetherMechanismValue.ReachableUnquantified(
            "native-mechanic-known;future-trigger-or-timeline-unavailable"
        );
    }

    private enum NetherMechanismClassificationKind
    {
        Unsupported = 0,
        ForceChain,
        CriticalProbability,
        ContinuousAttackProbability,
        Defense,
        OrdinaryPortfolio,
        SharedMana,
        InitialSkillCharge,
        RecurringSkillCharge,
        StackLinked,
        ErosionLinked,
        CrestPayoff,
        UniformCrestGrant,
    }

    private readonly record struct NetherMechanismClassification(
        NetherMechanismClassificationKind Kind,
        NetherCombatMetricKind Metric,
        NetherStrategyBuffParameterEvidence? Parameter
    );

    private static NetherMechanismClassification ClassifyMechanism(
        NetherStrategyNativeMechanic? mechanic
    )
    {
        if (mechanic == null || !mechanic.IsKnown)
            return default;
        if (mechanic.Triggers.Any(trigger =>
                trigger.IsKnown && trigger.Kind == NetherStrategyTriggerKind.ActivateForceChain))
            return new(NetherMechanismClassificationKind.ForceChain, default, null);
        if (TryGetBuiltInBuffShape(
                mechanic,
                NetherKnownBuffType.CriticalUp,
                NetherStrategyBuffParameterReferenceKind.FixedPermille,
                out _,
                out _,
                out NetherStrategyBuffParameterEvidence? criticalParameter
            ))
            return new(
                NetherMechanismClassificationKind.CriticalProbability,
                NetherCombatMetricKind.CriticalProbability,
                criticalParameter
            );
        if (TryGetBuiltInBuffShape(
                mechanic,
                NetherKnownBuffType.ContinuousAttackProbabilityUp,
                NetherStrategyBuffParameterReferenceKind.FixedPermille,
                out _,
                out _,
                out NetherStrategyBuffParameterEvidence? continuousParameter
            ))
            return new(
                NetherMechanismClassificationKind.ContinuousAttackProbability,
                NetherCombatMetricKind.ContinuousAttackProbability,
                continuousParameter
            );
        if (TryGetBuiltInBuffShape(
                mechanic,
                NetherKnownBuffType.DefenceUp,
                NetherStrategyBuffParameterReferenceKind.RatePermille,
                out _,
                out _,
                out NetherStrategyBuffParameterEvidence? defenseParameter
            ))
            return new(
                NetherMechanismClassificationKind.Defense,
                NetherCombatMetricKind.Defence,
                defenseParameter
            );
        if (TryGetOrdinaryPortfolioMetric(
                mechanic,
                out NetherCombatMetricKind metric,
                out NetherStrategyBuffParameterEvidence? parameter
            ))
            return new(NetherMechanismClassificationKind.OrdinaryPortfolio, metric, parameter);

        NetherMechanismClassificationKind effect = mechanic.AbilityEffect.Kind switch
        {
            NetherStrategyAbilityEffectKind.ChargeMana =>
                NetherMechanismClassificationKind.SharedMana,
            NetherStrategyAbilityEffectKind.SkillCharge =>
                NetherMechanismClassificationKind.InitialSkillCharge,
            NetherStrategyAbilityEffectKind.StackLinkedBuff =>
                NetherMechanismClassificationKind.StackLinked,
            NetherStrategyAbilityEffectKind.ErosionLinkedBuff =>
                NetherMechanismClassificationKind.ErosionLinked,
            _ => NetherMechanismClassificationKind.Unsupported,
        };
        if (effect == NetherMechanismClassificationKind.ErosionLinked)
        {
            NetherStrategyBuffParameterEvidence? minimum =
                mechanic.AbilityEffect.MinLinkedBuff?.BuffParameter;
            return new(effect, minimum == null ? default : MetricFor(minimum), minimum);
        }
        if (effect != NetherMechanismClassificationKind.Unsupported)
            return new(effect, default, null);
        if (IsCrestPayoffTrigger(mechanic))
            return new(NetherMechanismClassificationKind.CrestPayoff, default, null);
        if (mechanic.AbilityEffect.BuffParameters.Any(row =>
                row != null && row.IsKnown
                && row.BuffType.Value == (int)NetherKnownBuffType.SkillChargeEfficiency))
            return new(NetherMechanismClassificationKind.RecurringSkillCharge, default, null);
        if (mechanic.BuffStrategies.Any(row => row.IsKnown && row.BuffType.Value is
                (int)NetherKnownBuffType.CrestPassion or
                (int)NetherKnownBuffType.CrestImpact))
            return new(NetherMechanismClassificationKind.UniformCrestGrant, default, null);
        return default;
    }

    private static bool IsCrestPayoffTrigger(NetherStrategyNativeMechanic mechanic) =>
        mechanic.Triggers.Any(trigger =>
            trigger.IsKnown
            && (trigger.Kind is NetherStrategyTriggerKind.ReceiveBuff
                or NetherStrategyTriggerKind.SpendBuff)
            && (trigger.Parameter1 is
                (int)NetherKnownBuffType.CrestPassion
                or (int)NetherKnownBuffType.CrestImpact)
        );

    private static NetherMechanismValue MapErosionLinkedValue(
        NetherStrategyNativeMechanic mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        NetherCodePolicyRouteEvidence? routeEvidence
    )
    {
        NetherStrategyLinkedBuffThresholdEvidence? minimum = mechanic.AbilityEffect.MinLinkedBuff;
        NetherStrategyLinkedBuffThresholdEvidence? maximum = mechanic.AbilityEffect.MaxLinkedBuff;
        if (minimum == null || maximum == null
            || minimum.BuffParameter == null || maximum.BuffParameter == null
            || !minimum.BuffParameter.IsKnown || !maximum.BuffParameter.IsKnown
            || minimum.BuffParameter.BuffType != maximum.BuffParameter.BuffType
            || !minimum.BuffParameter.ParameterReference.IsKnown
            || !maximum.BuffParameter.ParameterReference.IsKnown
            || minimum.BuffParameter.ParameterReference.Kind
                != maximum.BuffParameter.ParameterReference.Kind)
        {
            return NetherMechanismValue.Missing(
                "erosion-linked-native-threshold-relationship-unavailable"
            );
        }
        NetherCombatMetricKind metric = MetricFor(minimum.BuffParameter);
        NetherStrategyBuffParameterReferenceKind expectedReference = ReferenceKindFor(metric);
        if (metric == NetherCombatMetricKind.Unknown
            || expectedReference == NetherStrategyBuffParameterReferenceKind.Unknown
            || minimum.BuffParameter.ParameterReference.Kind != expectedReference)
        {
            return NetherMechanismValue.Missing(
                "erosion-linked-native-buff-domain-unavailable:"
                    + minimum.BuffParameter.BuffType.Value
            );
        }
        if (routeEvidence?.IsKnown != true || routeEvidence.ConfirmedCombats.Count == 0)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "confirmed-route-combat-erosion-unavailable"
            );
        }
        if (party == null)
            return NetherMechanismValue.Missing("erosion-linked-target-party-unavailable");
        var targetRows = party
            .Where(member => member != null && member.IsAlive)
            // AbilityErosionLinkedBuff.Param.Create stores MinParameter.TargetFilter as its
            // runtime filter; Max supplies interpolation value only. This mirrors the exact
            // IAbilityPassiveBuff.TryGetTargetFilter implementation rather than the unrelated
            // first AbilityEffect.BuffParameters entry.
            .Select(member => new
            {
                Member = member,
                Match = MatchTarget(mechanic, minimum.BuffParameter, member),
            })
            .ToArray();
        NetherTargetMatch? unknownTarget = targetRows.FirstOrDefault(row =>
            row.Match.Kind == NetherTargetMatchKind.Unknown)?.Match;
        if (unknownTarget != null)
            return NetherMechanismValue.Missing(unknownTarget.Detail);
        var recipients = targetRows.Where(row => row.Match.Kind == NetherTargetMatchKind.Match)
            .Select(row => row.Member)
            .ToArray();
        if (recipients.Length == 0)
        {
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.ErosionLinkedPayoff,
                0,
                "erosion-linked-no-authoritative-recipient",
                minimum.BuffParameter.BuffType,
                minimum.BuffParameter.ParameterReference.Kind
            );
        }
        NetherMechanismValue value = new NetherMechanismSpecificValuation()
            .EvaluateErosionLinkedPayoff(new NetherErosionLinkedPayoffInput(
                minimum.PerMille,
                maximum.PerMille,
                minimum.BuffParameter.ParameterReference.Value,
                maximum.BuffParameter.ParameterReference.Value,
                routeEvidence.ConfirmedCombats
            )
            {
                BuffType = minimum.BuffParameter.BuffType,
                ParameterReferenceKind = minimum.BuffParameter.ParameterReference.Kind,
            });
        return value.Kind == NetherCombatValueEvidenceKind.Quantified
            ? value with
            {
                RecipientQuantities = recipients.Select(member =>
                    new NetherMechanismRecipientQuantity(
                        member.CharacterId,
                        member.PartyPosition,
                        metric,
                        value.Quantity
                    )
                ).ToArray(),
                Detail = value.Detail + ";exact-recipient-count=" + recipients.Length,
            }
            : value;
    }

    private static NetherEquipmentCombatTier MapCombatTier(
        NetherStrategyNativeMechanic? mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party
    )
    {
        if (mechanic == null || !mechanic.IsKnown)
            return NetherEquipmentCombatTier.None;
        NetherCodeTargetRow row = TryMapTargetRow(mechanic.Target, out NetherCodeTargetRow mapped, out _)
            ? mapped
            : NetherCodeTargetRow.None;
        NetherMechanismClassification classification = ClassifyMechanism(mechanic);
        if (classification.Kind == NetherMechanismClassificationKind.ForceChain)
        {
            return NetherEquipmentCombatTierClassifier.ForQualitative(
                row is NetherCodeTargetRow.Back or NetherCodeTargetRow.All
                    ? NetherMechanismQualitativePriority.BackForceChainHigh
                    : row == NetherCodeTargetRow.Forward
                        ? NetherMechanismQualitativePriority.FrontForceChainFallback
                        : NetherMechanismQualitativePriority.None
            );
        }
        if (classification.Parameter == null || party == null)
            return NetherEquipmentCombatTier.None;
        var targets = party
            .Where(member => member != null && member.IsAlive)
            .Select(member => new
            {
                Member = member,
                Match = MatchTarget(mechanic, classification.Parameter, member),
            })
            .ToArray();
        if (targets.Any(target => target.Match.Kind == NetherTargetMatchKind.Unknown))
            return NetherEquipmentCombatTier.None;
        NetherPartyPosition[] recipients = targets
            .Where(target => target.Match.Kind == NetherTargetMatchKind.Match)
            .Select(target => target.Member.PartyPosition)
            .ToArray();
        if (recipients.Length == 0)
            return NetherEquipmentCombatTier.None;
        return NetherEquipmentCombatTierClassifier.ForMetric(classification.Metric, recipients);
    }

    private static IReadOnlyDictionary<long, NetherPartyPosition> MapRecipientPositions(
        IReadOnlyList<NetherStrategyPartyMember>? party
    ) => party == null
        ? new Dictionary<long, NetherPartyPosition>()
        : party.Where(member => member != null && member.IsAlive && member.CharacterId > 0)
            .GroupBy(member => member.CharacterId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().PartyPosition);

    private static NetherStrategyBuffParameterReferenceKind ReferenceKindFor(
        NetherCombatMetricKind metric
    ) => metric switch
    {
        NetherCombatMetricKind.Attack or NetherCombatMetricKind.Defence
            or NetherCombatMetricKind.MaxHp =>
            NetherStrategyBuffParameterReferenceKind.RatePermille,
        NetherCombatMetricKind.DamageModifier or NetherCombatMetricKind.TakenDamage
            or NetherCombatMetricKind.Resistance or NetherCombatMetricKind.ElementDamage
            or NetherCombatMetricKind.CriticalProbability
            or NetherCombatMetricKind.ContinuousAttackProbability =>
            NetherStrategyBuffParameterReferenceKind.FixedPermille,
        _ => NetherStrategyBuffParameterReferenceKind.Unknown,
    };

    private static NetherCombatMetricKind MetricFor(NetherStrategyBuffParameterEvidence row)
    {
        NetherKnownBuffType buffType = (NetherKnownBuffType)row.BuffType.Value;
        return buffType switch
        {
            NetherKnownBuffType.AttackUp1 or NetherKnownBuffType.AttackUp2 =>
                NetherCombatMetricKind.Attack,
            NetherKnownBuffType.DefenceUp => NetherCombatMetricKind.Defence,
            NetherKnownBuffType.CriticalUp => NetherCombatMetricKind.CriticalProbability,
            NetherKnownBuffType.ContinuousAttackProbabilityUp =>
                NetherCombatMetricKind.ContinuousAttackProbability,
            NetherKnownBuffType.MaxHpRateUp => NetherCombatMetricKind.MaxHp,
            NetherKnownBuffType.DamageUp => NetherCombatMetricKind.DamageModifier,
            NetherKnownBuffType.TakenDamageDown => NetherCombatMetricKind.TakenDamage,
            NetherKnownBuffType.DebuffResistProbabilityUp
                or NetherKnownBuffType.AbnormalResistProbabilityUp
                or NetherKnownBuffType.AbnormalBurntResistProbabilityUp
                or NetherKnownBuffType.AbnormalFrozenResistProbabilityUp
                or NetherKnownBuffType.AbnormalParalysisResistProbabilityUp
                or NetherKnownBuffType.AbnormalStonedResistProbabilityUp
                or NetherKnownBuffType.AbnormalCharmedResistProbabilityUp
                or NetherKnownBuffType.AbnormalLossResistProbabilityUp =>
                NetherCombatMetricKind.Resistance,
            NetherKnownBuffType.ElementArtifactTargetDamageUp
                or NetherKnownBuffType.ElementFireTargetDamageUp
                or NetherKnownBuffType.ElementWaterTargetDamageUp
                or NetherKnownBuffType.ElementEarthTargetDamageUp
                or NetherKnownBuffType.ElementLightTargetDamageUp
                or NetherKnownBuffType.ElementDarkTargetDamageUp =>
                NetherCombatMetricKind.ElementDamage,
            _ => NetherCombatMetricKind.Unknown,
        };
    }

    private static bool TryMapTargetRow(
        NetherStrategyTargetEvidence target,
        out NetherCodeTargetRow row,
        out string error
    )
    {
        row = NetherCodeTargetRow.None;
        error = string.Empty;
        if (!target.IsKnown)
        {
            error = string.IsNullOrWhiteSpace(target.UnknownReason)
                ? "native-target-parameters-unavailable"
                : target.UnknownReason;
            return false;
        }
        if (target.Kind != NetherStrategyTargetKind.Friend)
        {
            error = "native-target-kind-not-authoritatively-mapped:" + target.Kind;
            return false;
        }
        int rawFlags = (int)target.PartyPositionFlags;
        if ((rawFlags & ~0x0e) != 0)
        {
            error = "native-target-unknown-flag-bits:" + rawFlags;
            return false;
        }
        if (target.ElementTypeFlags != 0 || target.UnionTypeFlags != 0
            || target.SearchType != 0 || target.RandomCount != 0)
        {
            error = "native-target-live-relationship-unavailable";
            return false;
        }
        if (target.PartyPositionFlags == NetherPartyPositionFlags.Forward)
            row = NetherCodeTargetRow.Forward;
        else if (target.PartyPositionFlags == NetherPartyPositionFlags.Back)
            row = NetherCodeTargetRow.Back;
        else if (target.PartyPositionFlags == (
                NetherPartyPositionFlags.Forward
                | NetherPartyPositionFlags.Back
                | NetherPartyPositionFlags.Assist
            ))
            row = NetherCodeTargetRow.All;
        else
        {
            error = "native-target-position-combination-unsupported:" + rawFlags;
            return false;
        }
        return true;
    }

    private sealed record NativeSpecialComparisonMapResult(
        bool IsKnown,
        IReadOnlyList<NetherNativeSpecialComparisonEvidence> Comparisons,
        string Error
    )
    {
        public static NativeSpecialComparisonMapResult Known(
            IReadOnlyList<NetherNativeSpecialComparisonEvidence> comparisons
        ) => new(true, comparisons, string.Empty);

        public static NativeSpecialComparisonMapResult Unknown(string error) => new(
            false,
            Array.Empty<NetherNativeSpecialComparisonEvidence>(),
            error
        );
    }

    private static NativeSpecialComparisonMapResult MapNativeComparisons(
        NetherStrategyNativeMechanic? candidate,
        long removalCodeId,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics
    )
    {
        if (candidate == null || ownedMechanics == null)
        {
            return NativeSpecialComparisonMapResult.Unknown(
                "native-special-comparison-input-unavailable"
            );
        }
        NetherStrategyNativeMechanic[] before = ownedMechanics.ToArray();
        NetherStrategyNativeMechanic[] after = ownedMechanics
            .Where(mechanic => mechanic != null && mechanic.MechanicId != removalCodeId)
            .Append(candidate)
            .ToArray();
        return MapNativeComparisons(before, after, party);
    }

    private static NativeSpecialComparisonMapResult MapNativeComparisons(
        IReadOnlyList<NetherStrategyNativeMechanic> beforePortfolio,
        IReadOnlyList<NetherStrategyNativeMechanic> afterPortfolio,
        IReadOnlyList<NetherStrategyPartyMember>? party
    )
    {
        if (beforePortfolio == null || afterPortfolio == null)
        {
            return NativeSpecialComparisonMapResult.Unknown(
                "native-special-comparison-input-unavailable"
            );
        }
        HashSet<long> retainedIds = beforePortfolio
            .Where(row => row != null)
            .Select(row => row.MechanicId)
            .Intersect(afterPortfolio.Where(row => row != null).Select(row => row.MechanicId))
            .ToHashSet();
        NetherStrategyNativeMechanic[] changedBefore = beforePortfolio
            .Where(row => row != null && !retainedIds.Contains(row.MechanicId))
            .ToArray();
        NetherStrategyNativeMechanic[] changedAfter = afterPortfolio
            .Where(row => row != null && !retainedIds.Contains(row.MechanicId))
            .ToArray();
        NetherMechanismClassificationKind[] changedKinds = changedBefore.Concat(changedAfter)
            .Select(row => ClassifyMechanism(row).Kind)
            .ToArray();
        bool hasCritical = changedKinds.Contains(
            NetherMechanismClassificationKind.CriticalProbability
        );
        bool hasContinuous = changedKinds.Contains(
            NetherMechanismClassificationKind.ContinuousAttackProbability
        );
        bool hasDefense = changedBefore.Concat(changedAfter).Any(mechanic =>
        {
            NetherMechanismClassification classification = ClassifyMechanism(mechanic);
            return classification.Kind == NetherMechanismClassificationKind.Defense
                || classification.Kind == NetherMechanismClassificationKind.OrdinaryPortfolio
                    && classification.Metric is NetherCombatMetricKind.MaxHp
                        or NetherCombatMetricKind.TakenDamage;
        });
        if (!hasCritical && !hasContinuous && !hasDefense)
            return NativeSpecialComparisonMapResult.Known([]);
        if (party == null)
        {
            return NativeSpecialComparisonMapResult.Unknown(
                "native-special-comparison-party-unavailable"
            );
        }

        var comparisons = new List<NetherNativeSpecialComparisonEvidence>();
        if (hasCritical)
        {
            if (!TryBuildProbabilityComparison(
                beforePortfolio,
                afterPortfolio,
                party,
                NetherKnownBuffType.CriticalUp,
                NetherCharacterParameterKind.CriticalProbability,
                requireLiveMaximum: false,
                out IReadOnlyList<NetherCharacterProbabilityEvidence> criticalRows
            ))
            {
                return NativeSpecialComparisonMapResult.Unknown(
                    "native-special-comparison-critical-unavailable"
                );
            }
            comparisons.Add(NetherNativeSpecialComparisonEvidence.Critical(criticalRows));
        }
        if (hasContinuous)
        {
            if (!TryBuildProbabilityComparison(
                beforePortfolio,
                afterPortfolio,
                party,
                NetherKnownBuffType.ContinuousAttackProbabilityUp,
                NetherCharacterParameterKind.ContinuousAttackProbability,
                requireLiveMaximum: true,
                out IReadOnlyList<NetherCharacterProbabilityEvidence> continuousRows
            ))
            {
                return NativeSpecialComparisonMapResult.Unknown(
                    "native-special-comparison-continuous-unavailable"
                );
            }
            comparisons.Add(NetherNativeSpecialComparisonEvidence.Continuous(continuousRows));
        }
        if (hasDefense)
        {
            if (!TryBuildDefenseComparison(
                beforePortfolio,
                afterPortfolio,
                party,
                out IReadOnlyList<NetherCharacterEffectiveHpEvidence> defenseRows
            ))
            {
                return NativeSpecialComparisonMapResult.Unknown(
                    "native-special-comparison-defense-unavailable"
                );
            }
            comparisons.Add(NetherNativeSpecialComparisonEvidence.Defense(defenseRows));
        }
        return NativeSpecialComparisonMapResult.Known(comparisons);
    }

    private static bool TryBuildNativePortfolioComparison(
        NetherStrategyNativeMechanic? candidate,
        long removalCodeId,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        NetherCodePolicyRouteEvidence? routeEvidence,
        IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics,
        out NetherNativePortfolioComparisonInput comparison,
        out string error
    )
    {
        comparison = new NetherNativePortfolioComparisonInput([], [], BossDurationSeconds: 1);
        error = string.Empty;
        if (candidate == null || party == null || ownedMechanics == null)
        {
            error = "native-retained-portfolio-input-unavailable";
            return false;
        }
        if (routeEvidence?.BossDurationKnown != true || routeEvidence.BossDurationSeconds <= 0)
        {
            error = string.IsNullOrWhiteSpace(routeEvidence?.BossDurationUnknownReason)
                ? "boss-duration-unavailable"
                : routeEvidence.BossDurationUnknownReason;
            return false;
        }

        NetherStrategyNativeMechanic[] before = ownedMechanics.ToArray();
        NetherStrategyNativeMechanic[] after = ownedMechanics
            .Where(mechanic => mechanic != null && mechanic.MechanicId != removalCodeId)
            .Append(candidate)
            .ToArray();
        if (!TryBuildNativePortfolioWindows(
                before,
                party,
                routeEvidence.BossDurationSeconds,
                out IReadOnlyList<NetherNativeBuffWindow> beforeWindows,
                out error
            )
            || !TryBuildNativePortfolioWindows(
                after,
                party,
                routeEvidence.BossDurationSeconds,
                out IReadOnlyList<NetherNativeBuffWindow> afterWindows,
                out error
            ))
        {
            return false;
        }

        comparison = new NetherNativePortfolioComparisonInput(
            beforeWindows,
            afterWindows,
            routeEvidence.BossDurationSeconds
        );
        return true;
    }

    private static bool TryBuildNativePortfolioWindows(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        IReadOnlyList<NetherStrategyPartyMember> party,
        int bossDurationSeconds,
        out IReadOnlyList<NetherNativeBuffWindow> windows,
        out string error
    )
    {
        var mapped = new List<NetherNativeBuffWindow>();
        error = string.Empty;
        if (bossDurationSeconds <= 0)
        {
            windows = Array.Empty<NetherNativeBuffWindow>();
            error = "boss-duration-unavailable";
            return false;
        }
        foreach (NetherStrategyNativeMechanic mechanic in mechanics)
        {
            if (mechanic == null || !mechanic.IsKnown)
            {
                windows = Array.Empty<NetherNativeBuffWindow>();
                error = mechanic?.UnknownReason ?? "native-retained-portfolio-mechanic-unavailable";
                return false;
            }
            NetherMechanismClassification classification = ClassifyMechanism(mechanic);
            if (classification.Kind != NetherMechanismClassificationKind.OrdinaryPortfolio)
            {
                // These mechanisms are valued by their exact typed comparison channel below.
                if (classification.Kind != NetherMechanismClassificationKind.Unsupported)
                {
                    continue;
                }
                windows = Array.Empty<NetherNativeBuffWindow>();
                error = "native-retained-portfolio-mechanic-unsupported:" + mechanic.MechanicId;
                return false;
            }
            NetherCombatMetricKind metric = classification.Metric;
            NetherStrategyBuffParameterEvidence? parameter = classification.Parameter;
            if (parameter == null)
            {
                windows = Array.Empty<NetherNativeBuffWindow>();
                error = "native-retained-portfolio-parameter-unavailable:" + mechanic.MechanicId;
                return false;
            }

            NetherStrategyBuffEvidence[] strategies = mechanic.BuffStrategies
                .Where(row => row != null && row.IsKnown
                    && row.BuffType == parameter.BuffType)
                .ToArray();
            if (strategies.Length != 1
                || strategies[0].Coexistence is not (
                    NetherStrategyBuffCoexistenceKind.Allow
                    or NetherStrategyBuffCoexistenceKind.HigherValue))
            {
                windows = Array.Empty<NetherNativeBuffWindow>();
                error = "native-retained-portfolio-coexistence-unavailable:" + mechanic.MechanicId;
                return false;
            }
            NetherStrategyBuffParameterReferenceEvidence reference = parameter.ParameterReference;
            if (!TryBuildNativeSchedule(
                    mechanic,
                    bossDurationSeconds,
                    out IReadOnlyList<(int StartSecond, int DurationSeconds)> schedule,
                    out error
                ))
            {
                windows = Array.Empty<NetherNativeBuffWindow>();
                return false;
            }
            foreach (NetherStrategyPartyMember member in party
                         .Where(member => member != null && member.IsAlive)
                         .OrderBy(member => member.PartyIndex))
            {
                NetherTargetMatch target = MatchTarget(mechanic, parameter, member);
                if (target.Kind == NetherTargetMatchKind.Unknown)
                {
                    windows = Array.Empty<NetherNativeBuffWindow>();
                    error = target.Detail;
                    return false;
                }
                if (target.Kind == NetherTargetMatchKind.NoMatch)
                    continue;
                int triggerOrder = 0;
                foreach ((int startSecond, int durationSeconds) in schedule)
                {
                    mapped.Add(new NetherNativeBuffWindow(
                        mechanic.MechanicId,
                        member.CharacterId,
                        parameter.BuffType,
                        strategies[0].EffectKind,
                        strategies[0].Coexistence,
                        metric,
                        reference.Value,
                        startSecond,
                        durationSeconds
                    )
                    {
                        MatchedBuffTypes = strategies[0].AdditionalMatchedTypes.ToArray(),
                        PositiveCumulativeLimit = reference.Limit,
                        TriggerOrder = triggerOrder++,
                    });
                }
            }
        }
        windows = mapped;
        return true;
    }

    private static bool TryGetOrdinaryPortfolioMetric(
        NetherStrategyNativeMechanic? mechanic,
        out NetherCombatMetricKind metric,
        out NetherStrategyBuffParameterEvidence? parameter
    )
    {
        metric = NetherCombatMetricKind.Unknown;
        parameter = null;
        if (mechanic == null || !mechanic.IsKnown)
        {
            return false;
        }
        bool passiveBuiltIn = mechanic.AbilityEffect.Kind
                == NetherStrategyAbilityEffectKind.PassiveBuff
            && mechanic.Triggers.Count == 1
            && mechanic.Triggers[0].Kind == NetherStrategyTriggerKind.BuiltIn;
        bool timedParameter = mechanic.AbilityEffect.Kind
                == NetherStrategyAbilityEffectKind.ParameterBuff
            && mechanic.Triggers.Count == 1
            && mechanic.Triggers[0].IsKnown
            && mechanic.Triggers[0].Kind is
                NetherStrategyTriggerKind.StartBattle or NetherStrategyTriggerKind.Duration;
        if (!passiveBuiltIn && !timedParameter)
            return false;
        (NetherStrategyBuffParameterEvidence Parameter, NetherCombatMetricKind Metric)[] parameters =
            mechanic.AbilityEffect.BuffParameters
            .Where(row => row != null && row.IsKnown && row.ParameterReference.IsKnown)
            .Select(row => (Parameter: row, Metric: MetricFor(row)))
            .Where(row => row.Metric != NetherCombatMetricKind.Unknown)
            .ToArray();
        if (parameters.Length != 1)
            return false;
        NetherStrategyBuffParameterEvidence selected = parameters[0].Parameter;
        NetherStrategyBuffParameterReferenceEvidence reference = selected.ParameterReference;
        if (reference.ValueType != 0 || reference.Value < 0 || reference.Limit < 0)
            return false;
        NetherCombatMetricKind selectedMetric = parameters[0].Metric;
        NetherStrategyBuffParameterReferenceKind expectedReference = ReferenceKindFor(
            selectedMetric
        );
        if (reference.Kind != expectedReference)
            return false;
        metric = selectedMetric;
        parameter = selected;
        return true;
    }

    private static bool TryBuildNativeSchedule(
        NetherStrategyNativeMechanic mechanic,
        int bossDurationSeconds,
        out IReadOnlyList<(int StartSecond, int DurationSeconds)> schedule,
        out string error
    )
    {
        schedule = Array.Empty<(int, int)>();
        error = string.Empty;
        if (mechanic.AbilityEffect.Kind == NetherStrategyAbilityEffectKind.PassiveBuff
            && mechanic.Triggers.Count == 1
            && mechanic.Triggers[0].Kind == NetherStrategyTriggerKind.BuiltIn)
        {
            if (!IsDeterministicTrigger(mechanic.Triggers[0]))
            {
                error = "native-buff-trigger-control-unavailable:" + mechanic.MechanicId;
                return false;
            }
            schedule = new[] { (0, bossDurationSeconds) };
            return true;
        }
        if (mechanic.AbilityEffect.Kind != NetherStrategyAbilityEffectKind.ParameterBuff
            || mechanic.Triggers.Count != 1
            || !mechanic.AbilityEffect.EndSituationKnown
            || mechanic.AbilityEffect.EndSituationCondition != 7
            || mechanic.AbilityEffect.EndSituationValue <= 0
            || mechanic.AbilityEffect.EndSituationValue % 1000 != 0)
        {
            error = "native-buff-duration-relationship-unavailable:" + mechanic.MechanicId;
            return false;
        }

        NetherStrategyTriggerEvidence trigger = mechanic.Triggers[0];
        if (!IsDeterministicTrigger(trigger))
        {
            error = "native-buff-trigger-control-unavailable:" + mechanic.MechanicId;
            return false;
        }
        int durationSeconds = mechanic.AbilityEffect.EndSituationValue / 1000;
        if (trigger.Kind == NetherStrategyTriggerKind.StartBattle)
        {
            schedule = new[] { (0, durationSeconds) };
            return true;
        }
        if (trigger.Kind != NetherStrategyTriggerKind.Duration
            || !mechanic.DurationKnown || mechanic.Duration <= 0
            || mechanic.Duration % 1000 != 0
            || trigger.Parameter1 != mechanic.Duration)
        {
            error = "native-repeat-interval-unavailable:" + mechanic.MechanicId;
            return false;
        }
        int periodSeconds = mechanic.Duration / 1000;
        schedule = Enumerable.Range(1, Math.Max(0, (bossDurationSeconds - 1) / periodSeconds))
            .Select(index => (index * periodSeconds, durationSeconds))
            .ToArray();
        return true;
    }

    private static bool IsDeterministicTrigger(NetherStrategyTriggerEvidence trigger)
    {
        NetherStrategyTriggerControlEvidence control = trigger.ControlRelationships;
        return trigger.IsKnown
            && control.IsKnown
            && (control.ProbabilityType == NetherStrategyTriggerProbabilityType.NotApplicable
                || (control.ProbabilityType == NetherStrategyTriggerProbabilityType.Fixed
                    && control.FixedProbabilityPermille == 1000))
            && control.ExecuteCountLimit?.IsKnown == true
            && control.ExecuteCountLimit.Kind == NetherStrategyExecuteCountLimitKind.None
            && control.SituationCosts.Count == 0;
    }

    private static bool TryBuildDefenseComparison(
        NetherStrategyNativeMechanic candidate,
        long removalCodeId,
        IReadOnlyList<NetherStrategyPartyMember> party,
        IReadOnlyList<NetherStrategyNativeMechanic> ownedMechanics,
        out IReadOnlyList<NetherCharacterEffectiveHpEvidence> rows
    )
    => TryBuildDefenseComparison(
        ownedMechanics.ToArray(),
        ownedMechanics
            .Where(mechanic => mechanic.MechanicId != removalCodeId)
            .Append(candidate)
            .ToArray(),
        party,
        out rows
    );

    private static bool TryBuildDefenseComparison(
        IReadOnlyList<NetherStrategyNativeMechanic> beforeMechanics,
        IReadOnlyList<NetherStrategyNativeMechanic> afterMechanics,
        IReadOnlyList<NetherStrategyPartyMember> party,
        out IReadOnlyList<NetherCharacterEffectiveHpEvidence> rows
    )
    {
        rows = Array.Empty<NetherCharacterEffectiveHpEvidence>();
        if (beforeMechanics.Any(mechanic => mechanic == null || !mechanic.IsKnown)
            || afterMechanics.Any(mechanic => mechanic == null || !mechanic.IsKnown))
        {
            return false;
        }
        var mapped = new List<NetherCharacterEffectiveHpEvidence>();
        foreach (NetherStrategyPartyMember member in party
                     .Where(member => member != null && member.IsAlive)
                     .OrderBy(member => member.PartyIndex))
        {
            if (!member.EffectiveParametersKnown
                || !member.ParameterCalculationsKnown
                || !TryGetEffectiveParameter(member, NetherCharacterParameterKind.Hp, out int beforeHp)
                || !TryGetEffectiveParameter(member, NetherCharacterParameterKind.Defence, out int beforeDefence)
                || !TryGetCalculation(
                    member,
                    NetherCharacterParameterKind.Hp,
                    out NetherStrategyParameterCalculationEvidence hpCalculation)
                || !TryGetCalculation(
                    member,
                    NetherCharacterParameterKind.Defence,
                    out NetherStrategyParameterCalculationEvidence defenceCalculation)
                || !NetherNativeUnitParameterProjection.TryCalculate(
                    hpCalculation,
                    additionalAllTargetModifier: 0,
                    out int capturedBeforeHp)
                || capturedBeforeHp != beforeHp
                || !NetherNativeUnitParameterProjection.TryCalculate(
                    defenceCalculation,
                    additionalAllTargetModifier: 0,
                    out int capturedBeforeDefence)
                || capturedBeforeDefence != beforeDefence
                || !TryCombinedDefensiveBuffValue(
                    beforeMechanics,
                    member,
                    NetherKnownBuffType.MaxHpRateUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int beforeMaxHpBuff)
                || !TryCombinedDefensiveBuffValue(
                    afterMechanics,
                    member,
                    NetherKnownBuffType.MaxHpRateUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int afterMaxHpBuff)
                || !TryCombinedDefensiveBuffValue(
                    beforeMechanics,
                    member,
                    NetherKnownBuffType.DefenceUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int beforeDefenceBuff)
                || !TryCombinedDefensiveBuffValue(
                    afterMechanics,
                    member,
                    NetherKnownBuffType.DefenceUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int afterDefenceBuff)
                || !TryCombinedDefensiveBuffValue(
                    beforeMechanics,
                    member,
                    NetherKnownBuffType.TakenDamageDown,
                    NetherStrategyBuffParameterReferenceKind.FixedPermille,
                    out int beforeTakenDamageDown)
                || !TryCombinedDefensiveBuffValue(
                    afterMechanics,
                    member,
                    NetherKnownBuffType.TakenDamageDown,
                    NetherStrategyBuffParameterReferenceKind.FixedPermille,
                    out int afterTakenDamageDown)
                || !NetherNativeUnitParameterProjection.TryCalculate(
                    hpCalculation,
                    checked(afterMaxHpBuff - beforeMaxHpBuff),
                    out int afterHp)
                || !NetherNativeUnitParameterProjection.TryCalculate(
                    defenceCalculation,
                    checked(afterDefenceBuff - beforeDefenceBuff),
                    out int afterDefence)
                || !TryEffectiveHp(
                    beforeHp,
                    beforeDefence,
                    beforeTakenDamageDown,
                    out decimal beforeEffectiveHp)
                || !TryEffectiveHp(
                    afterHp,
                    afterDefence,
                    afterTakenDamageDown,
                    out decimal afterEffectiveHp))
            {
                return false;
            }
            mapped.Add(new NetherCharacterEffectiveHpEvidence(
                member.CharacterId,
                member.PartyPosition,
                beforeEffectiveHp,
                afterEffectiveHp,
                IsKnown: true
            ));
        }
        if (mapped.Count == 0)
            return false;
        rows = mapped;
        return true;
    }

    private static bool TryEffectiveHp(
        int hp,
        int defence,
        int takenDamageDownPermille,
        out decimal effectiveHp
    )
    {
        effectiveHp = 0;
        // UnitDamageCalculator clamps (1000 - TotalDefence) to [0,1000]. A zero damage factor is
        // native saturation, not a finite scalar. DamageModifier independently subtracts the exact
        // TakenDamageDown fixed-permille value from the incoming-damage factor. Both must remain
        // finite to compare a complete defensive portfolio in one native EHP domain.
        if (hp <= 0 || defence < 0 || defence >= 1000
            || takenDamageDownPermille < 0 || takenDamageDownPermille >= 1000)
            return false;
        effectiveHp = hp * 1_000_000m
            / ((1000 - defence) * (1000 - takenDamageDownPermille));
        return true;
    }

    private static bool TryCombinedDefensiveBuffValue(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        NetherStrategyPartyMember member,
        NetherKnownBuffType buffType,
        NetherStrategyBuffParameterReferenceKind referenceKind,
        out int combined
    )
    {
        combined = 0;
        var contributions = new List<(
            int Value,
            int Limit,
            NetherStrategyBuffCoexistenceKind Coexistence
        )>();
        foreach (NetherStrategyNativeMechanic mechanic in mechanics)
        {
            NetherMechanismClassification classification = ClassifyMechanism(mechanic);
            bool belongsToDomain = buffType switch
            {
                NetherKnownBuffType.DefenceUp =>
                    classification.Kind == NetherMechanismClassificationKind.Defense,
                NetherKnownBuffType.MaxHpRateUp =>
                    classification.Kind == NetherMechanismClassificationKind.OrdinaryPortfolio
                        && classification.Metric == NetherCombatMetricKind.MaxHp,
                NetherKnownBuffType.TakenDamageDown =>
                    classification.Kind == NetherMechanismClassificationKind.OrdinaryPortfolio
                        && classification.Metric == NetherCombatMetricKind.TakenDamage,
                _ => false,
            };
            if (!belongsToDomain)
                continue;
            if (!TryGetBuiltInBuff(
                    mechanic,
                    buffType,
                    referenceKind,
                    out int value,
                    out int limit,
                    out NetherStrategyBuffParameterEvidence? parameter))
            {
                return false;
            }
            NetherTargetMatch target = MatchTarget(mechanic, parameter!, member);
            if (target.Kind == NetherTargetMatchKind.Unknown)
                return false;
            if (target.Kind == NetherTargetMatchKind.NoMatch)
                continue;
            NetherStrategyBuffEvidence[] strategies = mechanic.BuffStrategies
                .Where(row => row.IsKnown && row.BuffType.Value == (int)buffType)
                .ToArray();
            if (strategies.Length != 1)
                return false;
            contributions.Add((value, limit, strategies[0].Coexistence));
        }
        return TryCombineContributions(contributions, out combined);
    }

    private static bool TryBuildProbabilityComparison(
        NetherStrategyNativeMechanic candidate,
        long removalCodeId,
        IReadOnlyList<NetherStrategyPartyMember> party,
        IReadOnlyList<NetherStrategyNativeMechanic> ownedMechanics,
        NetherKnownBuffType buffType,
        NetherCharacterParameterKind parameterKind,
        bool requireLiveMaximum,
        out IReadOnlyList<NetherCharacterProbabilityEvidence> rows
    )
    => TryBuildProbabilityComparison(
        ownedMechanics.ToArray(),
        ownedMechanics
            .Where(mechanic => mechanic.MechanicId != removalCodeId)
            .Append(candidate)
            .ToArray(),
        party,
        buffType,
        parameterKind,
        requireLiveMaximum,
        out rows
    );

    private static bool TryBuildProbabilityComparison(
        IReadOnlyList<NetherStrategyNativeMechanic> beforeMechanics,
        IReadOnlyList<NetherStrategyNativeMechanic> afterMechanics,
        IReadOnlyList<NetherStrategyPartyMember> party,
        NetherKnownBuffType buffType,
        NetherCharacterParameterKind parameterKind,
        bool requireLiveMaximum,
        out IReadOnlyList<NetherCharacterProbabilityEvidence> rows
    )
    {
        rows = Array.Empty<NetherCharacterProbabilityEvidence>();
        if (beforeMechanics.Any(mechanic => mechanic == null || !mechanic.IsKnown)
            || afterMechanics.Any(mechanic => mechanic == null || !mechanic.IsKnown))
        {
            return false;
        }
        var mapped = new List<NetherCharacterProbabilityEvidence>();
        foreach (NetherStrategyPartyMember member in party
                     .Where(member => member != null && member.IsAlive)
                     .OrderBy(member => member.PartyIndex))
        {
            if (!member.EffectiveParametersKnown
                || !TryGetEffectiveParameter(member, parameterKind, out int before)
                || requireLiveMaximum && !member.ContinuousAttackCountMaximumKnown
                || !TryCombinedFixedBuffValue(beforeMechanics, member, buffType, out int currentBuff)
                || !TryCombinedFixedBuffValue(afterMechanics, member, buffType, out int afterBuff))
            {
                return false;
            }
            int portfolioBefore = checked(before + currentBuff);
            int after = checked(before + afterBuff);
            if (portfolioBefore < 0 || after < 0)
                return false;
            mapped.Add(new NetherCharacterProbabilityEvidence(
                member.CharacterId,
                portfolioBefore,
                after,
                requireLiveMaximum ? member.ContinuousAttackCountMaximum : 0,
                member.PartyPosition
            ));
        }
        if (mapped.Count == 0)
            return false;
        rows = mapped;
        return true;
    }

    private static bool TryCombinedFixedBuffValue(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        NetherStrategyPartyMember member,
        NetherKnownBuffType buffType,
        out int combined
    ) => TryCombinedBuiltInBuffValue(
        mechanics,
        member,
        buffType,
        NetherStrategyBuffParameterReferenceKind.FixedPermille,
        out combined
    );

    private static bool TryCombinedRateBuffValue(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        NetherStrategyPartyMember member,
        NetherKnownBuffType buffType,
        out int combined
    ) => TryCombinedBuiltInBuffValue(
        mechanics,
        member,
        buffType,
        NetherStrategyBuffParameterReferenceKind.RatePermille,
        out combined
    );

    private static bool TryCombinedBuiltInBuffValue(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        NetherStrategyPartyMember member,
        NetherKnownBuffType buffType,
        NetherStrategyBuffParameterReferenceKind referenceKind,
        out int combined
    )
    {
        combined = 0;
        var contributions = new List<(int Value, int Limit, NetherStrategyBuffCoexistenceKind Coexistence)>();
        foreach (NetherStrategyNativeMechanic mechanic in mechanics)
        {
            if (!TryGetBuiltInBuff(
                    mechanic,
                    buffType,
                    referenceKind,
                    out int value,
                    out int limit,
                    out NetherStrategyBuffParameterEvidence? parameter
                ))
            {
                if (TryGetBuiltInBuffShape(
                        mechanic,
                        buffType,
                        referenceKind,
                        out _,
                        out _,
                        out _
                    ))
                {
                    return false;
                }
                continue;
            }
            NetherTargetMatch target = MatchTarget(mechanic, parameter!, member);
            if (target.Kind == NetherTargetMatchKind.Unknown)
                return false;
            if (target.Kind == NetherTargetMatchKind.NoMatch)
                continue;
            NetherStrategyBuffEvidence[] strategies = mechanic.BuffStrategies
                .Where(row => row.IsKnown && row.BuffType.Value == (int)buffType)
                .ToArray();
            if (strategies.Length != 1)
                return false;
            contributions.Add((value, limit, strategies[0].Coexistence));
        }
        return TryCombineContributions(contributions, out combined);
    }

    private static bool TryGetBuiltInBuff(
        NetherStrategyNativeMechanic? mechanic,
        NetherKnownBuffType buffType,
        NetherStrategyBuffParameterReferenceKind referenceKind,
        out int value,
        out int limit,
        out NetherStrategyBuffParameterEvidence? parameter
    )
    {
        if (!TryGetBuiltInBuffShape(
                mechanic,
                buffType,
                referenceKind,
                out value,
                out limit,
                out parameter
            ))
        {
            return false;
        }
        NetherStrategyTriggerEvidence[] builtIn = mechanic!.Triggers
            .Where(trigger => trigger.Kind == NetherStrategyTriggerKind.BuiltIn)
            .ToArray();
        return builtIn.Length == 1 && IsDeterministicTrigger(builtIn[0]);
    }

    private static bool TryGetBuiltInBuffShape(
        NetherStrategyNativeMechanic? mechanic,
        NetherKnownBuffType buffType,
        NetherStrategyBuffParameterReferenceKind referenceKind,
        out int value,
        out int limit,
        out NetherStrategyBuffParameterEvidence? parameter
    )
    {
        value = 0;
        limit = 0;
        parameter = null;
        if (mechanic == null || !mechanic.IsKnown
            || mechanic.AbilityEffect.Kind != NetherStrategyAbilityEffectKind.PassiveBuff
            || !mechanic.Triggers.Any(trigger =>
                trigger.Kind == NetherStrategyTriggerKind.BuiltIn))
        {
            return false;
        }
        NetherStrategyBuffParameterEvidence[] parameters = mechanic.AbilityEffect.BuffParameters
            .Where(parameter => parameter != null
                && parameter.IsKnown
                && parameter.BuffType.Value == (int)buffType)
            .ToArray();
        if (parameters.Length != 1)
            return false;
        NetherStrategyBuffParameterReferenceEvidence reference = parameters[0].ParameterReference;
        if (!reference.IsKnown
            || reference.Kind != referenceKind
            || reference.ValueType != 0
            || reference.Value < 0 || reference.Limit < 0)
        {
            return false;
        }
        value = reference.Value;
        limit = reference.Limit;
        parameter = parameters[0];
        return true;
    }

    private static bool TryCombineContributions(
        IReadOnlyList<(int Value, int Limit, NetherStrategyBuffCoexistenceKind Coexistence)> contributions,
        out int combined
    )
    {
        combined = 0;
        if (contributions.Count == 0)
            return true;
        NetherStrategyBuffCoexistenceKind[] coexistence = contributions
            .Select(row => row.Coexistence)
            .Distinct()
            .ToArray();
        if (coexistence.Length != 1)
            return false;
        if (coexistence[0] == NetherStrategyBuffCoexistenceKind.Allow)
        {
            combined = checked(contributions.Sum(row => row.Value));
            int positiveLimit = contributions.Max(row => row.Limit);
            if (positiveLimit > 0)
                combined = Math.Min(combined, positiveLimit);
            return true;
        }
        if (coexistence[0] == NetherStrategyBuffCoexistenceKind.HigherValue)
        {
            combined = contributions.Max(row => row.Value);
            return true;
        }
        return false;
    }

    private enum NetherTargetMatchKind
    {
        Unknown = 0,
        NoMatch,
        Match,
    }

    private sealed record NetherTargetMatch(NetherTargetMatchKind Kind, string Detail)
    {
        public static NetherTargetMatch Match { get; } = new(NetherTargetMatchKind.Match, string.Empty);
        public static NetherTargetMatch NoMatch { get; } = new(NetherTargetMatchKind.NoMatch, string.Empty);
        public static NetherTargetMatch Unknown(string detail) => new(
            NetherTargetMatchKind.Unknown,
            string.IsNullOrWhiteSpace(detail) ? "native-target-filter-unavailable" : detail
        );
    }

    private static NetherTargetMatch MatchTarget(
        NetherStrategyNativeMechanic mechanic,
        NetherStrategyBuffParameterEvidence parameter,
        NetherStrategyPartyMember member
    )
    {
        if (!TryMapTargetRow(mechanic.Target, out NetherCodeTargetRow row, out string targetError))
            return NetherTargetMatch.Unknown(targetError + ":" + mechanic.MechanicId);
        bool targetMatches = row switch
        {
            NetherCodeTargetRow.Forward => member.PartyPosition == NetherPartyPosition.Forward,
            NetherCodeTargetRow.Back => member.PartyPosition == NetherPartyPosition.Back,
            NetherCodeTargetRow.All => member.PartyPosition is
                NetherPartyPosition.Forward or NetherPartyPosition.Back or NetherPartyPosition.Assist,
            _ => false,
        };
        if (!targetMatches)
            return NetherTargetMatch.NoMatch;
        NetherStrategyBuffTargetFilterEvidence? filter = parameter.TargetFilter;
        if (filter == null)
            return NetherTargetMatch.Match;
        if (!filter.IsKnown)
        {
            return NetherTargetMatch.Unknown(
                string.IsNullOrWhiteSpace(filter.UnknownReason)
                    ? "native-target-filter-parameters-unavailable:" + mechanic.MechanicId
                    : filter.UnknownReason + ":" + mechanic.MechanicId
            );
        }
        if ((filter.ElementTypeFlags & ~0x7e) != 0
            || (filter.ElementWeakTypeFlags & ~0x7e) != 0
            || ((int)filter.PartyPositionFlags & ~0x0e) != 0
            || (filter.UnionTypeFlags & ~0x3e) != 0
            || (filter.JobGroupFlags & ~0x00ff_ffff) != 0
            || (filter.JobSpeciesFlags & ~0x7e) != 0)
        {
            return NetherTargetMatch.Unknown(
                "native-target-filter-unknown-flag-bits:" + mechanic.MechanicId
            );
        }
        if (filter.RequiredBuffTypes.Count > 0
            || filter.ElementWeakTypeFlags != 0 || filter.UnionTypeFlags != 0
            || filter.JobGroupFlags != 0 || filter.JobSpeciesFlags != 0
            || filter.CharacterSizeFlags != 0)
        {
            // BuffTargetFilter.IsMatchTarget evaluates these live unit/buff relationships. The
            // immutable offer party evidence does not expose them, so this dependent mechanic is
            // unknown rather than falsely treated as having no recipients.
            return NetherTargetMatch.Unknown(
                "native-target-filter-live-relationship-unavailable:" + mechanic.MechanicId
            );
        }
        if (filter.ElementTypeFlags != 0)
        {
            // Fresh Project.Master evidence: ElementType values Artifact..Dark are 1..6 while
            // ElementTypeFlag values are the exact independent flags 2,4,8,16,32,64. Keep the
            // relationship explicit so a future enum value fails closed instead of relying on
            // ordinal arithmetic.
            int elementFlag = member.ElementType switch
            {
                1 => 2,
                2 => 4,
                3 => 8,
                4 => 16,
                5 => 32,
                6 => 64,
                _ => 0,
            };
            if (elementFlag == 0)
            {
                return NetherTargetMatch.Unknown(
                    "native-target-filter-element-unavailable:" + mechanic.MechanicId
                );
            }
            if ((filter.ElementTypeFlags & elementFlag) == 0)
                return NetherTargetMatch.NoMatch;
        }
        return filter.PartyPositionFlags == NetherPartyPositionFlags.None
            || (filter.PartyPositionFlags & PositionFlag(member.PartyPosition)) != 0
                ? NetherTargetMatch.Match
                : NetherTargetMatch.NoMatch;
    }

    private static NetherTargetMatch MatchAbilityTarget(
        NetherStrategyNativeMechanic mechanic,
        NetherStrategyPartyMember member
    )
    {
        NetherStrategyTargetEvidence target = mechanic.Target;
        if (!target.IsKnown)
        {
            return NetherTargetMatch.Unknown(
                (string.IsNullOrWhiteSpace(target.UnknownReason)
                    ? "native-mana-target-parameters-unavailable"
                    : target.UnknownReason) + ":" + mechanic.MechanicId
            );
        }
        if (target.Kind != NetherStrategyTargetKind.Friend)
        {
            return NetherTargetMatch.Unknown(
                "native-mana-target-kind-not-authoritatively-mapped:"
                    + target.Kind + ":" + mechanic.MechanicId
            );
        }
        int positionFlags = (int)target.PartyPositionFlags;
        if ((positionFlags & ~0x0e) != 0 || (target.ElementTypeFlags & ~0x7e) != 0
            || (target.UnionTypeFlags & ~0x3e) != 0)
        {
            return NetherTargetMatch.Unknown(
                "native-mana-target-unknown-flag-bits:" + mechanic.MechanicId
            );
        }
        if (target.UnionTypeFlags != 0 || target.SearchType != 0 || target.RandomCount != 0)
        {
            // AbilityTargetGroupBase resolves these against live unit/selection relationships that
            // the immutable offer package does not expose. The party-global mana pool matters only
            // after the native target resolver proves at least one trigger recipient.
            return NetherTargetMatch.Unknown(
                "native-mana-target-live-relationship-unavailable:" + mechanic.MechanicId
            );
        }
        if (target.PartyPositionFlags == NetherPartyPositionFlags.None)
        {
            return NetherTargetMatch.Unknown(
                "native-mana-target-position-unavailable:" + mechanic.MechanicId
            );
        }
        if ((target.PartyPositionFlags & PositionFlag(member.PartyPosition)) == 0)
            return NetherTargetMatch.NoMatch;
        if (target.ElementTypeFlags == 0)
            return NetherTargetMatch.Match;

        int elementFlag = member.ElementType switch
        {
            1 => 2,
            2 => 4,
            3 => 8,
            4 => 16,
            5 => 32,
            6 => 64,
            _ => 0,
        };
        if (elementFlag == 0)
        {
            return NetherTargetMatch.Unknown(
                "native-mana-target-element-unavailable:" + mechanic.MechanicId
            );
        }
        return (target.ElementTypeFlags & elementFlag) != 0
            ? NetherTargetMatch.Match
            : NetherTargetMatch.NoMatch;
    }

    private static NetherPartyPositionFlags PositionFlag(NetherPartyPosition position) => position switch
    {
        NetherPartyPosition.Forward => NetherPartyPositionFlags.Forward,
        NetherPartyPosition.Back => NetherPartyPositionFlags.Back,
        NetherPartyPosition.Assist => NetherPartyPositionFlags.Assist,
        _ => NetherPartyPositionFlags.None,
    };

    private static bool TryGetEffectiveParameter(
        NetherStrategyPartyMember member,
        NetherCharacterParameterKind kind,
        out int value
    )
    {
        NetherStrategyEffectiveParameter[] rows = member.EffectiveParameters
            .Where(row => row.Kind == kind)
            .ToArray();
        value = rows.Length == 1 ? rows[0].Value : 0;
        return rows.Length == 1;
    }

    private static bool TryGetCalculation(
        NetherStrategyPartyMember member,
        NetherCharacterParameterKind kind,
        out NetherStrategyParameterCalculationEvidence calculation
    )
    {
        NetherStrategyParameterCalculationEvidence[] rows = member.ParameterCalculations
            .Where(row => row.Kind == kind)
            .ToArray();
        calculation = rows.Length == 1 ? rows[0] : default;
        return rows.Length == 1;
    }

    private static NetherCodeFamily ResolveActiveResearchFamily(
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherStrategyResearchFamilyState>? research
    )
    {
        if (settings.StrategyMode != NetherStrategyMode.Research || research == null)
            return NetherCodeFamily.Unknown;
        if (IsIncomplete(settings.ResearchPrimaryFamily, research))
            return settings.ResearchPrimaryFamily;
        if (IsIncomplete(settings.ResearchSecondaryFamily, research))
            return settings.ResearchSecondaryFamily;
        return NetherCodeFamily.Unknown;
    }

    private static bool IsIncomplete(
        NetherCodeFamily family,
        IReadOnlyList<NetherStrategyResearchFamilyState> research
    )
    {
        if (family == NetherCodeFamily.Unknown)
            return false;
        NetherStrategyResearchFamilyState[] matches = research
            .Where(row => row.Family == family)
            .ToArray();
        return matches.Length == 1
            && matches[0].IsProjectedNormalSettlementKnown
            && (long)matches[0].WalletPoints + matches[0].ProjectedNormalSettlementPoints
                < ResearchCompletionPoints;
    }
}
