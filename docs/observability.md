# Observability

WhatsApp.Core emits standard .NET diagnostics through `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter`. No OpenTelemetry packages are required - wire these sources into your existing OpenTelemetry, Application Insights, or Prometheus setup.

## Activity sources

| Source name | Package | Traces |
|-------------|---------|--------|
| `WhatsApp.Core` | WhatsApp.Core | Outbound API operations |
| `WhatsApp.Core.AspNetCore` | WhatsApp.Core.AspNetCore | Webhook receive, signature validation, parsing, dispatch |

### Outbound activities (WhatsApp.Core)

| Activity name | Kind | When |
|---------------|------|------|
| `WhatsApp.Core/send_message` | Client | Message send |
| `WhatsApp.Core/upload_media` | Client | Media upload |
| `WhatsApp.Core/get_media` | Client | Media metadata retrieval |
| `WhatsApp.Core/download_media` | Client | Media download |
| `WhatsApp.Core/delete_media` | Client | Media deletion |
| `WhatsApp.Core/mark_as_read` | Client | Read receipt |

### Webhook activities (WhatsApp.Core.AspNetCore)

| Activity name | Kind | When |
|---------------|------|------|
| `WhatsApp.Core.AspNetCore/receive_webhook` | Server | Webhook POST received |
| `WhatsApp.Core.AspNetCore/validate_signature` | Internal | HMAC validation |
| `WhatsApp.Core.AspNetCore/parse_webhook` | Internal | JSON parsing |
| `WhatsApp.Core.AspNetCore/dispatch_event` | Internal | Handler invocation |

## Metrics

### WhatsApp.Core meter (`WhatsApp.Core`)

| Metric | Type | Description |
|--------|------|-------------|
| `whatsapp.core.requests` | Counter | Outbound Graph API requests |
| `whatsapp.core.request_failures` | Counter | Failed outbound requests |
| `whatsapp.core.request.duration` | Histogram | Request duration (ms) |
| `whatsapp.core.media.bytes` | Histogram | Media bytes uploaded or downloaded |

### WhatsApp.Core.AspNetCore meter (`WhatsApp.Core.AspNetCore`)

| Metric | Type | Description |
|--------|------|-------------|
| `whatsapp.core.aspnetcore.webhooks_received` | Counter | Webhook POST requests received |
| `whatsapp.core.aspnetcore.invalid_signatures` | Counter | Signature validation failures |
| `whatsapp.core.aspnetcore.events_parsed` | Counter | Events parsed from webhooks |
| `whatsapp.core.aspnetcore.dispatch_failures` | Counter | Handler dispatch failures |

## Safe tags

Diagnostic tags intentionally exclude personal data:

| Safe tags | Unsafe (never used) |
|-----------|---------------------|
| Account name | Phone numbers |
| Operation name | Message body text |
| Message type | Access tokens |
| HTTP status code | Media URLs |
| Meta error code | Template parameter values |
| Webhook event type | Webhook signatures |
| Success / failure | App secrets |

## OpenTelemetry integration

Register the activity sources and meters in your OpenTelemetry configuration:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("WhatsApp.Core")
        .AddSource("WhatsApp.Core.AspNetCore"))
    .WithMetrics(metrics => metrics
        .AddMeter("WhatsApp.Core")
        .AddMeter("WhatsApp.Core.AspNetCore"));
```

No additional WhatsApp.Core packages are needed for this wiring.

## Structured logging

The library uses `ILogger` with structured logging throughout. Log levels follow these conventions:

| Level | Examples |
|-------|----------|
| Debug | Request URI construction, option snapshots (redacted) |
| Information | Successful operations, webhook events received |
| Warning | Transient API failures, handler exceptions, insecure config |
| Error | Unexpected failures, unparseable responses |

Default logging does not include message content, phone numbers, or secrets. If you enable debug logging in production, review your log aggregation policies for PII compliance.

## Handler logging example

Follow the sample application's pattern - log event type and message id only:

```csharp
logger.LogInformation(
    "Received inbound text message. EventType={EventType}, MessageId={MessageId}",
    nameof(WhatsAppTextMessageEvent),
    notification.MessageId);
```

## Monitoring recommendations

| Signal | Alert on |
|--------|----------|
| `whatsapp.core.request_failures` rate | Sustained increase (API issues or auth problems) |
| `whatsapp.core.aspnetcore.invalid_signatures` | Any non-zero value in production |
| `whatsapp.core.aspnetcore.dispatch_failures` | Handler exceptions |
| `whatsapp.core.request.duration` p99 | Latency regression |
| HTTP 401/403 responses | Token expiry or permission changes |

Correlate `MetaTraceId` from `WhatsAppApiException` with Meta support when investigating API errors.

## Related documentation

- [Architecture](architecture.md) - Where diagnostics are emitted in the request flow
- [Error handling](error-handling.md) - Exception metadata for alerting
- [Security](../SECURITY.md) - What not to log
