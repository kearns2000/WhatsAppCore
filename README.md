# WhatsApp.Core

<img src="https://raw.githubusercontent.com/kearns2000/WhatsAppCore/main/assets/logo.png" alt="WhatsApp.Core logo" width="160" height="160" />

A modern .NET SDK for the [Meta WhatsApp Cloud API](https://developers.facebook.com/docs/whatsapp/cloud-api). Send strongly typed messages, manage media, receive webhooks, and integrate with ASP.NET Core - without reimplementing HTTP plumbing, signature validation, or JSON serialization yourself.

> **Disclaimer:** WhatsApp.Core is a community-maintained library. It is **not** affiliated with, endorsed by, or sponsored by Meta Platforms, Inc. or WhatsApp LLC. WhatsApp is a trademark of its respective owner. See [NOTICE.md](NOTICE.md) for the full disclaimer.

## What it does

WhatsApp.Core wraps the official Meta Graph API endpoints for WhatsApp Business messaging:

- **Outbound messaging** - Send text, templates, images, documents, audio, video, stickers, locations, contacts, interactive messages, and reactions through strongly typed request models and convenience extension methods.
- **Media operations** - Upload, retrieve metadata, stream-download, and delete media assets.
- **Webhook receiving** - Parse Meta webhook payloads into typed events, verify subscription handshakes, and validate HMAC signatures.
- **ASP.NET Core integration** - Map a webhook endpoint with one line, register typed handlers, and configure dispatch behavior.
- **Dependency injection** - Register from configuration or code, with support for multiple named WhatsApp accounts.
- **Observability** - Built-in `ActivitySource` and `Meter` instrumentation compatible with OpenTelemetry collectors.
- **Testing support** - A fake client, webhook payload builders, and signature helpers in the optional `WhatsApp.Core.Testing` package.

## What it does not do

WhatsApp.Core intentionally does **not**:

- Automate WhatsApp Web, browser sessions, QR-code pairing, or any unofficial gateway.
- Provide guaranteed message delivery, queueing, or durable webhook processing (you bring your own infrastructure for that).
- Automatically retry message sends (a failed POST may have reached Meta; retrying could duplicate messages).
- Validate or enrich phone numbers via network lookups.
- Ship as an official Meta or WhatsApp SDK.

## Installation

Install the core package:

```bash
dotnet add package WhatsApp.Core
```

For ASP.NET Core webhook support:

```bash
dotnet add package WhatsApp.Core.AspNetCore
```

For test doubles and webhook builders:

```bash
dotnet add package WhatsApp.Core.Testing
```

Packages target `net8.0` and `net10.0`. The sample application in this repository uses .NET 10.

> Before the first public release, verify that all package IDs (`WhatsApp.Core`, `WhatsApp.Core.AspNetCore`, `WhatsApp.Core.Testing`) are available on NuGet.

## Meta prerequisites

Before using this library you need:

1. A [Meta for Developers](https://developers.facebook.com/) account.
2. A Meta app with the **WhatsApp** product enabled.
3. A WhatsApp Business Account (WABA) linked to the app.
4. A phone number registered with the Cloud API, yielding a **Phone Number ID**.
5. A permanent or long-lived **Access Token** with `whatsapp_business_messaging` and `whatsapp_business_management` permissions.
6. An **App Secret** (for webhook signature validation) and a **Verify Token** (for the subscription handshake) if you receive webhooks.

Configure the webhook callback URL in the Meta developer console to point at your application's endpoint (for example `https://your-host/webhooks/whatsapp`).

## Basic configuration

Register the default account from configuration:

```csharp
using WhatsApp.Core.DependencyInjection;

builder.Services.AddWhatsAppCore(
    builder.Configuration.GetSection("WhatsApp"));
```

`appsettings.json`:

```json
{
  "WhatsApp": {
    "PhoneNumberId": "YOUR_PHONE_NUMBER_ID",
    "AccessToken": "YOUR_ACCESS_TOKEN",
    "AppSecret": "YOUR_APP_SECRET",
    "VerifyToken": "YOUR_VERIFY_TOKEN",
    "GraphApiVersion": "v21.0"
  }
}
```

Store secrets in user secrets, environment variables, or a secret manager - never commit real credentials.

Resolve the client and send a message:

```csharp
using WhatsApp.Core.Client;
using WhatsApp.Core.DependencyInjection;

builder.Services.AddWhatsAppCore(
    builder.Configuration.GetSection("WhatsApp"));

var app = builder.Build();

IWhatsAppClient client =
    app.Services.GetRequiredService<IWhatsAppClient>();

await client.SendTextAsync(
    to: "353...",
    body: "Hello from WhatsApp.Core",
    stopToken: CancellationToken.None);
```

Programmatic registration is also supported:

```csharp
builder.Services.AddWhatsAppCore(options =>
{
    options.PhoneNumberId = "...";
    options.AccessToken = "...";
    options.AppSecret = "...";
    options.VerifyToken = "...";
    options.GraphApiVersion = "v21.0";
});
```

See [docs/configuration.md](docs/configuration.md) for all options.

## Sending a text message

```csharp
var response = await client.SendTextAsync(
    to: "353871234567",
    body: "Hello!",
    previewUrl: true,
    stopToken: stopToken);

string messageId = response.Messages[0].Id;
```

Reply to an inbound message by passing a reply context:

```csharp
await client.SendTextAsync(
    to: "353871234567",
    body: "Thanks for your message.",
    context: new WhatsAppReplyContext { MessageId = "wamid.HBg..." },
    stopToken: stopToken);
```

See [docs/sending-messages.md](docs/sending-messages.md) for all message types.

## Sending a template

Template messages require pre-approved templates in your WABA:

```csharp
await client.SendTemplateAsync(
    to: "353871234567",
    templateName: "hello_world",
    languageCode: "en_US",
    components:
    [
        new WhatsAppTemplateComponent
        {
            ComponentType = "body",
            Parameters = [WhatsAppTemplateParameter.ForText("Patrick")],
        },
    ],
    stopToken: stopToken);
```

## Uploading and sending media

Upload a file, then reference the returned media id in a message:

```csharp
await using var file = File.OpenRead("photo.jpg");

var upload = await client.UploadMediaAsync(
    content: file,
    fileName: "photo.jpg",
    contentType: "image/jpeg",
    stopToken: stopToken);

await client.SendImageAsync(
    to: "353871234567",
    mediaId: upload.Id,
    caption: "Here is the photo",
    stopToken: stopToken);
```

Alternatively, send media by public URL without uploading:

```csharp
await client.SendImageAsync(
    to: "353871234567",
    link: "https://example.com/image.jpg",
    caption: "Sample image",
    stopToken: stopToken);
```

See [docs/media.md](docs/media.md) for download, metadata, and deletion.

## Mapping the webhook endpoint

```csharp
using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Webhooks;

builder.Services.AddWhatsAppWebhooks();

var app = builder.Build();

app.MapWhatsAppWebhook("/webhooks/whatsapp");
```

This maps:

- `GET /webhooks/whatsapp` - Meta subscription verification (`hub.mode`, `hub.verify_token`, `hub.challenge`).
- `POST /webhooks/whatsapp` - Inbound message and status deliveries.

See [docs/webhooks.md](docs/webhooks.md) for handler registration, deduplication, and dispatch modes.

## Writing a typed webhook handler

Implement `IWhatsAppWebhookHandler<TEvent>` and register it:

```csharp
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.DependencyInjection;
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

builder.Services
    .AddWhatsAppWebhooks()
    .AddWhatsAppWebhookHandler<TextMessageHandler, WhatsAppTextMessageEvent>();
```

Available typed events include `WhatsAppTextMessageEvent`, `WhatsAppImageMessageEvent`, `WhatsAppInteractiveReplyMessageEvent`, `WhatsAppMessageDeliveredEvent`, `WhatsAppMessageFailedEvent`, and `UnknownWhatsAppMessageEvent` for forward compatibility.

## Validating signatures

By default, POST webhook deliveries must include a valid `X-Hub-Signature-256` header computed with your app secret. Validation runs on the raw request body before JSON parsing.

Disable validation only for local development, with an explicit insecure opt-in:

```csharp
builder.Services.AddWhatsAppWebhooks(options =>
{
    options.RequireSignatureValidation = false;
    options.AllowInsecureNoSignatureValidation = true;
});
```

Without `AllowInsecureNoSignatureValidation`, options validation fails at startup. Never disable signature validation in production.

Generate test signatures with `WhatsApp.Core.Testing`:

```csharp
using WhatsApp.Core.Testing.Signatures;

string signature = WhatsAppWebhookTestSignatures.CreateSignature(payloadBytes, appSecret);
```

## Named accounts

Register multiple WhatsApp Business numbers in one application:

```csharp
builder.Services.AddWhatsAppCore("support", options =>
{
    options.PhoneNumberId = "...";
    options.AccessToken = "...";
    options.GraphApiVersion = "v21.0";
});

builder.Services.AddWhatsAppCore("marketing", options =>
{
    options.PhoneNumberId = "...";
    options.AccessToken = "...";
    options.GraphApiVersion = "v21.0";
});
```

Resolve clients by name:

```csharp
IWhatsAppClientFactory factory = app.Services.GetRequiredService<IWhatsAppClientFactory>();

IWhatsAppClient support = factory.CreateClient("support");
IWhatsAppClient marketing = factory.CreateClient("marketing");
```

Each account uses an isolated `HttpClient`, credentials, and diagnostics tags. Map separate webhook endpoints per account with `WhatsAppWebhookOptions.AccountName`.

## Error handling

The library throws two primary exception types:

- **`WhatsAppValidationException`** - Client-side validation failed before any HTTP call (missing recipient, empty body, invalid media source combination, etc.).
- **`WhatsAppApiException`** - The Graph API returned a non-success response. Carries `StatusCode`, `ErrorCode`, `MetaTraceId`, `IsTransient`, and `RetryAfter` for informed retry decisions.

Message sends are **never** automatically retried. Optional safe retries apply only to idempotent operations (media metadata GET, media DELETE) when enabled in `WhatsAppResilienceOptions`.

See [docs/error-handling.md](docs/error-handling.md).

## Diagnostics

WhatsApp.Core emits standard .NET diagnostics without requiring OpenTelemetry packages:

| Source | Name |
|--------|------|
| ActivitySource | `WhatsApp.Core` |
| Meter | `WhatsApp.Core` |
| ActivitySource (webhooks) | `WhatsApp.Core.AspNetCore` |
| Meter (webhooks) | `WhatsApp.Core.AspNetCore` |

Metrics include outbound request counts, failures, duration, media bytes, webhooks received, invalid signatures, and dispatch failures. Safe tags only - no phone numbers, message bodies, or tokens.

See [docs/observability.md](docs/observability.md).

## Testing with the fake client

```csharp
using WhatsApp.Core.Testing.Fakes;

var fake = new FakeWhatsAppClient();
fake.QueueResponse(new SendMessageResponse { /* ... */ });

await fake.SendTextAsync("353871234567", "test", stopToken: default);

RecordedWhatsAppRequest request = fake.Requests[0];
Assert.Equal(RecordedWhatsAppOperation.SendMessage, request.Operation);
```

Build webhook payloads for integration tests:

```csharp
using WhatsApp.Core.Testing.Builders;

string json = WhatsAppWebhookBuilder.Create()
    .WithPhoneNumberId("PHONE_NUMBER_ID")
    .AddTextMessage("wamid.abc", "353871234567", "Hello")
    .AddDeliveredStatus("wamid.outbound", "353871234567")
    .BuildJson();
```

See [docs/testing.md](docs/testing.md).

## Security guidance

- Store `AccessToken`, `AppSecret`, and `VerifyToken` in secret managers - not source control.
- Enable webhook signature validation in every deployed environment.
- Do not log message bodies, complete phone numbers, access tokens, or temporary media URLs.
- Use HTTPS for your webhook callback URL.
- Pin an explicit Graph API version (`v21.0`); never use the `latest` alias.
- Acknowledge webhooks quickly and process events asynchronously via durable queues when work is substantial.
- Implement durable deduplication for handlers that are not naturally idempotent.

See [SECURITY.md](SECURITY.md) for vulnerability reporting.

## Versioning policy

This project follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking public API changes.
- **Minor** - Backward-compatible features.
- **Patch** - Backward-compatible bug fixes.

Pre-release versions use suffixes such as `alpha`, `beta`, or `rc`. The current version is **0.1.0-alpha.1**.

During the 0.x series, minor releases may include breaking changes as the public API stabilizes. Pin exact versions in production until 1.0.

Graph API versions are configured independently via `WhatsAppOptions.GraphApiVersion` and are not tied to package version numbers.

## Trademark and affiliation disclaimer

WhatsApp.Core is an independent, community-maintained open-source project. It is **not** affiliated with, endorsed by, or sponsored by Meta Platforms, Inc., WhatsApp LLC, or any of their subsidiaries.

**WhatsApp** is a registered trademark of WhatsApp LLC. The project logo is an unofficial mark for this community library and does not imply endorsement by Meta or WhatsApp.

For the full notice, see [NOTICE.md](NOTICE.md).

## Documentation

| Document | Description |
|----------|-------------|
| [architecture.md](docs/architecture.md) | Project boundaries, request flows, extension points |
| [configuration.md](docs/configuration.md) | All configuration options |
| [sending-messages.md](docs/sending-messages.md) | Outbound message types |
| [webhooks.md](docs/webhooks.md) | Receiving and handling webhooks |
| [media.md](docs/media.md) | Upload, download, and delete media |
| [error-handling.md](docs/error-handling.md) | Exceptions and retry guidance |
| [observability.md](docs/observability.md) | Tracing and metrics |
| [testing.md](docs/testing.md) | Fake client and test builders |

## Sample application

Run the included sample API:

```bash
dotnet run --project samples/WhatsApp.Core.Sample.Api
```

Configure credentials via user secrets or `appsettings.Development.json`, then explore the Swagger UI at `http://localhost:5080/swagger` or use `WhatsApp.Core.Sample.Api.http`.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT - see [LICENSE](LICENSE).
