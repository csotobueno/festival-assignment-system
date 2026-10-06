using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneEligibilityPolicyTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    public void Filter_ShouldUseRequestEligibilityForIndividualAndGroupRequests(
        bool allowsFrontStanding,
        int attendeeCount)
    {
        var request = CreateRequest(allowsFrontStanding, attendeeCount);
        var first = Zone.Create(ZoneId.New(), "Zone B", ZoneType.MiddleLeft);
        var front = Zone.Create(ZoneId.New(), "Front Standing", ZoneType.FrontStanding);
        var last = Zone.Create(ZoneId.New(), "Zone A", ZoneType.MiddleLeft);
        Zone[] available = [first, front, last];

        var eligible = ZoneEligibilityPolicy.Filter(request, available);

        Zone[] expected = allowsFrontStanding ? [first, front, last] : [first, last];
        Assert.Equal(expected, eligible);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Same(expected[index], eligible[index]);
        }

        Assert.Equal(new[] { first, front, last }, available);
        Assert.Equal(allowsFrontStanding, request.Eligibility.AllowsFrontStanding);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filter_ShouldPreserveZones_WhenNoFrontStandingIsAvailable(
        bool allowsFrontStanding)
    {
        Zone[] available =
        [
            Zone.Create(ZoneId.New(), "Zone B", ZoneType.MiddleLeft),
            Zone.Create(ZoneId.New(), "Zone A", ZoneType.MiddleLeft)
        ];

        var eligible = ZoneEligibilityPolicy.Filter(
            CreateRequest(allowsFrontStanding), available);

        Assert.Equal(available, eligible);
    }

    [Theory]
    [InlineData(ZoneType.MiddleLeft)]
    [InlineData(ZoneType.MiddleCenter)]
    [InlineData(ZoneType.MiddleRight)]
    [InlineData(ZoneType.UpperLeft)]
    [InlineData(ZoneType.UpperCenter)]
    [InlineData(ZoneType.UpperRight)]
    public void Filter_ShouldPreserveNonFrontTypes_WhenFrontStandingIsDisallowed(ZoneType type)
    {
        var zone = Zone.Create(ZoneId.New(), "Front", type);

        var eligible = ZoneEligibilityPolicy.Filter(CreateRequest(false), [zone]);

        Assert.Same(zone, Assert.Single(eligible));
    }

    [Fact]
    public void Filter_ShouldIdentifyFrontStandingByTypeRatherThanDisplayName()
    {
        var front = Zone.Create(ZoneId.New(), "Zone A", ZoneType.FrontStanding);
        var other = Zone.Create(ZoneId.New(), "Front Standing", ZoneType.MiddleLeft);

        var eligible = ZoneEligibilityPolicy.Filter(CreateRequest(false), [front, other]);

        Assert.Same(other, Assert.Single(eligible));
    }

    [Fact]
    public void Filter_ShouldExcludeEveryFrontStandingZone_WhenFrontStandingIsDisallowed()
    {
        Zone[] available =
        [
            Zone.Create(ZoneId.New(), "Zone A", ZoneType.FrontStanding),
            Zone.Create(ZoneId.New(), "Zone B", ZoneType.FrontStanding)
        ];

        var eligible = ZoneEligibilityPolicy.Filter(CreateRequest(false), available);

        Assert.Empty(eligible);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filter_ShouldReturnEmpty_WhenNoZonesAreAvailable(bool allowsFrontStanding)
    {
        var eligible = ZoneEligibilityPolicy.Filter(CreateRequest(allowsFrontStanding), []);

        Assert.Empty(eligible);
    }

    [Fact]
    public void Filter_ShouldThrow_WhenRequestIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ZoneEligibilityPolicy.Filter(null!, []));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Filter_ShouldThrow_WhenAvailableZonesAreNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ZoneEligibilityPolicy.Filter(CreateRequest(true), null!));

        Assert.Equal("availableZones", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filter_ShouldThrow_WhenAvailableZonesContainNull(bool allowsFrontStanding)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ZoneEligibilityPolicy.Filter(CreateRequest(allowsFrontStanding), [null!]));

        Assert.Equal("availableZones", exception.ParamName);
    }

    private static AssignmentRequest CreateRequest(
        bool allowsFrontStanding,
        int attendeeCount = 1)
    {
        return AssignmentRequest.Create(
            AssignmentRequestId.New(),
            FestivalDayId.New(),
            Enumerable.Range(1, attendeeCount)
                .Select(number => AttendeeCode.Create($"ATT-{number:000}"))
                .ToArray(),
            new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero),
            new RequestEligibility(allowsFrontStanding));
    }
}
