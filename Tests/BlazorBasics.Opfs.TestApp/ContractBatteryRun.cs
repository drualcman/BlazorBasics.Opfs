using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>The contract battery of this page or worker, whose results the browser tests read from <c>opfsContractResults</c>.</summary>
internal static class ContractBatteryRun
{
    public static async Task RunAsync()
    {
        OpfsFileSystem fileSystem = new();
        await fileSystem.DeleteAsync(BrowserContractTarget.TESTS_FOLDER);

        string? quotaTest = TestHooks.QueryValue(QuotaExhaustionTests.TEST_QUERY);
        List<Type> battery =
        [
            typeof(BrowserReadContractTests),
            typeof(BrowserWriteContractTests),
            typeof(BrowserWriterContractTests),
            typeof(BrowserRemoveContractTests),
            typeof(BrowserCopyMoveContractTests),
            typeof(BrowserFolderMoveContractTests),
            typeof(BrowserNameContractTests),
            typeof(BrowserLockContractTests),
            typeof(BrowserFetchContractTests),
            typeof(BrowserOnlyTests),
            typeof(LargeFileTests)
        ];
        if (TestHooks.InWorker())
        {
            battery.Add(typeof(WorkerOnlyTests));
        }
        List<BrowserTestResult> results = quotaTest is null
            ? await BrowserTestRunner.RunAsync(battery)
            : await BrowserTestRunner.RunAsync([typeof(QuotaExhaustionTests)], quotaTest);

        await fileSystem.DeleteAsync(BrowserContractTarget.TESTS_FOLDER);
        JSHost.GlobalThis.SetProperty("opfsContractResults", JsonSerializer.Serialize(results));
    }
}
