using System.Data;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Configurations;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Festival.Infrastructure.Tests.Persistence.Model;

public sealed class FestivalDbContextModelTests
{
    private static readonly string[] ApprovedTables =
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
    public void Model_ShouldContainOnlyApprovedTables()
    {
        using var context = CreateContext();

        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(tableName => tableName is not null)
            .Distinct()
            .Order()
            .ToArray();

        tableNames.Should().Equal(ApprovedTables);
    }

    [Fact]
    public void Model_ShouldContainOnlyExpectedEntityTypes()
    {
        using var context = CreateContext();

        var entityClrTypes = context.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .ToArray();

        entityClrTypes.Should().BeEquivalentTo(
            [
                typeof(Assignment),
                typeof(AssignmentRequestAttendeeRow),
                typeof(AssignmentRequestRow),
                typeof(Attendee),
                typeof(AssignmentWindow),
                typeof(FestivalDay),
                typeof(Spot),
                typeof(Zone)
            ]);
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
        assignmentWindow.GetTableName().Should().Be("FestivalDays");
    }

    [Fact]
    public void FestivalDay_ShouldHaveAssignmentWindowCheckConstraint()
    {
        using var context = CreateContext();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var festivalDay = GetEntityType(designTimeModel, typeof(FestivalDay));

        var checkConstraint = festivalDay.GetCheckConstraints()
            .Single(constraint =>
                constraint.Name ==
                "CK_FestivalDays_AssignmentWindow_StartBeforeEnd");

        checkConstraint.Sql.Should()
            .Be("\"AssignmentWindowStart\" < \"AssignmentWindowEnd\"");
    }

    [Fact]
    public void Model_ShouldConfigureExpectedPostgreSqlColumnTypes()
    {
        using var context = CreateContext();

        AssertColumnType(context.Model, typeof(Attendee), "Id", "uuid");
        AssertColumnType(context.Model, typeof(FestivalDay), "Date", "date");
        AssertColumnType(
            context.Model,
            typeof(FestivalDay),
            "AssignmentWindow.Start",
            "time without time zone");
        AssertColumnType(
            context.Model,
            typeof(AssignmentRequestRow),
            "RequestedAt",
            "timestamp with time zone");
        AssertColumnType(
            context.Model,
            typeof(AssignmentRequestRow),
            "ResolvedAt",
            "timestamp with time zone");
        AssertColumnType(context.Model, typeof(Spot), "Number", "integer");
        AssertColumnType(
            context.Model,
            typeof(AssignmentRequestRow),
            "Status",
            "character varying(32)");
    }

    [Fact]
    public void Model_ShouldConfigureFinalStringLengths()
    {
        using var context = CreateContext();

        AssertMaxLength(
            context.Model,
            typeof(Attendee),
            "Code",
            PersistenceLengths.AttendeeCode);
        AssertMaxLength(
            context.Model,
            typeof(Attendee),
            "Name",
            PersistenceLengths.AttendeeName);
        AssertMaxLength(
            context.Model,
            typeof(Zone),
            "Name",
            PersistenceLengths.ZoneName);
        AssertMaxLength(
            context.Model,
            typeof(Spot),
            "Code",
            PersistenceLengths.SpotCode);
        AssertMaxLength(
            context.Model,
            typeof(Spot),
            "RowCode",
            PersistenceLengths.RowCode);
        AssertMaxLength(
            context.Model,
            typeof(AssignmentRequestRow),
            "Status",
            PersistenceLengths.AssignmentRequestStatus);
        AssertMaxLength(
            context.Model,
            typeof(AssignmentRequestRow),
            "RejectionCode",
            PersistenceLengths.OutcomeCode);
        AssertMaxLength(
            context.Model,
            typeof(AssignmentRequestRow),
            "RejectionMessage",
            PersistenceLengths.OutcomeMessage);
        AssertMaxLength(
            context.Model,
            typeof(AssignmentRequestRow),
            "FailureCode",
            PersistenceLengths.OutcomeCode);
        AssertMaxLength(
            context.Model,
            typeof(AssignmentRequestRow),
            "FailureMessage",
            PersistenceLengths.OutcomeMessage);
        AssertMaxLength(
            context.Model,
            typeof(AssignmentRequestAttendeeRow),
            "AttendeeCode",
            PersistenceLengths.AttendeeCode);
        AssertMaxLength(
            context.Model,
            typeof(Assignment),
            "SpotCode",
            PersistenceLengths.SpotCode);
        AssertMaxLength(
            context.Model,
            typeof(Assignment),
            "RowCode",
            PersistenceLengths.RowCode);
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
    public void Model_ShouldHaveOnlyExpectedShadowProperties()
    {
        using var context = CreateContext();

        var shadowProperties = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties()
                .Where(property => property.IsShadowProperty())
                .Select(property => new
                {
                    EntityType = entityType.ClrType,
                    Property = property.Name
                }))
            .ToArray();

        shadowProperties.Should().BeEquivalentTo(
            [
                new
                {
                    EntityType = typeof(AssignmentWindow),
                    Property = "FestivalDayId"
                }
            ]);
    }

    [Fact]
    public void Model_ShouldHaveOnlyExpectedRelationships()
    {
        using var context = CreateContext();

        var relationships = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys()
                .Select(foreignKey => new
                {
                    Dependent = entityType.ClrType,
                    Principal = foreignKey.PrincipalEntityType.ClrType,
                    Properties = string.Join(
                        ",",
                        foreignKey.Properties.Select(property => property.Name)),
                    DeleteBehavior = foreignKey.DeleteBehavior
                }))
            .ToArray();

        relationships.Should().BeEquivalentTo(
            [
                new
                {
                    Dependent = typeof(Assignment),
                    Principal = typeof(AssignmentRequestRow),
                    Properties = "AssignmentRequestId",
                    DeleteBehavior = DeleteBehavior.Restrict
                },
                new
                {
                    Dependent = typeof(Assignment),
                    Principal = typeof(Attendee),
                    Properties = "AttendeeId",
                    DeleteBehavior = DeleteBehavior.Restrict
                },
                new
                {
                    Dependent = typeof(Assignment),
                    Principal = typeof(FestivalDay),
                    Properties = "FestivalDayId",
                    DeleteBehavior = DeleteBehavior.Restrict
                },
                new
                {
                    Dependent = typeof(Assignment),
                    Principal = typeof(Spot),
                    Properties = "SpotCode",
                    DeleteBehavior = DeleteBehavior.Restrict
                },
                new
                {
                    Dependent = typeof(AssignmentWindow),
                    Principal = typeof(FestivalDay),
                    Properties = "FestivalDayId",
                    DeleteBehavior = DeleteBehavior.Cascade
                },
                new
                {
                    Dependent = typeof(AssignmentRequestAttendeeRow),
                    Principal = typeof(AssignmentRequestRow),
                    Properties = "AssignmentRequestId",
                    DeleteBehavior = DeleteBehavior.Cascade
                },
                new
                {
                    Dependent = typeof(AssignmentRequestRow),
                    Principal = typeof(FestivalDay),
                    Properties = "FestivalDayId",
                    DeleteBehavior = DeleteBehavior.Restrict
                },
                new
                {
                    Dependent = typeof(Spot),
                    Principal = typeof(Zone),
                    Properties = "ZoneId",
                    DeleteBehavior = DeleteBehavior.Restrict
                }
            ]);
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

        var indexes = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetIndexes()
                .Select(index => new
                {
                    EntityType = entityType.ClrType,
                    Properties = string.Join(
                        ",",
                        index.Properties.Select(property => property.Name)),
                    index.IsUnique
                }))
            .ToArray();

        indexes.Should().HaveCount(8);
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
        if (propertyName.StartsWith(
            "AssignmentWindow.",
            StringComparison.Ordinal))
        {
            var ownedType = GetEntityType(model, typeof(FestivalDay))
                .FindNavigation("AssignmentWindow")!
                .TargetEntityType;

            return ownedType.FindProperty(
                propertyName["AssignmentWindow.".Length..])!;
        }

        return GetEntityType(model, entityClrType)
            .FindProperty(propertyName)!;
    }

    private static void AssertColumnType(
        IModel model,
        Type entityClrType,
        string propertyName,
        string expectedColumnType)
    {
        GetProperty(model, entityClrType, propertyName)
            .GetColumnType()
            .Should()
            .Be(expectedColumnType);
    }

    private static void AssertMaxLength(
        IModel model,
        Type entityClrType,
        string propertyName,
        int expectedMaxLength)
    {
        GetProperty(model, entityClrType, propertyName)
            .GetMaxLength()
            .Should()
            .Be(expectedMaxLength);
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
