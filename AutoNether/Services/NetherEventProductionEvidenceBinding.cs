#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Joins the exact pre-entry proof and the immutable strategy package to a newly mapped native
/// Event popup. The native callback remains the only mutation seam; this class only carries the
/// route commitment and mode facts across that seam.
/// </summary>
internal static class NetherEventProductionEvidenceBinding
{
    private const int ResearchCompletionPoints = 20_000;

    public static NetherRuntimePopupContext Bind(
        NetherRuntimePopupContext popup,
        NetherStrategyEvidencePackage? package,
        NetherRuntimeInteractivePreEntryInputsResult? interactive,
        NetherAutoClimbSettings settings
    )
    {
        if (popup == null)
            throw new ArgumentNullException(nameof(popup));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (popup.Kind != NetherRuntimePopupKind.Event)
            return popup;

        bool researchFactsKnown = settings.StrategyMode != NetherStrategyMode.Research;
        bool researchIncomplete = false;
        string researchError = string.Empty;
        if (settings.StrategyMode == NetherStrategyMode.Research && package != null)
        {
            researchFactsKnown = TryMapResearchObjective(
                package,
                settings,
                out researchIncomplete,
                out researchError
            );
        }
        else if (settings.StrategyMode == NetherStrategyMode.Research && package == null)
        {
            researchFactsKnown = false;
            researchError = "event-strategy-package-unavailable";
        }

        NetherRuntimeInteractivePreEntryInputsResult? exactInteractive =
            HasMatchingInteractiveSnapshot(package, interactive)
                ? interactive
                : null;

        NetherEventOption[] boundOptions = popup.Options
            .Select(option => BindOption(
                popup,
                option,
                package,
                exactInteractive,
                settings,
                researchFactsKnown,
                researchError
            ))
            .ToArray();
        NetherRuntimePopupContext boundPopup = popup with { Options = boundOptions };
        NetherEventStrategyEvidence evidence = AggregateStrategyEvidence(
            boundOptions,
            settings,
            researchFactsKnown,
            researchIncomplete,
            researchError
        );
        IReadOnlyDictionary<NetherEventCommitmentKey, NetherEventCommitment> commitments =
            BuildCommitments(boundPopup, package, exactInteractive);

        return boundPopup with
        {
            EventStrategyEvidence = evidence,
            ExpectedEventCommitment = commitments.Count == 1
                ? commitments.Values.Single()
                : null,
            ExpectedEventCommitments = commitments,
        };
    }

    private static NetherEventOption BindOption(
        NetherRuntimePopupContext popup,
        NetherEventOption option,
        NetherStrategyEvidencePackage? package,
        NetherRuntimeInteractivePreEntryInputsResult? interactive,
        NetherAutoClimbSettings settings,
        bool researchFactsKnown,
        string researchError
    )
    {
        bool exact = option.RequiresExactBinding || popup.Kind == NetherRuntimePopupKind.Event;
        bool hasProjection = TryFindProjection(
            interactive,
            option,
            out NetherInteractiveOptionProjection? projection,
            out long projectionNodeId
        );
        NetherStrategyVisibleMapEvidence? visible = package?.VisibleMap is { IsKnown: true, Value: not null }
            ? package.VisibleMap.Value
            : null;
        bool hasVisible = TryFindVisibleOption(visible, option, out NetherStrategyVisibleContentRow? visibleRow, out NetherStrategyVisibleEventOptionEvidence? visibleOption);
        bool dependentRows = hasVisible && HasExactDependentRows(visible, option);
        NetherInteractivePartialDeathEligibility? partialProof = option.PartialDeathEligibility
            ?? projection?.PartialDeathEligibility;
        bool partialEvidence = !exact || HasExactPartialDeathEvidence(
            option,
            projection,
            projectionNodeId,
            partialProof
        );
        bool route = !exact || hasProjection
            && projection!.IsKnown
            && projection.HasRouteSafetyEvidence
            && projection.RouteSafetyAllowed
            && hasVisible
            && dependentRows;
        int committedGoldMinimum = projection?.HasCommittedProcurementEvidence == true
            ? projection.CommittedGoldMinimum
            : option.CommittedGoldMinimum;
        int committedKeyMinimum = projection?.HasCommittedProcurementEvidence == true
            ? projection.CommittedKeyMinimum
            : option.CommittedKeyMinimum;
        int projectedGold = 0;
        int projectedKeys = 0;
        bool resourceProjection = package?.Server != null
            && projection != null
            && TryProjectResources(package.Server, projection.ExpectedEffects, out projectedGold, out projectedKeys);
        bool resource = !exact || resourceProjection;
        bool budget = !exact || resourceProjection
            && projectedGold >= committedGoldMinimum
            && projectedKeys >= committedKeyMinimum;
        bool semantic = !exact || string.IsNullOrWhiteSpace(option.UnknownReason)
            && option.Effects.All(effect => effect != null
                && effect.Known
                && effect.ContentKnown
                && effect.Kind != NetherEffectKind.Unknown
                && effect.Amount >= 0)
            && hasVisible
            && visibleRow!.IsKnown
            && visibleOption!.Effects.All(effect => effect.IsKnown)
            && dependentRows
            && partialEvidence
            && (projection == null || NetherEventPolicy.EffectFingerprintsEqual(
                option.Effects,
                projection.ExpectedEffects
            ));
        bool modeKnown = !exact || settings.StrategyMode != NetherStrategyMode.Research || researchFactsKnown;
        bool known = route && resource && budget && semantic && modeKnown;
        string reason = string.IsNullOrWhiteSpace(option.UnknownReason)
            ? known
                ? string.Empty
                : !hasProjection
                    ? "event-option-route-projection-unavailable"
                    : !route
                        ? projection?.RouteSafetyUnknownReason.Length > 0
                            ? projection.RouteSafetyUnknownReason
                            : "event-option-route-evidence-unavailable"
                        : !resource
                            ? "event-option-resource-evidence-unavailable"
                            : !budget
                                ? "event-option-committed-budget-would-break"
                                : !semantic
                                ? "event-option-semantic-evidence-unavailable"
                                : researchError.Length > 0
                                    ? researchError
                                    : "event-option-strategy-evidence-unavailable"
            : option.UnknownReason;
        NetherEventRewardEvidence? reward = option.RewardEvidence
            ?? projection?.Reward
            ?? FindReward(visible, option.EventId, option.EventPartId);
        NetherEventBattleEvidence? battle = option.BattleEvidence
            ?? projection?.Battle
            ?? FindBattle(visible, option.EventId, option.EventPartId);
        long floorId = projection?.FloorId > 0
            ? projection.FloorId
            : package?.Server?.CurrentFloorId > 0
                ? package.Server.CurrentFloorId
                : popup.FloorId;
        long nodeId = projection?.NodeId > 0
            ? projection.NodeId
            : projectionNodeId > 0
                ? projectionNodeId
                : package?.Server?.CurrentNodeId > 0
                    ? package.Server.CurrentNodeId
                    : popup.NodeId;
        if (exact && (floorId <= 0 || nodeId <= 0))
            reason = "event-option-floor-node-identity-unavailable";
        NetherEventStrategyEvidence optionEvidence = new()
        {
            IsKnown = known && floorId > 0 && nodeId > 0,
            Mode = settings.StrategyMode,
            ResearchIncomplete = !researchFactsKnown ||
                partialProof?.ExactTreasureRank is > 0 and < 5,
            HasRankFiveTreasureObjective = (option.IsMandatoryRankFiveKeyObjective
                || partialProof?.AllowsHpPaidEventKey == true)
                && partialEvidence
                && partialProof?.ExactTreasureRank == 5,
            HasRouteEvidence = route,
            HasResourceEvidence = resource && budget,
            HasSemanticEvidence = semantic,
            HasPartialDeathEvidence = partialEvidence,
            AllowsPartialActiveDeaths = projection?.AllowsPartialActiveDeaths == true,
            UnknownReason = reason,
        };
        return option with
        {
            RequiresExactBinding = exact,
            FloorId = floorId,
            NodeId = nodeId,
            BattleEvidence = battle,
            RewardEvidence = reward,
            HasRouteSafetyEvidence = projection?.HasRouteSafetyEvidence == true,
            RouteSafetyAllowed = projection?.RouteSafetyAllowed ?? false,
            RouteSafetyUnknownReason = projection?.RouteSafetyUnknownReason ?? reason,
            AllowsPartialActiveDeaths = projection?.AllowsPartialActiveDeaths == true,
            PartialDeathEligibility = partialProof,
            CommittedGoldMinimum = committedGoldMinimum,
            CommittedKeyMinimum = committedKeyMinimum,
            IsMandatoryRankFiveKeyObjective = (option.IsMandatoryRankFiveKeyObjective
                || partialProof?.AllowsHpPaidEventKey == true)
                && partialProof?.ExactTreasureRank == 5,
            StrategyEvidence = optionEvidence,
            UnknownReason = known && floorId > 0 && nodeId > 0
                ? option.UnknownReason
                : reason,
        };
    }

    private static bool HasExactPartialDeathEvidence(
        NetherEventOption option,
        NetherInteractiveOptionProjection? projection,
        long projectionNodeId,
        NetherInteractivePartialDeathEligibility? proof
    )
    {
        if (proof == null)
            return projection?.AllowsPartialActiveDeaths != true;
        long nodeId = projection?.NodeId > 0 ? projection.NodeId : projectionNodeId;
        return proof.IsKnown
            && proof.EventId == option.EventId
            && proof.EventPartId == option.EventPartId
            && proof.ObjectiveNodeId == nodeId
            && (proof.AllowsHpPaidEventKey || proof.AllowsTreasureHpPayment)
            && projection?.AllowsPartialActiveDeaths == true;
    }

    private static NetherEventStrategyEvidence AggregateStrategyEvidence(
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        bool researchFactsKnown,
        bool researchIncomplete,
        string researchError
    )
    {
        NetherEventStrategyEvidence[] known = options
            .Select(option => option.StrategyEvidence)
            .Where(evidence => evidence != null)
            .Cast<NetherEventStrategyEvidence>()
            .ToArray();
        bool route = known.Any(evidence => evidence.HasRouteEvidence);
        bool resource = known.Any(evidence => evidence.HasResourceEvidence);
        bool semantic = known.Any(evidence => evidence.HasSemanticEvidence);
        bool anyKnown = known.Any(evidence => evidence.IsKnown);
        string reason = options.Count == 0
            ? "event-option-evidence-unavailable"
            : anyKnown
            ? known.Any(evidence => !evidence.IsKnown)
                ? "some-event-options-unknown"
                : string.Empty
            : researchError.Length > 0 && settings.StrategyMode == NetherStrategyMode.Research
                ? researchError
                : "event-option-evidence-unavailable";
        return new NetherEventStrategyEvidence
        {
            IsKnown = anyKnown,
            Mode = settings.StrategyMode,
            ResearchIncomplete = researchIncomplete,
            HasRankFiveTreasureObjective = known.Any(evidence => evidence.HasRankFiveTreasureObjective),
            HasRouteEvidence = route,
            HasResourceEvidence = resource,
            HasSemanticEvidence = semantic,
            UnknownReason = reason,
        };
    }

    private static bool TryMapResearchObjective(
        NetherStrategyEvidencePackage package,
        NetherAutoClimbSettings settings,
        out bool incomplete,
        out string error
    )
    {
        incomplete = false;
        error = string.Empty;
        if (!package.Research.IsKnown || package.Research.Value == null)
        {
            error = string.IsNullOrWhiteSpace(package.Research.UnknownReason)
                ? "event-research-evidence-unavailable"
                : package.Research.UnknownReason;
            return false;
        }

        bool IsComplete(NetherCodeFamily family)
        {
            if (family == NetherCodeFamily.Unknown)
                return true;
            NetherStrategyResearchFamilyState[] rows = package.Research.Value.Families
                .Where(row => row.Family == family)
                .ToArray();
            if (rows.Length != 1 || !rows[0].IsProjectedNormalSettlementKnown)
                return false;
            return (long)rows[0].WalletPoints + rows[0].ProjectedNormalSettlementPoints
                >= ResearchCompletionPoints;
        }

        bool primaryKnown = settings.ResearchPrimaryFamily == NetherCodeFamily.Unknown
            || package.Research.Value.Families.Count(row => row.Family == settings.ResearchPrimaryFamily) == 1
                && package.Research.Value.Families.Single(row => row.Family == settings.ResearchPrimaryFamily)
                    .IsProjectedNormalSettlementKnown;
        bool secondaryKnown = settings.ResearchSecondaryFamily == NetherCodeFamily.Unknown
            || package.Research.Value.Families.Count(row => row.Family == settings.ResearchSecondaryFamily) == 1
                && package.Research.Value.Families.Single(row => row.Family == settings.ResearchSecondaryFamily)
                    .IsProjectedNormalSettlementKnown;
        if (!primaryKnown || !secondaryKnown)
        {
            error = "event-research-projected-settlement-unknown";
            return false;
        }

        incomplete = !IsComplete(settings.ResearchPrimaryFamily)
            || !IsComplete(settings.ResearchSecondaryFamily);
        return true;
    }

    private static bool TryFindProjection(
        NetherRuntimeInteractivePreEntryInputsResult? interactive,
        NetherEventOption option,
        out NetherInteractiveOptionProjection? projection,
        out long nodeId
    )
    {
        projection = null;
        nodeId = 0;
        if (interactive == null || !interactive.IsSuccess)
            return false;
        NetherInteractiveEventOptionKey key = new(option.EventId, option.EventPartId, option.OptionNumber);
        var matches = new List<(long NodeId, NetherInteractiveOptionProjection Projection)>();
        foreach (KeyValuePair<long, NetherRuntimeInteractivePreEntryCaptureResult> entry in interactive.ByFloorNodeId)
        {
            NetherInteractiveFloorPreEntrySafetyResult safety = entry.Value.Safety;
            if (!safety.IsSafe)
                continue;
            NetherInteractiveOptionProjection? candidate = null;
            if (safety.OptionProjectionByKey.TryGetValue(key, out NetherInteractiveOptionProjection? exact)
                && exact != null)
            {
                candidate = exact;
            }
            else if (safety.SafeOptionProjectionByEventId.TryGetValue(option.EventId, out NetherInteractiveOptionProjection? selected)
                && selected != null
                && selected.OptionNumber == option.OptionNumber
                && selected.EventPartId == option.EventPartId)
            {
                candidate = selected;
            }
            if (candidate == null)
                continue;
            if (option.FloorId > 0 && candidate.FloorId != option.FloorId)
                continue;
            if (option.NodeId > 0 && candidate.NodeId != option.NodeId)
                continue;
            matches.Add((entry.Key, candidate));
        }
        if (matches.Count != 1)
            return false;
        projection = matches[0].Projection;
        nodeId = matches[0].NodeId;
        return true;
    }

    private static bool HasMatchingInteractiveSnapshot(
        NetherStrategyEvidencePackage? package,
        NetherRuntimeInteractivePreEntryInputsResult? interactive
    ) => package?.Server != null
        && interactive != null
        && interactive.IsSuccess
        && interactive.SnapshotFingerprint is NetherSnapshotFingerprint fingerprint
        && fingerprint == package.Identity.SnapshotFingerprint;

    private static bool TryFindVisibleOption(
        NetherStrategyVisibleMapEvidence? visible,
        NetherEventOption option,
        out NetherStrategyVisibleContentRow? row,
        out NetherStrategyVisibleEventOptionEvidence? visibleOption
    )
    {
        row = null;
        visibleOption = null;
        if (visible == null)
            return false;
        NetherStrategyVisibleContentRow[] rows = visible.ContentRows
            .Where(candidate => candidate.Kind == NetherStrategyVisibleContentKind.Event
                && candidate.EventId == option.EventId
                && candidate.EventPartId == option.EventPartId)
            .ToArray();
        if (rows.Length != 1 || !rows[0].IsKnown)
            return false;
        NetherStrategyVisibleEventOptionEvidence[] options = rows[0].EventOptions
            .Where(candidate => candidate.OptionNumber == option.OptionNumber
                && candidate.EventPartId == option.EventPartId)
            .ToArray();
        if (options.Length != 1
            || options[0].Effects.Any(effect => !effect.IsKnown)
            || !VisibleOptionMatches(option, options[0]))
            return false;
        row = rows[0];
        visibleOption = options[0];
        return true;
    }

    private static bool VisibleOptionMatches(
        NetherEventOption option,
        NetherStrategyVisibleEventOptionEvidence visibleOption
    )
    {
        if (option.Effects.Any(effect => effect == null
                || !NetherEventNativeMapping.IsValidResourceEffectContentId(effect.Kind, effect.ContentId))
            || visibleOption.Effects.Any(effect => effect.IsPresent
                && !NetherEventNativeMapping.IsValidResourceEffectContentId(effect.EffectKind, effect.ContentId)))
        {
            return false;
        }
        NetherStrategyVisibleEventEffectEvidence[] visibleEffects = visibleOption.Effects
            .Where(effect => effect.IsPresent)
            .ToArray();
        if (visibleEffects.Length != option.Effects.Count)
            return false;
        for (int index = 0; index < option.Effects.Count; index++)
        {
            NetherEffect expected = option.Effects[index];
            NetherStrategyVisibleEventEffectEvidence actual = visibleEffects[index];
            if (!actual.IsKnown
                || expected == null
                || expected.Kind != actual.EffectKind
                || expected.Amount != actual.Amount
                || expected.ContentId != actual.ContentId)
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasExactDependentRows(
        NetherStrategyVisibleMapEvidence? visible,
        NetherEventOption option
    )
    {
        if (visible == null)
            return false;
        foreach (NetherEffect effect in option.Effects)
        {
            NetherStrategyVisibleContentKind kind = effect.Kind switch
            {
                NetherEffectKind.Item => NetherStrategyVisibleContentKind.Item,
                NetherEffectKind.Battle => NetherStrategyVisibleContentKind.Battle,
                _ => NetherStrategyVisibleContentKind.Unknown,
            };
            if (kind == NetherStrategyVisibleContentKind.Unknown)
                continue;
            NetherStrategyVisibleContentRow[] rows = visible.ContentRows
                .Where(row => row.Kind == kind
                    && row.EventId == option.EventId
                    && row.EventPartId == option.EventPartId)
                .ToArray();
            if (rows.Length != 1 || !rows[0].IsKnown)
                return false;
            if (kind == NetherStrategyVisibleContentKind.Item && rows[0].ContentId != effect.ContentId)
                return false;
            if (kind == NetherStrategyVisibleContentKind.Item)
            {
                NetherEventRewardEvidence? reward = option.RewardEvidence ?? effect.RewardEvidence;
                if (reward == null
                    || !reward.IsKnown
                    || rows[0].MasterRowId != reward.ItemId
                    || rows[0].ItemType != reward.ItemType
                    || rows[0].ItemRarity != (int)reward.Rarity
                    || rows[0].Amount != reward.Amount)
                    return false;
            }
            if (kind == NetherStrategyVisibleContentKind.Battle && rows[0].MasterRowId != effect.Amount)
                return false;
            if (kind == NetherStrategyVisibleContentKind.Battle)
            {
                NetherEventBattleEvidence? battle = option.BattleEvidence ?? effect.BattleEvidence;
                if (battle != null
                    && (rows[0].BattleStageId != battle.BattleStageId
                        || rows[0].BattleType != battle.BattleType
                        || rows[0].CodeDropRatio != battle.CodeDropRatio))
                    return false;
            }
        }
        return true;
    }

    private static bool TryProjectResources(
        NetherStrategyServerEvidence server,
        IReadOnlyList<NetherEffect> effects,
        out int projectedGold,
        out int projectedKeys
    )
    {
        projectedGold = 0;
        projectedKeys = 0;
        if (server == null || server.NetherGold < 0 || server.TreasureKeyCount < 0 || effects == null)
            return false;
        return NetherEventResourceProjection.TryProject(
            server.NetherGold,
            server.TreasureKeyCount,
            effects,
            out projectedGold,
            out projectedKeys
        );
    }

    private static IReadOnlyDictionary<NetherEventCommitmentKey, NetherEventCommitment> BuildCommitments(
        NetherRuntimePopupContext popup,
        NetherStrategyEvidencePackage? package,
        NetherRuntimeInteractivePreEntryInputsResult? interactive
    )
    {
        if (package?.Server == null
            || interactive == null
            || !interactive.IsSuccess
            || interactive.SnapshotFingerprint is not NetherSnapshotFingerprint fingerprint
            || fingerprint != package.Identity.SnapshotFingerprint)
        {
            return new Dictionary<NetherEventCommitmentKey, NetherEventCommitment>();
        }

        var commitments = new Dictionary<NetherEventCommitmentKey, NetherEventCommitment>();
        foreach (NetherEventOption option in popup.Options.Where(option => option.RequiresExactBinding))
        {
            if (option.StrategyEvidence?.IsKnown != true
                || !TryFindProjection(
                    interactive,
                    option,
                    out NetherInteractiveOptionProjection? projection,
                    out _
                )
                || projection == null
                || !projection.IsKnown
                || !projection.HasRouteSafetyEvidence
                || !projection.RouteSafetyAllowed
                || option.FloorId <= 0
                || option.NodeId <= 0
            || !package.VisibleMap.IsKnown
            || package.VisibleMap.Value == null
                || !TryFindVisibleOption(
                    package.VisibleMap.Value,
                    option,
                    out _,
                    out _
                )
                || !TryProjectResources(
                    package.Server,
                    projection.ExpectedEffects,
                    out int projectedGold,
                    out int projectedKeys
                ))
            {
                continue;
            }
            NetherEventRewardEvidence? reward = projection.Reward
                ?? FindReward(package.VisibleMap.Value, projection.EventId, projection.EventPartId);
            NetherEventBattleEvidence? battle = projection.Battle
                ?? FindBattle(package.VisibleMap.Value, projection.EventId, projection.EventPartId);
            if (projection.ExpectedEffects.Any(effect => effect.Kind == NetherEffectKind.Item)
                    && (reward == null || !reward.IsKnown)
                || projection.ExpectedEffects.Any(effect => effect.Kind == NetherEffectKind.Battle)
                    && (battle == null || !battle.IsKnown))
            {
                continue;
            }
            NetherEventCommitment commitment = new NetherEventCommitment(
                projection.EventId,
                projection.EventPartId,
                projection.OptionNumber,
                option.Effects,
                checked(package.Server.ErosionPoint + projection.ErosionDelta),
                projection.HpDelta
            )
            {
                FloorId = option.FloorId,
                NodeId = option.NodeId,
                Reward = reward,
                Battle = battle,
                ProjectedNetherGold = projectedGold,
                ProjectedTreasureKeys = projectedKeys,
                CommittedGoldMinimum = option.CommittedGoldMinimum,
                CommittedKeyMinimum = option.CommittedKeyMinimum,
                PartialDeathEligibility = option.PartialDeathEligibility,
                AllowsPartialActiveDeaths = projection.AllowsPartialActiveDeaths,
            };
            commitments[new NetherEventCommitmentKey(
                commitment.EventId,
                commitment.EventPartId,
                commitment.FloorId,
                commitment.NodeId,
                commitment.OptionNumber
            )] = commitment;
        }
        return commitments;
    }

    private static NetherEventRewardEvidence? FindReward(
        NetherStrategyVisibleMapEvidence? visible,
        long eventId,
        long eventPartId
    )
    {
        NetherStrategyVisibleContentRow? row = visible?.ContentRows.FirstOrDefault(candidate =>
            candidate.Kind == NetherStrategyVisibleContentKind.Item
            && candidate.EventId == eventId
            && candidate.EventPartId == eventPartId
            && candidate.IsKnown
        );
        if (row is not NetherStrategyVisibleContentRow exact
            || exact.ContentId <= 0
            || exact.MasterRowId <= 0
            || exact.ItemType < 0
            || exact.ItemRarity < 0
            || exact.Amount < 0
            || !NetherEventNativeMapping.TryMapItemType(exact.ItemType, out int itemType))
        {
            return null;
        }
        return new NetherEventRewardEvidence(
            exact.ContentId,
            exact.MasterRowId,
            itemType,
            (NetherRewardRarity)exact.ItemRarity,
            exact.Amount
        );
    }

    private static NetherEventBattleEvidence? FindBattle(
        NetherStrategyVisibleMapEvidence? visible,
        long eventId,
        long eventPartId
    )
    {
        NetherStrategyVisibleContentRow? row = visible?.ContentRows.FirstOrDefault(candidate =>
            candidate.Kind == NetherStrategyVisibleContentKind.Battle
            && candidate.EventId == eventId
            && candidate.EventPartId == eventPartId
            && candidate.IsKnown
        );
        if (row is not NetherStrategyVisibleContentRow exact
            || exact.MasterRowId <= 0
            || exact.BattleStageId <= 0
            || exact.CodeDropRatio < 0)
        {
            return null;
        }
        return NetherEventBattleEvidence.Unknown(
            exact.MasterRowId,
            "event-battle-semantic-tier-unavailable-for-raw-type:" + exact.BattleType
        ) with
        {
            BattleStageId = exact.BattleStageId,
            BattleType = exact.BattleType,
            CodeDropRatio = exact.CodeDropRatio,
        };
    }

}
