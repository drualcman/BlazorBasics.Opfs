using System.Reflection;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>
/// Runs every <c>[Fact]</c> and <c>[Theory]</c> row of the given classes in this browser, one after another, each with a new
/// instance, and names each result after the class that declares the test. <paramref name="onlyTest"/> narrows it to one method.
/// </summary>
internal static class BrowserTestRunner
{
    public static async Task<List<BrowserTestResult>> RunAsync(IEnumerable<Type> testClasses, string? onlyTest = null)
    {
        List<BrowserTestResult> results = [];
        foreach (Type testClass in testClasses)
        {
            IEnumerable<MethodInfo> tests = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<FactAttribute>() is not null && (onlyTest is null || method.Name == onlyTest))
                .OrderBy(method => method.Name, StringComparer.Ordinal);
            foreach (MethodInfo test in tests)
            {
                results.AddRange(await RunRowsAsync(testClass, test));
            }
        }
        return results;
    }

    private static async Task<List<BrowserTestResult>> RunRowsAsync(Type testClass, MethodInfo test)
    {
        List<BrowserTestResult> results = [];
        string name = $"{test.DeclaringType!.Name.Split('`')[0]}.{test.Name}";
        object?[][] rows = test.GetCustomAttributes<InlineDataAttribute>().SelectMany(row => row.GetData(test)).ToArray();
        if (rows.Length == 0)
        {
            results.Add(await RunAsync(testClass, test, name, null));
        }
        for (int row = 0; row < rows.Length; row++)
        {
            results.Add(await RunAsync(testClass, test, $"{name}[{row}]", rows[row]));
        }
        return results;
    }

    private static async Task<BrowserTestResult> RunAsync(Type testClass, MethodInfo test, string name, object?[]? arguments)
    {
        string? failure = null;
        try
        {
            object instance = Activator.CreateInstance(testClass)!;
            if (test.Invoke(instance, arguments) is Task running)
            {
                await running;
            }
        }
        catch (Exception exception)
        {
            failure = (exception is TargetInvocationException { InnerException: Exception inner } ? inner : exception).ToString();
        }
        return new BrowserTestResult(name, failure is null, failure);
    }
}
