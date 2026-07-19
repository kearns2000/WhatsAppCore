using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.AspNetCore.Webhooks;
using WhatsApp.Core.Configuration;
using WhatsApp.Core.DependencyInjection;

namespace WhatsApp.Core.AspNetCore.Tests.Helpers;

internal sealed class WebhookTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private WebhookTestHost(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    public static async Task<WebhookTestHost> CreateAsync(
        Action<IServiceCollection>? configureServices = null,
        Action<WhatsAppWebhookOptions>? configureWebhook = null,
        Action<WhatsAppWebhookOptions>? mapEndpointOptions = null,
        string route = "/webhooks/whatsapp")
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddWhatsAppCore(options =>
        {
            options.PhoneNumberId = "PHONE_NUMBER_ID";
            options.AccessToken = "access-token";
            options.GraphApiVersion = "v21.0";
            options.AppSecret = "app-secret-123";
            options.VerifyToken = "verify-token-123";
        });

        builder.Services.AddWhatsAppWebhooks(configureWebhook);
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        if (mapEndpointOptions is null)
        {
            app.MapWhatsAppWebhook(route);
        }
        else
        {
            app.MapWhatsAppWebhook(route, mapEndpointOptions);
        }

        await app.StartAsync().ConfigureAwait(false);

        var client = app.GetTestClient();
        return new WebhookTestHost(app, client);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}

internal sealed class CapturingWebhookHandler<TEvent> : IWhatsAppWebhookHandler<TEvent>
    where TEvent : WhatsApp.Core.AspNetCore.Webhooks.WhatsAppWebhookEvent
{
    private readonly List<TEvent> _events = [];
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _expectedCount = 1;

    public IReadOnlyList<TEvent> Events => _events;

    public void ExpectCount(int count) => _expectedCount = count;

    public Task WaitAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return _completion.Task.WaitAsync(cts.Token);
    }

    public Task HandleAsync(TEvent notification, CancellationToken stopToken)
    {
        lock (_events)
        {
            _events.Add(notification);
            if (_events.Count >= _expectedCount)
            {
                _completion.TrySetResult();
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class TrackingDeduplicator : IWhatsAppWebhookDeduplicator
{
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    public int AcceptedCount { get; private set; }

    public Task<bool> TryAcceptAsync(WhatsApp.Core.AspNetCore.Webhooks.WhatsAppWebhookEvent notification, CancellationToken stopToken)
    {
        var key = WhatsAppWebhookDedupKey.For(notification);
        if (!_seen.Add(key))
        {
            return Task.FromResult(false);
        }

        AcceptedCount++;
        return Task.FromResult(true);
    }
}
