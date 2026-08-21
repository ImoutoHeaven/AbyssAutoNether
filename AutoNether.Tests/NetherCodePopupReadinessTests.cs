#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherCodePopupReadinessTests
{
    [Fact]
    public void Default_discriminated_results_fail_closed()
    {
        NetherCodePopupReadinessResult popupReadiness = default;
        NetherOwnedCodePopupRegistrationDecision registrationReadiness = default;
        NetherRuntimePopupResult runtimePopup = default;

        Assert.Equal(NetherCodePopupReadinessKind.Invalid, popupReadiness.Kind);
        Assert.False(popupReadiness.IsReady);
        Assert.False(popupReadiness.IsPending);
        Assert.Equal(NetherOwnedCodePopupRegistrationKind.Invalid, registrationReadiness.Kind);
        Assert.False(registrationReadiness.IsReady);
        Assert.False(registrationReadiness.IsAwaiting);
        Assert.Equal(NetherRuntimePopupResultKind.Invalid, runtimePopup.Kind);
        Assert.False(runtimePopup.IsSuccess);
        Assert.False(runtimePopup.IsPending);
        Assert.False(runtimePopup.IsNativeContinuation);
        Assert.False(runtimePopup.IsDefinitelyAbsent);
    }

    [Fact]
    public void Registered_popup_with_offer_ids_but_without_initialized_model_is_not_ready()
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable: true,
            offerIdCount: 3,
            modelReadable: true,
            hasModel: false
        );

        Assert.False(result.IsReady);
        Assert.True(result.IsPending);
        Assert.Equal("code-offer-model-not-ready", result.Detail);
    }

    [Fact]
    public void Initialized_model_and_nonempty_offer_ids_are_ready()
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable: true,
            offerIdCount: 3,
            modelReadable: true,
            hasModel: true
        );

        Assert.True(result.IsReady);
        Assert.False(result.IsPending);
        Assert.Equal(string.Empty, result.Detail);
    }

    [Theory]
    [InlineData(false, 0, "code-offer-ids-member-unavailable")]
    [InlineData(true, 0, "code-offer-ids-empty")]
    public void Missing_or_empty_offer_ids_are_permanent_mapping_failures(
        bool offerIdsReadable,
        int offerIdCount,
        string expectedDetail
    )
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable,
            offerIdCount,
            modelReadable: true,
            hasModel: true
        );

        Assert.False(result.IsReady);
        Assert.False(result.IsPending);
        Assert.Equal(expectedDetail, result.Detail);
    }

    [Fact]
    public void Missing_model_member_is_a_binding_failure_not_a_ready_popup()
    {
        NetherCodePopupReadinessResult result = NetherCodePopupReadiness.Evaluate(
            offerIdsReadable: true,
            offerIdCount: 3,
            modelReadable: false,
            hasModel: false
        );

        Assert.False(result.IsReady);
        Assert.False(result.IsPending);
        Assert.Equal("code-offer-model-member-unavailable", result.Detail);
    }

    [Fact]
    public void Pending_budget_is_scoped_to_the_exact_runtime_owner_and_popup_sequence()
    {
        var gate = new NetherPopupReadinessGate(maximumPendingPolls: 1);
        NetherPopupReadinessIdentity first = Identity(runtime: 7, owner: 11, sequence: 13);

        Assert.Equal(NetherNativeActionResultKind.Started, gate.Await(first, "popup").Kind);
        Assert.Equal(NetherNativeActionResultKind.BindingUnavailable, gate.Await(first, "popup").Kind);

        Assert.Equal(
            NetherNativeActionResultKind.Started,
            gate.Await(first with { Sequence = 0 }, "popup").Kind
        );

        Assert.Equal(
            NetherNativeActionResultKind.Started,
            gate.Await(first with { RuntimeGeneration = 8 }, "popup").Kind
        );
        Assert.Equal(
            NetherNativeActionResultKind.Started,
            gate.Await(first with { OwnerGeneration = 12 }, "popup").Kind
        );
        Assert.Equal(
            NetherNativeActionResultKind.Started,
            gate.Await(first with { Sequence = 14 }, "popup").Kind
        );
    }

    [Fact]
    public void Invalid_or_cleared_identity_cannot_inherit_a_previous_wait_budget()
    {
        var gate = new NetherPopupReadinessGate(maximumPendingPolls: 1);
        NetherPopupReadinessIdentity identity = Identity(runtime: 3, owner: 5, sequence: 7);

        Assert.Equal(NetherNativeActionResultKind.Started, gate.Await(identity, "popup").Kind);
        Assert.Equal(
            NetherNativeActionResultKind.BindingUnavailable,
            gate.Await(identity with { OwnerGeneration = 0 }, "popup").Kind
        );

        gate.Clear();
        Assert.Equal(NetherNativeActionResultKind.Started, gate.Await(identity, "popup").Kind);
    }

    [Fact]
    public void Sequence_zero_wait_exists_only_before_the_first_exact_registration()
    {
        NetherOwnedCodePopupRegistrationDecision firstGap = Registration(
            observedSequence: 0,
            hasRegistration: false
        );

        Assert.True(firstGap.IsAwaiting);
        Assert.Equal(
            "awaiting-live-battle-result-code-popup:generation=11",
            firstGap.Detail
        );
    }

    [Fact]
    public void Live_result_owner_waits_for_a_replacement_after_its_initial_popup_closes_during_model_initialization()
    {
        NetherOwnedCodePopupRegistrationDecision replacementGap = Registration(
            observedSequence: 13,
            hasRegistration: false
        );

        Assert.True(replacementGap.IsAwaiting);
        Assert.Equal(
            "awaiting-live-battle-result-code-popup-replacement:generation=11:previous-sequence=13",
            replacementGap.Detail
        );
    }

    [Fact]
    public void Exact_registration_requires_the_same_runtime_owner_and_observed_sequence()
    {
        Assert.True(Registration(observedSequence: 13, hasRegistration: true).IsReady);

        Assert.Equal(
            NetherOwnedCodePopupRegistrationKind.Unavailable,
            Registration(
                observedSequence: 13,
                hasRegistration: true,
                registrationRuntime: 8
            ).Kind
        );
        Assert.Equal(
            NetherOwnedCodePopupRegistrationKind.Unavailable,
            Registration(
                observedSequence: 13,
                hasRegistration: true,
                registrationOwner: NetherActionKind.RecoveredCodeOffer
            ).Kind
        );
        Assert.Equal(
            NetherOwnedCodePopupRegistrationKind.Unavailable,
            Registration(
                observedSequence: 13,
                hasRegistration: true,
                registrationSequence: 14
            ).Kind
        );
    }

    private static NetherOwnedCodePopupRegistrationDecision Registration(
        long observedSequence,
        bool hasRegistration,
        long registrationRuntime = 7,
        NetherActionKind registrationOwner = NetherActionKind.BattleSettlement,
        long registrationSequence = 13
    ) => NetherOwnedCodePopupRegistrationReadiness.Evaluate(
        "battle-result-code",
        currentRuntimeGeneration: 7,
        expectedOwnerAction: NetherActionKind.BattleSettlement,
        expectedOwnerGeneration: 11,
        observedSequence,
        hasRegistration,
        registrationIsLive: true,
        registrationRuntimeGeneration: registrationRuntime,
        registrationOwnerAction: registrationOwner,
        registrationOwnerGeneration: 11,
        registrationSequence
    );

    private static NetherPopupReadinessIdentity Identity(long runtime, long owner, long sequence) =>
        new(runtime, NetherActionKind.SelectFloor, owner, sequence);
}
