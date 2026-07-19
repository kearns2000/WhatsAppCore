using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using WhatsApp.Core.Client;
using WhatsApp.Core.Configuration;
using WhatsApp.Core.DependencyInjection;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;
using WhatsApp.Core.Tests.Helpers;

namespace WhatsApp.Core.Tests;

public sealed class WhatsAppClientHttpTests
{
    [Fact]
    public async Task SendTextAsync_PostsToVersionedMessagesUriWithBearerToken()
    {
        var (provider, handler) = WhatsAppClientTestHost.Create();
        var client = WhatsAppClientTestHost.GetClient(provider);

        await client.SendTextAsync("15551234567", "Hello");

        Assert.Single(handler.Requests);
        var request = handler.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://graph.test.local/v21.0/123456789/messages", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("test-access-token", request.Headers.Authorization.Parameter);

        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"body\":\"Hello\"", body, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"text\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendRawMessageAsync_AllowsEscapeHatchPayload()
    {
        var (provider, handler) = WhatsAppClientTestHost.Create();
        var client = WhatsAppClientTestHost.GetClient(provider);

        var payload = new JsonObject
        {
            ["to"] = "15551234567",
            ["type"] = "text",
            ["text"] = new JsonObject { ["body"] = "Raw" },
        };

        await client.SendRawMessageAsync(payload);

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"messaging_product\":\"whatsapp\"", body, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"Raw\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_PostsReadReceiptPayload()
    {
        var (provider, handler) = WhatsAppClientTestHost.Create();
        var client = WhatsAppClientTestHost.GetClient(provider);

        await client.MarkMessageAsReadAsync("wamid.read");

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"status\":\"read\"", body, StringComparison.Ordinal);
        Assert.Contains("\"message_id\":\"wamid.read\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NamedAccounts_UseIsolatedHttpClients()
    {
        var handlerA = new RecordingHttpMessageHandler();
        var handlerB = new RecordingHttpMessageHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWhatsAppCore("AccountA", o =>
        {
            o.PhoneNumberId = "aaa";
            o.AccessToken = "token-a";
            o.GraphApiVersion = "v21.0";
            o.BaseAddress = new Uri("https://a.test/");
        });
        services.AddWhatsAppCore("AccountB", o =>
        {
            o.PhoneNumberId = "bbb";
            o.AccessToken = "token-b";
            o.GraphApiVersion = "v21.0";
            o.BaseAddress = new Uri("https://b.test/");
        });
        services.AddHttpClient("WhatsApp.Core:AccountA").ConfigurePrimaryHttpMessageHandler(() => handlerA);
        services.AddHttpClient("WhatsApp.Core:AccountB").ConfigurePrimaryHttpMessageHandler(() => handlerB);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IWhatsAppClientFactory>();

        await factory.CreateClient("AccountA").SendTextAsync("15551234567", "A");
        await factory.CreateClient("AccountB").SendTextAsync("15551234567", "B");

        Assert.Single(handlerA.Requests);
        Assert.Single(handlerB.Requests);
        Assert.Contains("aaa/messages", handlerA.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Contains("bbb/messages", handlerB.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("token-a", handlerA.LastRequest.Headers.Authorization!.Parameter);
        Assert.Equal("token-b", handlerB.LastRequest.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ApiError_ParsesMetaErrorAndRetryAfter()
    {
        var (provider, _) = WhatsAppClientTestHost.Create(responder: _ =>
            RecordingHttpMessageHandler.CreateJsonResponse(
                HttpStatusCode.BadRequest,
                """
                {
                  "error": {
                    "message": "Invalid parameter",
                    "type": "OAuthException",
                    "code": 100,
                    "error_subcode": 33,
                    "fbtrace_id": "trace-123"
                  }
                }
                """,
                response => response.Headers.TryAddWithoutValidation("Retry-After", "30")));

        var client = WhatsAppClientTestHost.GetClient(provider);
        var ex = await Assert.ThrowsAsync<WhatsAppApiException>(() => client.SendTextAsync("15551234567", "Hi"));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal(100, ex.ErrorCode);
        Assert.Equal(33, ex.ErrorSubcode);
        Assert.Equal("OAuthException", ex.ErrorType);
        Assert.Equal("trace-123", ex.MetaTraceId);
        Assert.Equal("Invalid parameter", ex.Message);
        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter);
    }

    [Fact]
    public async Task OperationCanceled_PropagatesCancellation()
    {
        var (provider, _) = WhatsAppClientTestHost.Create(responder: _ => throw new TaskCanceledException());
        var client = WhatsAppClientTestHost.GetClient(provider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SendTextAsync("15551234567", "Hi", stopToken: cts.Token));
    }

    [Fact]
    public async Task ApiError_FallsBackToStatusCodeMessage_WhenBodyIsNotJson()
    {
        var (provider, _) = WhatsAppClientTestHost.Create(responder: _ =>
            RecordingHttpMessageHandler.CreateJsonResponse(HttpStatusCode.InternalServerError, "<html>proxy error</html>"));

        var client = WhatsAppClientTestHost.GetClient(provider);
        var ex = await Assert.ThrowsAsync<WhatsAppApiException>(() => client.SendTextAsync("15551234567", "Hi"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
        Assert.Contains("500", ex.Message, StringComparison.Ordinal);
        Assert.Contains("InternalServerError", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpClient_UsesConfiguredTimeout()
    {
        var (provider, _) = WhatsAppClientTestHost.Create(configure: o => o.Timeout = TimeSpan.FromSeconds(42));
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(WhatsAppHttpClientNames.For(WhatsAppOptions.DefaultAccountName));

        Assert.Equal(TimeSpan.FromSeconds(42), httpClient.Timeout);
    }
}
