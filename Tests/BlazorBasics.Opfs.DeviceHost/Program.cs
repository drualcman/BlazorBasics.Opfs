using BlazorBasics.Opfs.BrowserTests;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

string publishedFolder = Path.GetFullPath(args[0]);
string reportsFolder = Path.GetFullPath(args[1]);
int port = int.Parse(args[2]);

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
WebApplication application = builder.Build();

PhysicalFileProvider files = new(publishedFolder);
application.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files, DefaultFileNames = ["device.html"] });
application.UseStaticFiles(new StaticFileOptions
{
    FileProvider = files,
    ContentTypeProvider = new FileExtensionContentTypeProvider(),
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});
FetchFixtureRoutes.Map(application);
new BackForwardReports().Map(application);
DeviceReportRoute.Map(application, reportsFolder);

await application.StartAsync();
Console.WriteLine($"Serving the device checks at http://127.0.0.1:{port}");
Console.WriteLine($"Reports go to {reportsFolder}");
await application.WaitForShutdownAsync();
