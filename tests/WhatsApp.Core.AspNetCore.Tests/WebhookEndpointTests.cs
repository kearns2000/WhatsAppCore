using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.AspNetCore.Signature;
using WhatsApp.Core.AspNetCore.Tests.Helpers;
using WhatsApp.Core.AspNetCore.Webhooks;
using WhatsApp.Core.Testing.Builders;
using WhatsApp.Core.Testing.Signatures;

namespace WhatsApp.Core.AspNetCore.Tests;

public sealed class WebhookVerificationTests
{
    [Fact]
    public async Task Verification_ReturnsChallengeWhenTokenMatches()
    {
        await using var host = await WebhookTestHost.CreateAsync();
        var response = await host.Client.GetAsync(
            "/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=verify-token-123&hub.challenge=challenge-42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("challenge-42", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Verification_ReturnsForbiddenForBadToken()
    {
        await using var host = await WebhookTestHost.CreateAsync();
        var response = await host.Client.GetAsync(
            "/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=challenge-42");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed class WebhookSignatureTests
{
    [Fact]
    public async Task Delivery_RejectsMissingSignature()
    {
        await using var host = await WebhookTestHost.CreateAsync();
        var body = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "Hi").BuildJson();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await host.Client.PostAsync("/webhooks/whatsapp", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delivery_RejectsInvalidSignature()
    {
        await using var host = await WebhookTestHost.CreateAsync();
        var bytes = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "Hi").BuildBytes();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/whatsapp")
        {
            Content = new ByteArrayContent(bytes),
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        request.Headers.Add(WhatsAppWebhookSignature.HeaderName, "sha256=deadbeef");

        var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delivery_AcceptsValidSignatureFromTestingHelper()
    {
        var handler = new CapturingWebhookHandler<WhatsAppTextMessageEvent>();
        await using var host = await WebhookTestHost.CreateAsync(services =>
            services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler));

        var bytes = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "Hi").BuildBytes();
        using var request = CreateSignedRequest(bytes);
        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await handler.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(handler.Events);
        Assert.Equal("Hi", handler.Events[0].Body);
    }

    [Fact]
    public async Task Delivery_RejectsMalformedSignaturePrefix()
    {
        await using var host = await WebhookTestHost.CreateAsync();
        var bytes = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "Hi").BuildBytes();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/whatsapp")
        {
            Content = new ByteArrayContent(bytes),
        };
        request.Headers.Add(WhatsAppWebhookSignature.HeaderName, "md5=abc");

        var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    internal static HttpRequestMessage CreateSignedRequest(byte[] body, string appSecret = "app-secret-123")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/whatsapp")
        {
            Content = new ByteArrayContent(body),
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        request.Headers.Add(
            WhatsAppWebhookSignature.HeaderName,
            WhatsAppWebhookTestSignatures.CreateSignature(body, appSecret));
        return request;
    }
}

public sealed class WebhookDeliveryTests
{
    [Fact]
    public async Task Delivery_DispatchesMultipleMessagesAndStatuses()
    {
        var textHandler = new CapturingWebhookHandler<WhatsAppTextMessageEvent>();
        var deliveredHandler = new CapturingWebhookHandler<WhatsAppMessageDeliveredEvent>();
        textHandler.ExpectCount(1);
        deliveredHandler.ExpectCount(1);

        await using var host = await WebhookTestHost.CreateAsync(services =>
        {
            services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(textHandler);
            services.AddWhatsAppWebhookHandler<WhatsAppMessageDeliveredEvent>(deliveredHandler);
        });

        var bytes = WhatsAppWebhookBuilder.Create()
            .AddTextMessage("wamid.in", "15550001111", "Hello")
            .AddDeliveredStatus("wamid.out", "15550002222")
            .BuildBytes();

        var response = await host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await Task.WhenAll(
            textHandler.WaitAsync(TimeSpan.FromSeconds(5)),
            deliveredHandler.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal("Hello", textHandler.Events[0].Body);
        Assert.Equal("wamid.out", deliveredHandler.Events[0].MessageId);
    }

    [Fact]
    public async Task Delivery_ParsesUnknownMessageType()
    {
        var handler = new CapturingWebhookHandler<UnknownWhatsAppMessageEvent>();
        await using var host = await WebhookTestHost.CreateAsync(services =>
            services.AddWhatsAppWebhookHandler<UnknownWhatsAppMessageEvent>(handler));

        var bytes = WhatsAppWebhookBuilder.Create()
            .AddMessage(new System.Text.Json.Nodes.JsonObject
            {
                ["from"] = "15550001111",
                ["id"] = "wamid.unknown",
                ["timestamp"] = "1700000000",
                ["type"] = "future_type",
                ["future_type"] = new System.Text.Json.Nodes.JsonObject { ["foo"] = "bar" },
            })
            .BuildBytes();

        var response = await host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await handler.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("future_type", handler.Events[0].MessageType);
    }

    [Fact]
    public async Task Delivery_RejectsOversizedBody()
    {
        await using var host = await WebhookTestHost.CreateAsync(
            configureWebhook: o => o.MaxRequestBodyBytes = 32);

        var bytes = WhatsAppWebhookBuilder.Create()
            .AddTextMessage("wamid.big", "15550001111", new string('x', 256))
            .BuildBytes();

        var response = await host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Delivery_AllowsSignatureValidationDisabledForTests()
    {
        var handler = new CapturingWebhookHandler<WhatsAppTextMessageEvent>();
        await using var host = await WebhookTestHost.CreateAsync(
            configureServices: services => services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler),
            configureWebhook: o =>
            {
                o.RequireSignatureValidation = false;
                o.AllowInsecureNoSignatureValidation = true;
            });

        var body = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "NoSig").BuildJson();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await host.Client.PostAsync("/webhooks/whatsapp", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await handler.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("NoSig", handler.Events[0].Body);
    }
}

public sealed class WebhookDispatchTests
{
    [Fact]
    public async Task Deduplicator_SkipsDuplicateEvents()
    {
        var deduplicator = new TrackingDeduplicator();
        var handler = new CapturingWebhookHandler<WhatsAppTextMessageEvent>();
        handler.ExpectCount(1);

        await using var host = await WebhookTestHost.CreateAsync(services =>
        {
            services.AddSingleton<IWhatsAppWebhookDeduplicator>(deduplicator);
            services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler);
        });

        var bytes = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.dup", "15550001111", "Once").BuildBytes();
        var request = WebhookSignatureTests.CreateSignedRequest(bytes);

        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes))).StatusCode);

        await handler.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(handler.Events);
        Assert.Equal(1, deduplicator.AcceptedCount);
    }

    [Fact]
    public async Task ParallelDispatch_InvokesHandlersConcurrently()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        var handler = new SlowParallelHandler(gate, () => Interlocked.Increment(ref started));

        await using var host = await WebhookTestHost.CreateAsync(
            configureServices: services => services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler),
            configureWebhook: o =>
            {
                o.DispatchMode = WhatsAppWebhookDispatchMode.Parallel;
                o.MaxDegreeOfParallelism = 4;
            });

        var builder = WhatsAppWebhookBuilder.Create();
        for (var i = 0; i < 3; i++)
        {
            builder.AddTextMessage($"wamid.{i}", "15550001111", $"Msg {i}");
        }

        var bytes = builder.BuildBytes();
        var sendTask = host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes));
        await Task.Delay(100);
        Assert.True(Volatile.Read(ref started) >= 2);
        gate.SetResult();
        Assert.Equal(HttpStatusCode.OK, (await sendTask).StatusCode);
    }

    [Fact]
    public async Task ParallelDispatch_UsesPerEndpointOptions()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        var handler = new SlowParallelHandler(gate, () => Interlocked.Increment(ref started));

        // Ambient DI options stay sequential; only the mapped endpoint opts into parallel dispatch.
        await using var host = await WebhookTestHost.CreateAsync(
            configureServices: services => services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler),
            configureWebhook: o => o.DispatchMode = WhatsAppWebhookDispatchMode.Sequential,
            mapEndpointOptions: o =>
            {
                o.DispatchMode = WhatsAppWebhookDispatchMode.Parallel;
                o.MaxDegreeOfParallelism = 4;
            });

        var builder = WhatsAppWebhookBuilder.Create();
        for (var i = 0; i < 3; i++)
        {
            builder.AddTextMessage($"wamid.endpoint.{i}", "15550001111", $"Msg {i}");
        }

        var bytes = builder.BuildBytes();
        var sendTask = host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes));
        await Task.Delay(100);
        Assert.True(Volatile.Read(ref started) >= 2);
        gate.SetResult();
        Assert.Equal(HttpStatusCode.OK, (await sendTask).StatusCode);
    }

    [Fact]
    public async Task MemoryDeduplicator_SkipsReplayByDefault()
    {
        var handler = new CapturingWebhookHandler<WhatsAppTextMessageEvent>();
        handler.ExpectCount(1);

        await using var host = await WebhookTestHost.CreateAsync(services =>
            services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler));

        var bytes = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.replay", "15550001111", "Once").BuildBytes();

        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes))).StatusCode);

        await handler.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(handler.Events);
    }

    [Fact]
    public async Task SequentialDispatch_InvokesHandlersOneAtATime()
    {
        var handler = new SequentialGateHandler();
        await using var host = await WebhookTestHost.CreateAsync(
            configureServices: services => services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler),
            configureWebhook: o => o.DispatchMode = WhatsAppWebhookDispatchMode.Sequential);

        var bytes = WhatsAppWebhookBuilder.Create()
            .AddTextMessage("wamid.1", "15550001111", "First")
            .AddTextMessage("wamid.2", "15550001111", "Second")
            .BuildBytes();

        var sendTask = host.Client.SendAsync(WebhookSignatureTests.CreateSignedRequest(bytes));
        await handler.WaitForFirstHandlerAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, handler.StartedCount);

        handler.ReleaseFirstHandler();
        Assert.Equal(HttpStatusCode.OK, (await sendTask).StatusCode);
        Assert.Equal(2, handler.StartedCount);
    }

    [Fact]
    public async Task Delivery_PropagatesCancellationToHandlers()
    {
        var handler = new CancellableSlowHandler();
        await using var host = await WebhookTestHost.CreateAsync(services =>
            services.AddWhatsAppWebhookHandler<WhatsAppTextMessageEvent>(handler));

        var bytes = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "Hi").BuildBytes();
        using var cts = new CancellationTokenSource();
        using var request = WebhookSignatureTests.CreateSignedRequest(bytes);

        var sendTask = host.Client.SendAsync(request, cts.Token);
        await handler.WaitUntilStartedAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();

        await handler.WaitForCancellationObservedAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);
    }

    private sealed class SlowParallelHandler(TaskCompletionSource gate, Action onStart) : IWhatsAppWebhookHandler<WhatsAppTextMessageEvent>
    {
        public async Task HandleAsync(WhatsAppTextMessageEvent notification, CancellationToken stopToken)
        {
            onStart();
            await gate.Task.WaitAsync(stopToken).ConfigureAwait(false);
        }
    }

    private sealed class SequentialGateHandler : IWhatsAppWebhookHandler<WhatsAppTextMessageEvent>
    {
        private readonly TaskCompletionSource _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _startedCount;

        public int StartedCount => Volatile.Read(ref _startedCount);

        public Task WaitForFirstHandlerAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return _firstStarted.Task.WaitAsync(cts.Token);
        }

        public void ReleaseFirstHandler() => _firstRelease.TrySetResult();

        public async Task HandleAsync(WhatsAppTextMessageEvent notification, CancellationToken stopToken)
        {
            var count = Interlocked.Increment(ref _startedCount);
            if (count == 1)
            {
                _firstStarted.TrySetResult();
                await _firstRelease.Task.WaitAsync(stopToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class CancellableSlowHandler : IWhatsAppWebhookHandler<WhatsAppTextMessageEvent>
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitUntilStartedAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return _started.Task.WaitAsync(cts.Token);
        }

        public Task WaitForCancellationObservedAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return _cancelled.Task.WaitAsync(cts.Token);
        }

        public async Task HandleAsync(WhatsAppTextMessageEvent notification, CancellationToken stopToken)
        {
            _started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, stopToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _cancelled.TrySetResult();
                throw;
            }
        }
    }
}
