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
            hard[candidate.CodeId] = MapHardEligibility(candidate, mechanic, party);
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
                    Survival = MapSurvivalRepair(routeEvidence, removal),
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
            HardExcludedCodeIds = BuildHardExcludedCodeIds(snapshot, ownedMechanics, party),
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

    private static IReadOnlyList<long> BuildHardExcludedCodeIds(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherStrategyNativeMechanic>? ownedMechanics,
        IReadOnlyList<NetherStrategyPartyMember>? party
    )
    {
        if (ownedMechanics == null)
            return Array.Empty<long>();

        var result = new List<long>();
        foreach (NetherCodeState code in snapshot.Codes
                     .Where(code => code != null && code.CodeId > 0 && code.PossessionAmount > 0)
                     .GroupBy(code => code.CodeId)
                     .Select(group => group.First()))
        {
            NetherStrategyNativeMechanic[] matches = ownedMechanics
                .Where(mechanic => mechanic != null && mechanic.MechanicId == code.CodeId)
                .ToArray();
            if (matches.Length != 1)
                continue;
            NetherCodeCandidate candidate = new(code.CodeId, code.Family, code.AbilityLevel)
            {
                Category = code.Category,
                Rarity = code.Rarity,
                Power = code.Power,
                IsKnown = code.IsKnown,
                EffectSemanticsKnown = code.EffectSemanticsKnown,
            };
            NetherCodeHardEligibilityEvidence hard = MapHardEligibility(candidate, matches[0], party);
            if (hard.IsKnown && hard.RiskRule is
                    NetherCodeRiskRule.MinimumErosionSeventy
                    or NetherCodeRiskRule.AdverseErosionAdjustment)
            {
                result.Add(code.CodeId);
            }
        }
        return result.OrderBy(codeId => codeId).ToArray();
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
        NetherCodePolicyRouteEvidence? routeEvidence,
        long removalCodeId
    )
    {
        if (routeEvidence?.IsKnown != true)
        {
            return removalCodeId == 0 && routeEvidence?.IsBattleResultBeforeFloorRebind == true
                ? NetherSurvivalRepairEvidence.ResultOwnedAdditiveWithoutRouteBaseline(
                    routeEvidence.UnknownReason
                )
                : NetherSurvivalRepairEvidence.Unknown;
        }
        if (!routeEvidence.SurvivalBaselineKnown)
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
        NetherStrategyNativeMechanic mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party
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
            || TryMapTargetRow(mechanic, party, out _, out _);
        NetherCodeTargetRow targetRow = crestFamily == NetherCodeFamily.Unknown
            ? NetherCodeTargetRow.None
            : TryMapTargetRow(mechanic, party, out NetherCodeTargetRow mapped, out _)
                ? mapped
                : NetherCodeTargetRow.None;
        bool crestTargetKnown = crestFamily == NetherCodeFamily.Unknown
            || mappedCrestTarget;

        NetherStrategyResearchRateOverwriteEvidence researchRate =
            mechanic.ResearchRateOverwrite;
        if (researchRate.IsPresent
            && (!researchRate.IsKnown
                || researchRate.Family != candidate.Family
                || researchRate.Rate <= 0))
        {
            string reason = string.IsNullOrWhiteSpace(researchRate.UnknownReason)
                ? "selectable-research-rate-overwrite-unavailable:" + mechanic.MechanicId
                : researchRate.UnknownReason;
            return new NetherCodeHardEligibilityEvidence
            {
                IsKnown = false,
                UnknownReason = reason,
                UniformCrestFamily = crestFamily,
                UniformCrestTargetRow = targetRow,
                RiskRule = risk,
            };
        }

        return new NetherCodeHardEligibilityEvidence
        {
            IsKnown = crestTargetKnown,
            UnknownReason = crestTargetKnown
                ? string.Empty
                : "uniform-crest-target-row-unavailable:" + mechanic.MechanicId,
            UniformCrestFamily = crestFamily,
            UniformCrestTargetRow = targetRow,
            RiskRule = risk,
            ResearchRateOverwrite = researchRate.IsPresent ? researchRate.Rate : 0,
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
                mechanic,
                party,
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
                .Select(member => MatchTarget(mechanic, classification.Parameter, member, party))
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
                        mechanic,
                        party,
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
                .Select(member => MatchAbilityTarget(mechanic, member, party))
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
            return MapCrestPayoffValue(mechanic, classification, party);
        if (classification.Kind == NetherMechanismClassificationKind.RecurringSkillCharge)
        {
            return NetherMechanismValue.ReachableUnquantified(
                "code-offer-lifecycle-recurring-skill-charge-timeline-unavailable"
            );
        }
        if (classification.Kind == NetherMechanismClassificationKind.UniformCrestGrant)
            return MapUniformCrestGrantValue(mechanic, party);
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
        {
            return TryGetCrestPayoffParameter(
                mechanic,
                out NetherCombatMetricKind crestMetric,
                out NetherStrategyBuffParameterEvidence? crestParameter
            )
                ? new(
                    NetherMechanismClassificationKind.CrestPayoff,
                    crestMetric,
                    crestParameter
                )
                : new(NetherMechanismClassificationKind.CrestPayoff, default, null);
        }
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

    private static bool TryGetCrestPayoffParameter(
        NetherStrategyNativeMechanic mechanic,
        out NetherCombatMetricKind metric,
        out NetherStrategyBuffParameterEvidence? parameter
    )
    {
        metric = NetherCombatMetricKind.Unknown;
        parameter = null;
        (NetherStrategyBuffParameterEvidence Parameter, NetherCombatMetricKind Metric)[] rows =
            mechanic.AbilityEffect.BuffParameters
                .Where(row => row != null && row.IsKnown && row.ParameterReference.IsKnown)
                .Select(row => (Parameter: row, Metric: MetricFor(row)))
                .Where(row => row.Metric != NetherCombatMetricKind.Unknown)
                .Where(row => row.Parameter.ParameterReference.Kind == ReferenceKindFor(row.Metric)
                    && row.Parameter.ParameterReference.ValueType == 0
                    && row.Parameter.ParameterReference.Value >= 0
                    && row.Parameter.ParameterReference.Limit >= 0)
                .ToArray();
        if (rows.Length != 1)
            return false;
        metric = rows[0].Metric;
        parameter = rows[0].Parameter;
        return true;
    }

    private static NetherMechanismValue MapCrestPayoffValue(
        NetherStrategyNativeMechanic mechanic,
        NetherMechanismClassification classification,
        IReadOnlyList<NetherStrategyPartyMember>? party
    )
    {
        const string LegacyUnknown = "crest-provider-consumer-ability-paths-unavailable";
        if (party == null || party.Count == 0
            || party.Where(member => member != null && member.IsAlive)
                .Any(member => !member.AbilityMechanicsKnown))
        {
            return NetherMechanismValue.ReachableUnquantified(LegacyUnknown);
        }
        NetherStrategyBuffParameterEvidence? payoffParameter = classification.Parameter;
        if (payoffParameter == null || classification.Metric == NetherCombatMetricKind.Unknown)
            return NetherMechanismValue.Missing("crest-payoff-parameter-unavailable");

        NetherStrategyTriggerEvidence[] crestTriggers = mechanic.Triggers
            .Where(trigger => trigger.IsKnown
                && trigger.Kind is NetherStrategyTriggerKind.ReceiveBuff
                    or NetherStrategyTriggerKind.SpendBuff
                && trigger.Parameter1 is
                    (int)NetherKnownBuffType.CrestPassion
                    or (int)NetherKnownBuffType.CrestImpact)
            .ToArray();
        if (crestTriggers.Length != 1)
            return NetherMechanismValue.Missing("crest-payoff-trigger-relationship-unavailable");
        NetherStrategyTriggerEvidence crestTrigger = crestTriggers[0];
        NetherCrestIdentity crestIdentity = crestTrigger.Parameter1 switch
        {
            (int)NetherKnownBuffType.CrestPassion => NetherCrestIdentity.Passion,
            (int)NetherKnownBuffType.CrestImpact => NetherCrestIdentity.Impact,
            _ => NetherCrestIdentity.Unknown,
        };
        if (crestIdentity == NetherCrestIdentity.Unknown)
            return NetherMechanismValue.Missing("crest-payoff-native-crest-unavailable");

        var matched = party
            .Where(member => member != null && member.IsAlive)
            .Select(member => new
            {
                Member = member,
                Match = MatchTarget(mechanic, payoffParameter, member, party),
            })
            .ToArray();
        NetherTargetMatch? unknownTarget = matched.FirstOrDefault(row =>
            row.Match.Kind == NetherTargetMatchKind.Unknown)?.Match;
        if (unknownTarget != null)
            return NetherMechanismValue.Missing(unknownTarget.Detail);
        NetherStrategyPartyMember[] recipients = matched
            .Where(row => row.Match.Kind == NetherTargetMatchKind.Match)
            .Select(row => row.Member)
            .ToArray();
        if (recipients.Length == 0)
        {
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.CrestRecipientPayoff,
                0,
                "crest-payoff-no-authoritative-recipient",
                payoffParameter.BuffType,
                payoffParameter.ParameterReference.Kind
            );
        }

        var paths = new List<NetherCrestPayoffRecipient>(recipients.Length);
        foreach (NetherStrategyPartyMember recipient in recipients)
        {
            CrestPathEvidence provider = FindCrestProviderPath(
                party,
                recipient,
                crestTrigger.Parameter1
            );
            int candidateAbilityLevel = mechanic.MasterEffectParameter2 is >= 0 and <= int.MaxValue
                ? (int)mechanic.MasterEffectParameter2
                : 0;
            bool candidateTriggerReachable = IsTriggerControlReachable(
                crestTrigger,
                candidateAbilityLevel,
                requireNoSituationCost: true
            );
            CrestPathEvidence consumer;
            if (crestTrigger.Kind == NetherStrategyTriggerKind.ReceiveBuff)
            {
                consumer = new CrestPathEvidence(
                    PathKnown: true,
                    Reachable: provider.Reachable && candidateTriggerReachable
                );
            }
            else
            {
                CrestPathEvidence spend = FindCrestSpendPath(
                    party,
                    recipient,
                    crestTrigger.Parameter1
                );
                consumer = spend with
                {
                    Reachable = spend.Reachable && candidateTriggerReachable,
                };
            }
            paths.Add(new NetherCrestPayoffRecipient(recipient.CharacterId, crestIdentity)
            {
                ProviderPathKnown = provider.PathKnown,
                ProviderReachable = provider.Reachable,
                ConsumerPathKnown = consumer.PathKnown,
                ConsumerReachable = consumer.Reachable,
            });
        }

        int payoff = payoffParameter.ParameterReference.Value;
        NetherMechanismValue value = new NetherMechanismSpecificValuation().EvaluateCrestPayoff(
            new NetherCrestPayoffInput(paths, payoff)
        );
        if (value.Kind != NetherCombatValueEvidenceKind.Quantified)
            return value;
        var quantity = new NetherMechanismQuantity(
            NetherMechanismQuantityKind.CrestRecipientPayoff,
            value.Quantity.Value
        )
        {
            BuffType = payoffParameter.BuffType,
            ParameterReferenceKind = payoffParameter.ParameterReference.Kind,
        };
        return value with
        {
            Quantity = quantity,
            Detail = value.Detail + ";exact-recipient-count=" + recipients.Length,
            RecipientQuantities = recipients.Select((recipient, index) =>
                new NetherMechanismRecipientQuantity(
                    recipient.CharacterId,
                    recipient.PartyPosition,
                    classification.Metric,
                    new NetherMechanismQuantity(
                        NetherMechanismQuantityKind.CrestRecipientPayoff,
                        paths[index].ProviderReachable && paths[index].ConsumerReachable
                            ? payoff
                            : 0
                    )
                    {
                        BuffType = payoffParameter.BuffType,
                        ParameterReferenceKind = payoffParameter.ParameterReference.Kind,
                    }
                )
            ).ToArray(),
        };
    }

    private readonly record struct CrestPathEvidence(bool PathKnown, bool Reachable);

    private enum CrestProviderShapeKind
    {
        None = 0,
        Unknown,
        Exact,
    }

    private static CrestPathEvidence FindCrestProviderPath(
        IReadOnlyList<NetherStrategyPartyMember> party,
        NetherStrategyPartyMember recipient,
        int crestBuffType
    )
    {
        bool relevantUnknown = false;
        foreach (NetherStrategyPartyMember source in party
                     .Where(member => member != null && member.IsAlive)
                     .OrderBy(member => member.PartyIndex))
        {
            foreach (NetherStrategyAbilityEffect ability in PartyAbilityEffects(source))
            {
                NetherStrategyPartyAbilityMechanic? graph = ability.Mechanic;
                if (graph == null)
                {
                    relevantUnknown = true;
                    continue;
                }
                CrestProviderShapeKind shape = TryGetCrestProviderParameter(
                    graph,
                    crestBuffType,
                    out NetherStrategyBuffParameterEvidence? parameter
                );
                if (shape == CrestProviderShapeKind.None)
                    continue;
                if (shape == CrestProviderShapeKind.Unknown || parameter == null)
                {
                    relevantUnknown = true;
                    continue;
                }
                NetherTargetMatch target = MatchPartyAbilityTarget(
                    ability.EffectId,
                    graph,
                    parameter,
                    source,
                    recipient,
                    party
                );
                if (target.Kind == NetherTargetMatchKind.Unknown)
                {
                    relevantUnknown = true;
                    continue;
                }
                if (target.Kind == NetherTargetMatchKind.NoMatch)
                    continue;
                if (graph.Triggers.Count == 0
                    || graph.Triggers.Any(trigger => !trigger.IsKnown))
                {
                    relevantUnknown = true;
                    continue;
                }
                if (graph.Triggers.Any(trigger => trigger.Kind is
                        NetherStrategyTriggerKind.ReceiveBuff
                        or NetherStrategyTriggerKind.SpendBuff))
                {
                    // A crest grant that itself waits on a buff receipt/spend is not an independent
                    // seed for the candidate's provider path. Do not make a circular graph reachable.
                    continue;
                }
                if (graph.Triggers.All(trigger => IsTriggerControlReachable(
                        trigger,
                        ability.Level,
                        requireNoSituationCost: true
                    )))
                {
                    return new CrestPathEvidence(PathKnown: true, Reachable: true);
                }
            }
        }
        return relevantUnknown
            ? new CrestPathEvidence(PathKnown: false, Reachable: false)
            : new CrestPathEvidence(PathKnown: true, Reachable: false);
    }

    private static CrestPathEvidence FindCrestSpendPath(
        IReadOnlyList<NetherStrategyPartyMember> party,
        NetherStrategyPartyMember recipient,
        int crestBuffType
    )
    {
        if (!recipient.AbilityMechanicsKnown)
            return new CrestPathEvidence(PathKnown: false, Reachable: false);
        bool relevantUnknown = false;
        foreach (NetherStrategyAbilityEffect ability in PartyAbilityEffects(recipient))
        {
            NetherStrategyPartyAbilityMechanic? graph = ability.Mechanic;
            if (graph == null)
            {
                relevantUnknown = true;
                continue;
            }
            foreach (NetherStrategyTriggerEvidence trigger in graph.Triggers)
            {
                if (!trigger.IsKnown)
                {
                    relevantUnknown = true;
                    continue;
                }
                if (!IsTriggerControlReachable(
                        trigger,
                        ability.Level,
                        requireNoSituationCost: false
                    ))
                {
                    continue;
                }
                if (trigger.ControlRelationships.SituationCosts.Any(cost =>
                        IsMatchingCrestSpendCost(cost, ability.Level, crestBuffType)))
                {
                    return new CrestPathEvidence(PathKnown: true, Reachable: true);
                }
            }
        }
        return relevantUnknown
            ? new CrestPathEvidence(PathKnown: false, Reachable: false)
            : new CrestPathEvidence(PathKnown: true, Reachable: false);
    }

    private static IEnumerable<NetherStrategyAbilityEffect> PartyAbilityEffects(
        NetherStrategyPartyMember member
    ) => member.CharacterAbilityEffects
        .Concat(member.EquipmentAbilityEffects)
        .Concat(member.GeneralAbilityEffects);

    private static CrestProviderShapeKind TryGetCrestProviderParameter(
        NetherStrategyPartyAbilityMechanic graph,
        int crestBuffType,
        out NetherStrategyBuffParameterEvidence? parameter
    )
    {
        parameter = null;
        NetherStrategyBuffParameterEvidence[] matching = graph.AbilityEffect.BuffParameters
            .Where(row => row != null && row.BuffType.Value == crestBuffType)
            .ToArray();
        if (matching.Length == 0)
            return CrestProviderShapeKind.None;
        if (!graph.AbilityEffect.IsKnown
            || graph.AbilityEffect.Kind is not (
                NetherStrategyAbilityEffectKind.ParameterBuff
                or NetherStrategyAbilityEffectKind.PassiveBuff
                or NetherStrategyAbilityEffectKind.StackLinkedBuff)
            || matching.Length != 1
            || !matching[0].IsKnown
            || !matching[0].ParameterReference.IsKnown
            || graph.AbilityEffect.Conditions.Count != 0
            || matching[0].ParameterReference.Kind
                != NetherStrategyBuffParameterReferenceKind.CrestGrantStack
            || matching[0].ParameterReference.Value <= 0)
        {
            return CrestProviderShapeKind.Unknown;
        }
        NetherStrategyBuffEvidence[] strategies = graph.BuffStrategies
            .Where(row => row != null && row.BuffType.Value == crestBuffType)
            .ToArray();
        if (strategies.Length != 1
            || !strategies[0].IsKnown
            || strategies[0].EffectKind != NetherStrategyBuffEffectKind.Buff
            || strategies[0].StatusPriority != NetherStrategyStatusPriorityKind.Crest
            || strategies[0].Coexistence != NetherStrategyBuffCoexistenceKind.ExclusiveCrest)
        {
            return CrestProviderShapeKind.Unknown;
        }
        parameter = matching[0];
        return CrestProviderShapeKind.Exact;
    }

    private static bool IsTriggerControlReachable(
        NetherStrategyTriggerEvidence trigger,
        int level,
        bool requireNoSituationCost
    )
    {
        if (!trigger.IsKnown || trigger.Kind is
                NetherStrategyTriggerKind.Unknown or NetherStrategyTriggerKind.NativeRunState)
        {
            return false;
        }
        NetherStrategyTriggerControlEvidence control = trigger.ControlRelationships;
        bool probabilityReachable = control.ProbabilityType switch
        {
            NetherStrategyTriggerProbabilityType.NotApplicable => true,
            NetherStrategyTriggerProbabilityType.Fixed => control.FixedProbabilityPermille > 0,
            NetherStrategyTriggerProbabilityType.AbilityLevel when level is >= 1 and <= 10
                && control.LevelProbabilityPermille.Count >= level =>
                control.LevelProbabilityPermille[level - 1] > 0,
            _ => false,
        };
        if (!probabilityReachable || control.ExecuteCountLimit?.IsKnown != true)
            return false;
        NetherStrategyExecuteCountLimitEvidence limit = control.ExecuteCountLimit!;
        bool countReachable = limit.Kind switch
        {
            NetherStrategyExecuteCountLimitKind.None => true,
            NetherStrategyExecuteCountLimitKind.Battle
                or NetherStrategyExecuteCountLimitKind.Quest when limit.RawValueType == 0 =>
                limit.FixedCountLimit > 0,
            NetherStrategyExecuteCountLimitKind.Battle
                or NetherStrategyExecuteCountLimitKind.Quest when limit.RawValueType == 1
                    && level is >= 1 and <= 10
                    && limit.LevelCountLimits.Count >= level =>
                limit.LevelCountLimits[level - 1] > 0,
            _ => false,
        };
        if (!countReachable || requireNoSituationCost && control.SituationCosts.Count != 0)
            return false;
        return trigger.Kind switch
        {
            NetherStrategyTriggerKind.ActionCount => trigger.Parameter1 > 0,
            NetherStrategyTriggerKind.Duration => trigger.Parameter1 > 0,
            _ => true,
        };
    }

    private static bool IsMatchingCrestSpendCost(
        NetherStrategySituationCostEvidence cost,
        int level,
        int crestBuffType
    )
    {
        if (cost == null || !cost.IsKnown)
            return false;
        if (cost.Kind == NetherStrategySituationCostKind.BuffStack)
            return cost.BuffType == crestBuffType && cost.FixedStack > 0;
        return cost.Kind == NetherStrategySituationCostKind.BuffStackPerLevel
            && level is >= 1 and <= 10
            && cost.LevelBuffTypes.Count >= level
            && cost.LevelStacks.Count >= level
            && cost.LevelBuffTypes[level - 1] == crestBuffType
            && cost.LevelStacks[level - 1] > 0;
    }

    private static NetherMechanismValue MapUniformCrestGrantValue(
        NetherStrategyNativeMechanic mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party
    )
    {
        if (party == null)
            return NetherMechanismValue.Missing("uniform-crest-target-party-unavailable");
        if (mechanic.AbilityEffect.Kind != NetherStrategyAbilityEffectKind.ParameterBuff
            || !mechanic.AbilityEffect.ParametersKnown)
        {
            return NetherMechanismValue.Missing(
                "uniform-crest-parameter-buff-relationship-unavailable"
            );
        }
        if (mechanic.Triggers.Count == 0
            || mechanic.Triggers.Any(trigger => !trigger.IsKnown))
        {
            return NetherMechanismValue.Missing("uniform-crest-trigger-unavailable");
        }
        if (mechanic.Triggers.Any(trigger =>
                trigger.Kind != NetherStrategyTriggerKind.StartBattle))
        {
            return NetherMechanismValue.ReachableUnquantified(
                "uniform-crest-trigger-timeline-unavailable"
            );
        }

        NetherStrategyBuffEvidence[] strategies = mechanic.BuffStrategies
            .Where(row => row != null && row.BuffType.Value is
                (int)NetherKnownBuffType.CrestPassion or
                (int)NetherKnownBuffType.CrestImpact)
            .ToArray();
        if (strategies.Length != 1
            || !strategies[0].IsKnown
            || strategies[0].EffectKind != NetherStrategyBuffEffectKind.Buff
            || strategies[0].StatusPriority != NetherStrategyStatusPriorityKind.Crest
            || strategies[0].Coexistence != NetherStrategyBuffCoexistenceKind.ExclusiveCrest)
        {
            return NetherMechanismValue.Missing("uniform-crest-native-strategy-unavailable");
        }

        NetherStrategyBuffType buffType = strategies[0].BuffType;
        NetherStrategyBuffParameterEvidence[] parameters = mechanic.AbilityEffect.BuffParameters
            .Where(row => row != null && row.BuffType == buffType)
            .ToArray();
        if (parameters.Length != 1
            || !parameters[0].IsKnown
            || !parameters[0].ParameterReference.IsKnown
            || parameters[0].ParameterReference.Kind
                != NetherStrategyBuffParameterReferenceKind.CrestGrantStack)
        {
            return NetherMechanismValue.Missing("uniform-crest-grant-parameter-unavailable");
        }

        NetherStrategyBuffParameterEvidence parameter = parameters[0];
        NetherStrategyBuffParameterReferenceEvidence reference = parameter.ParameterReference;
        NetherStrategyNamedValue[] stackFields = reference.NativeValues?
            .Where(row => row.Name == "grantStackCount")
            .ToArray() ?? Array.Empty<NetherStrategyNamedValue>();
        if (stackFields.Length != 1
            || stackFields[0].Value < 0
            || stackFields[0].Value > int.MaxValue
            || reference.Value != (int)stackFields[0].Value)
        {
            return NetherMechanismValue.Missing("uniform-crest-grant-stack-unavailable");
        }

        NetherCombatMetricKind metric = buffType.Value switch
        {
            // Fresh current BuffCrestPassionStrategy.get_AffectCriticalUp returns BuffType 30;
            // BuffCrestImpactStrategy.get_AffectAttackUp returns BuffType 10. The exact native
            // quantity remains a Crest stack, while this metric tags only the recipient combat tier.
            (int)NetherKnownBuffType.CrestPassion => NetherCombatMetricKind.CriticalProbability,
            (int)NetherKnownBuffType.CrestImpact => NetherCombatMetricKind.Attack,
            _ => NetherCombatMetricKind.Unknown,
        };
        if (metric == NetherCombatMetricKind.Unknown)
            return NetherMechanismValue.Missing("uniform-crest-consumer-domain-unavailable");

        var targetRows = party
            .Where(member => member != null && member.IsAlive)
            .Select(member => new
            {
                Member = member,
                Match = MatchTarget(mechanic, parameter, member, party),
            })
            .ToArray();
        NetherTargetMatch? unknownTarget = targetRows.FirstOrDefault(row =>
            row.Match.Kind == NetherTargetMatchKind.Unknown)?.Match;
        if (unknownTarget != null)
            return NetherMechanismValue.Missing(unknownTarget.Detail);
        NetherStrategyPartyMember[] recipients = targetRows
            .Where(row => row.Match.Kind == NetherTargetMatchKind.Match)
            .Select(row => row.Member)
            .ToArray();
        if (recipients.Any(member => member.CharacterId <= 0)
            || recipients.Select(member => member.CharacterId).Distinct().Count()
                != recipients.Length)
        {
            return NetherMechanismValue.Missing("uniform-crest-recipient-identity-unavailable");
        }

        int grantStackCount = (int)stackFields[0].Value;
        var quantity = new NetherMechanismQuantity(
            NetherMechanismQuantityKind.CrestStackGrant,
            grantStackCount
        )
        {
            BuffType = buffType,
            ParameterReferenceKind = reference.Kind,
        };
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.CrestStackGrant,
            (decimal)grantStackCount * recipients.Length,
            "native-start-battle-uniform-crest-stack-grant;exact-recipient-count="
                + recipients.Length,
            buffType,
            reference.Kind
        ) with
        {
            RecipientQuantities = recipients.Select(member =>
                new NetherMechanismRecipientQuantity(
                    member.CharacterId,
                    member.PartyPosition,
                    metric,
                    quantity
                )
            ).ToArray(),
        };
    }

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
                Match = MatchTarget(mechanic, minimum.BuffParameter, member, party),
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
        NetherCodeTargetRow row = TryMapTargetRow(mechanic, party, out NetherCodeTargetRow mapped, out _)
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
                Match = MatchTarget(mechanic, classification.Parameter, member, party),
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
        NetherStrategyNativeMechanic mechanic,
        IReadOnlyList<NetherStrategyPartyMember>? party,
        out NetherCodeTargetRow row,
        out string error
    )
    {
        row = NetherCodeTargetRow.None;
        error = string.Empty;
        NetherStrategyTargetEvidence target = mechanic.Target;
        if (!target.IsKnown)
        {
            error = string.IsNullOrWhiteSpace(target.UnknownReason)
                ? "native-target-parameters-unavailable"
                : target.UnknownReason;
            return false;
        }
        if (target.Kind == NetherStrategyTargetKind.Self)
        {
            // Fresh native control flow is exact: NetherCodeAbilityController first applies
            // Ability.Scope.IsMatch to each party unit and installs the ability only on matches.
            // AbilityTargetSelf then resolves that installed ability's owner. Target=Self therefore
            // says where an installed ability lands; Scope says which party units own that ability.
            if (target.IgnoreDeadUnit
                || target.ElementTypeFlags != 0
                || target.PartyPositionFlags != NetherPartyPositionFlags.None
                || target.UnionTypeFlags != 0
                || target.JobGroupFlags != 0
                || target.JobSpeciesFlags != 0
                || target.CharacterSizeFlags != 0
                || target.RequiredBuffTypes == null
                || target.RequiredBuffTypes.Count != 0
                || target.SearchType != 0
                || target.RandomCount != 0
                || target.NearestCount != 0
                || target.CurrentHpLeastCount != 0)
            {
                error = "native-self-target-parameters-unavailable";
                return false;
            }
            NetherStrategyAbilityScopeEvidence scope = mechanic.Scope;
            if (!scope.IsKnown)
            {
                error = string.IsNullOrWhiteSpace(scope.UnknownReason)
                    ? "native-self-target-scope-unavailable"
                    : scope.UnknownReason;
                return false;
            }
            if (scope.Kind != NetherStrategyAbilityScopeKind.PlayerSide)
            {
                error = "native-self-target-scope-kind-not-authoritatively-mapped:"
                    + scope.Kind;
                return false;
            }
            if (!HasOnlyScopeFlagBits(scope.ElementTypeFlags, 0x7e)
                || !HasOnlyScopeFlagBits(scope.ManaTypeFlags, 0x0c)
                || !HasOnlyScopeFlagBits(scope.PartyPositionFlags, 0x0e)
                || !HasOnlyScopeFlagBits(scope.UnionTypeFlags, 0x3e)
                || !HasOnlyScopeFlagBits(scope.JobGroupFlags, 0x00ff_ffff)
                || !HasOnlyScopeFlagBits(scope.JobSpeciesFlags, 0x7e))
            {
                error = "native-self-target-scope-unknown-flag-bits";
                return false;
            }
            if (!mechanic.PartyCoverageKnown)
            {
                error = "native-self-target-scope-coverage-unavailable";
                return false;
            }
            if (mechanic.PartyCoverage <= 0)
            {
                error = "native-self-target-scope-has-no-recipient";
                return false;
            }

            if (party == null || party.Count == 0)
            {
                // Without character identities, same-popup positive coverage still proves a
                // recipient only when every non-position Scope filter is the native broad default.
                // The explicit position flag then identifies the exact recipient row (30008 is the
                // observed Assist shape) without inventing element/crest/job relationships.
                if (scope.ElementTypeFlags != -1
                    || scope.ManaTypeFlags != -1
                    || scope.UnionTypeFlags != -1
                    || scope.JobGroupFlags != -1
                    || scope.JobSpeciesFlags != -1)
                {
                    error = "native-self-target-filtered-scope-party-unavailable";
                    return false;
                }
                return TryMapSelfScopePositionRow(
                    scope.PartyPositionFlags,
                    out row,
                    out error
                );
            }

            var matched = new List<NetherStrategyPartyMember>();
            foreach (NetherStrategyPartyMember member in party)
            {
                NetherTargetMatch match = MatchSelfScopeMember(scope, member);
                if (match.Kind == NetherTargetMatchKind.Unknown)
                {
                    error = match.Detail;
                    return false;
                }
                if (match.Kind == NetherTargetMatchKind.Match)
                    matched.Add(member);
            }
            if (mechanic.PartyCoverage != matched.Count)
            {
                error = "native-self-target-scope-coverage-mismatch:"
                    + mechanic.PartyCoverage + ":" + matched.Count;
                return false;
            }
            int matchedPositionFlags = matched.Aggregate(
                0,
                (flags, member) => flags | (int)PositionFlag(member.PartyPosition)
            );
            return TryMapSelfScopePositionRow(
                matchedPositionFlags,
                out row,
                out error
            );
        }
        if (target.Kind != NetherStrategyTargetKind.Friend)
        {
            error = "native-target-kind-not-authoritatively-mapped:" + target.Kind;
            return false;
        }
        int rawFlags = (int)target.PartyPositionFlags;
        if (!HasOnlyScopeFlagBits(rawFlags, 0x0e)
            || !HasOnlyScopeFlagBits(target.ElementTypeFlags, 0x7e)
            || !HasOnlyScopeFlagBits(target.UnionTypeFlags, 0x3e)
            || !HasOnlyScopeFlagBits(target.JobGroupFlags, 0x00ff_ffff)
            || !HasOnlyScopeFlagBits(target.JobSpeciesFlags, 0x7e)
            || !HasOnlyScopeFlagBits(target.CharacterSizeFlags, 0x1e))
        {
            error = "native-target-unknown-flag-bits:" + rawFlags;
            return false;
        }
        if (target.RequiredBuffTypes == null
            || target.RequiredBuffTypes.Count != 0
            || target.ElementTypeFlags != -1
            || target.UnionTypeFlags != -1
            || target.JobGroupFlags != -1
            || target.JobSpeciesFlags != -1
            || target.CharacterSizeFlags != -1
            || target.SearchType != 0)
        {
            error = "native-target-live-relationship-unavailable";
            return false;
        }
        if (rawFlags == -1)
            row = NetherCodeTargetRow.All;
        else if (target.PartyPositionFlags == NetherPartyPositionFlags.Forward)
            row = NetherCodeTargetRow.Forward;
        else if (target.PartyPositionFlags == NetherPartyPositionFlags.Back)
            row = NetherCodeTargetRow.Back;
        else if (target.PartyPositionFlags == NetherPartyPositionFlags.Assist)
            row = NetherCodeTargetRow.Assist;
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

    private static bool TryMapSelfScopePositionRow(
        int rawFlags,
        out NetherCodeTargetRow row,
        out string error
    )
    {
        row = rawFlags switch
        {
            -1 or 0x0e => NetherCodeTargetRow.All,
            (int)NetherPartyPositionFlags.Forward => NetherCodeTargetRow.Forward,
            (int)NetherPartyPositionFlags.Back => NetherCodeTargetRow.Back,
            (int)NetherPartyPositionFlags.Assist => NetherCodeTargetRow.Assist,
            _ => NetherCodeTargetRow.None,
        };
        error = row == NetherCodeTargetRow.None
            ? "native-self-target-scope-position-combination-unsupported:" + rawFlags
            : string.Empty;
        return row != NetherCodeTargetRow.None;
    }

    private static bool HasOnlyScopeFlagBits(int value, int knownMask) =>
        value == -1 || value >= 0 && (value & ~knownMask) == 0;

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
        if (candidate == null || ownedMechanics == null)
        {
            error = "native-retained-portfolio-input-unavailable";
            return false;
        }

        NetherMechanismClassification candidateClassification = ClassifyMechanism(candidate);
        bool resultOwnedAddition = removalCodeId == 0
            && routeEvidence?.IsBattleResultBeforeFloorRebind == true;
        if (resultOwnedAddition
            && candidateClassification.Kind is not (
                NetherMechanismClassificationKind.Unsupported
                or NetherMechanismClassificationKind.OrdinaryPortfolio))
        {
            // TryBuildNativePortfolioWindows deliberately excludes every recognized typed
            // mechanism: those quantities are compared by the mechanism/special channels. A
            // result-page free addition removes no held Code, so a typed-only candidate leaves the
            // ordinary BuffController portfolio exactly unchanged. Empty before/after windows are
            // therefore the complete zero marginal and need no invented future route duration.
            comparison = new NetherNativePortfolioComparisonInput(
                BeforeWindows: [],
                AfterWindows: [],
                BossDurationSeconds: 1
            );
            return true;
        }

        if (party == null)
        {
            error = "native-retained-portfolio-party-unavailable";
            return false;
        }

        int comparisonSeconds;
        if (routeEvidence?.BossDurationKnown == true && routeEvidence.BossDurationSeconds > 0)
        {
            comparisonSeconds = routeEvidence.BossDurationSeconds;
        }
        else if (removalCodeId == 0 && routeEvidence?.IsBattleResultBeforeFloorRebind == true)
        {
            if (!CanUseBattleResultAdditiveUnitInterval(candidate))
            {
                error = "battle-result-additive-native-horizon-required:" + candidate.MechanicId;
                return false;
            }

            // The native result popup exists before the next route can be rebound. A unit interval
            // is complete only for an unconditional Allow addition, which cannot suppress retained
            // buffs, or a deterministic permanent BuiltIn buff. Finite non-Allow coexistence can
            // remove a retained group that never resumes and therefore still needs the real horizon.
            comparisonSeconds = 1;
        }
        else
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
                comparisonSeconds,
                out IReadOnlyList<NetherNativeBuffWindow> beforeWindows,
                out error
            )
            || !TryBuildNativePortfolioWindows(
                after,
                party,
                comparisonSeconds,
                out IReadOnlyList<NetherNativeBuffWindow> afterWindows,
                out error
            ))
        {
            return false;
        }

        comparison = new NetherNativePortfolioComparisonInput(
            beforeWindows,
            afterWindows,
            comparisonSeconds
        );
        return true;
    }

    private static bool CanUseBattleResultAdditiveUnitInterval(
        NetherStrategyNativeMechanic candidate
    )
    {
        if (!candidate.IsKnown
            || candidate.AbilityEffect.Conditions.Count != 0
            || candidate.BuffStrategies.Any(strategy => strategy == null || !strategy.IsKnown))
        {
            return false;
        }

        if (candidate.BuffStrategies.Count > 0
            && candidate.BuffStrategies.All(strategy =>
                strategy.Coexistence == NetherStrategyBuffCoexistenceKind.Allow))
        {
            return true;
        }

        return candidate.AbilityEffect.Kind == NetherStrategyAbilityEffectKind.PassiveBuff
            && candidate.Triggers.Count == 1
            && candidate.Triggers[0].Kind == NetherStrategyTriggerKind.BuiltIn
            && IsDeterministicTrigger(candidate.Triggers[0]);
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
                NetherTargetMatch target = MatchTarget(mechanic, parameter, member, party);
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
                    party,
                    NetherKnownBuffType.MaxHpRateUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int beforeMaxHpBuff)
                || !TryCombinedDefensiveBuffValue(
                    afterMechanics,
                    member,
                    party,
                    NetherKnownBuffType.MaxHpRateUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int afterMaxHpBuff)
                || !TryCombinedDefensiveBuffValue(
                    beforeMechanics,
                    member,
                    party,
                    NetherKnownBuffType.DefenceUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int beforeDefenceBuff)
                || !TryCombinedDefensiveBuffValue(
                    afterMechanics,
                    member,
                    party,
                    NetherKnownBuffType.DefenceUp,
                    NetherStrategyBuffParameterReferenceKind.RatePermille,
                    out int afterDefenceBuff)
                || !TryCombinedDefensiveBuffValue(
                    beforeMechanics,
                    member,
                    party,
                    NetherKnownBuffType.TakenDamageDown,
                    NetherStrategyBuffParameterReferenceKind.FixedPermille,
                    out int beforeTakenDamageDown)
                || !TryCombinedDefensiveBuffValue(
                    afterMechanics,
                    member,
                    party,
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
        IReadOnlyList<NetherStrategyPartyMember> party,
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
            NetherTargetMatch target = MatchTarget(mechanic, parameter!, member, party);
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
                || !TryCombinedFixedBuffValue(
                    beforeMechanics,
                    member,
                    party,
                    buffType,
                    out int currentBuff
                )
                || !TryCombinedFixedBuffValue(
                    afterMechanics,
                    member,
                    party,
                    buffType,
                    out int afterBuff
                ))
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
        IReadOnlyList<NetherStrategyPartyMember> party,
        NetherKnownBuffType buffType,
        out int combined
    ) => TryCombinedBuiltInBuffValue(
        mechanics,
        member,
        party,
        buffType,
        NetherStrategyBuffParameterReferenceKind.FixedPermille,
        out combined
    );

    private static bool TryCombinedRateBuffValue(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        NetherStrategyPartyMember member,
        IReadOnlyList<NetherStrategyPartyMember> party,
        NetherKnownBuffType buffType,
        out int combined
    ) => TryCombinedBuiltInBuffValue(
        mechanics,
        member,
        party,
        buffType,
        NetherStrategyBuffParameterReferenceKind.RatePermille,
        out combined
    );

    private static bool TryCombinedBuiltInBuffValue(
        IReadOnlyList<NetherStrategyNativeMechanic> mechanics,
        NetherStrategyPartyMember member,
        IReadOnlyList<NetherStrategyPartyMember> party,
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
            NetherTargetMatch target = MatchTarget(mechanic, parameter!, member, party);
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

    private static NetherTargetMatch MatchSelfScopeMember(
        NetherStrategyAbilityScopeEvidence scope,
        NetherStrategyPartyMember member
    )
    {
        if (!scope.IsKnown || scope.Kind != NetherStrategyAbilityScopeKind.PlayerSide)
        {
            return NetherTargetMatch.Unknown(
                string.IsNullOrWhiteSpace(scope.UnknownReason)
                    ? "native-self-target-scope-unavailable"
                    : scope.UnknownReason
            );
        }
        if (scope.IgnoreDeadUnit && !member.IsAlive)
            return NetherTargetMatch.NoMatch;

        // Fresh AbilityScopePlayerSide.IsMatch treats the native None/Unknown element values as
        // unrestricted. Concrete ElementType values 1..6 map to flags 2..64.
        if (scope.ElementTypeFlags != -1
            && member.ElementType is not 0 and not 99)
        {
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
                return NetherTargetMatch.Unknown("native-self-target-scope-element-unavailable");
            if ((scope.ElementTypeFlags & elementFlag) == 0)
                return NetherTargetMatch.NoMatch;
        }

        // General/None mana is an explicit native bypass. Passion and Impact are flags 4 and 8.
        if (scope.ManaTypeFlags != -1
            && member.Crest is not NetherCrestIdentity.Unknown and not NetherCrestIdentity.General)
        {
            int manaFlag = member.Crest switch
            {
                NetherCrestIdentity.Passion => 4,
                NetherCrestIdentity.Impact => 8,
                _ => 0,
            };
            if (manaFlag == 0)
                return NetherTargetMatch.Unknown("native-self-target-scope-mana-unavailable");
            if ((scope.ManaTypeFlags & manaFlag) == 0)
                return NetherTargetMatch.NoMatch;
        }

        if (scope.PartyPositionFlags != -1
            && member.PartyPosition != NetherPartyPosition.Unknown
            && (scope.PartyPositionFlags & (int)PositionFlag(member.PartyPosition)) == 0)
        {
            return NetherTargetMatch.NoMatch;
        }

        if (scope.UnionTypeFlags != -1
            || scope.JobGroupFlags != -1
            || scope.JobSpeciesFlags != -1)
        {
            // The immutable Code-offer party model exposes no authoritative union/job identities.
            // Preserve native uncertainty instead of treating a filtered unit as a non-match.
            return NetherTargetMatch.Unknown(
                "native-self-target-scope-live-identity-unavailable"
            );
        }
        return NetherTargetMatch.Match;
    }

    private static NetherTargetMatch MatchTarget(
        NetherStrategyNativeMechanic mechanic,
        NetherStrategyBuffParameterEvidence parameter,
        NetherStrategyPartyMember member,
        IReadOnlyList<NetherStrategyPartyMember> party
    )
    {
        if (!TryMapTargetRow(mechanic, party, out NetherCodeTargetRow row, out string targetError))
            return NetherTargetMatch.Unknown(targetError + ":" + mechanic.MechanicId);
        NetherTargetMatch targetMatch;
        if (mechanic.Target.Kind == NetherStrategyTargetKind.Self)
        {
            targetMatch = MatchSelfScopeMember(mechanic.Scope, member);
            if (targetMatch.Kind == NetherTargetMatchKind.Unknown)
            {
                return NetherTargetMatch.Unknown(
                    targetMatch.Detail + ":" + mechanic.MechanicId
                );
            }
        }
        else
        {
            bool targetMatches = row switch
            {
                NetherCodeTargetRow.Forward => member.PartyPosition == NetherPartyPosition.Forward,
                NetherCodeTargetRow.Back => member.PartyPosition == NetherPartyPosition.Back,
                NetherCodeTargetRow.Assist => member.PartyPosition == NetherPartyPosition.Assist,
                NetherCodeTargetRow.All => member.PartyPosition is
                    NetherPartyPosition.Forward or NetherPartyPosition.Back or NetherPartyPosition.Assist,
                _ => false,
            };
            targetMatch = targetMatches ? NetherTargetMatch.Match : NetherTargetMatch.NoMatch;
        }
        if (targetMatch.Kind == NetherTargetMatchKind.NoMatch)
            return NetherTargetMatch.NoMatch;
        return MatchBuffTargetFilter(parameter.TargetFilter, member, mechanic.MechanicId);
    }

    private static NetherTargetMatch MatchPartyAbilityTarget(
        long effectId,
        NetherStrategyPartyAbilityMechanic graph,
        NetherStrategyBuffParameterEvidence parameter,
        NetherStrategyPartyMember source,
        NetherStrategyPartyMember recipient,
        IReadOnlyList<NetherStrategyPartyMember> party
    )
    {
        NetherStrategyTargetEvidence target = graph.Target;
        if (!target.IsKnown)
        {
            return NetherTargetMatch.Unknown(
                (string.IsNullOrWhiteSpace(target.UnknownReason)
                    ? "party-ability-target-parameters-unavailable"
                    : target.UnknownReason) + ":" + effectId
            );
        }

        NetherTargetMatch targetMatch;
        if (target.Kind == NetherStrategyTargetKind.Self)
        {
            if (!HasNoGroupTargetParameters(target))
            {
                return NetherTargetMatch.Unknown(
                    "party-ability-self-target-parameters-unavailable:" + effectId
                );
            }
            targetMatch = source.CharacterId == recipient.CharacterId
                ? NetherTargetMatch.Match
                : NetherTargetMatch.NoMatch;
        }
        else if (target.Kind == NetherStrategyTargetKind.Friend)
        {
            targetMatch = MatchGroupTarget(target, recipient, effectId, "party-ability");
        }
        else
        {
            return NetherTargetMatch.Unknown(
                "party-ability-target-kind-not-authoritatively-mapped:"
                    + target.Kind + ":" + effectId
            );
        }
        if (targetMatch.Kind != NetherTargetMatchKind.Match)
            return targetMatch;

        // Keep the complete native signature at this seam: a future exact implementation for
        // Random/Nearest/LeastCurrentHp can correlate the filtered set against this same party.
        _ = party;
        return MatchBuffTargetFilter(parameter.TargetFilter, recipient, effectId);
    }

    private static bool HasNoGroupTargetParameters(NetherStrategyTargetEvidence target) =>
        !target.IgnoreDeadUnit
        && target.ElementTypeFlags == 0
        && target.PartyPositionFlags == NetherPartyPositionFlags.None
        && target.UnionTypeFlags == 0
        && target.JobGroupFlags == 0
        && target.JobSpeciesFlags == 0
        && target.CharacterSizeFlags == 0
        && target.RequiredBuffTypes != null
        && target.RequiredBuffTypes.Count == 0
        && target.SearchType == 0
        && target.RandomCount == 0
        && target.NearestCount == 0
        && target.CurrentHpLeastCount == 0;

    private static NetherTargetMatch MatchGroupTarget(
        NetherStrategyTargetEvidence target,
        NetherStrategyPartyMember member,
        long mechanicId,
        string detailPrefix
    )
    {
        if (!target.IsKnown || target.RequiredBuffTypes == null)
        {
            return NetherTargetMatch.Unknown(
                (string.IsNullOrWhiteSpace(target.UnknownReason)
                    ? detailPrefix + "-target-parameters-unavailable"
                    : target.UnknownReason) + ":" + mechanicId
            );
        }
        if (!HasOnlyScopeFlagBits(target.ElementTypeFlags, 0x7e)
            || !HasOnlyScopeFlagBits((int)target.PartyPositionFlags, 0x0e)
            || !HasOnlyScopeFlagBits(target.UnionTypeFlags, 0x3e)
            || !HasOnlyScopeFlagBits(target.JobGroupFlags, 0x00ff_ffff)
            || !HasOnlyScopeFlagBits(target.JobSpeciesFlags, 0x7e)
            || !HasOnlyScopeFlagBits(target.CharacterSizeFlags, 0x1e))
        {
            return NetherTargetMatch.Unknown(
                detailPrefix + "-target-unknown-flag-bits:" + mechanicId
            );
        }
        if (target.IgnoreDeadUnit && !member.IsAlive)
            return NetherTargetMatch.NoMatch;

        if (member.ElementType is not 0 and not 99 && target.ElementTypeFlags != -1)
        {
            int elementFlag = ElementFlag(member.ElementType);
            if (elementFlag == 0)
            {
                return NetherTargetMatch.Unknown(
                    detailPrefix + "-target-element-unavailable:" + mechanicId
                );
            }
            if ((target.ElementTypeFlags & elementFlag) == 0)
                return NetherTargetMatch.NoMatch;
        }
        if (member.PartyPosition != NetherPartyPosition.Unknown
            && (int)target.PartyPositionFlags != -1
            && (target.PartyPositionFlags & PositionFlag(member.PartyPosition)) == 0)
        {
            return NetherTargetMatch.NoMatch;
        }

        if (target.UnionTypeFlags != -1
            || target.JobGroupFlags != -1
            || target.JobSpeciesFlags != -1
            || target.CharacterSizeFlags != -1
            || target.RequiredBuffTypes.Count != 0)
        {
            return NetherTargetMatch.Unknown(
                detailPrefix + "-target-live-identity-unavailable:" + mechanicId
            );
        }
        if (target.SearchType == 0)
            return NetherTargetMatch.Match;
        if (target.SearchType is 1 or 2 or 11)
        {
            int selectedCount = target.SearchType switch
            {
                1 => target.RandomCount,
                2 => target.NearestCount,
                11 => target.CurrentHpLeastCount,
                _ => 0,
            };
            return selectedCount <= 0
                ? NetherTargetMatch.NoMatch
                : NetherTargetMatch.Unknown(
                    detailPrefix + "-target-live-selection-unavailable:" + mechanicId
                );
        }
        return NetherTargetMatch.Unknown(
            detailPrefix + "-target-search-type-unavailable:" + mechanicId
        );
    }

    private static NetherTargetMatch MatchBuffTargetFilter(
        NetherStrategyBuffTargetFilterEvidence? filter,
        NetherStrategyPartyMember member,
        long mechanicId
    )
    {
        if (filter == null)
            return NetherTargetMatch.Match;
        if (!filter.IsKnown)
        {
            return NetherTargetMatch.Unknown(
                string.IsNullOrWhiteSpace(filter.UnknownReason)
                    ? "native-target-filter-parameters-unavailable:" + mechanicId
                    : filter.UnknownReason + ":" + mechanicId
            );
        }
        if (!HasOnlyScopeFlagBits(filter.ElementTypeFlags, 0x7e)
            || !HasOnlyScopeFlagBits(filter.ElementWeakTypeFlags, 0x7e)
            || !HasOnlyScopeFlagBits((int)filter.PartyPositionFlags, 0x0e)
            || !HasOnlyScopeFlagBits(filter.UnionTypeFlags, 0x3e)
            || !HasOnlyScopeFlagBits(filter.JobGroupFlags, 0x00ff_ffff)
            || !HasOnlyScopeFlagBits(filter.JobSpeciesFlags, 0x7e)
            || !HasOnlyScopeFlagBits(filter.CharacterSizeFlags, 0x1e))
        {
            return NetherTargetMatch.Unknown(
                "native-target-filter-unknown-flag-bits:" + mechanicId
            );
        }
        if (filter.IgnoreDeadUnit && !member.IsAlive)
            return NetherTargetMatch.NoMatch;
        if (filter.RequiredBuffTypes == null
            || filter.RequiredBuffTypes.Count > 0
            || filter.ElementWeakTypeFlags != 0
            || filter.UnionTypeFlags != -1
            || filter.JobGroupFlags != -1
            || filter.JobSpeciesFlags != -1
            || filter.CharacterSizeFlags != -1)
        {
            // BuffTargetFilter.IsMatchTarget evaluates these live unit/buff relationships. The
            // immutable offer party evidence does not expose them, so this dependent mechanic is
            // unknown rather than falsely treated as having no recipients.
            return NetherTargetMatch.Unknown(
                "native-target-filter-live-relationship-unavailable:" + mechanicId
            );
        }
        if (member.ElementType is not 0 and not 99 && filter.ElementTypeFlags != -1)
        {
            // Fresh Project.Master evidence: ElementType values Artifact..Dark are 1..6 while
            // ElementTypeFlag values are the exact independent flags 2,4,8,16,32,64. Keep the
            // relationship explicit so a future enum value fails closed instead of relying on
            // ordinal arithmetic.
            int elementFlag = ElementFlag(member.ElementType);
            if (elementFlag == 0)
            {
                return NetherTargetMatch.Unknown(
                    "native-target-filter-element-unavailable:" + mechanicId
                );
            }
            if ((filter.ElementTypeFlags & elementFlag) == 0)
                return NetherTargetMatch.NoMatch;
        }
        return (int)filter.PartyPositionFlags == -1
            || (filter.PartyPositionFlags & PositionFlag(member.PartyPosition)) != 0
                ? NetherTargetMatch.Match
                : NetherTargetMatch.NoMatch;
    }

    private static NetherTargetMatch MatchAbilityTarget(
        NetherStrategyNativeMechanic mechanic,
        NetherStrategyPartyMember member,
        IReadOnlyList<NetherStrategyPartyMember> party
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
        if (target.Kind == NetherStrategyTargetKind.Self)
        {
            if (!TryMapTargetRow(mechanic, party, out _, out string selfError))
                return NetherTargetMatch.Unknown(selfError + ":" + mechanic.MechanicId);
            NetherTargetMatch match = MatchSelfScopeMember(mechanic.Scope, member);
            return match.Kind == NetherTargetMatchKind.Unknown
                ? NetherTargetMatch.Unknown(match.Detail + ":" + mechanic.MechanicId)
                : match;
        }
        if (target.Kind != NetherStrategyTargetKind.Friend)
        {
            return NetherTargetMatch.Unknown(
                "native-mana-target-kind-not-authoritatively-mapped:"
                    + target.Kind + ":" + mechanic.MechanicId
            );
        }
        return MatchGroupTarget(target, member, mechanic.MechanicId, "native-mana");
    }

    private static int ElementFlag(int elementType) => elementType switch
    {
        1 => 2,
        2 => 4,
        3 => 8,
        4 => 16,
        5 => 32,
        6 => 64,
        _ => 0,
    };

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
        if (settings.StrategyMode != NetherStrategyMode.Research)
            return NetherCodeFamily.Unknown;
        NetherResearchObjectiveResolution objective = NetherResearchObjectivePolicy.Resolve(
            settings.ResearchPrimaryFamily,
            settings.ResearchSecondaryFamily,
            research
        );
        return objective.IsValid && objective.HasIncompleteTargets
            ? objective.ActiveFamily
            : NetherCodeFamily.Unknown;
    }
}
