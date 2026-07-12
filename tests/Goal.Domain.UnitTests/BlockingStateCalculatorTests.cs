using FluentAssertions;
using Goal.Domain.Goals;
using Goal.Domain.Services;
using Goal.Domain.Sprints;
using Xunit;

namespace Goal.Domain.UnitTests;

public class BlockingStateCalculatorTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private static (SprintMemberState state, Sprint sprint, GoalSettings settings) Build(
        int baseTarget = 100, int debt = 0, int earned = 0,
        decimal unblockPct = 0.70m, int finalDays = 1, decimal finalPct = 1.00m,
        int sprintDurationDays = 14)
    {
        var settings = new GoalSettings
        {
            BaseXpTargetPerSprint = baseTarget,
            UnblockThresholdPct = unblockPct,
            FinalTriggerDaysBefore = finalDays,
            FinalTriggerTargetPct = finalPct
        };
        var state = new SprintMemberState { BaseTargetXp = baseTarget, CarriedDebtXp = debt, EarnedXp = earned };
        state.RecalculateTargets(unblockPct); // effective target = base + debt

        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sprint = new Sprint { StartAt = start, EndAt = start.AddDays(sprintDurationDays) };
        return (state, sprint, settings);
    }

    [Fact]
    public void Blocked_when_below_threshold_and_far_from_end()
    {
        var (state, sprint, settings) = Build(baseTarget: 100, earned: 50); // threshold = 70
        var now = sprint.StartAt.AddDays(5);                                 // plenty of time left

        var result = BlockingStateCalculator.Calculate(state, sprint, settings, now, Utc);

        result.IsBlocked.Should().BeTrue();
        result.TargetXp.Should().Be(70);
        result.XpRemainingToUnblock.Should().Be(20);
        result.RequiresFullCompletion.Should().BeFalse();
    }

    [Fact]
    public void Unblocked_when_threshold_reached()
    {
        var (state, sprint, settings) = Build(baseTarget: 100, earned: 70); // exactly at 70%
        var now = sprint.StartAt.AddDays(5);

        var result = BlockingStateCalculator.Calculate(state, sprint, settings, now, Utc);

        result.IsBlocked.Should().BeFalse();
        result.XpRemainingToUnblock.Should().Be(0);
    }

    [Fact]
    public void Final_trigger_raises_target_to_full_on_last_day()
    {
        // At 70% earned, normally unblocked, but on the final day the target jumps to 100%.
        var (state, sprint, settings) = Build(baseTarget: 100, earned: 70);
        var now = sprint.EndAt.AddDays(-1); // 1 day remaining -> final trigger active

        var result = BlockingStateCalculator.Calculate(state, sprint, settings, now, Utc);

        result.TargetXp.Should().Be(100);
        result.RequiresFullCompletion.Should().BeTrue();
        result.IsBlocked.Should().BeTrue();
        result.XpRemainingToUnblock.Should().Be(30);
    }

    [Fact]
    public void Debt_raises_effective_target_and_threshold()
    {
        // base 100 + debt 50 = effective 150; threshold = ceil(150*0.7) = 105
        var (state, sprint, settings) = Build(baseTarget: 100, debt: 50, earned: 90);
        var now = sprint.StartAt.AddDays(5);

        var result = BlockingStateCalculator.Calculate(state, sprint, settings, now, Utc);

        result.EffectiveTargetXp.Should().Be(150);
        result.UnblockThresholdXp.Should().Be(105);
        result.IsBlocked.Should().BeTrue();          // 90 < 105
        result.XpRemainingToUnblock.Should().Be(15);
    }

    [Fact]
    public void Configurable_threshold_is_respected()
    {
        // A goal configured with a stricter 90% unblock threshold.
        var (state, sprint, settings) = Build(baseTarget: 100, earned: 80, unblockPct: 0.90m);
        var now = sprint.StartAt.AddDays(5);

        var result = BlockingStateCalculator.Calculate(state, sprint, settings, now, Utc);

        result.UnblockThresholdXp.Should().Be(90);
        result.IsBlocked.Should().BeTrue();           // 80 < 90
    }

    [Fact]
    public void CurrentPct_is_capped_at_one()
    {
        var (state, sprint, settings) = Build(baseTarget: 100, earned: 150);
        var now = sprint.StartAt.AddDays(5);

        var result = BlockingStateCalculator.Calculate(state, sprint, settings, now, Utc);

        result.CurrentPct.Should().Be(1m);
        result.IsBlocked.Should().BeFalse();
    }
}
