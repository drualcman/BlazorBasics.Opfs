using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>Keeps on this computer what <c>device.js</c> sends, because text is hard to copy out of a phone.</summary>
internal static class DeviceReportRoute
{
    private const int LARGEST_REPORT_CHARACTERS = 256 * 1024;

    public static void Map(IEndpointRouteBuilder routes, string reportsFolder)
    {
        Directory.CreateDirectory(reportsFolder);
        routes.MapPost("/fixture/device-report/{id:regex(^[a-z0-9-]{{1,40}}$)}", async (string id, HttpContext context) =>
        {
            using StreamReader reader = new(context.Request.Body, Encoding.UTF8);
            char[] buffer = new char[LARGEST_REPORT_CHARACTERS];
            int length = await reader.ReadBlockAsync(buffer, context.RequestAborted);
            string path = Path.Combine(reportsFolder, $"{id}.txt");
            await File.WriteAllTextAsync(path, new string(buffer, 0, length), context.RequestAborted);
            Console.WriteLine($"Report received: {path}");
        });
    }
}
