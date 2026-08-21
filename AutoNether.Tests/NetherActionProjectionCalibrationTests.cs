using AutoNether.Services;
using Xunit;

namespace AutoNether.Tests;

public class NetherActionProjectionCalibrationTests
{
    [Fact]
    public void Matching_event_projection_clears_without_a_pause()
    {
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 25, hpDelta: -50), Snapshot(erosion: 20, hp: 900));

        NetherProjectionObservation observation = calibration.Observe(Snapshot(erosion: 25, hp: 850));

        Assert.False(observation.IsDrift);
        Assert.False(observation.RequiresRebaseline);
    }

    [Fact]
    public void Lower_than_projected_hp_or_wrong_erosion_fails_closed_as_drift()
    {
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 25, hpDelta: -50), Snapshot(erosion: 20, hp: 900));

        NetherProjectionObservation observation = calibration.Observe(Snapshot(erosion: 24, hp: 840));

        Assert.True(observation.IsDrift);
        Assert.Equal(NetherPauseReason.ErosionDrift, observation.PauseReason);
    }

    [Fact]
    public void Ordinary_event_projection_allows_an_untouched_living_character_when_damage_stays_within_authoritative_bounds()
    {
        NetherSnapshot before = Snapshot(erosion: 20, hp: 1000) with
        {
            Characters = [
                new NetherCharacterState(100, 1000),
                new NetherCharacterState(101, 1000),
            ],
        };
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 25, hpDelta: -100), before);

        NetherProjectionObservation observation = calibration.Observe(before with
        {
            ErosionPoint = 25,
            Characters = [
                new NetherCharacterState(100, 900),
                new NetherCharacterState(101, 1000),
            ],
        });

        Assert.False(observation.IsDrift);
        Assert.Equal(NetherPauseReason.None, observation.PauseReason);
    }

    [Fact]
    public void Code_change_rebaselines_erosion_but_still_rejects_unexpected_damage()
    {
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 25, hpDelta: -50), Snapshot(erosion: 20, hp: 900, codeHash: "before"));

        NetherProjectionObservation observation = calibration.Observe(Snapshot(erosion: 20, hp: 840, codeHash: "after"));

        Assert.True(observation.IsDrift);
        Assert.Equal(NetherPauseReason.UnsafeHp, observation.PauseReason);
    }

    [Fact]
    public void Authorized_partial_death_projection_accepts_a_dead_member_and_a_survivor()
    {
        NetherSnapshot before = Snapshot(erosion: 20, hp: 100) with
        {
            Characters = [
                new NetherCharacterState(1, 100),
                new NetherCharacterState(2, 400),
            ],
        };
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 20, hpDelta: -100, allowPartialActiveDeaths: true), before);

        NetherProjectionObservation observation = calibration.Observe(before with
        {
            ErosionPoint = 20,
            Characters = [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 300),
            ],
        });

        Assert.False(observation.IsDrift);
    }

    [Fact]
    public void Partial_death_projection_rejects_full_party_death()
    {
        NetherSnapshot before = Snapshot(erosion: 20, hp: 100) with
        {
            Characters = [
                new NetherCharacterState(1, 100),
                new NetherCharacterState(2, 400),
            ],
        };
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 20, hpDelta: -100, allowPartialActiveDeaths: true), before);

        NetherProjectionObservation observation = calibration.Observe(before with
        {
            Characters = [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 0, IsActive: false),
            ],
        });

        Assert.True(observation.IsDrift);
        Assert.Equal(NetherPauseReason.UnsafeHp, observation.PauseReason);
    }

    [Fact]
    public void Unauthorized_partial_death_projection_remains_fail_closed()
    {
        NetherSnapshot before = Snapshot(erosion: 20, hp: 100) with
        {
            Characters = [
                new NetherCharacterState(1, 100),
                new NetherCharacterState(2, 400),
            ],
        };
        var calibration = new NetherActionProjectionCalibration();
        calibration.Expect(Decision(erosion: 20, hpDelta: -100), before);

        NetherProjectionObservation observation = calibration.Observe(before with
        {
            Characters = [
                new NetherCharacterState(1, 0, IsActive: false),
                new NetherCharacterState(2, 300),
            ],
        });

        Assert.True(observation.IsDrift);
        Assert.Equal(NetherPauseReason.UnsafeHp, observation.PauseReason);
    }

    private static NetherEventDecision Decision(
        int erosion,
        int hpDelta,
        bool allowPartialActiveDeaths = false
    ) => new()
    {
        Kind = NetherEventDecisionKind.Select,
        ProjectedErosion = erosion,
        HpDelta = hpDelta,
        AllowsPartialActiveDeaths = allowPartialActiveDeaths,
    };

    private static NetherSnapshot Snapshot(int erosion, int hp, string codeHash = "same") => new()
    {
        ErosionPoint = erosion,
        CodeHash = codeHash,
        Characters = [new NetherCharacterState(100, hp)],
    };
}
