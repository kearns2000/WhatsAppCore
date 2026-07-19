# Webhooks

WhatsApp.Core.AspNetCore receives Meta webhook deliveries, validates signatures, parses payloads into typed events, and dispatches them to your handlers.

## Setup

Register webhook services and map the endpoint:

```csharp
using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Webhooks;
using WhatsApp.Core.DependencyInjection;

builder.Services.AddWhatsAppCore(
    builder.Configuration.GetSection("WhatsApp"));

builder.Services.AddWhatsAppWebhooks();

var app = builder.Build();

app.MapWhatsAppWebhook("/webhooks/whatsapp");
```

Ensure `AppSecret` and `VerifyToken` are configured for the account used by the endpoint.

Configure the callback URL in the Meta developer console to match your deployed URL (for example `https://api.example.com/webhooks/whatsapp`).

## Verification handshake (GET)

When you subscribe to webhooks, Meta sends a GET request:

```
GET /webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=YOUR_TOKEN&hub.challenge=RANDOM_STRING
```

The endpoint compares `hub.verify_token` against your configured `VerifyToken` using constant-time comparison. On success, it returns `hub.challenge` as plain text with a 200 status. On failure, it returns a non-success response without revealing the expected token.

Test locally:

```http
GET http://localhost:5080/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=REPLACE_ME&hub.challenge=test-challenge-12345
```

## Delivery (POST)

Meta sends message and status updates as POST requests with a JSON body and an `X-Hub-Signature-256` header:

```
X-Hub-Signature-256: sha256=abc123...
```

The endpoint:

1. Reads the raw body (bounded by `MaxRequestBodyBytes`).
2. Validates the HMAC-SHA256 signature against your `AppSecret`.
3. Parses the JSON envelope into zero or more typed events.
4. Filters duplicates through `IWhatsAppWebhookDeduplicator`.
5. Dispatches accepted events to registered handlers.
6. Returns 200 OK.

## Typed events

Every event inherits shared metadata from `WhatsAppWebhookEvent`:

| Property | Description |
|----------|-------------|
| `WhatsAppBusinessAccountId` | WABA id |
| `PhoneNumberId` | Phone number id (when available) |
| `DisplayPhoneNumber` | Display number (when available) |
| `ReceivedAt` | UTC instant the webhook was received |
| `ExtensionData` | Unknown JSON properties preserved for forward compatibility |

### Inbound messages

| Event type | Description |
|------------|-------------|
| `WhatsAppTextMessageEvent` | Free-form text (`Body`) |
| `WhatsAppImageMessageEvent` | Image with media metadata |
| `WhatsAppDocumentMessageEvent` | Document with media metadata |
| `WhatsAppAudioMessageEvent` | Audio message |
| `WhatsAppVideoMessageEvent` | Video message |
| `WhatsAppStickerMessageEvent` | Sticker message |
| `WhatsAppLocationMessageEvent` | Location pin |
| `WhatsAppContactMessageEvent` | Shared contact cards |
| `WhatsAppInteractiveReplyMessageEvent` | Reply to a button or list |
| `WhatsAppButtonReplyMessageEvent` | Legacy button reply |
| `WhatsAppReactionMessageEvent` | Emoji reaction |
| `UnknownWhatsAppMessageEvent` | Unrecognized type with raw JSON |

Inbound message events include `MessageId`, `From`, `Timestamp`, and optional `Context` (reply-to).

### Delivery statuses

| Event type | Description |
|------------|-------------|
| `WhatsAppMessageSentEvent` | Message accepted by Meta |
| `WhatsAppMessageDeliveredEvent` | Delivered to recipient device |
| `WhatsAppMessageReadEvent` | Read by recipient |
| `WhatsAppMessageFailedEvent` | Delivery failed (includes `Errors`) |
| `UnknownWhatsAppMessageStatusEvent` | Unrecognized status value |

Status events include `MessageId`, `RecipientId`, and `Timestamp`.

## Writing handlers

Implement `IWhatsAppWebhookHandler<TEvent>`:

```csharp
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

public sealed class TextMessageHandler(ILogger<TextMessageHandler> logger)
    : IWhatsAppWebhookHandler<WhatsAppTextMessageEvent>
{
    public Task HandleAsync(WhatsAppTextMessageEvent notification, CancellationToken stopToken)
    {
        logger.LogInformation(
            "Inbound text. EventType={EventType}, MessageId={MessageId}",
            nameof(WhatsAppTextMessageEvent),
            notification.MessageId);

        return Task.CompletedTask;
    }
}
```

Register with DI:

```csharp
builder.Services
    .AddWhatsAppWebhookHandler<TextMessageHandler, WhatsAppTextMessageEvent>();
```

Multiple handlers for different event types can be registered. All matching handlers are invoked.

### Logging guidance

Log safe metadata only:

- Event type name
- Message id (`wamid...`)
- Interactive reply type
- Error counts

Do **not** log by default:

- Message body text
- Phone numbers (`From`, `RecipientId`)
- Template parameter values
- Media URLs

## Dispatch modes

| Mode | Behavior |
|------|----------|
| `Sequential` (default) | Events processed one at a time, in order |
| `Parallel` | Events processed concurrently, bounded by `MaxDegreeOfParallelism` |

Configure:

```csharp
builder.Services.AddWhatsAppWebhooks(options =>
{
    options.DispatchMode = WhatsAppWebhookDispatchMode.Parallel;
    options.MaxDegreeOfParallelism = 4;
});
```

Handler exceptions are logged with event type and message id, then isolated - one failing handler does not prevent others from running.

## Deduplication

Meta may redeliver the same webhook. The default `MemoryWhatsAppWebhookDeduplicator` rejects events whose dedup key was seen within the last 24 hours on the same process. For multi-instance deployments or handlers with side effects, implement durable shared-store deduplication:

```csharp
public sealed class RedisWebhookDeduplicator : IWhatsAppWebhookDeduplicator
{
    public async Task<bool> TryAcceptAsync(
        WhatsAppWebhookEvent notification, CancellationToken stopToken)
    {
        string key = WhatsAppWebhookDedupKey.For(notification);
        // Return false if key already exists in your store
        return await TryInsertAsync(key, stopToken);
    }
}
```

Register before or after `AddWhatsAppWebhooks` (use `AddSingleton` so it replaces the default):

```csharp
builder.Services.AddSingleton<IWhatsAppWebhookDeduplicator, RedisWebhookDeduplicator>();
builder.Services.AddWhatsAppWebhooks();
```

Use `NoOpWhatsAppWebhookDeduplicator` only when handlers are fully idempotent and you intentionally want every delivery (including replays) to run.

Dedup keys have the form `{wabaId}:{eventTypeName}:{messageId}`.

## Replacing the receiver

For production workloads with substantial processing, replace `IWhatsAppWebhookReceiver` to enqueue events and return 200 quickly:

```csharp
public sealed class QueueWebhookReceiver : IWhatsAppWebhookReceiver
{
    public async Task ReceiveAsync(
        IReadOnlyList<WhatsAppWebhookEvent> events,
        WhatsAppWebhookOptions options,
        CancellationToken stopToken)
    {
        foreach (var e in events)
        {
            await _queue.EnqueueAsync(e, stopToken);
        }
    }
}

builder.Services.AddSingleton<IWhatsAppWebhookReceiver, QueueWebhookReceiver>();
```

The default receiver does not provide guaranteed delivery - it invokes handlers in-process before responding.

## Signature validation

Enabled by default. Disable only for local development, and only with an explicit insecure opt-in:

```csharp
builder.Services.AddWhatsAppWebhooks(options =>
{
    options.RequireSignatureValidation = false;
    options.AllowInsecureNoSignatureValidation = true;
});
```

Without `AllowInsecureNoSignatureValidation = true`, options validation fails at startup.

Generate valid test signatures:

```csharp
using WhatsApp.Core.Testing.Signatures;

byte[] body = Encoding.UTF8.GetBytes(json);
string signature = WhatsAppWebhookTestSignatures.CreateSignature(body, appSecret);
```

## Multiple endpoints

Map separate endpoints for different accounts:

```csharp
app.MapWhatsAppWebhook("/webhooks/support", o => o.AccountName = "support");
app.MapWhatsAppWebhook("/webhooks/sales", o => o.AccountName = "sales");
```

## Related documentation

- [Configuration](configuration.md) - Webhook options
- [Architecture](architecture.md) - Webhook flow diagram
- [Testing](testing.md) - Webhook builders and test hosts
- [Error handling](error-handling.md) - Handler exception behavior
