# Testing

WhatsApp.Core ships a dedicated testing package (`WhatsApp.Core.Testing`) with fakes, builders, and signature helpers. It does not depend on any test framework - use it with xUnit, NUnit, MSTest, or any other runner.

## Installation

```bash
dotnet add package WhatsApp.Core.Testing
```

For ASP.NET Core webhook integration tests, also reference `Microsoft.AspNetCore.Mvc.Testing`.

## FakeWhatsAppClient

`FakeWhatsAppClient` implements `IWhatsAppClient` and records every outbound operation:

```csharp
using WhatsApp.Core.Testing.Fakes;
using WhatsApp.Core.Responses;

var fake = new FakeWhatsAppClient();

fake.QueueResponse(new SendMessageResponse
{
    Messages = [new SendMessageResponseMessage { Id = "wamid.test.123" }],
    Contacts = [new SendMessageResponseContact { Input = "353871234567", WaId = "353871234567" }],
    Metadata = new SendMessageResponseMetadata { RequestId = "req-1" },
});

await fake.SendTextAsync("353871234567", "Hello", stopToken: default);

Assert.Single(fake.Requests);
RecordedWhatsAppRequest recorded = fake.Requests[0];
Assert.Equal(RecordedWhatsAppOperation.SendMessage, recorded.Operation);
Assert.Equal("353871234567", recorded.Message!.To);
```

### Key capabilities

| Method / property | Description |
|-------------------|-------------|
| `Requests` | Ordered list of recorded outbound operations |
| `QueueResponse(SendMessageResponse)` | Configure the next response |
| `QueueException(Exception)` | Configure the next call to throw |
| `Reset()` | Clear recorded requests and queued responses |

The fake is thread-safe and respects cancellation tokens. Mutable request content is copied before storage.

### Testing media operations

```csharp
fake.QueueMediaUpload(new MediaUploadResponse { Id = "MEDIA_123" });
fake.QueueMediaMetadata(new MediaMetadata { Id = "MEDIA_123", MimeType = "image/jpeg" });

await fake.UploadMediaAsync(stream, "photo.jpg", "image/jpeg", stopToken: default);
await fake.GetMediaAsync("MEDIA_123", stopToken: default);
```

Configure download responses and assert recorded operations similarly.

### Replacing the real client in DI

```csharp
services.AddSingleton<IWhatsAppClient>(new FakeWhatsAppClient());
```

Or register alongside production setup in test fixtures.

## WhatsAppWebhookBuilder

Build realistic webhook JSON payloads for integration tests:

```csharp
using WhatsApp.Core.Testing.Builders;

string json = WhatsAppWebhookBuilder.Create()
    .WithBusinessAccountId("WABA_123")
    .WithPhoneNumberId("PHONE_456")
    .WithDisplayPhoneNumber("15550001111")
    .AddTextMessage("wamid.inbound.1", "353871234567", "Hello")
    .AddDeliveredStatus("wamid.outbound.1", "353871234567")
    .AddFailedStatus("wamid.outbound.2", "353871234567", errorCode: 131047)
    .BuildJson();
```

Available builder methods:

| Method | Description |
|--------|-------------|
| `AddTextMessage` | Inbound text message |
| `AddMessage(JsonObject)` | Arbitrary inbound message |
| `AddDeliveredStatus` | Delivery status |
| `AddReadStatus` | Read status |
| `AddSentStatus` | Sent status |
| `AddFailedStatus` | Failed status with error details |

Use `BuildJson()` for the complete webhook envelope or `Build()` for a `JsonObject`.

## Webhook signature helpers

Generate valid `X-Hub-Signature-256` headers for integration tests:

```csharp
using System.Text;
using WhatsApp.Core.Testing.Signatures;

byte[] payload = Encoding.UTF8.GetBytes(json);
string signature = WhatsAppWebhookTestSignatures.CreateSignature(payload, appSecret);

request.Headers.Add("X-Hub-Signature-256", signature);
```

The algorithm matches production validation: HMAC-SHA256 over the exact raw bytes, prefixed with `sha256=`.

## ASP.NET Core integration tests

The test suite uses an in-memory test host pattern. A minimal example:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Webhooks;
using WhatsApp.Core.DependencyInjection;

await using var factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.AddWhatsAppCore(options =>
            {
                options.PhoneNumberId = "PHONE_123";
                options.AccessToken = "test-token";
                options.AppSecret = "test-secret";
                options.VerifyToken = "test-verify";
                options.GraphApiVersion = "v21.0";
            });

            services.AddWhatsAppWebhooks(options =>
            {
                options.RequireSignatureValidation = true;
            });

            services.AddWhatsAppWebhookHandler<CapturingTextHandler, WhatsAppTextMessageEvent>();
        });
    });

var client = factory.CreateClient();
// Send signed POST to /webhooks/whatsapp
```

See `tests/WhatsApp.Core.AspNetCore.Tests/` for comprehensive examples covering verification, signatures, multi-event payloads, deduplication, and dispatch modes.

## JSON serialization tests

Core tests compare outbound JSON structurally (not string-for-string) to verify:

- Snake-case property names (`messaging_product`, `recipient_type`)
- Optional properties omitted when null
- Recipient normalization
- Template component serialization
- Interactive payload shapes

See `tests/WhatsApp.Core.Tests/MessageJsonPayloadTests.cs`.

## Package consumer tests

`tests/WhatsApp.Core.PackageTests/` packs all NuGet packages and verifies that a temporary consumer project can restore, compile, and use public extension methods without project references leaking into package metadata.

Run the full suite:

```bash
dotnet test --configuration Release
```

## Testing checklist

| Scenario | How to test |
|----------|-------------|
| Outbound message JSON shape | `FakeWhatsAppClient` + inspect `recorded.Message` or HTTP tests |
| API error handling | `fake.QueueException(new WhatsAppApiException(...))` |
| Webhook verification | GET with `hub.mode`, `hub.verify_token`, `hub.challenge` |
| Webhook signature | `WhatsAppWebhookTestSignatures.CreateSignature` + POST |
| Typed handler dispatch | Register a capturing handler, POST a built payload |
| Unknown message types | Builder with custom `AddMessage` + assert `UnknownWhatsAppMessageEvent` |
| Cancellation | Pass a canceled `CancellationToken` to fake or client |
| Named account isolation | Register two accounts, assert separate HttpClient/auth |

## Related documentation

- [Webhooks](webhooks.md) - Handler registration and event types
- [Architecture](architecture.md) - Extension points for custom receivers
- [Sample application](../samples/WhatsApp.Core.Sample.Api/) - Reference implementation
