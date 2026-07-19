using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using WhatsApp.Core.AspNetCore.Signature;
using WhatsApp.Core.Client;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Messages;
using WhatsApp.Core.Responses;
using WhatsApp.Core.Testing.Builders;
using WhatsApp.Core.Testing.Fakes;
using WhatsApp.Core.Testing.Signatures;

namespace WhatsApp.Core.Testing.Tests;

public sealed class FakeWhatsAppClientTests
{
    [Fact]
    public async Task RecordsRequestsInOrder()
    {
        var fake = new FakeWhatsAppClient("Sales");
        fake.QueueResponse(FakeWhatsAppClient.CreateDefaultSendResponse("wamid.1"));
        fake.QueueResponse(FakeWhatsAppClient.CreateDefaultSendResponse("wamid.2"));

        await fake.SendTextAsync("15551234567", "One");
        await fake.SendTextAsync("15551234567", "Two");

        Assert.Equal(2, fake.Requests.Count);
        Assert.Equal(RecordedWhatsAppOperation.SendMessage, fake.Requests[0].Operation);
        Assert.Equal(RecordedWhatsAppOperation.SendMessage, fake.Requests[1].Operation);
        Assert.Equal("Sales", fake.AccountName);
    }

    [Fact]
    public async Task SupportsConcurrentCalls()
    {
        var fake = new FakeWhatsAppClient();
        for (var i = 0; i < 20; i++)
        {
            fake.QueueResponse(FakeWhatsAppClient.CreateDefaultSendResponse($"wamid.{i}"));
        }

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 20),
            async (i, _) => await fake.SendTextAsync("15551234567", $"Msg {i}"));

        Assert.Equal(20, fake.Requests.Count);
    }

    [Fact]
    public async Task QueuedException_IsThrownOnDequeue()
    {
        var fake = new FakeWhatsAppClient();
        fake.QueueException(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fake.SendTextAsync("15551234567", "Hi"));
    }

    [Fact]
    public async Task Reset_ClearsRequestsAndQueues()
    {
        var fake = new FakeWhatsAppClient();
        fake.QueueResponse(FakeWhatsAppClient.CreateDefaultSendResponse());
        await fake.SendTextAsync("15551234567", "Hi");
        fake.Reset();

        fake.EnsureNoRequests();
        fake.EnsureNoUnusedQueuedResults();
    }

    [Fact]
    public async Task Cancellation_IsObserved()
    {
        var fake = new FakeWhatsAppClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fake.SendTextAsync("15551234567", "Hi", stopToken: cts.Token));
    }

    [Fact]
    public async Task UploadMedia_CopiesStreamContent()
    {
        var fake = new FakeWhatsAppClient();
        fake.QueueMediaUploadResponse(new MediaUploadResponse("media-1", new WhatsAppResponseMetadata(HttpStatusCode.OK, null, null, new Dictionary<string, string>())));

        await using var stream = new MemoryStream("payload"u8.ToArray());
        await fake.UploadMediaAsync(stream, "file.bin", "application/octet-stream");

        var recorded = fake.Requests[0];
        Assert.Equal("file.bin", recorded.FileName);
        Assert.Equal("application/octet-stream", recorded.ContentType);
        Assert.Equal("payload", Encoding.UTF8.GetString(recorded.ContentCopy!));
    }
}

public sealed class WhatsAppWebhookBuilderTests
{
    [Fact]
    public void Build_IncludesMessagesStatusesAndContacts()
    {
        var json = WhatsAppWebhookBuilder.Create()
            .WithPhoneNumberId("PNID")
            .AddTextMessage("wamid.1", "15550001111", "Hello")
            .AddDeliveredStatus("wamid.2", "15550002222")
            .BuildJson();

        Assert.Contains("\"phone_number_id\":\"PNID\"", json, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"Hello\"", json, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"delivered\"", json, StringComparison.Ordinal);
        Assert.Contains("\"wa_id\":\"15550001111\"", json, StringComparison.Ordinal);
    }
}

public sealed class WhatsAppWebhookTestSignaturesTests
{
    [Fact]
    public void CreateSignature_MatchesAspNetCoreValidator()
    {
        var payload = WhatsAppWebhookBuilder.Create().AddTextMessage("wamid.1", "15550001111", "Hi").BuildBytes();
        const string secret = "app-secret-123";

        var testingSignature = WhatsAppWebhookTestSignatures.CreateSignature(payload, secret);
        var aspNetSignature = WhatsAppWebhookSignature.Compute(payload, secret);

        Assert.Equal(aspNetSignature, testingSignature);
        Assert.True(new WhatsAppWebhookSignatureValidator(secret).IsValid(payload, testingSignature));
    }
}

internal static class FakeWhatsAppClientExtensions
{
    public static Task<SendMessageResponse> SendTextAsync(
        this IWhatsAppClient client,
        string to,
        string body,
        CancellationToken stopToken = default) =>
        client.SendMessageAsync(new TextMessageRequest { To = to, Body = body }, stopToken);
}
