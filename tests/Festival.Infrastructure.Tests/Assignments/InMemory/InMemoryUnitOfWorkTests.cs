using Festival.Infrastructure.Assignments.InMemory;
using FluentAssertions;

namespace Festival.Infrastructure.Tests.Assignments.InMemory;

public sealed class InMemoryUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnZero()
    {
        var unitOfWork = new InMemoryUnitOfWork();

        var result = await unitOfWork.SaveChangesAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldHonorAlreadyCancelledToken()
    {
        var unitOfWork = new InMemoryUnitOfWork();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var save = () => unitOfWork.SaveChangesAsync(cancellation.Token);

        await save.Should().ThrowAsync<OperationCanceledException>();
    }
}
