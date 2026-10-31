namespace BlazorBasics.Opfs.TestApp;

/// <summary>
/// Whether the estimate goes down after removing a file, which <c>OpfsStorageGuard</c> of ShotUp needs. Several rounds show
/// how fast a stuck estimate would reach the quota, and the wait gives a slow estimate time to catch up.
/// </summary>
internal sealed class EstimateMeasurement
{
    private const string FOLDER = "opfs-device-checks";
    private const string FILE = $"{FOLDER}/estimate.bin";
    private const int MEBIBYTE = 1024 * 1024;
    private const int FILE_MEBIBYTES = 64;
    private const int ROUNDS = 3;
    private const int SETTLE_STEP_SECONDS = 5;
    private const int SETTLE_STEPS = 6;

    private readonly OpfsFileSystem FileSystem = new();
    private readonly OpfsStorageInfo Storage = new();

    public async Task<EstimateAnswer> RunAsync()
    {
        long fileLength = FILE_MEBIBYTES * (long)MEBIBYTE;
        await FileSystem.DeleteAsync(FOLDER);
        OpfsStorageEstimate before = (await Storage.EstimateAsync())!;
        List<EstimateRound> rounds = [];
        for (int round = 0; round < ROUNDS; round++)
        {
            await WriteFileAsync();
            long afterWrite = await UsageAsync();
            await FileSystem.DeleteAsync(FOLDER);
            rounds.Add(new EstimateRound(afterWrite, await UsageAsync()));
        }

        long peak = rounds.Max(round => round.AfterWrite);
        long settled = rounds[^1].AfterDelete;
        int waited = 0;
        int step = 0;
        while (step < SETTLE_STEPS && !WentDown(peak, settled, fileLength))
        {
            await Task.Delay(TimeSpan.FromSeconds(SETTLE_STEP_SECONDS));
            waited += SETTLE_STEP_SECONDS;
            settled = await UsageAsync();
            step++;
        }
        return new EstimateAnswer(fileLength, before.Quota, before.Usage, rounds, settled, waited, VerdictOf(before.Usage, peak, settled, fileLength));
    }

    private static bool WentDown(long peak, long settled, long fileLength) => peak - settled >= fileLength / 2;

    private static string VerdictOf(long before, long peak, long settled, long fileLength)
    {
        string verdict = "never-rose";
        if (peak - before >= fileLength / 2)
        {
            verdict = WentDown(peak, settled, fileLength) ? "went-down" : "stayed-up";
        }
        return verdict;
    }

    private async Task<long> UsageAsync() => (await Storage.EstimateAsync())!.Usage;

    private async Task WriteFileAsync()
    {
        byte[] chunk = new byte[MEBIBYTE];
        await using IOpfsWriter writer = await FileSystem.OpenWriterAsync(FILE);
        for (int written = 0; written < FILE_MEBIBYTES; written++)
        {
            await writer.WriteAsync(chunk);
        }
        await writer.CompleteAsync();
    }
}
