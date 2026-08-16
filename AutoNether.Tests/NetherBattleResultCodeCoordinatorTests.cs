#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherBattleResultCodeCoordinatorTests
{
    [Fact]
    public void Live_result_code_offer_is_selected_before_next_can_continue()
    {
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = Candidates(30024),
            Popup = ResultPopup(),
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        NetherBattleResultCodeStep invoked = flow.Pump(driver, Settings(), null, allowInvoke: true);

        Assert.Equal(NetherBattleResultCodeStepKind.AwaitingNative, invoked.Kind);
        Assert.Equal(NetherActionKind.SelectCode, driver.InvokedActions.Single().Kind);
        Assert.Equal(30024, driver.InvokedActions.Single().CodeId);

        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Pending("code-confirm-pending"));
        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.Completed("code-confirm-terminal"));
        Assert.Equal(
            NetherBattleResultCodeStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Equal(
            NetherBattleResultCodeStepKind.Completed,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Single(driver.InvokedActions);
    }

    [Fact]
    public void Result_owner_blocks_next_while_its_popup_registration_is_pending()
    {
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = Candidates(30024),
            Popup = ResultPopup() with { Sequence = 0 },
            PopupIsPending = true,
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        Assert.Equal(
            NetherBattleResultCodeStepKind.AwaitingPopup,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Empty(driver.InvokedActions);

        driver.Popup = ResultPopup();
        driver.PopupIsPending = false;
        Assert.Equal(
            NetherBattleResultCodeStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Single(driver.InvokedActions);
    }

    [Fact]
    public void Reload_ready_redecides_same_result_popup_then_selects_once()
    {
        var driver = new Driver
        {
            Snapshot = Snapshot() with
            {
                CodeReloadCount = 2,
                CodeCapacity = 1,
                Codes = new[] { RushState(51000, power: 100) },
                CodeHash = "codes:rush-51000",
            },
            Candidates = Candidates(52001, NetherCodeCategory.Impact),
            Popup = ResultPopup(),
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        Assert.Equal(
            NetherBattleResultCodeStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Equal(NetherActionKind.ReloadCode, driver.InvokedActions.Single().Kind);

        driver.NativeSteps.Enqueue(NetherBattleResultCodeNativeStep.ReloadReady("fresh-offer"));
        Assert.Equal(
            NetherBattleResultCodeStepKind.ReloadReady,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );

        driver.Snapshot = driver.Snapshot with { CodeReloadCount = 1 };
        driver.Candidates = Candidates(52002, NetherCodeCategory.Rush, power: 200);
        driver.Popup = ResultPopup() with { DecisionEpoch = 1 };
        Assert.Equal(
            NetherBattleResultCodeStepKind.AwaitingNative,
            flow.Pump(driver, Settings(), null, allowInvoke: true).Kind
        );
        Assert.Equal(
            new[] { NetherActionKind.ReloadCode, NetherActionKind.SelectCode },
            driver.InvokedActions.Select(action => action.Kind)
        );
    }

    [Fact]
    public void F12_off_before_result_code_decision_performs_no_mutation()
    {
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = Candidates(30024),
            Popup = ResultPopup(),
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        NetherBattleResultCodeStep step = flow.Pump(driver, Settings(), null, allowInvoke: false);

        Assert.Equal(NetherBattleResultCodeStepKind.CanceledBeforeInvoke, step.Kind);
        Assert.Empty(driver.InvokedActions);
    }

    [Fact]
    public void No_authoritative_offer_allows_result_next_without_popup_guessing()
    {
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = new NetherRuntimeCodeCandidatesResult(
                Array.Empty<NetherCodeCandidate>(),
                IsMasterComplete: true,
                Detail: string.Empty
            ),
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        NetherBattleResultCodeStep step = flow.Pump(driver, Settings(), null, allowInvoke: true);

        Assert.Equal(NetherBattleResultCodeStepKind.Completed, step.Kind);
        Assert.Empty(driver.InvokedActions);
    }

    [Fact]
    public void Result_owner_uses_authoritative_equipment_value_not_reversed_displayed_power()
    {
        NetherCodeCandidate displayedHigh = Candidate(39991, power: 99_999);
        NetherCodeCandidate nativeHigh = Candidate(39992, power: 1);
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = new NetherRuntimeCodeCandidatesResult(
                [displayedHigh, nativeHigh],
                IsMasterComplete: true,
                Detail: string.Empty
            ),
            Popup = ResultPopup(),
            PolicyEvidence = EquipmentEvidence(displayedHigh, nativeHigh),
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        NetherBattleResultCodeStep step = flow.Pump(driver, Settings(), null, allowInvoke: true);

        Assert.Equal(NetherBattleResultCodeStepKind.AwaitingNative, step.Kind);
        Assert.Equal(nativeHigh.CodeId, Assert.Single(driver.InvokedActions).CodeId);
    }

    [Fact]
    public void Result_owner_research_uses_exact_active_family_not_reversed_displayed_power()
    {
        NetherCodeCandidate wrongFamilyDisplayedHigh = Candidate(
            39995,
            NetherCodeCategory.Impact,
            power: 99_999
        );
        NetherCodeCandidate activeRush = Candidate(39996, NetherCodeCategory.Rush, power: 1);
        NetherCodePolicyEvidence policyEvidence = EquipmentEvidence(
            wrongFamilyDisplayedHigh,
            activeRush
        ) with
        {
            ActiveResearchFamily = NetherCodeFamily.Rush,
            Research =
            [
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Rush, 0, 0, 0),
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Impact, 0, 0, 0),
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Safe, 0, 0, 0),
                new NetherStrategyResearchFamilyState(NetherCodeFamily.Risk, 0, 0, 0),
            ],
        };
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = new NetherRuntimeCodeCandidatesResult(
                [wrongFamilyDisplayedHigh, activeRush],
                IsMasterComplete: true,
                Detail: string.Empty
            ),
            Popup = ResultPopup(),
            PolicyEvidence = policyEvidence,
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);
        NetherAutoClimbSettings settings = Settings() with
        {
            StrategyMode = NetherStrategyMode.Research,
            ResearchPrimaryFamily = NetherCodeFamily.Rush,
        };

        NetherBattleResultCodeStep step = flow.Pump(driver, settings, null, allowInvoke: true);

        Assert.Equal(NetherBattleResultCodeStepKind.AwaitingNative, step.Kind);
        Assert.Equal(activeRush.CodeId, Assert.Single(driver.InvokedActions).CodeId);
    }

    [Fact]
    public void Result_owner_uses_mechanism_tier_and_rejects_unknown_candidate_locally()
    {
        NetherCodeCandidate unknownDisplayedHigh = Candidate(39997, power: 99_999);
        NetherCodeCandidate forceChain = Candidate(39998, power: 1);
        NetherMechanismValue forceValue = NetherMechanismValue.Qualitative(
            NetherMechanismQualitativePriority.BackForceChainHigh,
            "force-chain-completion-message"
        );
        NetherCodePolicyEvidence evidence = EquipmentEvidence(
            unknownDisplayedHigh,
            forceChain
        ) with
        {
            MechanicsByCodeId = new Dictionary<long, NetherCodeHardEligibilityEvidence>
            {
                [unknownDisplayedHigh.CodeId] = new()
                {
                    IsKnown = false,
                    UnknownReason = "ability-effect-asset-unavailable",
                },
                [forceChain.CodeId] = new() { IsKnown = true },
            },
            MechanismValuesByCodeId = new Dictionary<long, NetherMechanismValue>
            {
                [unknownDisplayedHigh.CodeId] = NetherMechanismValue.Missing(
                    "ability-effect-asset-unavailable"
                ),
                [forceChain.CodeId] = forceValue,
            },
            EquipmentMutationValuesByKey = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>
            {
                [new NetherCodeMutationKey(forceChain.CodeId, 0)] = Mutation(
                    forceChain.CodeId,
                    nativeValuePermille: 0
                ) with
                {
                    MechanismValue = forceValue,
                    Survival = NetherSurvivalRepairEvidence.Known(false, false),
                    MechanismPortfolio = NetherMechanismPortfolioComparisonEvidence.Known(
                        [],
                        [new NetherMechanismPortfolioEntry(forceChain.CodeId, forceValue)]
                    ),
                },
            },
        };
        var driver = new Driver
        {
            Snapshot = Snapshot(),
            Candidates = new NetherRuntimeCodeCandidatesResult(
                [unknownDisplayedHigh, forceChain],
                IsMasterComplete: true,
                Detail: string.Empty
            ),
            Popup = ResultPopup(),
            PolicyEvidence = evidence,
        };
        var flow = new NetherBattleResultCodeCoordinator(maximumPopupPolls: 2);

        NetherBattleResultCodeStep step = flow.Pump(driver, Settings(), null, allowInvoke: true);

        Assert.Equal(NetherBattleResultCodeStepKind.AwaitingNative, step.Kind);
        Assert.Equal(forceChain.CodeId, Assert.Single(driver.InvokedActions).CodeId);
    }

    private static NetherRuntimePopupContext ResultPopup() => new()
    {
        Kind = NetherRuntimePopupKind.CodeOffer,
        RuntimeGeneration = 3,
        OwnerAction = NetherActionKind.BattleSettlement,
        OwnerGeneration = 9,
        Sequence = 12,
        DecisionEpoch = 0,
    };

    private static NetherSnapshot Snapshot() => new()
    {
        Status = NetherSessionStatus.Play,
        NetherId = 1,
        MapId = 1,
        CurrentFloorId = 27,
        CurrentNodeId = 38654705666,
        FloorLevel = 8,
        FloorIndex = 1,
        CodeReloadCount = 1,
        CodeCapacity = 28,
        Characters = new[] { new NetherCharacterState(1001, 900) },
        Codes = Array.Empty<NetherCodeState>(),
        Floors = Array.Empty<NetherFloorNode>(),
        CharacterHpHash = "1001:900:1",
        CodeHash = string.Empty,
        MapHash = "map",
    };

    private static NetherCodeCandidate Candidate(
        long codeId,
        NetherCodeCategory category = NetherCodeCategory.Safe,
        int power = 0
    ) => NetherCodeRuntimeSemanticMapper.MapCandidate(
        codeId,
        (int)category,
        effectType: 1,
        effectParameter1: 100006,
        effectParameter2: 1,
        effectParameter3: 0,
        rarity: 1,
        power: power
    ) with { PartyCoverageKnown = true, PartyCoverage = 1 };

    private static NetherRuntimeCodeCandidatesResult Candidates(
        long codeId,
        NetherCodeCategory category = NetherCodeCategory.Safe,
        int power = 0
    ) => new(
        [Candidate(codeId, category, power)],
        IsMasterComplete: true,
        Detail: string.Empty
    );

    private static NetherCodePolicyEvidence EquipmentEvidence(
        params NetherCodeCandidate[] candidates
    )
    {
        return new NetherCodePolicyEvidence
        {
            MechanicsByCodeId = candidates.ToDictionary(
                candidate => candidate.CodeId,
                _ => new NetherCodeHardEligibilityEvidence { IsKnown = true }
            ),
            MechanismValuesByCodeId = candidates.ToDictionary(
                candidate => candidate.CodeId,
                _ => KnownZeroMechanism()
            ),
            EquipmentMutationValuesByKey = new Dictionary<
                NetherCodeMutationKey,
                NetherCodeEquipmentMutationEvidence
            >
            {
                [new NetherCodeMutationKey(candidates[0].CodeId, 0)] = Mutation(
                    candidates[0].CodeId,
                    nativeValuePermille: 100
                ),
                [new NetherCodeMutationKey(candidates[^1].CodeId, 0)] = Mutation(
                    candidates[^1].CodeId,
                    nativeValuePermille: candidates.Length == 1 ? 100 : 200
                ),
            },
            ActiveParty =
            [
                new NetherStrategyPartyMember(
                    1001,
                    0,
                    NetherPartyPosition.Back,
                    1,
                    NetherCrestIdentity.Impact,
                    900,
                    true,
                    1,
                    0
                ),
            ],
        };
    }

    private static NetherCodePolicyEvidence DefaultEquipmentEvidence(
        NetherSnapshot snapshot,
        IReadOnlyList<NetherCodeCandidate> candidates
    )
    {
        NetherCodePolicyEvidence basis = EquipmentEvidence(candidates.ToArray());
        var mutations = new Dictionary<NetherCodeMutationKey, NetherCodeEquipmentMutationEvidence>();
        foreach (NetherCodeCandidate candidate in candidates)
        {
            if (snapshot.Codes.Count < snapshot.CodeCapacity)
            {
                mutations[new NetherCodeMutationKey(candidate.CodeId, 0)] = Mutation(
                    candidate.CodeId,
                    nativeValuePermille: 100
                );
                continue;
            }
            foreach (NetherCodeState removed in snapshot.Codes)
            {
                mutations[new NetherCodeMutationKey(candidate.CodeId, removed.CodeId)] = Mutation(
                    candidate.CodeId,
                    nativeValuePermille: Math.Max(100, removed.Power + 100),
                    removeCodeId: removed.CodeId
                );
            }
        }
        return basis with { EquipmentMutationValuesByKey = mutations };
    }

    private static NetherCodeEquipmentMutationEvidence Mutation(
        long candidateCodeId,
        int nativeValuePermille,
        long removeCodeId = 0
    ) => new(
        candidateCodeId,
        removeCodeId,
        new NetherNativePortfolioComparisonInput(
            BeforeWindows: [],
            AfterWindows:
            [
                new NetherNativeBuffWindow(
                    candidateCodeId,
                    RecipientCharacterId: 1001,
                    new NetherStrategyBuffType(10),
                    NetherStrategyBuffEffectKind.Buff,
                    NetherStrategyBuffCoexistenceKind.Allow,
                    NetherCombatMetricKind.Attack,
                    nativeValuePermille,
                    StartSecond: 0,
                    DurationSeconds: 10
                ),
            ],
            BossDurationSeconds: 10
        ),
        KnownZeroMechanism()
    )
    {
        CombatTier = NetherEquipmentCombatTier.RearOrFullOffense,
        Survival = NetherSurvivalRepairEvidence.Known(false, false),
        MechanismPortfolio = NetherMechanismPortfolioComparisonEvidence.Known([], []),
        RecipientPositions = new Dictionary<long, NetherPartyPosition>
        {
            [1001] = NetherPartyPosition.Back,
        },
    };

    private static NetherMechanismValue KnownZeroMechanism() =>
        NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.None,
            0,
            "known-zero-mechanism"
        );

    private static NetherCodeState RushState(long codeId, int power) =>
        NetherCodeRuntimeSemanticMapper.MapState(
            codeId,
            (int)NetherCodeCategory.Rush,
            effectType: 1,
            effectParameter1: 100006,
            effectParameter2: 1,
            effectParameter3: 0,
            rarity: 1,
            power: power,
            possessionAmount: 1
        ) with { PartyCoverageKnown = true, PartyCoverage = 1 };

    private static NetherAutoClimbSettings Settings() => new()
    {
        CombatLane = NetherCombatLane.Auto,
        CodeReloadReserve = 1,
    };

    private sealed class Driver : INetherBattleResultCodeDriver
    {
        public NetherSnapshot Snapshot { get; set; } = Snapshot();
        public NetherRuntimeCodeCandidatesResult Candidates { get; set; } = Candidates(30024);
        public NetherRuntimePopupContext? Popup { get; set; }
        public bool PopupIsPending { get; set; }
        public NetherCodePolicyEvidence? PolicyEvidence { get; set; }
        public List<NetherPlannedAction> InvokedActions { get; } = new();
        public Queue<NetherBattleResultCodeNativeStep> NativeSteps { get; } = new();

        public NetherRuntimeSnapshotResult TryCaptureBattleResultCodeSnapshot() =>
            NetherRuntimeSnapshotResult.Success(Snapshot);

        public NetherRuntimeCodeCandidatesResult TryGetCodeCandidates() => Candidates;

        public NetherRuntimePopupResult TryGetBattleResultCodePopup() => Popup == null
            ? NetherRuntimePopupResult.Failure("popup-registration-missing")
            : PopupIsPending
                ? NetherRuntimePopupResult.Pending(Popup, "code-offer-model-not-ready")
                : NetherRuntimePopupResult.Success(Popup);

        public NetherRuntimeCodePolicyEvidenceResult TryCaptureCodePolicyEvidence(
            NetherSnapshot snapshot,
            NetherRuntimeCodeCandidatesResult candidates,
            NetherAutoClimbSettings settings
        ) => NetherRuntimeCodePolicyEvidenceResult.Success(
            PolicyEvidence ?? DefaultEquipmentEvidence(snapshot, candidates.Candidates)
        );

        public NetherNativeActionResult InvokeBattleResultCode(
            NetherRuntimePopupContext popup,
            NetherPlannedAction action
        )
        {
            InvokedActions.Add(action);
            return NetherNativeActionResult.Started("result-code-invoked");
        }

        public NetherBattleResultCodeNativeStep PollBattleResultCodeNative() =>
            NativeSteps.Count == 0
                ? NetherBattleResultCodeNativeStep.Pending("result-code-pending")
                : NativeSteps.Dequeue();
    }
}
