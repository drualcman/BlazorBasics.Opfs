namespace BlazorBasics.Opfs.ContractTests;

public abstract class LockContractTests<TTarget> : OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    private string LockName => $"contract:{Target.Folder}";

    [Fact]
    public async Task An_exclusive_lock_refuses_every_other_holder()
    {
        using IDisposable? held = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);

        Assert.NotNull(held);
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive));
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared));
    }

    [Fact]
    public async Task Shared_locks_go_together_and_refuse_an_exclusive_one()
    {
        using IDisposable? first = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared);
        using IDisposable? second = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Shared);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Null(await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive));
    }

    [Fact]
    public async Task A_released_lock_can_be_taken_again_at_once()
    {
        IDisposable? held = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);
        held!.Dispose();
        held.Dispose();

        using IDisposable? again = await Target.Locks.TryAcquireAsync(LockName, OpfsLockMode.Exclusive);

        Assert.NotNull(again);
    }
}
