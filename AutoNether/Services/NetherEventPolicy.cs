#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

internal sealed record NetherEventOption(int OptionNumber, IReadOnlyList<NetherEffect> Effects);

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
    ) => Decide(snapshot, options, settings, modifiers, isRecovery: false);

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
    ) => Decide(snapshot, options, settings, modifiers, isRecovery: true);

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
        bool hpPaymentRejectedByFloor = false;
        NetherEventDecision? firstSpecificRejection = null;
        foreach (NetherEventOption option in options)
        {
            bool isKeyPayment = IsVerifiedTreasureKeyPayment(option);
            bool isHpPayment = IsVerifiedTreasureHpPayment(option);
            if (!isKeyPayment && !isHpPayment)
                continue;

            if (!TryValidateOption(
                    option,
                    snapshot,
                    settings,
                    modifiers,
                    out EventCandidate candidate,
                    out NetherEventDecision rejection
                ))
            {
                if (rejection.PauseReason != NetherPauseReason.NoSafeRoute && firstSpecificRejection == null)
                    firstSpecificRejection = rejection;
                continue;
            }

            if (isKeyPayment)
            {
                keyCandidates.Add(candidate);
                continue;
            }

            if (!HasMinimumActiveHpAfterDelta(snapshot, candidate.HpDelta, settings.MinimumCharacterHpPermille))
            {
                hpPaymentRejectedByFloor = true;
                continue;
            }
            hpCandidates.Add(candidate);
        }

        // The persisted enum value remains KeyOnly for configuration compatibility. Its runtime
        // contract is key-preferred: a verified one-key option wins, while a native HP-payment
        // option is an allowed fallback only when every living member stays above the configured
        // route floor.
        if (keyCandidates.Count > 0)
        {
            EventCandidate selectedKey = keyCandidates
                .OrderByDescending(candidate => candidate.Benefit)
                .ThenBy(candidate => candidate.Option.OptionNumber)
                .First();
            return Select(selectedKey);
        }

        if (hpCandidates.Count > 0)
        {
            EventCandidate selectedHp = hpCandidates
                .OrderByDescending(candidate => candidate.HpDelta)
                .ThenByDescending(candidate => candidate.Benefit)
                .ThenBy(candidate => candidate.Option.OptionNumber)
                .First();
            return Select(selectedHp);
        }

        if (hpPaymentRejectedByFloor)
            return Pause(NetherPauseReason.UnsafeHp, "treasure-hp-payment-below-minimum");
        if (firstSpecificRejection != null)
            return firstSpecificRejection;
        return Pause(NetherPauseReason.NoSafeRoute, "no-verified-key-or-safe-hp-treasure-option");
    }

    private static bool IsVerifiedTreasureKeyPayment(NetherEventOption? option) =>
        option?.Effects != null
        && option.Effects.Count(effect => effect.Kind == NetherEffectKind.TreasureKeyUsed && effect.Amount == 1) == 1
        && option.Effects.All(effect =>
            (effect.Kind == NetherEffectKind.TreasureKeyUsed && effect.Amount == 1)
            || IsVerifiedTreasureBenefit(effect.Kind));

    private static bool IsVerifiedTreasureHpPayment(NetherEventOption? option) =>
        option?.Effects != null
        && option.Effects.Count(effect => effect.Kind == NetherEffectKind.Damage && effect.Amount > 0) == 1
        && option.Effects.All(effect =>
            (effect.Kind == NetherEffectKind.Damage && effect.Amount > 0)
            || IsVerifiedTreasureBenefit(effect.Kind));

    private static bool IsVerifiedTreasureBenefit(NetherEffectKind kind) => kind is
        NetherEffectKind.ErosionHeal
        or NetherEffectKind.Item
        or NetherEffectKind.NetherGoldGain
        or NetherEffectKind.TreasureKeyGain
        or NetherEffectKind.AbyssCodeOffer;

    private static bool HasMinimumActiveHpAfterDelta(
        NetherSnapshot snapshot,
        int hpDelta,
        int minimumHpPermille
    )
    {
        bool foundActive = false;
        try
        {
            foreach (NetherCharacterState character in snapshot.Characters)
            {
                if (!character.IsActive)
                    continue;
                foundActive = true;
                if (checked(character.HpPermille + hpDelta) < minimumHpPermille)
                    return false;
            }
        }
        catch (OverflowException)
        {
            return false;
        }
        return foundActive;
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
        bool isRecovery
    )
    {
        ValidateInputs(snapshot, options, settings);
        var candidates = new List<EventCandidate>();
        NetherPauseReason firstRejection = NetherPauseReason.NoSafeRoute;
        string firstDetail = "no-safe-event-option";
        foreach (NetherEventOption option in options)
        {
            if (!TryValidateOption(
                    option,
                    snapshot,
                    settings,
                    modifiers,
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
            .OrderByDescending(candidate => belowHpSoftLimit && candidate.HpDelta > 0)
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
        out EventCandidate candidate,
        out NetherEventDecision rejection
    )
    {
        candidate = default;
        rejection = default!;
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
            NetherCodeTransformDecision transform = _transformPolicy.Decide(snapshot.Codes, snapshot.CodeCapacity);
            if (!transform.CanTransform)
            {
                rejection = Pause(transform.PauseReason, transform.Detail);
                return false;
            }
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
        if (snapshot.Characters.Any(character => character.IsActive && character.HpPermille + hpDelta <= 0))
        {
            rejection = Pause(NetherPauseReason.UnsafeHp, "lethal-event-damage");
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
            0,
            option.Effects.Any(effect => effect.Kind == NetherEffectKind.AbyssCodeOffer) ? 1 : 0,
            benefit,
            startsBattle,
            optionalBattle
        );
        return true;
    }

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
        bool OptionalBattle
    )
    {
        // Recovery must never select damage/erosion, but an otherwise neutral native option
        // is a valid safe fallback.  Requiring a positive reward here can deadlock the only
        // harmless recovery popup even though it has no projected downside.
        public bool HasPositiveOrNeutralRecoveryEffect => ErosionDelta <= 0 && HpDelta >= 0;
    }
}
