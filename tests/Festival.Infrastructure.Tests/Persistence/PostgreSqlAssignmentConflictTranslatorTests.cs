using Festival.Application.Assignments.Persistence;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Festival.Infrastructure.Tests.Persistence;

public sealed class PostgreSqlAssignmentConflictTranslatorTests
{
    [Theory]
    [InlineData(
        PostgreSqlAssignmentConflictTranslator.SpotAlreadyAssignedConstraint,
        AssignmentPersistenceConflict.SpotAlreadyAssigned)]
    [InlineData(
        PostgreSqlAssignmentConflictTranslator.AttendeeAlreadyAssignedConstraint,
        AssignmentPersistenceConflict.AttendeeAlreadyAssigned)]
    [InlineData(
        PostgreSqlAssignmentConflictTranslator.DuplicateRequestAssignmentConstraint,
        AssignmentPersistenceConflict.DuplicateRequestAssignment)]
    public void TryTranslate_ShouldMapKnownUniqueAssignmentConstraint(
        string constraintName,
        AssignmentPersistenceConflict expectedConflict)
    {
        var exception = CreatePostgresException("23505", constraintName);

        var translated = PostgreSqlAssignmentConflictTranslator.TryTranslate(
            exception,
            out var conflict);

        translated.Should().BeTrue();
        conflict.Should().Be(expectedConflict);
    }

    [Fact]
    public void TryTranslate_ShouldTraverseEfCoreInnerExceptionShape()
    {
        var postgresException = CreatePostgresException(
            "23505",
            PostgreSqlAssignmentConflictTranslator.SpotAlreadyAssignedConstraint);
        var exception = new DbUpdateException(
            "Save failed.",
            new InvalidOperationException("Provider wrapper.", postgresException));

        var translated = PostgreSqlAssignmentConflictTranslator.TryTranslate(
            exception,
            out var conflict);

        translated.Should().BeTrue();
        conflict.Should().Be(
            AssignmentPersistenceConflict.SpotAlreadyAssigned);
    }

    [Fact]
    public void TryTranslate_ShouldDeclineNonUniquePostgreSqlError()
    {
        var exception = CreatePostgresException(
            "23503",
            PostgreSqlAssignmentConflictTranslator.SpotAlreadyAssignedConstraint);

        PostgreSqlAssignmentConflictTranslator.TryTranslate(
                exception,
                out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void TryTranslate_ShouldDeclineUnknownUniqueConstraint()
    {
        var exception = CreatePostgresException(
            "23505",
            "IX_Attendees_AttendeeCode");

        PostgreSqlAssignmentConflictTranslator.TryTranslate(
                exception,
                out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void TryTranslate_ShouldDeclineExceptionWithoutPostgresException()
    {
        var exception = new DbUpdateException(
            "Save failed.",
            new InvalidOperationException("Not a PostgreSQL exception."));

        PostgreSqlAssignmentConflictTranslator.TryTranslate(
                exception,
                out _)
            .Should()
            .BeFalse();
    }

    private static PostgresException CreatePostgresException(
        string sqlState,
        string constraintName)
    {
        return new PostgresException(
            "Provider message.",
            "ERROR",
            "ERROR",
            sqlState,
            constraintName: constraintName);
    }
}
