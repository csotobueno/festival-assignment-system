using System.Reflection;
using System.Text.RegularExpressions;
using Festival.Domain.Assignments;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Festival.Infrastructure.Tests.Persistence.Migrations;

public sealed class FestivalInitialMigrationTests
{
    private static readonly string[] ExpectedApplicationTables =
    [
        "AssignmentRequestAttendees",
        "AssignmentRequests",
        "Assignments",
        "Attendees",
        "FestivalDays",
        "Spots",
        "Zones"
    ];

    [Fact]
    public void InfrastructureAssembly_ShouldContainOneInitialMigration()
    {
        var migrations = typeof(FestivalDbContext).Assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(Migration)))
            .Where(type => !type.IsAbstract)
            .ToArray();

        migrations.Should().ContainSingle();
        migrations[0].Name.Should().Be(nameof(InitialCreate));

        var migrationAttribute = migrations[0]
            .GetCustomAttribute<MigrationAttribute>();

        migrationAttribute.Should().NotBeNull();
        migrationAttribute!.Id.Should().EndWith("_InitialCreate");
    }

    [Fact]
    public void InitialMigration_ShouldTargetFestivalDbContext()
    {
        var contextAttribute = typeof(InitialCreate)
            .GetCustomAttribute<DbContextAttribute>();

        contextAttribute.Should().NotBeNull();
        contextAttribute!.ContextType.Should().Be(typeof(FestivalDbContext));
    }

    [Fact]
    public void InfrastructureAssembly_ShouldContainFestivalDbContextModelSnapshot()
    {
        typeof(FestivalDbContextModelSnapshot)
            .Should()
            .BeAssignableTo<ModelSnapshot>();

        var contextAttribute = typeof(FestivalDbContextModelSnapshot)
            .GetCustomAttribute<DbContextAttribute>();

        contextAttribute.Should().NotBeNull();
        contextAttribute!.ContextType.Should().Be(typeof(FestivalDbContext));
    }

    [Fact]
    public void MigrationSqlScript_ShouldBeGeneratedWithoutOpeningDatabaseConnection()
    {
        using var context = CreateContext();

        var script = GenerateScript(context);

        script.Should().NotBeNullOrWhiteSpace();
        context.Database.GetDbConnection().State.Should()
            .Be(System.Data.ConnectionState.Closed);
    }

    [Fact]
    public void MigrationSqlScript_ShouldCreateExpectedApplicationTables()
    {
        using var context = CreateContext();

        var script = GenerateScript(context);
        var createdTables = Regex.Matches(
                script,
                "^CREATE TABLE \"(?<table>[^\"]+)\"",
                RegexOptions.Multiline)
            .Select(match => match.Groups["table"].Value)
            .Order()
            .ToArray();

        createdTables.Should().Equal(ExpectedApplicationTables);
    }

    [Fact]
    public void MigrationSqlScript_ShouldContainCriticalIndexesAndCheckConstraint()
    {
        using var context = CreateContext();

        var script = GenerateScript(context);

        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_Attendees_AttendeeCode\" ON \"Attendees\" (\"AttendeeCode\")");
        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_FestivalDays_Date\" ON \"FestivalDays\" (\"Date\")");
        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_Spots_ZoneId_RowCode_SpotNumber\" ON \"Spots\" (\"ZoneId\", \"RowCode\", \"SpotNumber\")");
        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_AssignmentRequestAttendees_AssignmentRequestId_AttendeeCode\" ON \"AssignmentRequestAttendees\" (\"AssignmentRequestId\", \"AttendeeCode\")");
        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_Assignments_FestivalDayId_SpotCode\" ON \"Assignments\" (\"FestivalDayId\", \"SpotCode\")");
        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_Assignments_FestivalDayId_AttendeeId\" ON \"Assignments\" (\"FestivalDayId\", \"AttendeeId\")");
        script.Should().Contain(
            "CREATE UNIQUE INDEX \"IX_Assignments_AssignmentRequestId_AttendeeId\" ON \"Assignments\" (\"AssignmentRequestId\", \"AttendeeId\")");
        script.Should().Contain(
            "CREATE INDEX \"IX_AssignmentRequests_FestivalDayId_RequestedAt\" ON \"AssignmentRequests\" (\"FestivalDayId\", \"RequestedAt\")");
        script.Should().Contain(
            "CONSTRAINT \"CK_FestivalDays_AssignmentWindow_StartBeforeEnd\" CHECK (\"AssignmentWindowStart\" < \"AssignmentWindowEnd\")");
    }

    [Fact]
    public void MigrationSqlScript_ShouldContainExpectedForeignKeyDeleteBehaviors()
    {
        using var context = CreateContext();

        var script = GenerateScript(context);

        script.Should().Contain(
            "REFERENCES \"FestivalDays\" (\"FestivalDayId\") ON DELETE RESTRICT");
        script.Should().Contain(
            "REFERENCES \"Zones\" (\"ZoneId\") ON DELETE RESTRICT");
        script.Should().Contain(
            "REFERENCES \"AssignmentRequests\" (\"AssignmentRequestId\") ON DELETE CASCADE");
        script.Should().Contain(
            "REFERENCES \"AssignmentRequests\" (\"AssignmentRequestId\") ON DELETE RESTRICT");
        script.Should().Contain(
            "REFERENCES \"Attendees\" (\"AttendeeId\") ON DELETE RESTRICT");
        script.Should().Contain(
            "REFERENCES \"Spots\" (\"SpotCode\") ON DELETE RESTRICT");

        Regex.Matches(script, "ON DELETE CASCADE")
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void MigrationSqlScript_ShouldNotContainUnexpectedObjects()
    {
        using var context = CreateContext();

        var script = GenerateScript(context);

        script.Should().NotContain("CREATE TABLE \"AssignmentWindow\"");
        script.Should().NotContain("CREATE TABLE \"AssignmentRequest\"");
        script.Should().NotContain(typeof(AssignmentRequest).FullName);
        script.Should().NotContain("IX_Assignments_AttendeeId");
        script.Should().NotContain("IX_Assignments_SpotCode");
    }

    private static FestivalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql("Host=localhost;Database=festival-migration-tests")
            .Options;

        return new FestivalDbContext(options);
    }

    private static string GenerateScript(
        FestivalDbContext context)
    {
        return context.GetService<IMigrator>().GenerateScript();
    }
}
