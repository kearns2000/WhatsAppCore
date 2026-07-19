using Microsoft.Extensions.Time.Testing;
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Tests;

public sealed class MemoryWhatsAppWebhookDeduplicatorTests
{
    [Fact]
    public async Task TryAcceptAsync_ReacceptsAfterRetentionExpires()
    {
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-07-19T12:00:00Z"));
        var deduplicator = new MemoryWhatsAppWebhookDeduplicator(time, TimeSpan.FromMinutes(5));
        var notification = CreateTextEvent("wamid.expiry");

        Assert.True(await deduplicator.TryAcceptAsync(notification, CancellationToken.None));
        Assert.False(await deduplicator.TryAcceptAsync(notification, CancellationToken.None));

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.True(await deduplicator.TryAcceptAsync(notification, CancellationToken.None));
    }

    private static WhatsAppTextMessageEvent CreateTextEvent(string messageId) => new()
    {
        WhatsAppBusinessAccountId = "WABA",
        PhoneNumberId = "PHONE",
        MessageId = messageId,
        From = "15550001111",
        Timestamp = DateTimeOffset.UtcNow,
        ReceivedAt = DateTimeOffset.UtcNow,
        Body = "hi",
    };
}
