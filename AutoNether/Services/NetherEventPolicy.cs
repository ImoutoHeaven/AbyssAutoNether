#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal sealed record NetherEventOption(int OptionNumber, IReadOnlyList<NetherEffect> Effects)
{
    public long EventId { get; init; }
    public long EventPartId { get; init; }
    public NetherInteractivePartialDeathEligibility? PartialDeathEligibility { get; init; }
}

internal enum NetherEventDecisionKind
{
    Select,
    Pause,
}

internal sealed record NetherEventDecision
{
    public NetherEventDecisionKind Kind { get; init; }
    public NetherActionKind ActionKind { get; init; }
    public int OptionNumber { get; init; }
    public long ReplacementCodeId { get; init; }
    public int ProjectedErosion { get; init; }
    public int ExpectedErosionDelta { get; init; }
    public int HpDelta { get; init; }
    /// <summary>
    /// Immutable authoritative effect payload for the selected native option.  Reconcile must
    /// compare this exact server-visible resource delta rather than treating an option click as
    /// a generic visual close.
    /// </summary>
    public IReadOnlyList<NetherEffect> ExpectedEffects { get; init; } = Array.Empty<NetherEffect>();
    public bool StartsBattleAfterSelection { get; init; }
    public bool AllowsPartialActiveDeaths { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

internal readonly record struct NetherShopContent(
    long contentId,
    long itemId,
    int itemType,
    NetherRewardRarity rarity,
    int price,
    bool usesNetherGold,
    int amount = 1,
    bool known = true
)
{
    public long ContentId => contentId;
    public long ItemId => itemId;
    public int ItemType => itemType;
    public NetherRewardRarity Rarity => rarity;
    public int Price => price;
    public bool UsesNetherGold => usesNetherGold;
    public int Amount => amount;
    public bool Known => known;
    /// <summary>Exact MNetherFloorShopContents.content_type; retained for strategy evidence.</summary>
    public int RawContentType { get; init; }
}

internal enum NetherShopDecisionKind
{
    Leave,
    Buy,
    Pause,
}

internal sealed record NetherShopDecision
{
    public NetherShopDecisionKind Kind { get; init; }
    public long ContentId { get; init; }
    public int Amount { get; init; }
    /// <summary>Exact server-visible NetherGold debit for a buy transaction.</summary>
    public int GoldCost { get; init; }
    public NetherPauseReason PauseReason { get; init; }
    public string Detail { get; init; } = string.Empty;
}

internal sealed class NetherEventPolicy
{
    private readonly NetherErosionPolicy _erosionPolicy = new();
    private readonly NetherCodeTransformPolicy _transformPolicy = new();

    public NetherEventDecision DecideEvent(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    ) => DecideEvent(snapshot, options, settings, Array.Empty<NetherErosionModifier>());

    public NetherEventDecision DecideEvent(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    ) => Decide(
        snapshot,
        options,
        settings,
        modifiers,
        isRecovery: false,
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-outside-recovery"
        )
    );

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    ) => DecideRecovery(snapshot, options, settings, Array.Empty<NetherErosionModifier>());

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    ) => DecideRecovery(
        snapshot,
        options,
        settings,
        modifiers,
        NetherCodeTransformHardExclusionEvidence.Unknown(
            "code-transform-hard-exclusions-not-captured"
        )
    );

    public NetherEventDecision DecideRecovery(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        NetherCodeTransformHardExclusionEvidence hardExclusions
    ) => Decide(snapshot, options, settings, modifiers, isRecovery: true, hardExclusions);

    public NetherEventDecision DecideTreasure(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    ) => DecideTreasure(snapshot, options, settings, Array.Empty<NetherErosionModifier>());

    public NetherEventDecision DecideTreasure(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    )
    {
        ValidateInputs(snapshot, options, settings);
        if (settings.TreasureMode != NetherTreasureMode.KeyOnly)
            return Pause(NetherPauseReason.NoSafeRoute, "treasure-mode-off");

        var keyCandidates = new List<EventCandidate>();
        var hpCandidates = new List<EventCandidate>();
        foreach (NetherEventOption option in options)
        {
            bool isExactHpPayment = IsExactTreasureHpPayment(option);
            bool isStrategicallyEligibleHpPayment = isExactHpPayment
                && option.PartialDeathEligibility?.AllowsTreasureHpPayment == true;
            if (!TryValidateOption(
                    option,
                    snapshot,
                    settings,
                    modifiers,
                    allowPartialActiveDeaths: isStrategicallyEligibleHpPayment,
                    new NetherCodeTransformEligibilityEvidence
                    {
                        StrategyMode = settings.StrategyMode,
                        EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
                        IsRecovery = false,
                    },
                    out EventCandidate candidate,
                    out _
                ))
            {
                continue;
            }
            int exactKeyCosts = option.Effects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyUsed && effect.Amount == 1);
            bool hasOnlySafePayments = option.Effects.All(effect => effect.Kind is not NetherEffectKind.Damage and not NetherEffectKind.Erosion);
            bool hasNoOtherKeyCost = option.Effects.All(effect => effect.Kind != NetherEffectKind.TreasureKeyUsed || effect.Amount == 1);
            if (exactKeyCosts == 1 && hasNoOtherKeyCost && hasOnlySafePayments && snapshot.TreasureKeyCount >= 1)
                keyCandidates.Add(candidate);
            else if (snapshot.TreasureKeyCount < 1 && isStrategicallyEligibleHpPayment)
                hpCandidates.Add(candidate);
        }

        // The live popup exposes distinct Key/Hp/Abyss panels.  A verified one-key option is
        // always preferred.  The exact Damage-only Hp panel is a fallback only when no key is
        // held; the Erosion/Abyss panel is never promoted to a substitute.
        List<EventCandidate> candidates = keyCandidates.Count > 0 ? keyCandidates : hpCandidates;
        if (candidates.Count == 0)
            return Pause(NetherPauseReason.NoSafeRoute, "no-key-only-treasure-option");

        EventCandidate selected = candidates
            .OrderByDescending(candidate => candidate.Benefit)
            .ThenBy(candidate => candidate.Option.OptionNumber)
            .First();
        return Select(selected);
    }

    public NetherShopDecision DecideShop(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherShopContent> contents,
        NetherAutoClimbSettings settings
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (contents == null)
            throw new ArgumentNullException(nameof(contents));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (settings.ShopMode == NetherShopMode.Off)
            return new NetherShopDecision { Kind = NetherShopDecisionKind.Leave };
        // The native shop mixes MItems with valid ID-less products (keys, code effects, etc.).
        // EquipmentBags ignores those known non-item rows; ItemId is required only for an
        // actual equipment candidate, never as a blanket validity condition for the popup.
        if (contents.Any(content => !content.Known || content.ContentId <= 0 || content.Amount <= 0 || content.Price < 0))
            return new NetherShopDecision { Kind = NetherShopDecisionKind.Pause, PauseReason = NetherPauseReason.UnknownMasterData, Detail = "invalid-shop-content" };

        NetherShopContent? selected = contents
            .Where(content => content.ItemId > 0)
            .Where(content => content.ItemType == 91)
            .Where(content => content.Rarity >= NetherRewardRarity.Gold)
            .Where(content => content.UsesNetherGold)
            .Where(content => content.Price <= snapshot.NetherGold)
            .OrderByDescending(content => content.Rarity)
            .ThenBy(content => content.Price)
            .ThenBy(content => content.ContentId)
            .Cast<NetherShopContent?>()
            .FirstOrDefault();
        if (selected == null)
            return new NetherShopDecision { Kind = NetherShopDecisionKind.Leave };

        return new NetherShopDecision
        {
            Kind = NetherShopDecisionKind.Buy,
            ContentId = selected.Value.ContentId,
            Amount = selected.Value.Amount,
            GoldCost = selected.Value.Price,
        };
    }

    private NetherEventDecision Decide(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        bool isRecovery,
        NetherCodeTransformHardExclusionEvidence hardExclusions
    )
    {
        ValidateInputs(snapshot, options, settings);
        NetherCodeTransformEligibilityEvidence transformEligibility =
            BuildTransformEligibility(
                snapshot,
                options,
                settings,
                modifiers,
                isRecovery,
                hardExclusions
            );
        var candidates = new List<EventCandidate>();
        NetherPauseReason firstRejection = NetherPauseReason.NoSafeRoute;
        string firstDetail = "no-safe-event-option";
        foreach (NetherEventOption option in options)
        {
            bool allowPartialActiveDeaths = !isRecovery
                && IsExactHpPaidKeyEvent(option)
                && snapshot.TreasureKeyCount == 0
                && option.PartialDeathEligibility?.AllowsHpPaidEventKey == true;
            if (!TryValidateOption(
                    option,
                    snapshot,
                    settings,
                    modifiers,
                    allowPartialActiveDeaths,
                    transformEligibility,
                    out EventCandidate candidate,
                    out NetherEventDecision rejection
                ))
            {
                if (firstRejection == NetherPauseReason.NoSafeRoute)
                {
                    firstRejection = rejection.PauseReason;
                    firstDetail = rejection.Detail;
                }
                continue;
            }

            if (isRecovery && !candidate.HasPositiveOrNeutralRecoveryEffect)
            {
                firstRejection = NetherPauseReason.NoSafeRoute;
                firstDetail = "no-positive-recovery-effect";
                continue;
            }
            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return Pause(firstRejection, firstDetail);

        bool belowHpSoftLimit = snapshot.Characters.Any(character => character.IsActive && character.HpPermille < settings.MinimumCharacterHpPermille);
        EventCandidate selected = candidates
            // A transform candidate can exist only after the exact Recovery option set proved both
            // deterministic alternatives have zero clipped value.  Rank that committed recovery
            // action before their raw (but already saturated) effect amounts.
            .OrderByDescending(candidate => candidate.ReplacementCodeId > 0)
            .ThenByDescending(candidate => belowHpSoftLimit && candidate.HpDelta > 0)
            .ThenBy(candidate => candidate.ErosionDelta)
            .ThenByDescending(candidate => candidate.HpDelta)
            .ThenByDescending(candidate => candidate.SafeCodeBenefit)
            .ThenByDescending(candidate => candidate.Benefit)
            .ThenBy(candidate => candidate.OptionalBattle)
            .ThenBy(candidate => candidate.Option.OptionNumber)
            .First();
        return Select(selected);
    }

    private bool TryValidateOption(
        NetherEventOption option,
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        bool allowPartialActiveDeaths,
        NetherCodeTransformEligibilityEvidence transformEligibility,
        out EventCandidate candidate,
        out NetherEventDecision rejection
    )
    {
        candidate = default;
        rejection = default!;
        long replacementCodeId = 0;
        if (option == null || option.OptionNumber < 1 || option.Effects == null || option.Effects.Count is < 1 or > 4)
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "invalid-event-option");
            return false;
        }
        if (option.Effects.Any(effect => !effect.Known || !effect.ContentKnown || effect.Kind == NetherEffectKind.Unknown || effect.Amount < 0))
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "unknown-event-effect");
            return false;
        }
        if (option.Effects.Count(effect => effect.Kind == NetherEffectKind.AbyssCodeTransform) > 1
            || option.Effects.Count(effect => effect.Kind == NetherEffectKind.AbyssCodeOffer) > 1)
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "ambiguous-code-event-trigger");
            return false;
        }
        if (option.Effects.Any(effect => effect.Kind == NetherEffectKind.AbyssCodeTransform))
        {
            NetherCodeTransformDecision transform = _transformPolicy.Decide(
                snapshot.Codes,
                snapshot.CodeCapacity,
                transformEligibility
            );
            if (!transform.CanTransform)
            {
                rejection = Pause(transform.PauseReason, transform.Detail);
                return false;
            }
            replacementCodeId = transform.RemoveCodeId;
        }
        if (option.Effects.Any(effect => effect.Kind == NetherEffectKind.NetherGoldUsed && effect.Amount > snapshot.NetherGold)
            || option.Effects.Any(effect => effect.Kind == NetherEffectKind.TreasureKeyUsed && effect.Amount > snapshot.TreasureKeyCount))
        {
            rejection = Pause(NetherPauseReason.NoSafeRoute, "insufficient-event-resource");
            return false;
        }

        int hpDelta;
        try
        {
            hpDelta = option.Effects.Aggregate(0, (total, effect) => effect.Kind switch
            {
                NetherEffectKind.Heal => checked(total + effect.Amount),
                NetherEffectKind.Damage => checked(total - effect.Amount),
                _ => total,
            });
        }
        catch (OverflowException)
        {
            rejection = Pause(NetherPauseReason.UnknownEffect, "event-hp-overflow");
            return false;
        }
        NetherCharacterState[] activeCharacters = snapshot.Characters
            .Where(character => character.IsActive)
            .ToArray();
        bool hpIsLethal = allowPartialActiveDeaths
            ? activeCharacters.Length == 0
                || activeCharacters.All(character => character.HpPermille + hpDelta <= 0)
            : activeCharacters.Any(character => character.HpPermille + hpDelta <= 0);
        if (hpIsLethal)
        {
            rejection = Pause(
                NetherPauseReason.UnsafeHp,
                allowPartialActiveDeaths ? "party-lethal-event-damage" : "lethal-event-damage"
            );
            return false;
        }

        NetherErosionProjection erosion = _erosionPolicy.ProjectEffects(
            snapshot.ErosionPoint,
            option.Effects,
            modifiers,
            settings.SoftErosionLimit,
            isMandatoryBoss: false
        );
        if (!erosion.IsAllowed)
        {
            rejection = Pause(erosion.PauseReason, erosion.Detail);
            return false;
        }

        int erosionDelta = erosion.ProjectedErosion - snapshot.ErosionPoint;
        bool startsBattle = option.Effects.Any(effect => effect.Kind == NetherEffectKind.Battle);
        bool optionalBattle = option.Effects.Any(effect => effect.Kind == NetherEffectKind.Battle && effect.IsOptionalBattle);
        int benefit = option.Effects.Count(effect => effect.Kind is NetherEffectKind.Item
            or NetherEffectKind.NetherGoldGain
            or NetherEffectKind.TreasureKeyGain
            or NetherEffectKind.AbyssCodeOffer);
        candidate = new EventCandidate(
            option,
            erosion.ProjectedErosion,
            erosionDelta,
            hpDelta,
            replacementCodeId,
            option.Effects.Any(effect => effect.Kind is NetherEffectKind.AbyssCodeOffer
                or NetherEffectKind.AbyssCodeTransform) ? 1 : 0,
            benefit,
            startsBattle,
            optionalBattle,
            allowPartialActiveDeaths
        );
        return true;
    }

    private NetherCodeTransformEligibilityEvidence BuildTransformEligibility(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers,
        bool isRecovery,
        NetherCodeTransformHardExclusionEvidence hardExclusions
    )
    {
        if (!isRecovery)
        {
            return new NetherCodeTransformEligibilityEvidence
            {
                StrategyMode = settings.StrategyMode,
                EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
                IsRecovery = false,
            };
        }
        if (hardExclusions == null || !hardExclusions.IsKnown)
        {
            return new NetherCodeTransformEligibilityEvidence
            {
                IsKnown = false,
                UnknownReason = hardExclusions?.UnknownReason
                    ?? "code-transform-hard-exclusions-unavailable",
            };
        }

        NetherEventOption[] transforms = options.Where(IsExactTransformOption).ToArray();
        NetherEventOption[] rests = options.Where(IsExactRestOption).ToArray();
        NetherEventOption[] purifications = options.Where(IsExactPurificationOption).ToArray();
        if (options.Count != 3 || transforms.Length != 1 || rests.Length != 1
            || purifications.Length != 1)
        {
            return new NetherCodeTransformEligibilityEvidence
            {
                IsKnown = false,
                UnknownReason = "recovery-transform-three-option-shape-unavailable",
            };
        }

        bool restHasValue = HasActualRecoveryValue(
            rests[0],
            snapshot,
            settings,
            modifiers
        );
        bool purificationHasValue = HasActualRecoveryValue(
            purifications[0],
            snapshot,
            settings,
            modifiers
        );
        return new NetherCodeTransformEligibilityEvidence
        {
            StrategyMode = settings.StrategyMode,
            EquipmentOptInEnabled = settings.EquipmentRecoveryCodeTransformEnabled,
            IsRecovery = true,
            DeterministicRecoveryChoicesHaveZeroValue = !restHasValue
                && !purificationHasValue,
            HardExcludedCodes = hardExclusions.HardExcludedCodes,
        };
    }

    private bool HasActualRecoveryValue(
        NetherEventOption option,
        NetherSnapshot snapshot,
        NetherAutoClimbSettings settings,
        IReadOnlyList<NetherErosionModifier> modifiers
    )
    {
        NetherEffect effect = option.Effects[0];
        bool hpValue = effect.Kind == NetherEffectKind.Heal
            && snapshot.Characters.Any(character => character.IsActive
                && character.HpPermille < 1000
                && effect.Amount > 0);
        NetherErosionProjection projection = _erosionPolicy.ProjectEffects(
            snapshot.ErosionPoint,
            option.Effects,
            modifiers,
            settings.SoftErosionLimit,
            isMandatoryBoss: false
        );
        // An ineligible or non-neutral deterministic branch is not proof of zero value.
        return hpValue || !projection.IsAllowed
            || projection.ProjectedErosion != snapshot.ErosionPoint;
    }

    private static bool IsExactTransformOption(NetherEventOption option) =>
        IsExactSingleEffect(option, NetherEffectKind.AbyssCodeTransform);

    private static bool IsExactRestOption(NetherEventOption option) =>
        IsExactSingleEffect(option, NetherEffectKind.Heal);

    private static bool IsExactPurificationOption(NetherEventOption option) =>
        IsExactSingleEffect(option, NetherEffectKind.ErosionHeal);

    private static bool IsExactSingleEffect(NetherEventOption option, NetherEffectKind kind) =>
        option != null
        && option.Effects != null
        && option.Effects.Count == 1
        && option.Effects[0] != null
        && option.Effects[0].Known
        && option.Effects[0].ContentKnown
        && option.Effects[0].Kind == kind
        && option.Effects[0].Amount >= 0;

    private static bool IsExactTreasureHpPayment(NetherEventOption option) =>
        option?.Effects != null
        && option.Effects.Count == 1
        && option.Effects[0].Kind == NetherEffectKind.Damage
        && option.Effects[0].Amount > 0;

    private static bool IsExactHpPaidKeyEvent(NetherEventOption option) =>
        option?.Effects != null
        && option.Effects.Count == 2
        && option.Effects.Count(effect => effect.Kind == NetherEffectKind.Damage && effect.Amount > 0) == 1
        && option.Effects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyGain && effect.Amount == 1) == 1;

    private static NetherEventDecision Select(EventCandidate candidate) => new()
    {
        Kind = NetherEventDecisionKind.Select,
        ActionKind = NetherActionKind.SelectEventOption,
        OptionNumber = candidate.Option.OptionNumber,
        ReplacementCodeId = candidate.ReplacementCodeId,
        ProjectedErosion = candidate.ProjectedErosion,
        ExpectedErosionDelta = candidate.ErosionDelta,
        HpDelta = candidate.HpDelta,
        ExpectedEffects = candidate.Option.Effects.ToArray(),
        StartsBattleAfterSelection = candidate.StartsBattle,
        AllowsPartialActiveDeaths = candidate.AllowsPartialActiveDeaths,
    };

    private static NetherEventDecision Pause(NetherPauseReason reason, string detail) => new()
    {
        Kind = NetherEventDecisionKind.Pause,
        PauseReason = reason,
        Detail = detail,
    };

    private static void ValidateInputs(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherEventOption> options,
        NetherAutoClimbSettings settings
    )
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
    }

    private readonly record struct EventCandidate(
        NetherEventOption Option,
        int ProjectedErosion,
        int ErosionDelta,
        int HpDelta,
        long ReplacementCodeId,
        int SafeCodeBenefit,
        int Benefit,
        bool StartsBattle,
        bool OptionalBattle,
        bool AllowsPartialActiveDeaths
    )
    {
        // Recovery must never select damage/erosion, but an otherwise neutral native option
        // is a valid safe fallback.  Requiring a positive reward here can deadlock the only
        // harmless recovery popup even though it has no projected downside.
        public bool HasPositiveOrNeutralRecoveryEffect => ErosionDelta <= 0 && HpDelta >= 0;
    }
}
