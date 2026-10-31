namespace BlazorBasics.Opfs.Services;

/// <summary>Finds out whether the file system really works here, and answers instead of throwing.</summary>
internal static class OpfsProbe
{
    private const string NOT_IN_BROWSER = "This is not running in a browser.";

    public static async Task<OpfsProbeResult> RunAsync(CancellationToken cancellationToken)
    {
        OpfsProbeResult result;
        if (OperatingSystem.IsBrowser())
        {
            result = await InBrowserAsync(cancellationToken);
        }
        else
        {
            result = new OpfsProbeResult(false, NOT_IN_BROWSER, new OpfsNotSupportedException());
        }
        return result;
    }

    [SupportedOSPlatform("browser")]
    private static async Task<OpfsProbeResult> InBrowserAsync(CancellationToken cancellationToken)
    {
        OpfsProbeResult result;
        try
        {
            string reason = await OpfsCall.RunAsync(() => OpfsCoordinationInterop.ProbeAsync().WaitAsync(cancellationToken), cancellationToken);
            result = new OpfsProbeResult(reason.Length == 0, reason);
        }
        catch (OpfsException failure)
        {
            result = new OpfsProbeResult(false, failure.Message, failure);
        }
        catch (OperationCanceledException failure)
        {
            result = new OpfsProbeResult(false, "The probe did not answer in time.", failure);
        }
        catch (Exception failure)
        {
            result = new OpfsProbeResult(false, $"The module could not load: {failure.Message}", failure);
        }
        return result;
    }
}
