using FluentAssertions;
using Goal.Domain.Assignments;
using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Services;
using Goal.Domain.Sprints;
using Xunit;

namespace Goal.Domain.UnitTests;

public class XpCalculatorTests
{
    private static GoalSettings Settings() => new()
    {
        BaseXpTargetPerSprint = 100,
        XpScalableEasy = 10,
        XpScalableMedium = 25,
        XpScalableHard = 50
    };

    [Fact]
    public void Manual_xp_uses_snapshot_value()
    {
        var a = new SprintTaskAssignment { SnapshotXpMode = XpMode.Manual, SnapshotManualXp = 42 };

        var award = XpCalculator.Calculate(a, Settings(), deliveredOnTime: false, qualifiesForStreak: false);

        award.Base.Should().Be(42);
        award.Total.Should().Be(42);
    }

    [Fact]
    public void Scalable_xp_uses_difficulty_table_plus_bonuses()
    {
        var a = new SprintTaskAssignment
        {
            SnapshotXpMode = XpMode.Scalable,
            SnapshotDifficulty = Difficulty.Hard,   // 50
            SnapshotOnTimeBonusXp = 10,
            SnapshotStreakBonusXp = 5
        };

        var award = XpCalculator.Calculate(a, Settings(), deliveredOnTime: true, qualifiesForStreak: true);

        award.Base.Should().Be(50);
        award.OnTimeBonus.Should().Be(10);
        award.StreakBonus.Should().Be(5);
        award.Total.Should().Be(65);
    }

    [Fact]
    public void Bonuses_excluded_when_not_qualified()
    {
        var a = new SprintTaskAssignment
        {
            SnapshotXpMode = XpMode.Scalable,
            SnapshotDifficulty = Difficulty.Medium, // 25
            SnapshotOnTimeBonusXp = 10,
            SnapshotStreakBonusXp = 5
        };

        var award = XpCalculator.Calculate(a, Settings(), deliveredOnTime: false, qualifiesForStreak: false);

        award.Total.Should().Be(25);
    }
}

public class SprintDebtCalculatorTests
{
    [Fact]
    public void End_debt_is_shortfall_against_effective_target()
    {
        var state = new SprintMemberState { BaseTargetXp = 100, CarriedDebtXp = 0, EarnedXp = 60 };
        state.RecalculateTargets(0.70m);

        var outcome = SprintDebtCalculator.Close(state);

        outcome.EndDebtXp.Should().Be(40);            // 100 - 60
        outcome.ReachedThreshold.Should().BeFalse();  // 60 < 70
    }

    [Fact]
    public void No_debt_when_target_met()
    {
        var state = new SprintMemberState { BaseTargetXp = 100, CarriedDebtXp = 0, EarnedXp = 110 };
        state.RecalculateTargets(0.70m);

        var outcome = SprintDebtCalculator.Close(state);

        outcome.EndDebtXp.Should().Be(0);
        outcome.ReachedThreshold.Should().BeTrue();
    }

    [Fact]
    public void Debt_accumulates_into_next_sprint_effective_target()
    {
        var settings = new GoalSettings { BaseXpTargetPerSprint = 100, UnblockThresholdPct = 0.70m, DebtCarryEnabled = true };
        var prev = new SprintMemberState { BaseTargetXp = 100, CarriedDebtXp = 0, EarnedXp = 60 };
        prev.RecalculateTargets(0.70m);
        var outcome = SprintDebtCalculator.Close(prev); // endDebt = 40

        var next = SprintDebtCalculator.CreateNextState(Guid.NewGuid(), Guid.NewGuid(), outcome, settings);

        next.CarriedDebtXp.Should().Be(40);
        next.EffectiveTargetXp.Should().Be(140);                 // 100 + 40
        next.UnblockThresholdXp.Should().Be(98);                 // ceil(140 * 0.7)
    }

    [Fact]
    public void Excess_xp_does_not_carry_over()
    {
        var settings = new GoalSettings { BaseXpTargetPerSprint = 100, UnblockThresholdPct = 0.70m, DebtCarryEnabled = true };
        var prev = new SprintMemberState { BaseTargetXp = 100, CarriedDebtXp = 0, EarnedXp = 150 };
        prev.RecalculateTargets(0.70m);
        var outcome = SprintDebtCalculator.Close(prev);

        var next = SprintDebtCalculator.CreateNextState(Guid.NewGuid(), Guid.NewGuid(), outcome, settings);

        next.CarriedDebtXp.Should().Be(0);
        next.EffectiveTargetXp.Should().Be(100); // no negative debt / no credit carried
    }

    [Fact]
    public void Debt_carry_can_be_disabled()
    {
        var settings = new GoalSettings { BaseXpTargetPerSprint = 100, UnblockThresholdPct = 0.70m, DebtCarryEnabled = false };
        var prev = new SprintMemberState { BaseTargetXp = 100, CarriedDebtXp = 0, EarnedXp = 60 };
        prev.RecalculateTargets(0.70m);
        var outcome = SprintDebtCalculator.Close(prev);

        var next = SprintDebtCalculator.CreateNextState(Guid.NewGuid(), Guid.NewGuid(), outcome, settings);

        next.CarriedDebtXp.Should().Be(0);
    }
}
