using BlazorBasics.Opfs.TestApp;

if (TestHooks.QueryValue(BackForwardLockProbe.PAGE_QUERY) is null)
{
    await ContractBatteryRun.RunAsync();
}
