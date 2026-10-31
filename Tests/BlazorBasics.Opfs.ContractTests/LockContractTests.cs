namespace BlazorBasics.Opfs.ContractTests;

public abstract class LockContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    private string LockName => $"contract:{Target.Folder}";

    [Fact]
    public async Task An_exclusive_lock_refuses_every_other_holder()
    {
        using IOpfsLock? held = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);

        Assert.NotNull(held);
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive));
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared));
    }

    [Fact]
    public async Task Shared_locks_go_together_and_refuse_an_exclusive_one()
    {
        using IOpfsLock? first = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared);
        using IOpfsLock? second = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive));
    }

    [Fact]
    public async Task A_released_lock_can_be_taken_again_at_once()
    {
        IOpfsLock? held = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);
        held!.Dispose();
        held.Dispose();

        using IOpfsLock? again = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);

        Assert.NotNull(again);
    }

    [Fact]
    public async Task A_lock_says_it_is_held_until_released_and_releasing_is_not_losing()
    {
        IOpfsLock? held = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);

        Assert.True(held!.IsHeld);
        Assert.False(held.Lost.IsCancellationRequested);

        held.Dispose();
        await Task.Delay(50);

        Assert.False(held.IsHeld);
        Assert.False(held.Lost.IsCancellationRequested);
    }

    [Fact]
    public async Task One_of_two_shared_locks_released_leaves_the_other_held()
    {
        IOpfsLock? first = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared);
        using IOpfsLock? second = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared);

        first!.Dispose();

        Assert.True(second!.IsHeld);
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive));
    }
}
