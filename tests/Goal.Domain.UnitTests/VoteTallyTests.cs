using FluentAssertions;
using Goal.Domain.Services;
using Xunit;

namespace Goal.Domain.UnitTests;

public class VoteTallyTests
{
    private const decimal Sixty = 0.60m;

    [Fact]
    public void Pending_until_enough_approvals()
    {
        // 5 eligible voters, need ceil(5*0.6)=3. Only 2 approvals so far.
        var r = VoteTally.Tally(eligibleVoters: 5, approvals: 2, rejections: 0, Sixty);

        r.Outcome.Should().Be(VotingOutcome.Pending);
        r.ApprovalsNeeded.Should().Be(3);
        r.ApprovalsRemaining.Should().Be(1);
    }

    [Fact]
    public void Approved_when_threshold_reached()
    {
        var r = VoteTally.Tally(eligibleVoters: 5, approvals: 3, rejections: 0, Sixty);

        r.Outcome.Should().Be(VotingOutcome.Approved);
        r.ApprovalsRemaining.Should().Be(0);
    }

    [Fact]
    public void Rejected_early_when_threshold_becomes_impossible()
    {
        // 5 voters, need 3. With 3 rejections only 2 can possibly approve -> impossible.
        var r = VoteTally.Tally(eligibleVoters: 5, approvals: 0, rejections: 3, Sixty);

        r.Outcome.Should().Be(VotingOutcome.Rejected);
    }

    [Fact]
    public void Solo_goal_auto_approves()
    {
        // No other eligible voters -> member isn't stuck forever.
        var r = VoteTally.Tally(eligibleVoters: 0, approvals: 0, rejections: 0, Sixty);

        r.Outcome.Should().Be(VotingOutcome.Approved);
    }

    [Fact]
    public void Cannot_be_approved_by_a_single_vote_when_many_eligible()
    {
        // 8 voters, need ceil(8*0.6)=5. One approval is not enough.
        var r = VoteTally.Tally(eligibleVoters: 8, approvals: 1, rejections: 0, Sixty);

        r.ApprovalsNeeded.Should().Be(5);
        r.Outcome.Should().Be(VotingOutcome.Pending);
    }
}
