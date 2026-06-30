using Festival.Domain.Attendees;

namespace Festival.Domain.Tests.Attendees;

public sealed class AttendeeTests
{
    [Fact]
    public void Create_ShouldReturnAttendee_WhenDataIsValid()
    {
        var id = AttendeeId.New();
        var code = AttendeeCode.Create("ATT-001");

        var attendee = Attendee.Create(
            id,
            code,
            "Christian Soto");

        Assert.Equal(id, attendee.Id);
        Assert.Equal(code, attendee.Code);
        Assert.Equal("Christian Soto", attendee.Name);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var attendee = Attendee.Create(
            AttendeeId.New(),
            AttendeeCode.Create("ATT-001"),
            "  Christian Soto  ");

        Assert.Equal("Christian Soto", attendee.Name);
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Attendee.Create(
                default,
                AttendeeCode.Create("ATT-001"),
                "Christian Soto"));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenCodeIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => Attendee.Create(
                AttendeeId.New(),
                null!,
                "Christian Soto"));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenNameIsEmpty(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Attendee.Create(
                AttendeeId.New(),
                AttendeeCode.Create("ATT-001"),
                name));

        Assert.Equal("name", exception.ParamName);
    }
}