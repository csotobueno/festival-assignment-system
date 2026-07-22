using System.Data;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Festival.Infrastructure.Tests.Persistence.Model;

public sealed class FestivalDbContextModelTests
{
    [Fact]
    public void Model_ShouldContainExpectedEntityTypesAndTables()
    {
        using var context = CreateContext();

        var expectedMappings = new Dictionary<Type, string>
        {
            [typeof(Attendee)] = "Attendees",
            [typeof(FestivalDay)] = "FestivalDays",
            [typeof(Zone)] = "Zones",
            [typeof(Spot)] = "Spots",
            [typeof(AssignmentRequestRow)] = "AssignmentRequests",
            [typeof(AssignmentRequestAttendeeRow)] =
                "AssignmentRequestAttendees",
            [typeof(Assignment)] = "Assignments"
        };

        foreach (var (clrType, tableName) in expectedMappings)
        {
            var entityType = context.Model.FindEntityType(clrType);

            entityType.Should().NotBeNull();
            entityType!.GetTableName().Should().Be(tableName);
        }

        context.Database.GetDbConnection().State.Should().Be(
            ConnectionState.Closed);
    }

    [Fact]
    public void Model_ShouldConfigureExpectedPrimaryKeys()
    {
        using var context = CreateContext();

        AssertPrimaryKey(context.Model, typeof(Attendee), "Id");
        AssertPrimaryKey(context.Model, typeof(FestivalDay), "Id");
        AssertPrimaryKey(context.Model, typeof(Zone), "Id");
        AssertPrimaryKey(context.Model, typeof(Spot), "Code");
        AssertPrimaryKey(
            context.Model,
            typeof(AssignmentRequestRow),
            "AssignmentRequestId");
        AssertPrimaryKey(
            context.Model,
            typeof(AssignmentRequestAttendeeRow),
            "AssignmentRequestId",
            "Position");
        AssertPrimaryKey(context.Model, typeof(Assignment), "Id");
    }

    [Fact]
    public void Model_ShouldConfigureRequiredRelationshipsAndDeleteBehaviors()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context.Model,
            typeof(Spot),
            typeof(Zone),
            DeleteBehavior.Restrict,
            "ZoneId");
        AssertForeignKey(
            context.Model,
            typeof(AssignmentRequestRow),
            typeof(FestivalDay),
            DeleteBehavior.Restrict,
            "FestivalDayId");
        AssertForeignKey(
            context.Model,
            typeof(AssignmentRequestAttendeeRow),
            typeof(AssignmentRequestRow),
            DeleteBehavior.Cascade,
            "AssignmentRequestId");
        AssertForeignKey(
            context.Model,
            typeof(Assignment),
            typeof(AssignmentRequestRow),
            DeleteBehavior.Restrict,
            "AssignmentRequestId");
        AssertForeignKey(
            context.Model,
            typeof(Assignment),
            typeof(FestivalDay),
            DeleteBehavior.Restrict,
            "FestivalDayId");
        AssertForeignKey(
            context.Model,
            typeof(Assignment),
            typeof(Attendee),
            DeleteBehavior.Restrict,
            "AttendeeId");
        AssertForeignKey(
            context.Model,
            typeof(Assignment),
            typeof(Spot),
            DeleteBehavior.Restrict,
            "SpotCode");
    }

    [Fact]
    public void Model_ShouldConfigureValueObjectConversions()
    {
        using var context = CreateContext();

        var convertedProperties = new (Type EntityType, string PropertyName)[]
        {
            (typeof(Attendee), "Id"),
            (typeof(Attendee), "Code"),
            (typeof(FestivalDay), "Id"),
            (typeof(Zone), "Id"),
            (typeof(Spot), "Code"),
            (typeof(Spot), "ZoneId"),
            (typeof(Spot), "RowCode"),
            (typeof(Spot), "Number"),
            (typeof(AssignmentRequestRow), "AssignmentRequestId"),
            (typeof(AssignmentRequestRow), "FestivalDayId"),
            (typeof(AssignmentRequestAttendeeRow), "AssignmentRequestId"),
            (typeof(AssignmentRequestAttendeeRow), "AttendeeCode"),
            (typeof(Assignment), "Id"),
            (typeof(Assignment), "AssignmentRequestId"),
            (typeof(Assignment), "FestivalDayId"),
            (typeof(Assignment), "AttendeeId"),
            (typeof(Assignment), "SpotCode"),
            (typeof(Assignment), "ZoneId"),
            (typeof(Assignment), "RowCode"),
            (typeof(Assignment), "SpotNumber")
        };

        foreach (var (entityType, propertyName) in convertedProperties)
        {
            var property = GetProperty(
                context.Model,
                entityType,
                propertyName);

            property.GetValueConverter().Should().NotBeNull(
                $"{entityType.Name}.{propertyName} is a persisted Value Object");
        }
    }

    [Fact]
    public void FestivalDay_ShouldMapAssignmentWindowToExpectedColumns()
    {
        using var context = CreateContext();
        var festivalDay = GetEntityType(context.Model, typeof(FestivalDay));
        var assignmentWindow = festivalDay
            .FindNavigation("AssignmentWindow")!
            .TargetEntityType;

        GetColumnName(assignmentWindow, "Start")
            .Should().Be("AssignmentWindowStart");
        GetColumnName(assignmentWindow, "End")
            .Should().Be("AssignmentWindowEnd");
        festivalDay.FindNavigation("AssignmentWindow")!
            .ForeignKey.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void AssignmentRequestRow_ShouldMapOutcomeDataAndBoundedStatus()
    {
        using var context = CreateContext();
        var request = GetEntityType(
            context.Model,
            typeof(AssignmentRequestRow));

        GetProperty(request, "RequestedAt").IsNullable.Should().BeFalse();
        AssertNullableColumn(request, "ResolvedAt", "ResolvedAt");
        AssertNullableColumn(request, "RejectionCode", "RejectionCode");
        AssertNullableColumn(request, "RejectionMessage", "RejectionMessage");
        AssertNullableColumn(request, "FailureCode", "FailureCode");
        AssertNullableColumn(request, "FailureMessage", "FailureMessage");

        var status = request.FindProperty("Status")!;

        status.GetTypeMapping().Converter!.ProviderClrType.Should()
            .Be<string>();
        status.GetMaxLength().Should().Be(32);
    }

    [Fact]
    public void AssignmentRequestAttendeeRow_ShouldMapRequiredAttendeeData()
    {
        using var context = CreateContext();
        var attendee = GetEntityType(
            context.Model,
            typeof(AssignmentRequestAttendeeRow));

        GetProperty(attendee, "Position").IsNullable.Should().BeFalse();
        GetProperty(attendee, "AttendeeCode").IsNullable.Should().BeFalse();
    }

    [Fact]
    public void AssignmentRequestRows_ShouldHaveCollectionRelationship()
    {
        using var context = CreateContext();
        var request = GetEntityType(
            context.Model,
            typeof(AssignmentRequestRow));

        var navigation = request.FindNavigation("Attendees");

        navigation.Should().NotBeNull();
        navigation!.IsCollection.Should().BeTrue();
        navigation.TargetEntityType.ClrType.Should()
            .Be(typeof(AssignmentRequestAttendeeRow));
        navigation.ForeignKey.DeleteBehavior.Should()
            .Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_ShouldNotMapAssignmentRequestDomainAggregate()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(AssignmentRequest))
            .Should()
            .BeNull();
    }

    [Fact]
    public void Model_ShouldConfigureApprovedUniqueIndexes()
    {
        using var context = CreateContext();

        AssertIndex(context.Model, typeof(Attendee), true, "Code");
        AssertIndex(context.Model, typeof(FestivalDay), true, "Date");
        AssertIndex(
            context.Model,
            typeof(Spot),
            true,
            "ZoneId",
            "RowCode",
            "Number");
        AssertIndex(
            context.Model,
            typeof(AssignmentRequestAttendeeRow),
            true,
            "AssignmentRequestId",
            "AttendeeCode");
        AssertIndex(
            context.Model,
            typeof(Assignment),
            true,
            "FestivalDayId",
            "SpotCode");
        AssertIndex(
            context.Model,
            typeof(Assignment),
            true,
            "FestivalDayId",
            "AttendeeId");
        AssertIndex(
            context.Model,
            typeof(Assignment),
            true,
            "AssignmentRequestId",
            "AttendeeId");
        AssertIndex(
            context.Model,
            typeof(AssignmentRequestRow),
            false,
            "FestivalDayId",
            "RequestedAt");
    }

    [Fact]
    public void Assignment_ShouldPreserveHistoricalSpotSnapshot()
    {
        using var context = CreateContext();
        var assignment = GetEntityType(context.Model, typeof(Assignment));

        var expectedProperties = new[]
        {
            "SpotCode",
            "ZoneId",
            "RowCode",
            "SpotNumber",
            "AssignedAt"
        };

        expectedProperties.Should().OnlyContain(
            propertyName => assignment.FindProperty(propertyName) != null);
    }

    private static FestivalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql("Host=localhost;Database=festival-metadata-tests")
            .Options;

        return new FestivalDbContext(options);
    }

    private static void AssertPrimaryKey(
        IModel model,
        Type entityClrType,
        params string[] propertyNames)
    {
        var keyProperties = GetEntityType(model, entityClrType)
            .FindPrimaryKey()!
            .Properties
            .Select(property => property.Name);

        keyProperties.Should().Equal(propertyNames);
    }

    private static void AssertForeignKey(
        IModel model,
        Type dependentClrType,
        Type principalClrType,
        DeleteBehavior deleteBehavior,
        params string[] propertyNames)
    {
        var foreignKey = GetEntityType(model, dependentClrType)
            .GetForeignKeys()
            .Single(candidate =>
                candidate.PrincipalEntityType.ClrType == principalClrType
                && candidate.Properties.Select(property => property.Name)
                    .SequenceEqual(propertyNames));

        foreignKey.DeleteBehavior.Should().Be(deleteBehavior);
        foreignKey.IsRequired.Should().BeTrue();
    }

    private static void AssertIndex(
        IModel model,
        Type entityClrType,
        bool isUnique,
        params string[] propertyNames)
    {
        var index = GetEntityType(model, entityClrType)
            .GetIndexes()
            .Single(candidate => candidate.Properties
                .Select(property => property.Name)
                .SequenceEqual(propertyNames));

        index.IsUnique.Should().Be(isUnique);
    }

    private static void AssertNullableColumn(
        IEntityType entityType,
        string propertyName,
        string columnName)
    {
        var property = entityType.FindProperty(propertyName)!;

        property.IsNullable.Should().BeTrue();
        GetColumnName(entityType, propertyName).Should().Be(columnName);
    }

    private static string? GetColumnName(
        IEntityType entityType,
        string propertyName)
    {
        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        return entityType.FindProperty(propertyName)!
            .GetColumnName(table);
    }

    private static IProperty GetProperty(
        IModel model,
        Type entityClrType,
        string propertyName)
    {
        return GetEntityType(model, entityClrType)
            .FindProperty(propertyName)!;
    }

    private static IProperty GetProperty(
        IEntityType entityType,
        string propertyName)
    {
        return entityType.FindProperty(propertyName)!;
    }

    private static IEntityType GetEntityType(
        IModel model,
        Type entityClrType)
    {
        return model.FindEntityType(entityClrType)!;
    }
}
