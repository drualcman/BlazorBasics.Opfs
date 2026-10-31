using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using BlazorBasics.Opfs.TestApp;

OpfsFileSystem fileSystem = new();
await fileSystem.DeleteAsync(BrowserContractTarget.TESTS_FOLDER);

string? quotaTest = TestHooks.QueryValue(QuotaExhaustionTests.TEST_QUERY);
List<BrowserTestResult> results = quotaTest is null
    ? await BrowserTestRunner.RunAsync(
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
    ])
    : await BrowserTestRunner.RunAsync([typeof(QuotaExhaustionTests)], quotaTest);

await fileSystem.DeleteAsync(BrowserContractTarget.TESTS_FOLDER);
JSHost.GlobalThis.SetProperty("opfsContractResults", JsonSerializer.Serialize(results));
