using System.Net;

namespace BlazorBasics.Opfs.Tests;

/// <summary>The fixture routes of the battery, in memory.</summary>
internal static class FetchFixture
{
    public static Task<HttpResponseMessage> AnswerAsync(Uri url, CancellationToken cancellationToken)
    {
        string[] route = url.AbsolutePath.Trim('/').Split('/');
        int size = int.Parse(route[1]);
        HttpResponseMessage response = route[0] switch
        {
            "status" => new HttpResponseMessage((HttpStatusCode)size),
            "bytes" => Body(new MemoryStream(new byte[size]), size),
            "chunked" => Body(new MemoryStream(new byte[size]), null),
            "cut" => Body(new FetchFixtureStream(size, stalls: false), null),
            _ => Body(new FetchFixtureStream(size, stalls: true), null)
        };
        return Task.FromResult(response);
    }

    private static HttpResponseMessage Body(Stream stream, long? declaredLength)
    {
        StreamContent content = new(stream);
        content.Headers.ContentLength = declaredLength;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}
