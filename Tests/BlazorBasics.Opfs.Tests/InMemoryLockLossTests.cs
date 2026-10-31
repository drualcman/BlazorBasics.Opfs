using BlazorBasics.Opfs.Abstractions;
using BlazorBasics.Opfs.Models;
using BlazorBasics.Opfs.Testing;

namespace BlazorBasics.Opfs.Tests;

public sealed class InMemoryLockLossTests
{
    [Fact]
    public async Task A_lost_lock_tells_its_holder_and_is_free_for_others()
    {
        InMemoryOpfsLocks locks = new();
        IOpfsLock? held = await locks.TryAcquireAsync("a", OpfsLockMode.Exclusive);

        locks.Lose("a");

        Assert.False(held!.IsHeld);
        Assert.True(held.Lost.IsCancellationRequested);
        using IOpfsLock? again = await locks.TryAcquireAsync("a", OpfsLockMode.Exclusive);
        Assert.NotNull(again);
    }

    [Fact]
    public async Task Every_shared_holder_loses_it()
    {
        InMemoryOpfsLocks locks = new();
        IOpfsLock? first = await locks.TryAcquireAsync("a", OpfsLockMode.Shared);
        IOpfsLock? second = await locks.TryAcquireAsync("a", OpfsLockMode.Shared);

        locks.Lose("a");

        Assert.True(first!.Lost.IsCancellationRequested);
        Assert.True(second!.Lost.IsCancellationRequested);
    }

    [Fact]
    public async Task Disposing_a_lost_lock_does_not_touch_the_next_holder()
    {
        InMemoryOpfsLocks locks = new();
        IOpfsLock? lost = await locks.TryAcquireAsync("a", OpfsLockMode.Exclusive);
        locks.Lose("a");
        using IOpfsLock? next = await locks.TryAcquireAsync("a", OpfsLockMode.Exclusive);

        lost!.Dispose();

        Assert.Null(await locks.TryAcquireAsync("a", OpfsLockMode.Exclusive));
    }

    [Fact]
    public async Task Losing_a_lock_nobody_holds_does_nothing()
    {
        InMemoryOpfsLocks locks = new();

        locks.Lose("a");

        using IOpfsLock? held = await locks.TryAcquireAsync("a", OpfsLockMode.Exclusive);
        Assert.NotNull(held);
    }
}
