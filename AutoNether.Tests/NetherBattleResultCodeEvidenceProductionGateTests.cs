#nullable enable

using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public sealed class NetherBattleResultCodeEvidenceProductionGateTests
{
    [Fact]
    public void Result_owned_popup_is_accepted_without_a_floor_scene_entered_generation()
    {
        // Fresh current-game Cpp2IL: AbyssCodeSelectPopupController.InitializeView receives and
        // stores NetherPartyModel; FloorSelection transitions to Result before the result view
        // later returns to FloorSelection. Result owner 4 and runtime 6 are intentionally distinct.
        object controller = new();
        object popup = new();
        object party = new();
        var boundary = new NetherBattleResultCodeEvidenceCaptureBoundary(
            controller,
            popup,
            party,
            RuntimeGeneration: 6,
            OwnerGeneration: 4,
            Sequence: 1,
            IsCurrentResultOwner: true
        );

        NetherBattleResultCodeEvidenceCaptureDecision accepted =
            NetherBattleResultCodeEvidenceProductionGate.Evaluate(boundary, boundary);

        Assert.True(accepted.IsAccepted, accepted.Detail);
    }

    [Fact]
    public void Replaced_party_model_is_rejected_before_policy_can_use_it()
    {
        object controller = new();
        object popup = new();
        var before = new NetherBattleResultCodeEvidenceCaptureBoundary(
            controller,
            popup,
            new object(),
            RuntimeGeneration: 6,
            OwnerGeneration: 4,
            Sequence: 1,
            IsCurrentResultOwner: true
        );
        var after = before with { PartyModel = new object() };

        NetherBattleResultCodeEvidenceCaptureDecision rejected =
            NetherBattleResultCodeEvidenceProductionGate.Evaluate(before, after);

        Assert.False(rejected.IsAccepted);
        Assert.Equal("battle-result-code-evidence-owner-replaced-during-capture", rejected.Detail);
    }
}
