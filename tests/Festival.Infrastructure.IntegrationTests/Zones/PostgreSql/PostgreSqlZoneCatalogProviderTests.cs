using Festival.Domain.Zones;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Zones.PostgreSql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Zones.PostgreSql;

public sealed class PostgreSqlZoneCatalogProviderTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task GetZonesAsync_ShouldPreserveEveryZoneIncludingFrontStandingAndAllZoneTypes()
    {
        await using var context = Fixture.CreateDbContext();
        var zones = Enum.GetValues<ZoneType>()
            .Reverse()
            .Select((type, index) => Zone.Create(
                ZoneId.Create(Guid.Parse(
                    $"20000000-0000-0000-0000-{index + 1:000000000000}")),
                $"Zone {index + 1}",
                type))
            .ToArray();
        context.Zones.AddRange(zones.Reverse());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var provider = new PostgreSqlZoneCatalogProvider(context);

        var result = await provider.GetZonesAsync();

        result.Should().BeEquivalentTo(zones, options => options.WithStrictOrdering());
        result.Should().Contain(zone => zone.Type == ZoneType.FrontStanding);
        context.ChangeTracker.Entries().Should().BeEmpty();
        result.Should().BeAssignableTo<ICollection<Zone>>()
            .Which.IsReadOnly.Should().BeTrue();

        await using var verificationContext = Fixture.CreateDbContext();
        (await verificationContext.Zones.CountAsync()).Should().Be(zones.Length);
    }

    [Fact]
    public async Task GetZonesAsync_ShouldReturnEmptyResultWhenCatalogIsEmpty()
    {
        await using var context = Fixture.CreateDbContext();
        var provider = new PostgreSqlZoneCatalogProvider(context);

        var result = await provider.GetZonesAsync();

        result.Should().BeEmpty();
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetZonesAsync_ShouldHonorAlreadyCancelledToken()
    {
        await using var context = Fixture.CreateDbContext();
        var provider = new PostgreSqlZoneCatalogProvider(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var read = () => provider.GetZonesAsync(cancellation.Token);

        await read.Should().ThrowAsync<OperationCanceledException>();
        context.ChangeTracker.Entries().Should().BeEmpty();
    }
}
