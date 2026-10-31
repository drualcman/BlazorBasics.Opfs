namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>The whole battery again with .NET inside a worker, one case per test and per engine.</summary>
[Collection(BrowserCollection.NAME)]
public sealed class ContractInWorkerTests(BrowserFixture fixture)
{
    private static readonly string[] WorkerOwnTests = ["BrowserOnlyTests", "LargeFileTests", "WorkerOnlyTests"];

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    [Theory]
    [MemberData(nameof(ContractInBrowserTests.ContractTests), MemberType = typeof(ContractInBrowserTests))]
    public async Task The_contract_test_passes_in_a_worker(string engine, string testName)
    {
        IReadOnlyDictionary<string, BrowserTestOutcome> outcomes = await fixture.WorkerOutcomesOfAsync(engine);

        Assert.True(outcomes.TryGetValue(testName, out BrowserTestOutcome? outcome), $"{testName} did not run in a worker of {engine}.");
        Assert.True(outcome.Passed, outcome.Failure);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task The_tests_of_the_browser_itself_pass_in_a_worker(string engine)
    {
        IReadOnlyDictionary<string, BrowserTestOutcome> outcomes = await fixture.WorkerOutcomesOfAsync(engine);
        List<BrowserTestOutcome> own = outcomes.Values.Where(outcome => WorkerOwnTests.Contains(outcome.Name.Split('.')[0])).ToList();

        Assert.Contains(own, outcome => outcome.Name.StartsWith("WorkerOnlyTests.", StringComparison.Ordinal));
        Assert.All(own, outcome => Assert.True(outcome.Passed, $"{outcome.Name}: {outcome.Failure}"));
    }
}
