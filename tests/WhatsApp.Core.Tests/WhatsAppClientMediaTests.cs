using System.Net;
using Microsoft.Extensions.DependencyInjection;
using WhatsApp.Core.DependencyInjection;
using WhatsApp.Core.Media;
using WhatsApp.Core.Tests.Helpers;

namespace WhatsApp.Core.Tests;

public sealed class WhatsAppClientMediaTests
{
    [Fact]
    public async Task UploadMediaAsync_SendsMultipartFields()
    {
        var (provider, handler) = WhatsAppClientTestHost.Create(responder: _ =>
            RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.OK, """{"id":"uploaded-media-id"}"""));

        var client = WhatsAppClientTestHost.GetClient(provider);
        await using var stream = new MemoryStream("hello media"u8.ToArray());
        var response = await client.UploadMediaAsync(stream, "photo.jpg", "image/jpeg");

        Assert.Equal("uploaded-media-id", response.MediaId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Contains("/123456789/media", handler.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);

        var bodyBytes = await handler.LastRequest.Content!.ReadAsByteArrayAsync();
        var bodyText = System.Text.Encoding.UTF8.GetString(bodyBytes);
        Assert.Contains("messaging_product", bodyText, StringComparison.Ordinal);
        Assert.Contains("image/jpeg", bodyText, StringComparison.Ordinal);
        Assert.Contains("photo.jpg", bodyText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DownloadMediaAsync_FetchesFreshMetadataThenStreamsContent()
    {
        var metadataCalls = 0;
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.Host == "graph.test.local")
            {
                metadataCalls++;
                return RecordingHttpMessageHandler.CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "id": "media-42",
                      "url": "https://lookaside.fbsbx.com/media-42",
                      "mime_type": "image/png",
                      "file_size": 5
                    }
                    """);
            }

            if (request.RequestUri!.Host == "lookaside.fbsbx.com")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent("bytes"u8.ToArray())
                    {
                        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") },
                    },
                };
            }

            return RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.NotFound, "{}");
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWhatsAppCore(o =>
        {
            o.PhoneNumberId = "123456789";
            o.AccessToken = "token";
            o.GraphApiVersion = "v21.0";
            o.BaseAddress = new Uri("https://graph.test.local/");
        });
        services.AddHttpClient("WhatsApp.Core:Default").ConfigurePrimaryHttpMessageHandler(() => handler);
        var client = WhatsAppClientTestHost.GetClient(services.BuildServiceProvider());

        await using var download = await client.DownloadMediaAsync("media-42");
        using var reader = new StreamReader(download.Content);
        var text = await reader.ReadToEndAsync();

        Assert.Equal("bytes", text);
        Assert.Equal("image/png", download.ContentType);
        Assert.Equal(1, metadataCalls);
    }

    [Fact]
    public async Task DownloadMediaAsync_DisposesContentStream()
    {
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.Host == "graph.test.local")
            {
                return RecordingHttpMessageHandler.CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "id": "media-99",
                      "url": "https://lookaside.fbsbx.com/file",
                      "mime_type": "text/plain"
                    }
                    """);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("x"u8.ToArray())
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain") },
                },
            };
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWhatsAppCore(o =>
        {
            o.PhoneNumberId = "123456789";
            o.AccessToken = "token";
            o.GraphApiVersion = "v21.0";
            o.BaseAddress = new Uri("https://graph.test.local/");
        });
        services.AddHttpClient("WhatsApp.Core:Default").ConfigurePrimaryHttpMessageHandler(() => handler);
        var client = WhatsAppClientTestHost.GetClient(services.BuildServiceProvider());

        var download = await client.DownloadMediaAsync("media-99");
        var stream = download.Content;
        await download.DisposeAsync();

        await Assert.ThrowsAnyAsync<ObjectDisposedException>(async () =>
            await stream.ReadAsync(new byte[1]).AsTask());
    }

    [Fact]
    public async Task DeleteMediaAsync_UsesDeleteWithPhoneNumberQuery()
    {
        var (provider, handler) = WhatsAppClientTestHost.Create(responder: _ =>
            RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.OK, "{}"));
        var client = WhatsAppClientTestHost.GetClient(provider);

        await client.DeleteMediaAsync("media-del");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest.Method);
        Assert.Contains("media-del", handler.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Contains("phone_number_id=123456789", handler.LastRequest.RequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMediaAsync_RetriesOnTransientFailureWhenEnabled()
    {
        var attempts = 0;
        var (provider, _) = WhatsAppClientTestHost.Create(
            configure: o =>
            {
                o.Resilience.EnableSafeRetries = true;
                o.Resilience.MaxSafeRetries = 2;
            },
            responder: _ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    return RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.ServiceUnavailable, "{}");
                }

                return RecordingHttpMessageHandler.CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "id": "media-1",
                      "url": "https://lookaside.fbsbx.com/media-1",
                      "mime_type": "image/jpeg"
                    }
                    """);
            });

        var client = WhatsAppClientTestHost.GetClient(provider);
        var metadata = await client.GetMediaAsync("media-1");

        Assert.Equal(2, attempts);
        Assert.Equal("media-1", metadata.MediaId);
    }

    [Fact]
    public async Task UploadMediaAsync_DoesNotDisposeCallerStream()
    {
        var (provider, _) = WhatsAppClientTestHost.Create(responder: _ =>
            RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.OK, """{"id":"uploaded-media-id"}"""));

        var client = WhatsAppClientTestHost.GetClient(provider);
        var stream = new MemoryStream("hello media"u8.ToArray());
        await client.UploadMediaAsync(stream, "photo.jpg", "image/jpeg");

        Assert.True(stream.CanRead);
        stream.Position = 0;
        Assert.Equal(11, stream.Length);
        await stream.DisposeAsync();
    }

    [Fact]
    public async Task DownloadMediaAsync_RejectsDisallowedHost()
    {
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.Host == "graph.test.local")
            {
                return RecordingHttpMessageHandler.CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "id": "media-evil",
                      "url": "https://evil.example/steal",
                      "mime_type": "image/png"
                    }
                    """);
            }

            return RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.NotFound, "{}");
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWhatsAppCore(o =>
        {
            o.PhoneNumberId = "123456789";
            o.AccessToken = "token";
            o.GraphApiVersion = "v21.0";
            o.BaseAddress = new Uri("https://graph.test.local/");
        });
        services.AddHttpClient("WhatsApp.Core:Default").ConfigurePrimaryHttpMessageHandler(() => handler);
        var client = WhatsAppClientTestHost.GetClient(services.BuildServiceProvider());

        await Assert.ThrowsAsync<WhatsApp.Core.Errors.WhatsAppValidationException>(
            () => client.DownloadMediaAsync("media-evil"));
    }
}
