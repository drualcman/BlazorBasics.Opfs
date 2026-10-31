using System.Reflection;
using BlazorBasics.Opfs.ContractTests;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>One case per test of the battery and per engine, which passes when that test passed in that browser.</summary>
[Collection(BrowserCollection.NAME)]
public sealed class ContractInBrowserTests(BrowserFixture fixture)
{
    private static readonly string[] BrowserOwnTests = ["BrowserOnlyTests", "LargeFileTests"];

    public static TheoryData<string, string> ContractTests()
    {
        TheoryData<string, string> cases = [];
        IEnumerable<Type> contractClasses = typeof(IOpfsContractTarget).Assembly.GetTypes().Where(type => type.Name.Contains("ContractTests`"));
        foreach (string engine in BrowserEngines.Names)
        {
            foreach (Type contractClass in contractClasses)
            {
                foreach (string test in contractClass.GetMethods().SelectMany(NamesOf))
                {
                    cases.Add(engine, $"{contractClass.Name.Split('`')[0]}.{test}");
                }
            }
        }
        return cases;
    }

    public static TheoryData<string> Engines() => new(BrowserEngines.Names);

    [Theory]
    [MemberData(nameof(ContractTests))]
    public async Task The_contract_test_passes_in_the_browser(string engine, string testName)
    {
        IReadOnlyDictionary<string, BrowserTestOutcome> outcomes = await fixture.OutcomesOfAsync(engine);

        Assert.True(outcomes.TryGetValue(testName, out BrowserTestOutcome? outcome), $"{testName} did not run in {engine}.");
        Assert.True(outcome.Passed, outcome.Failure);
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public async Task The_tests_of_the_browser_itself_pass(string engine)
    {
        IReadOnlyDictionary<string, BrowserTestOutcome> outcomes = await fixture.OutcomesOfAsync(engine);
        List<BrowserTestOutcome> own = outcomes.Values.Where(outcome => BrowserOwnTests.Contains(outcome.Name.Split('.')[0])).ToList();

        Assert.NotEmpty(own);
        Assert.All(own, outcome => Assert.True(outcome.Passed, $"{outcome.Name}: {outcome.Failure}"));
    }

    private static IEnumerable<string> NamesOf(MethodInfo method)
    {
        int rows = method.GetCustomAttributes<InlineDataAttribute>().Count();
        IEnumerable<string> names = rows == 0 ? [method.Name] : Enumerable.Range(0, rows).Select(row => $"{method.Name}[{row}]");
        return method.GetCustomAttribute<FactAttribute>() is null ? [] : names;
    }
}
