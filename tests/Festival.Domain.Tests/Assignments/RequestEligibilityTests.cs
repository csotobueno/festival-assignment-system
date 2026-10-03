using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class RequestEligibilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_ShouldExpressWhetherFrontStandingIsAllowed(bool allowsFrontStanding)
    {
        var eligibility = new RequestEligibility(allowsFrontStanding);

        Assert.Equal(allowsFrontStanding, eligibility.AllowsFrontStanding);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Equality_ShouldCompareByValue_WhenDecisionsMatch(bool allowsFrontStanding)
    {
        var first = new RequestEligibility(allowsFrontStanding);
        var second = new RequestEligibility(allowsFrontStanding);

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_ShouldDiffer_WhenFrontStandingDecisionsDiffer()
    {
        Assert.NotEqual(new RequestEligibility(true), new RequestEligibility(false));
    }
}
