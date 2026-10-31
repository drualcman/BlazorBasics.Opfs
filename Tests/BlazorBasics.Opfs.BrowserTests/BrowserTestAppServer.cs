using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// Serves the published test app and the fetch fixture on a free port of <c>127.0.0.1</c>, which the browser treats as a
/// secure context, as the Origin Private File System and Web Locks need.
/// </summary>
internal sealed class BrowserTestAppServer : IAsyncDisposable
{
    private readonly WebApplication Application;

    private BrowserTestAppServer(WebApplication application, BackForwardReports backForward)
    {
        Application = application;
        BackForward = backForward;
    }

    public BackForwardReports BackForward { get; }

    public string Address => Application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    public static async Task<BrowserTestAppServer> StartAsync()
    {
        string publishedFolder = Path.Combine(AppContext.BaseDirectory, "BrowserTestApp", "wwwroot");
        PhysicalFileProvider files = new(publishedFolder);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        WebApplication application = builder.Build();
        application.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        application.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        });
        FetchFixtureRoutes.Map(application);
        BackForwardReports backForward = new();
        backForward.Map(application);
        await application.StartAsync();
        return new BrowserTestAppServer(application, backForward);
    }

    public async ValueTask DisposeAsync()
    {
        await Application.StopAsync();
        await Application.DisposeAsync();
    }
}
