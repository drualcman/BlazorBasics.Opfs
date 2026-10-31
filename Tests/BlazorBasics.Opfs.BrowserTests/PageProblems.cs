using Microsoft.Playwright;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>What a page complained about: console errors, uncaught failures and requests the server refused.</summary>
internal sealed class PageProblems
{
    private const int FIRST_ERROR_STATUS = 400;

    private readonly List<string> Problems = [];

    public PageProblems(IPage page)
    {
        page.Console += (_, message) => Keep(message.Type == "error", $"console: {message.Text}");
        page.PageError += (_, failure) => Keep(true, $"uncaught: {failure}");
        page.Response += (_, response) => Keep(response.Status >= FIRST_ERROR_STATUS, $"{response.Status}: {response.Url}");
    }

    public IReadOnlyList<string> Seen
    {
        get
        {
            lock (Problems)
            {
                return [.. Problems];
            }
        }
    }

    private void Keep(bool isProblem, string description)
    {
        if (isProblem)
        {
            lock (Problems)
            {
                Problems.Add(description);
            }
        }
    }
}
